window.ralfEditor = (() => {
  let editor;
  let dotnet;
  let suppress = false;
  let initialization;

  function initialize(reference) {
    dotnet = reference;
    if (initialization) return initialization;

    initialization = new Promise((resolve, reject) => {
      if (typeof window.require !== 'function') {
        reject(new Error('Monaco loader is unavailable. Check the Internet connection or CDN access.'));
        return;
      }

      window.require.config({ paths: { vs: 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs' } });
      window.require(
        ['vs/editor/editor.main'],
        () => {
          const host = document.getElementById('monaco');
          if (!host) {
            reject(new Error('Monaco host element was not found.'));
            return;
          }

          editor = window.monaco.editor.create(host, {
            value: '',
            language: 'plaintext',
            theme: 'vs-dark',
            automaticLayout: true,
            minimap: { enabled: false },
            fontSize: 14
          });
          editor.onDidChangeModelContent(() => {
            if (!suppress && dotnet) {
              dotnet.invokeMethodAsync('EditorChanged', editor.getValue())
                .catch(error => console.error('EditorChanged failed', error));
            }
          });
          editor.addCommand(window.monaco.KeyMod.CtrlCmd | window.monaco.KeyCode.KeyS, () => {
            if (dotnet) {
              dotnet.invokeMethodAsync('SaveFromKeyboard')
                .catch(error => console.error('SaveFromKeyboard failed', error));
            }
          });
          resolve();
        },
        error => reject(error instanceof Error ? error : new Error(String(error)))
      );
    });

    return initialization;
  }

  return {
    initialize,
    async setValue(value, language) {
      await initialization;
      suppress = true;
      try {
        editor.setValue(value ?? '');
        window.monaco.editor.setModelLanguage(editor.getModel(), language || 'plaintext');
        editor.focus();
      } finally {
        suppress = false;
      }
    },
    async getValue() {
      await initialization;
      return editor.getValue();
    }
  };
})();
