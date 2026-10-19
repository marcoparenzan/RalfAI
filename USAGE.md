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

## Profili esterni

Oltre ai context incorporati (`dotnet`, `markdown`, `html`) si possono definire profili come file
Markdown con frontmatter, senza ricompilare. Si cercano in `~/.ralfai/profiles/*.md` e in
`<cartella target>/.ralfai/profiles/*.md` (il progetto vince sull'utente, entrambi vincono sui
built-in con lo stesso nome). Il nome del file è la chiave da usare con `--context` e in `F6`.

```markdown
---
name: Note di riunione
description: Scrive verbali in Markdown
tools: [FileSystem, Web_FetchUrl]   # gruppi (FileSystem, Web, DotNet, StaticServer) o singoli tool
---
Sei un assistente che lavora in {{targetFolder}}. ...
```

- `{{targetFolder}}` nel corpo viene sostituito col percorso assoluto della cartella target.
- I tool restano codice: il profilo sceglie per nome, non ne definisce di nuovi.
- Un profilo non valido viene saltato con un avviso su stderr, senza bloccare l'avvio.
- `ralf --export-profile dotnet` stampa un built-in come profilo, come punto di partenza.

## Editor: menzioni `@` e cronologia

- **`@`**: digita `@` seguito da parte di un nome (`@inpcol`) e compare un elenco dei file della
  cartella target (match sul nome, poi sul percorso, poi per caratteri in ordine; sono escluse
  `bin`, `obj`, `node_modules` e le cartelle che iniziano con `.`). `↑`/`↓` scelgono, `Tab` o
  `Enter` inseriscono `@percorso/relativo`, `Esc` chiude l'elenco. Il testo arriva all'agente così
  com'è: il percorso gli dice quale file leggere.
- **`↑` / `↓`** su un prompt vuoto richiamano i prompt precedenti; `↓` torna alla bozza. La
  cronologia è per cartella, in `.ralfai/history.jsonl` (ultimi 200, esclusi i comandi `/…`).
- **`/`** apre il menu dei comandi (`/switch-context`, `/switch-folder`, `/toggle-debug`,
  `/speak`, `/clear`) filtrato mentre digiti: `↑`/`↓` scelgono, `Tab` completa, `Enter` completa e,
  se il comando è già scritto per intero, lo esegue. Sono gli stessi comandi dei tasti `F5`–`F8`.
- **Scorciatoie readline**: `Ctrl+A`/`Ctrl+E` inizio/fine riga, `Ctrl+U`/`Ctrl+K` cancellano fino
  a inizio/fine riga, `Ctrl+W` cancella la parola precedente.
- **`Ctrl+Z`** annulla l'ultima modifica (la digitazione continua si annulla a blocchi, parola per
  parola; richiamo dalla cronologia e completamenti `@`/`/` si annullano in un colpo solo).
- **Paste lungo**: con `Ctrl+V` un incolla di 6+ righe o 600+ caratteri appare come
  `[Pasted #1: 42 lines]` nel prompt e viene espanso nel testo completo all'invio (e nella
  cronologia). Non vale per l'incolla "grezzo" del terminale, che arriva tasto per tasto.
