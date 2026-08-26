window.ralfEditor = (() => {
  let editor, dotnet, suppress = false;
  return {
    initialize(reference) {
      dotnet = reference;
      require.config({ paths: { vs: 'https://cdn.jsdelivr.net/npm/monaco-editor@0.52.2/min/vs' } });
      require(['vs/editor/editor.main'], () => {
        editor = monaco.editor.create(document.getElementById('monaco'), { value: '', language: 'plaintext', theme: 'vs-dark', automaticLayout: true, minimap: { enabled: false }, fontSize: 14 });
        editor.onDidChangeModelContent(() => { if (!suppress) dotnet.invokeMethodAsync('EditorChanged', editor.getValue()); });
        editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, () => dotnet.invokeMethodAsync('SaveFromKeyboard'));
      });
    },
    setValue(value, language) { if (!editor) return; suppress = true; editor.setValue(value); monaco.editor.setModelLanguage(editor.getModel(), language); suppress = false; editor.focus(); },
    getValue() { return editor ? editor.getValue() : ''; }
  };
})();
