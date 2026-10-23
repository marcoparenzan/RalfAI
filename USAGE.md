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

### Limiti per turno

Per fermare un modello che va in ciclo ci sono tre tetti per ogni turno, sotto `Limits` nel config (`0` = spento):

```json
"Limits": { "MaxToolCalls": 150, "MaxOutputTokens": 50000, "MaxReasoningChars": 30000 }
```

- **`MaxToolCalls`** (150) - *morbido*: oltre la soglia i tool non vengono più eseguiti e rispondono al modello
  "limite raggiunto, riassumi e fermati". Il turno finisce con un resoconto e puoi scrivere "continua". Compare un avviso
  giallo. Se il modello ignora l'istruzione per altre 10 chiamate, il turno viene interrotto.
- **`MaxOutputTokens`** (50000) - *duro*: se l'output di un turno (stimato mentre scorre, poi con i numeri reali del
  provider) supera il limite, il turno si interrompe con un errore che spiega il perché.
- **`MaxReasoningChars`** (30000) - *duro*: caratteri di ragionamento mostrati in un turno. Un turno sano ne ha poche migliaia;
  un modello che si ripete ne produce centinaia di migliaia.

Un turno interrotto in modo duro non viene ricordato dal modello (resta nel log come `[stopped]`) e il suo costo non
finisce in `cost-history.jsonl`, perché il provider non ha ancora comunicato l'uso. A 20.000 token compare comunque un avviso
con il suggerimento di premere `Esc`. Gli avvisi di tipo *warning* (limiti di token al minuto, tetto sui tool) ora si vedono
sempre, non solo con `--debug`.

### Annullare le modifiche (`/undo`)

A fine turno, se l'agente ha cambiato dei file, compare un riepilogo:

```text
✎ 5 file changed · /undo to revert
    - old.txt  [deleted]
    ~ README.md  +1 -0
    + src/App/Program.cs  [new, 12 lines]
```

`/undo` (anche dal menu `/`) mostra cosa verrebbe annullato, chiede conferma e riporta tutto com'era prima di quel turno:
i file modificati o cancellati tornano al contenuto di partenza, quelli nuovi vengono rimossi (insieme alle cartelle rimaste
vuote). Si può ripetere per tornare indietro di più turni.

- **Come funziona**: prima di ogni modifica fatta con i tool (`WriteFile`, `EditFile`, `ReplaceInFiles`, `DeleteFile`, gli
  strumenti per le immagini) il file viene copiato in `.ralfai/undo/<turno>/`. Le modifiche dei comandi esterni
  (`dotnet new`, `cargo new`…) si riconoscono confrontando l'elenco dei file prima e dopo il turno. Non serve git.
- **Cosa non si può annullare**: un file che esisteva già e che un *comando* ha modificato o cancellato (per esempio
  `dotnet add package` su un `.csproj`): il suo contenuto precedente non è stato salvato, quindi è solo segnalato come
  "cannot be reverted". Cartelle di build (`bin`, `obj`), `.git` e `.ralfai` non sono controllate.
- **Sicurezza**: se hai modificato a mano un file *dopo* il turno, `/undo` non lo sovrascrive e lo elenca tra i saltati.
  Funziona anche per un turno annullato con `Esc` o finito in errore.
- **Configurazione**: `"Undo": { "Keep": 10 }` sono i turni conservati (`0` disattiva tutto). Le copie stanno nella cartella
  del progetto, come il log di sessione: se il progetto contiene segreti, anche le copie li contengono.

### Registro delle richieste (`PROMPTS.md`)

Ogni richiesta che invii viene aggiunta da RalfAI stesso a `PROMPTS.md` nella radice della cartella target, prima che il
modello cominci a lavorare:

```markdown
## 31. "Aggiungi lo scenario RUST, con lo scaricamento dello strumento se manca"

_2026-10-23 16:30_
```

Una richiesta su più righe o più lunga di 100 caratteri ha un titolo breve e il testo completo sotto (fino a 4.000
caratteri). La numerazione prosegue dal numero più alto già nel file, qualunque formato abbia (`12. "testo"` o
`## 12. "testo"`), quindi i file scritti a mano o da versioni precedenti continuano a funzionare. Non costa nessuna chiamata
al modello, che infatti non è più incaricato di scriverlo, e non compare tra le modifiche annullabili con `/undo`.
Con `"PromptLog": { "File": "" }` si disattiva; un altro nome di file si indica nella stessa chiave.

### Permessi sui comandi

I tool sui file lavorano solo dentro la cartella del progetto e si possono annullare con `/undo`. I tool che lanciano
processi (`DotNet.*`, `Rust.*`) possono invece eseguire qualunque cosa, quindi passano da una politica:

- **Senza chiedere** (il flusso normale): `dotnet build/test/restore/new/add/format/publish/pack…`, `cargo build/check/test/new/add/fmt/clippy/doc…`,
  `rustup show` e le sole richieste di elenco.
- **Chiedono prima**: ciò che esegue il codice del progetto o altro codice (`dotnet run`, `dotnet exec`, `cargo run`), ciò che scarica o
  installa software (`dotnet tool`, `dotnet new install`, `cargo install`, `rustup target add`, `rustup toolchain install`, l'installazione
  di Rust), ciò che parla con un registro di pacchetti (`publish`, `login`) e tutto ciò che non è in elenco.

Quando serve un permesso compare, sopra lo spinner:

```text
⚠ Permission needed - this runs the project's code
    dotnet run --project src/App
    [y] allow once   [a] always allow dotnet run* this session   [n] deny (Esc too)
```

`y` lo consente una volta, `a` per il resto della sessione (la regola è `dotnet run*`), `n` o `Esc` lo rifiuta; un comando rifiutato
non viene richiesto di nuovo nella stessa sessione e il modello riceve il motivo, così può scegliere un'altra strada o dirti di
eseguirlo tu. Mentre la domanda è sul schermo `Esc` non interrompe il turno: i tasti rispondono alla domanda.

Configurazione (`Permissions` nel config):

```json
"Permissions": {
  "Mode": "ask",
  "Allow": [ "dotnet run*", "cargo run*" ],
  "Deny":  [ "dotnet nuget push*", "cargo publish*" ]
}
```

- `Mode`: `ask` (predefinito), `allow` (mai domande, come prima di questa funzione; vale ancora l'elenco `Deny`), `deny` (niente domande, ciò che
  non è consentito è rifiutato).
- Le regole sono glob sulla riga di comando in minuscolo, con `*`. Ordine: `Deny`, poi `Allow`, poi i "sempre" della sessione, poi l'elenco
  predefinito di ciò che chiede, poi quello di ciò che passa, infine la modalità.
- **Senza nessuno a cui chiedere** (input da pipe, script, Jupyter, Studio): un comando che richiede il permesso viene rifiutato e il modello
  riceve il messaggio. Per questi casi aggiungi i comandi a `Allow` o imposta `"Mode": "allow"`.
- Limite onesto: `build` e `test` eseguono comunque codice del progetto (target MSBuild, test), e un agente può scrivere un `.csproj` con
  un'azione `Exec`. La politica evita i casi evidenti, non è un sandbox.

### Contesto: compattazione dei risultati vecchi

Ogni chiamata al modello rimanda l'intera conversazione, e in una sessione lunga la maggior parte è output vecchio dei tool: file letti, corpi
interi di file scritti, log di build, screenshot. RalfAI sostituisce il grosso di ciò che è vecchio con un segnaposto **nella copia inviata al
modello**; la sessione, il log e `/undo` non cambiano:

- un risultato vecchio diventa `[earlier tool result omitted to save context: 5,016 characters. It began: "...". Call the tool again if you need its content.]`;
- il corpo di un file scritto, negli argomenti di una vecchia chiamata, diventa i primi 60 caratteri più `[N characters omitted…]` (il percorso resta);
- le immagini restano solo negli ultimi 2 messaggi dell'utente.

Gli ultimi 20.000 caratteri di output dei tool restano interi, così l'agente conserva la memoria di ciò che sta facendo adesso. I segnaposto sono
identici a ogni chiamata (e non dipendono dalla lingua del computer), quindi il prefisso del prompt non cambia e la cache del provider continua a servire.
Con `--debug` compare un avviso quando la compattazione aumenta.

```json
"Context": { "Compaction": "on", "RecentToolChars": 20000, "CompactMinChars": 600, "KeepImages": 2 }
```

`Compaction: "off"` la spegne; `RecentToolChars` è la finestra "intera" (più stretta = più risparmio, ma il modello può dover rileggere); sotto
`CompactMinChars` nulla viene sostituito. **Misure** (sessione di 3 turni su 10 moduli da 76 KB: leggili tutti, rinomina un metodo, due domande):
token in ingresso circa 256.000 senza compattazione, 178.000 con finestra 30.000, 155.000 con 20.000 (predefinito), 92.000 con 15.000, stessi file e risposte
corrette, tempi simili. Con 60.000 non si guadagnava nulla. Sessioni molto più lunghe risparmiano di più.

### Log di sessione

In `.ralfai/` di ogni progetto restano due file con compiti diversi:

- **`session.md`** - il resoconto leggibile: richieste, chiamate ai tool, risposte e ragionamento. Argomenti e output dei tool sono accorciati a
  `Log:ToolOutputChars` caratteri (2.000; `0` = per intero), così un file letto non resta lì per sempre. Oltre `Log:MaxMegabytes` (5; `0` = mai) viene
  rinominato `session.<data-ora>.md` e ne inizia uno nuovo.
- **`session.jsonl`** - ciò che serve per riprendere: un oggetto JSON per messaggio dell'utente o dell'agente, e nient'altro. È piccolo, si legge in
  un attimo e non dipende dal parsing del Markdown; una riga rovinata da un'interruzione non fa perdere il resto. Un progetto che ha solo il vecchio
  `session.md` viene convertito alla prima apertura (prima dell'eventuale rotazione, quindi la conversazione non si perde).

Alla ripresa il modello riceve solo la parte più recente (`History:ResumeMaxTokens`, 30.000 token stimati). Se una parte più vecchia è stata lasciata fuori, in
testa trova una **nota** con le richieste precedenti dell'utente, una riga ciascuna (le ultime 25, in ordine), e l'invito a controllare i file e `PROMPTS.md`
invece di indovinare. Costa poche centinaia di token e non richiede nessuna chiamata al modello. Su un progetto reale da 3 MB: migrazione in 0,1 s la
prima volta, 1 ms dopo; la storia di ripresa pesa 113 KB.

### Simboli e terminale (`--ascii`)

Frecce, puntini, righe e icone dell'interfaccia hanno bisogno di un terminale che le mostri. Su Windows una console aperta con la codepage OEM (850, 437…)
non sa rappresentarle e le scrive come `?`: era il `❯` del prompt che compariva come `?`. All'avvio RalfAI sceglie:

- **terminale moderno** (Windows Terminal, VS Code, ConEmu, WSL; un locale UTF-8 su Linux/macOS) o **uscita rediretta** (file, pipe): l'uscita passa a
  UTF-8 e i simboli si vedono;
- **altrimenti**: i simboli vengono sostituiti con equivalenti semplici (`→` diventa `->`, `…` diventa `...`, le righe diventano `-`, `✎` diventa `~`,
  le emoji diventano `[+]`, `(mic)`…). Le lettere accentate si vedono sempre.

Si forza con `"Ui": { "Glyphs": "ascii" }` (valori `auto`, `unicode`, `ascii`; variabile d'ambiente `RALFAI_Ui__Glyphs`) oppure con `--ascii` sulla riga di comando.
Anche l'input da pipe viene letto come UTF-8 (prima "è" arrivava come `├¿`) e il testo incollato dalla clipboard di Windows non perde più i caratteri fuori dalla codepage OEM.

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
