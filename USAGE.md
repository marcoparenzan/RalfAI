# Guida all'uso

## CLI

```bash
ralf
ralf D:\progetto --context dotnet --debug
ralf --version
```

La testata del prompt riporta sempre `RalfAI v<version>`, context, modello e cartella.

### Editor

- frecce e `Home`/`End`: spostamento del cursore;
- `Ctrl+←/→` e `Ctrl+Home/End`: parola/documento;
- `Backspace` e `Delete`: cancellazione;
- `Ctrl+V` o `Shift+Insert`: incolla atomico, senza consumare eventi tastiera;
- `Enter`: nuova riga; `Ctrl+Enter`: invio; `Esc`: pulizia bozza.

### Progresso e costi

Lo spinner mostra preparazione, rate limit, attesa modello, tool, estrazione usage e salvataggio.
`F8` o `--debug` mantiene il log operativo a schermo. Non viene inventato o esposto il
chain-of-thought privato: sono mostrate solo operazioni osservabili ed eventuale reasoning
esplicitamente restituito dal provider.

Dopo ogni risposta compaiono durata, token e costo stimato. Lo storico JSON Lines è:

```text
<progetto>/.ralfai/cost-history.jsonl
```

Configurare le tariffe USD per milione di token:

```json
"Pricing": {
  "openai": {
    "gpt-5.1": { "InputUsdPerMillionTokens": 1.25, "OutputUsdPerMillionTokens": 10.0 }
  }
}
```

## RalfAI Studio (Blazor Hybrid)

Installazione globale e avvio:

```powershell
.\ops\install-studio-tool.ps1
ralf-studio D:\progetto
```

Verifica versione e guida:

```powershell
ralf-studio --version
ralf-studio --help
```

Per installarlo nel manifest locale usare `install-studio-tool.ps1 -Local`, quindi eseguirlo
dalla cartella `src` con `dotnet tool run ralf-studio -- D:\progetto`.

In alternativa, senza installazione:

```bash
dotnet run --project src/RalfAI.Studio/RalfAI.Studio -- D:\progetto
```

Richiede Windows, .NET 10 Runtime e WebView2. Offre file tree (esclude `.git`, `bin`, `obj`),
Monaco Editor con syntax highlighting, salvataggio con `Ctrl+S` e preview Markdown live tramite
Markdig. Monaco è caricato da CDN e richiede connettività al primo avvio.

## Installazione/aggiornamento tool CLI

```powershell
.\ops\install-tool.ps1        # globale
.\ops\install-tool.ps1 -Local # manifest locale sotto src
```
