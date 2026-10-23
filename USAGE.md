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

### Context .NET 10 e .NET 11

`--context dotnet` genera progetti `net10.0` (C# 14); `--context dotnet11` genera progetti `net11.0`
(C# 15, serve l'SDK 11 installato per compilarli). Sono lo stesso context con la versione come parametro:
stessi tool, stesso flusso (scaffold in `src/`, build, test), cambiano framework e prompt.
Per cambiare al volo usa `F6`; la scelta è ricordata per cartella in `.ralfai/config.yaml`.

### Velocità

Un turno con molti tool è lento soprattutto per il numero di giri col modello: ogni giro rimanda tutta la
conversazione (in una sessione lunga anche 40.000+ token) e il modello ragiona prima di rispondere.
I limiti `RateLimits` **non** sono il collo di bottiglia: il limitatore distanzia solo l'inizio di ogni
turno (circa 2,5 s a 30 richieste/min) e non interviene tra le chiamate ai tool dello stesso turno;
alzarli non accelera nulla, serve solo se compaiono errori 429 (attesa di 60 s).

Leve che contano, in ordine di effetto:

- **Chiamate in parallelo**: il prompt di sistema chiede al modello di emettere insieme i tool
  indipendenti (più `WriteFile`, più `ReadFile`) invece di uno per giro.
- **`Reasoning:Effort`** (`none`, `low`, `medium`, `high`, `xhigh`; vuoto = default del provider):
  `"Reasoning": { "Effort": "low" }` riduce i tempi di ragionamento a ogni giro.
- **`History:ResumeMaxTokens`** (default 30000): all'avvio si rimanda al modello solo la parte più
  recente del log di sessione, non tutta la storia. `0` = tutto.

In una prova (stesso compito, un campione ciascuno): 99 s e 133k token in ingresso senza queste
misure, 76 s e 48k con le chiamate in parallelo, 54 s e 33k aggiungendo `Effort: low`.

### Context Rust

`--context rust` costruisce crate con Cargo (`cargo new/build/test/fmt/clippy`) e **scarica da solo gli
strumenti Rust che servono**, in locale:

- Se Rust c'è già (nel `PATH`, in `$CARGO_HOME` o in `~/.cargo`) viene usato così com'è.
- Se manca, l'agente scarica il `rustup-init` ufficiale da `static.rust-lang.org`, ne **verifica lo
  SHA-256** pubblicato e installa Rust stable in `~/.ralfai/rust` (o nella cartella indicata da
  `RALFAI_RUST_HOME`). Non tocca il profilo utente né il `PATH`: RalfAI passa `CARGO_HOME`/`RUSTUP_HOME`
  ai propri processi. Servono Internet e circa 1 GB di disco; la prima volta ci vogliono alcuni minuti.
- Componenti, target e toolchain aggiuntivi (`rust-src`, `wasm32-unknown-unknown`, `nightly`…) li scarica
  con `rustup` solo quando il compito li richiede; gli strumenti distribuiti come crate con `cargo install`.
- Su Windows il linker è quello MSVC: i **Visual Studio Build Tools** ("Sviluppo di applicazioni desktop
  con C++") non vengono installati da RalfAI; se mancano l'agente lo segnala all'avvio.
- L'output di cargo oltre 30.000 caratteri viene accorciato (inizio e fine) per non appesantire il contesto.

Per un profilo personalizzato in `.ralfai/profiles` basta `tools: [FileSystem, Web, Rust]`.

### Strumenti per i file

Tutti i context danno all'agente lo stesso set `FileSystem.*`, pensato per tenere basso il contesto:

| Strumento | A cosa serve |
|---|---|
| `EditFile` | cambia un pezzo di un file sostituendo un testo esatto e unico (`replaceAll` per tutte le occorrenze); mantiene fine riga e BOM |
| `ReplaceInFiles` | stessa modifica in molti file (rinomine, costanti), testo o regex, limitabile con un glob |
| `ReadFile` | legge un file; con `startLine`/`endLine` solo l'intervallo che serve; oltre ~100.000 caratteri taglia e lo dice |
| `SearchFiles` | cerca testo o regex nei file e restituisce `percorso:riga: testo` |
| `FindFiles` | trova file per nome con un glob (`**/*.cs`, `*.md`) |
| `ListFiles` | elenca la cartella, senza `bin`, `obj`, `.git`, `node_modules`, `.ralfai` (né `target` accanto a un `Cargo.toml`), fino a 500 voci |
| `WriteFile`, `DeleteFile` | crea o riscrive per intero; elimina. I file nuovi non hanno BOM; uno esistente tiene il suo |

Il prompt di sistema chiede di preferire `EditFile`/`ReplaceInFiles` alla riscrittura e di cercare con
`SearchFiles`/`FindFiles` invece di leggere tutto. Su un compito di prova (rinominare un metodo, cambiare una
costante e una riga in 10 file) le chiamate sono scese da circa 20 a 9 e i token in ingresso da circa 174.000 a
45.000, a parità di tempo e di risultato. Inoltre il controllo dei percorsi ora blocca anche le cartelle
"sorelle" con lo stesso prefisso (`D:\prog` non ammette più `D:\prog-altro`).

### Progresso e costi

Lo spinner mostra preparazione, rate limit, attesa modello, tool, estrazione usage e salvataggio.
`F8` o `--debug` mantiene il log operativo a schermo. Non viene inventato o esposto il
chain-of-thought privato: sono mostrate solo operazioni osservabili ed eventuale reasoning
esplicitamente restituito dal provider.

### Streaming, ragionamento e velocità

Risposta e ragionamento arrivano a righe mentre il modello li produce, senza aspettare la fine:

- **Ragionamento**: sezione `✻ Thinking` in grigio corsivo. È il riassunto del ragionamento che il
  provider sceglie di esporre (API Responses di Foundry/OpenAI) oppure il testo tra `<think>…</think>`
  dei modelli che lo emettono inline. Non è la catena di pensiero nascosta del modello. Si disattiva
  con `"Reasoning": { "Summary": "off" }`; se il modello non lo supporta, RalfAI lo disabilita da solo
  per la sessione e riprova.
- **Risposta**: sezione `agent`; i testi prima e dopo una chiamata a tool restano separati da una riga vuota.
- **Velocità**: sulla riga dello spinner, mentre genera: `~42 tok/s · avg 38 · session 35 · ~1.240 out tok`
  seguita dalla coda della riga in scrittura. `tok/s` è la velocità istantanea (ultimi 2 secondi),
  `avg` la media del turno, `session` la media dei turni conclusi. Il `~` indica un valore stimato dal
  testo ricevuto (≈4 caratteri per token) finché il provider non comunica l'uso reale.
- A fine turno la riga finale riporta anche `N tok/s (session avg M)`; i token di ragionamento nascosti
  contano nel totale reale ma non scorrono a schermo, quindi la velocità istantanea è un minimo.

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

Oltre ai context incorporati (`dotnet`, `dotnet11`, `rust`, `markdown`, `html`) si possono definire profili come file
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
- **Immagini**: un'immagine citata con `@` (`@screenshot.png`, anche con virgolette per i nomi con
  spazi: `@"mia foto.png"`) viene allegata al messaggio e vista dal modello. `Ctrl+V` con un'immagine
  nella clipboard (screenshot, "copia immagine" dal browser) la salva in `.ralfai/attachments/` e
  inserisce la menzione; se hai copiato dei file immagine da Esplora risorse, inserisce i loro percorsi.
  Se la clipboard contiene anche del testo, `Ctrl+V` incolla il testo. Formati: PNG, JPEG, GIF, WebP;
  fino a 8 immagini e 20 MB ciascuna; PNG e JPEG oltre i 2048 px vengono ridotti prima dell'invio. Prima
  della risposta compare una riga `📎 nome · larghezza×altezza · KB`. Serve un modello con visione;
  l'immagine resta nella conversazione della sessione, quindi pesa sui token dei turni successivi.
  Su Linux la lettura dalla clipboard richiede `wl-paste` o `xclip`; su macOS usa `@file`.
