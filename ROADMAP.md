# Roadmap

Stato delle iniziative macro per RalfAI. Aggiornata al 2026-07-27.

## ✅ Fatto

### Documentazione
README, architettura, guida all'uso e questa roadmap (questo set di documenti).

### Context non più hardcoded
Prima `Program.cs` istanziava `new DotNetContext()` direttamente. Ora `ContextRegistry`
risolve il context per nome (`--context <nome>` sulla riga di comando, oppure chiave
`AgentContext` in config/env — il default è cambiato più volte da allora, vedi la voce
"Global tool" più sotto per lo stato attuale). `DotNetContext` era l'unica implementazione al
momento di questa modifica — nessuna parte dell'app oltre `Program.cs` dipendeva da .NET
specificamente: aggiungere uno stack nuovo è un'implementazione di `IAgentContext` più una riga
in `ContextRegistry`, senza toccare `RalfAIApp`. Dettagli in [README.md](README.md#contexts).

### Speech-to-Text / Text-to-Speech con Azure AI Speech
Implementato con `Microsoft.CognitiveServices.Speech` (Azure Speech SDK), in
[Voice/SpeechService.cs](src/RalfAI.Core/Voice/SpeechService.cs) (motore condiviso):

- **`/voice`** (dentro `InputCollector`, console) e **bottone 🎤** (`ChatView`, Desktop) —
  ascoltano dal microfono di default, si fermano da soli alla pausa (`RecognizeOnceAsync`).
- **`/speak`** (console) e **bottone 🔊** (`ChatView`, Desktop) — leggono ad alta voce l'ultima
  risposta finale dell'agente (`AgentSession.LastFinalText`).
- Configurazione opzionale sotto `AzureSpeech` in `appsettings.json` (region, key, lingua di
  riconoscimento, voce); se assente, `SpeechService.FromConfig` ritorna `null` e le funzioni
  rispondono con un messaggio d'errore invece di far fallire l'avvio — nessun impatto sul resto
  dell'app. Dettagli in [USAGE.md](USAGE.md#voce-speech-to-text--text-to-speech) e
  [ARCHITECTURE.md](ARCHITECTURE.md).

### Motore condiviso (`RalfAI.Core`) ed estrazione di `AgentSession`
Prerequisito per avere più frontend senza duplicare la logica dell'agente. `RalfAIApp`
mischiava Semantic Kernel, rate limiting, logging e voce con il rendering Spectre.Console; è stato
estratto tutto in `AgentSession` (progetto `RalfAI.Core`, nessuna dipendenza UI), che espone
`SendAsync` più gli eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice` per farsi renderizzare da
qualsiasi frontend. La console (`RalfAI`) ora è un consumer sottile di `AgentSession`, non è
cambiato nulla nel suo comportamento visibile. Dettagli in [ARCHITECTURE.md](ARCHITECTURE.md).

### Global tool (`ralf`) e cartella target opzionale
`RalfAI` si installa come [.NET global tool](https://learn.microsoft.com/dotnet/core/tools/global-tools)
(`ralf`). L'argomento posizionale con la cartella è opzionale: senza, si apre un file browser a
schermo intero (`FileBrowser`) e l'ultima cartella usata viene ricordata (`LastFolderStore`) per i
riavvii successivi. Nuovi context oltre a `dotnet`: `markdown` (default) e `html` (con tool
`StaticServer` per servire l'app su HTTP locale). Dettagli in [USAGE.md](USAGE.md) e
[README.md](README.md).

### Sessione interattiva a schermo intero
La console gira nello screen buffer alternato del terminale, con composizione multi-riga
(`Ctrl+Enter` per inviare) e tasti funzione (`F1`-`F10`) al posto della vecchia lista di comandi
`/exe`, `/cancel`, `/history`: cambio context (`F6`), cambio cartella (`F7`), toggle debug (`F8`),
dettatura/lettura vocale (`F2`/`F3`). I comandi testuali (`/exe`, `/switch-context`, …) restano
disponibili solo quando lo stdin è rediretto (input da pipe/script). Dettagli in
[USAGE.md](USAGE.md#sessione-interattiva).

### Motore condiviso (`RalfAI.Core`) ed estrazione di `AgentEngine`
Prerequisito per avere più frontend senza duplicare la logica dell'agente. `RalfAIApp`
mischiava Semantic Kernel, rate limiting, logging e voce con il rendering Spectre.Console; è stato
estratto tutto in `AgentEngine` (progetto `RalfAI.Core`, nessuna dipendenza UI), che espone
`SendAsync` più gli eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice`. La console (`RalfAI`) è
un consumer sottile di `AgentEngine`. Il log di sessione vive in `<target>/.ralfai/session.md`
(non più `.chat/`). Dettagli in [ARCHITECTURE.md](ARCHITECTURE.md).

### Provider AI multipli (Azure AI Foundry, OpenAI, Anthropic)
`AgentEngine.BuildChatClient` sceglie il provider da `AIProvider` in config (`foundry` di default,
oppure `openai`/`anthropic`), ciascuno tramite l'SDK ufficiale di quel provider. Dettagli in
[README.md](README.md#choosing-a-provider) e [ARCHITECTURE.md](ARCHITECTURE.md).

### Documentazione sempre in radice (niente `docs/`)
I prompt di sistema di `dotnet`, `html` e `markdown` non mettono più la documentazione dentro
una sottocartella `docs/`: `README.md`/`USAGE.md`/`ARCHITECTURE.md`/`ROADMAP.md` vivono sempre
alla radice della cartella target, sullo stesso modello di questo stesso repo. `dotnet` e `html`
mantengono `src/`/`ops/` per codice e config. Dettagli in [ARCHITECTURE.md](ARCHITECTURE.md) e
[USAGE.md](USAGE.md).

### Log dei prompt per progetto (`PROMPTS.md`)
Quinto file nel set canonico di documentazione, ma non opzionale come gli altri quattro: ogni
context (`dotnet`, `html`, `markdown`) ora apre ogni turno con un passo "Log" che appende la
richiesta ricevuta — verbatim o riassunta se lunga/rumorosa — a `PROMPTS.md` alla radice della
cartella target, come voce numerata, prima di qualunque altra azione. Rende la storia di un
progetto generato tracciabile fino alle richieste che l'hanno guidata, indipendentemente dal
log interno di sessione (`.ralfai/session.md`, motore, non deliverable). Applicato anche a
questo stesso repo — vedi [PROMPTS.md](PROMPTS.md). Dettagli in [USAGE.md](USAGE.md).

### Manipolazione immagini JPG/PNG (`Image.*`)
Nuovo `ImagePlugin` (`RalfAI.Core`, `SixLabors.ImageSharp`) con `GetInfo`, `Resize`, `Crop`,
`Rotate`, `Flip`, `Grayscale`, `ConvertFormat` — registrati come tool engine-level, sempre
disponibili in ogni context, nessuna configurazione richiesta. Dettagli in
[ARCHITECTURE.md](ARCHITECTURE.md).

### Generazione immagini via AI (`Image.GenerateAI`)
Nuovo `ImageGenerationService`/`ImageGenerationPlugin` per generare immagini (loghi, icone,
illustrazioni) da un prompt testuale via DALL-E/gpt-image su Azure AI Foundry o OpenAI —
stesso switch `AIProvider` usato per la chat, ma con la propria sezione `ImageModelId` per
provider. Non disponibile su Anthropic (nessuna API di generazione immagini) né quando
`ImageModelId` non è configurato — il tool semplicemente non viene registrato, nessun impatto
sul resto dell'app. Dettagli in [README.md](README.md) e [ARCHITECTURE.md](ARCHITECTURE.md).

### Apertura file in editor esterno dal file browser (`F4`)
`FileBrowser` (folder picker iniziale e `F7` durante una sessione) apre ora il file
selezionato in un editor esterno con `F4`, via `Process.Start(UseShellExecute: true)`
(fire-and-forget, non attende la chiusura). Comando configurabile sotto la chiave `Editor`
(`RALFAI_Editor` per l'override via env) — default `code.exe` (VS Code) se non impostato.
`FileBrowser` resta senza dipendenza da `IConfiguration`: la risoluzione del comando avviene
nei chiamanti (`Program.cs`, `RalfAIApp.SwitchFolder`). Dettagli in
[ARCHITECTURE.md](ARCHITECTURE.md) e [USAGE.md](USAGE.md#file-browser).

### Help completo e separato per modalità interattiva/pipe
`F1` (interattivo) elenca ora anche `Ctrl+C` ed `Esc`/`Ctrl+X` (interruzione del turno in corso),
prima assenti dall'overlay pur essendo comportamenti reali. `/help` in modalità pipe non
richiama più la stessa tabella pensata per i tasti funzione — mostra invece la tabella dei
comandi testuali realmente disponibili in quella modalità (`/exe`, `/cancel`, `/voice`, `/speak`,
`/clear`, `/switch-context`, `/switch-folder`, `/toggle-debug`, `/help`, `exit`). Corretto anche
un bug: `/help` in modalità pipe chiamava `Console.ReadKey` su uno stdin rediretto, che lancia
`InvalidOperationException` — l'overlay pipe ora si limita a stampare la tabella senza attendere
un tasto. Dettagli in [USAGE.md](USAGE.md#sessione-interattiva).

### Audit dei link e delle voci di roadmap precedenti
Verifica sistematica di tutta la documentazione di questo repo: corretti tre anchor Markdown
rotti (due puntavano a titoli in italiano dentro `README.md`, che è in inglese; uno puntava a
una sezione mai esistita in `ARCHITECTURE.md`), un link relativo a un file sorgente che risaliva
di una cartella di troppo, e la voce "Context non più hardcoded" che citava una classe
(`AgentContextFactory`) non più presente nel codice (oggi `ContextRegistry`).

### File manager a doppio pannello stile Norton Commander (`F9`/`/files`) — poi rimosso
_Rimosso: sostituito dalle menzioni `@` nel prompt (vedi sotto). `DirectoryListing` e `FileViewer` restano, usati da `FileBrowser`._

Nuovo `NortonCommander`: due pannelli directory affiancati, `Tab` per cambiare quello attivo,
barra F-key classica (F3 view, F4 edit esterno, F5 copy, F6 move/rename, F7 mkdir, F8 delete,
F10/Esc quit) — copia e sposta usano di default la cartella dell'altro pannello come
destinazione. Nuovo `FileViewer` condiviso (scorrevole in verticale/orizzontale, numeri di riga,
rileva i binari, gestisce file fino a 5 MB) sostituisce il vecchio dump statico del `FileBrowser`
esistente, che lo riusa anch'esso per `F3` — `FileBrowser` resta il picker semplice a singolo
pannello per la cartella target (avvio senza argomenti, cambio cartella). Estratto anche
`DirectoryListing` (elenco/formattazione directory) per non duplicare quella logica fra i due.
Dettagli in [ARCHITECTURE.md](ARCHITECTURE.md) e [USAGE.md](USAGE.md#file-manager).

### Console più sobria: log di progresso senza estetica "hacker", cursore finto inequivocabile
Il log di progresso/tool-call scorrevole (introdotto per sostituire la singola riga che si
sovrascriveva) usava colori accesi (`yellow`/`cyan` in grassetto) e simboli (`⏺`/`◌`) che, in
uso, davano l'impressione di un terminale "sotto attacco". Sostituiti con testo `dim`/`grey` e un
semplice marcatore "·" — il comportamento (scorre, non si sovrascrive) resta invariato, cambia
solo lo stile. Il cursore finto dell'editor del prompt (un carattere invertito) poteva risultare
poco visibile a seconda del tema del terminale; sostituito con un blocco a colori espliciti
(`black on green`), sempre visibile indipendentemente dal tema. Dettagli in
[USAGE.md](USAGE.md#editor) e [USAGE.md](USAGE.md#progresso-e-costi).

### Editor del prompt più vicino a Claude Code, profili esterni
- **Menzioni `@file`** con popup fuzzy sui file della cartella target, **menu `/`** dei comandi,
  **cronologia** `↑`/`↓` per cartella (`.ralfai/history.jsonl`), scorciatoie readline
  (`Ctrl+A/E/U/K/W`), **`Ctrl+Z`** e paste lungo compattato (`[Pasted #1: 42 lines]`). Dettagli in
  [USAGE.md](USAGE.md).
- **Profili esterni**: i context si possono definire come file Markdown con frontmatter in
  `~/.ralfai/profiles` e `<target>/.ralfai/profiles`, senza ricompilare; `--export-profile`
  esporta un built-in come punto di partenza.
- Rimosso il file manager a doppio pannello (`NortonCommander`, `F9`/`/files`).

### Streaming di risposta e ragionamento, velocità in token/s
`AgentEngine.SendAsync` usa `RunStreamingAsync` ed emette `Streamed` (frammenti di ragionamento e di
risposta) e `RateChanged` (token/s istantanei, medi del turno e della sessione); la console stampa a
righe e mostra la velocità sullo spinner. Il provider Foundry usa l'API Responses (`AzureAIFoundry:Api`).
Dettagli in [USAGE.md](USAGE.md#streaming-ragionamento-e-velocità).

### Context Rust con toolchain scaricata in locale
`--context rust` (tool `Rust_EnsureToolchain`/`Rust_RunCargo`/`Rust_RunRustup`): usa il Rust già presente
oppure scarica `rustup-init` verificandone lo SHA-256 e installa in `~/.ralfai/rust` senza toccare profilo e
`PATH`; componenti e target extra via `rustup` su richiesta. Dettagli in [USAGE.md](USAGE.md#context-rust).

### Strumenti per i file più fini (`EditFile`, `ReplaceInFiles`, `SearchFiles`, `FindFiles`)
Modifica per sostituzione invece di riscrittura, ricerca e lettura a intervalli, elenchi senza cartelle di build
e con un tetto; corretto anche un controllo dei percorsi che ammetteva cartelle con lo stesso prefisso.
Dettagli in [USAGE.md](USAGE.md#strumenti-per-i-file).

### Tetti per turno contro i cicli del modello
`Limits:MaxToolCalls` (morbido), `MaxOutputTokens` e `MaxReasoningChars` (duri), configurabili e disattivabili.
Dettagli in [USAGE.md](USAGE.md#limiti-per-turno).

### Annullare i turni (`/undo`) e riepilogo delle modifiche
Registro per turno in `.ralfai/undo` (copia prima di ogni modifica dei tool, confronto prima/dopo per i comandi esterni),
riepilogo dei file cambiati con righe aggiunte/tolte, `/undo` con conferma che non sovrascrive modifiche successive.
Dettagli in [USAGE.md](USAGE.md#annullare-le-modifiche-undo).

### Pulizia del repo e script di installazione (2026-10-23)
`bin/` e `obj/` (circa 1.700 file) non sono più tracciati da git e sono in `.gitignore`, insieme a `.ralfai/undo/` e
`.ralfai/history.jsonl`. `ops/install-tool.ps1` e `ops/install-studio-tool.ps1` controllano il codice di uscita di ogni
comando `dotnet`, si fermano elencando i processi `ralf` / `ralf-studio` (con PID e ora di avvio) che bloccano
l'aggiornamento globale, e dicono quando la versione richiesta è già installata invece di stampare "installed".

### Registro di `PROMPTS.md` scritto dal motore (2026-10-23)
`PromptLog` aggiunge ogni richiesta a `PROMPTS.md` prima che il turno cominci, con numerazione continua (anche su file in
formati misti), titolo breve e testo completo per le richieste lunghe, data e ora. Il passo "Log" è stato tolto dai prompt
di `dotnet`, `dotnet11`, `rust`, `markdown` e `html`: un giro in meno col modello a ogni turno e nessun file di log
riscritto male. Configurabile con `PromptLog:File`. Dettagli in [USAGE.md](USAGE.md#registro-delle-richieste-promptsmd).

### Test del motore con un client finto (2026-10-23)
`AgentEngine.CreateAsync` ha un overload che accetta un `IChatClient`. `ScriptedChatClient` + `EngineHarness` permettono di
provare il ciclo del turno senza rete: streaming di risposta e ragionamento, tag `<think>` spezzati, uso dei token, richiesta
del riassunto di ragionamento e ripiego se il modello lo rifiuta, tool e risultati, riepilogo e `/undo`, tetti per turno
(morbido e duro), `PROMPTS.md`, allegati, annullamento con `Esc`, ripresa della sessione con storia limitata. 19 test nuovi
(115 in tutto); verificato con due mutazioni del motore che i test giusti falliscono. Dettagli in
[ARCHITECTURE.md](ARCHITECTURE.md#test-del-motore).

### Permessi sui comandi (2026-10-23)
`PermissionPolicy` decide se un tool che lancia processi (`DotNet.*`, `Rust.*`) può partire: il flusso normale (build, test, scaffold,
formattazione) passa senza chiedere; `run`, installazioni, registri di pacchetti e comandi non in elenco chiedono `y` / `a` / `n` sopra lo
spinner. Modalità `ask` / `allow` / `deny`, elenchi `Allow` e `Deny` a glob nel config, risposta "sempre" per la sessione, comando rifiutato non
richiesto di nuovo, nessun gestore (pipe, Studio, Jupyter) = rifiutato con un messaggio chiaro al modello. 42 test, di cui 10 sul motore.
Dettagli in [USAGE.md](USAGE.md#permessi-sui-comandi). Studio e Jupyter non possono ancora porre la domanda (vedi gli ultimi passi).

### Compattazione dei risultati vecchi dei tool (2026-10-23)
`CompactingChatClient` + `ContextCompactor` sostituiscono, nella copia inviata al modello, i risultati vecchi dei tool, i corpi dei file scritti e le
immagini non recenti con segnaposto deterministici (sessione e `/undo` intatti). Finestra "intera" `Context:RecentToolChars` = 20.000 caratteri. Misura su una
sessione di 3 turni: token in ingresso da circa 256.000 a circa 155.000 (−40%), risposte corrette; 60.000 non faceva risparmiare, quindi il valore
predefinito è stato scelto sui dati. 22 test. Dettagli in [USAGE.md](USAGE.md#contesto-compattazione-dei-risultati-vecchi).

### Log di sessione: storia in JSONL, resoconto limitato, nota sulle richieste vecchie (2026-10-23)
`session.jsonl` contiene solo i messaggi di utente e agente e serve per riprendere la sessione (rapido, robusto a righe rovinate, nessun parsing
del Markdown); un `session.md` del passato viene convertito alla prima apertura. `session.md` resta il resoconto leggibile ma con argomenti e output
dei tool accorciati (`Log:ToolOutputChars`) e rotazione oltre `Log:MaxMegabytes`. Quando la ripresa lascia fuori una parte vecchia della conversazione, il
modello trova in testa una nota con le richieste precedenti (una riga ciascuna), senza chiamate al modello. Su un progetto reale da 3 MB: avvio 136 ms la prima
volta e 1 ms dopo. 15 test. Dettagli in [USAGE.md](USAGE.md#log-di-sessione).

### Simboli e terminale: UTF-8 dove si può, ASCII dove serve (2026-10-23)
Causa del `?` al posto di `❯`, frecce ed emoji: la console Windows con codepage OEM. `TerminalGlyphs` passa a UTF-8 sui terminali che lo supportano
(o con l'uscita rediretta) e altrimenti sostituisce i simboli con equivalenti ASCII (`Ui:Glyphs` = auto / unicode / ascii, oppure `--ascii`), tenendo le lettere
accentate. L'input da pipe è letto come UTF-8 (prima "è" diventava `├¿`) e la clipboard di Windows è letta in UTF-8. 12 test. Dettagli in
[USAGE.md](USAGE.md#simboli-e-terminale---ascii).

### Contesti come file YAML e Context designer (2026-10-24)
I contesti non sono più nel codice: `dotnet`, `dotnet11`, `rust`, `markdown` e `html` sono file YAML (`name`, `description`,
`tools`, `prompt`) nella cartella dei contesti, accanto al file di configurazione esterno (o `Contexts:Folder`, o
`~/.ralfai/contexts`), più `<target>/.ralfai/contexts` per i singoli progetti. Quelli forniti vengono scritti al primo
avvio e poi sono file come gli altri (cancellati non tornano). L'elenco è letto a ogni richiesta, quindi un file nuovo è
subito in `F6` e in `--list-contexts`. Nel codice resta solo `contexts` (Context designer), che con gli strumenti
`Contexts.*` elenca strumenti e contesti, legge, controlla e salva i YAML. Provato col modello vero: ha creato un contesto
Python valido al primo tentativo. Ora un nome di strumento sbagliato dentro un gruppo esistente (`Web_Fetch`) blocca il
file invece di essere ignorato. `--export-context` sostituisce `--export-profile`; i vecchi profili `.md` sono ancora letti.
Nella console le righe degli strumenti mostrano solo la prima frase della descrizione. 30 test nuovi. Dettagli in
[USAGE.md](USAGE.md#contesti-file-yaml).

### Sorgente dei contesti, versioni, `dotnet10`, publish e global tool (2026-10-24)
I contesti forniti stanno nel repo in `config/contexts` con un `index.yaml` (versione, data, impronta SHA-256 e impronte
delle versioni precedenti); gli stessi file sono inclusi nel tool. All'avvio, se `Contexts:Check` è `true` (predefinito)
o se sono passati `Contexts:CheckEveryDays` giorni, RalfAI legge la sorgente (`Contexts:Source` o `--contexts-source`,
per difetto la cartella su GitHub), aggiunge i contesti che mancano, aggiorna quelli non modificati, lascia stare quelli
cambiati (lo dice una volta) e quelli cancellati, poi rimette `Contexts:Check` a `false` nel file di configurazione.
Stato in `.state.json`; i file della 1.15 non modificati vengono riconosciuti e aggiornati. `dotnet` segue l'ultima
versione (oggi 10), `dotnet10` è nuovo. Nel gruppo DotNet: `RunPublish` e `InstallGlobalTool` (pack in `.ralfai/nupkg` +
`dotnet tool update -g`, chiede il permesso). 29 test nuovi. La sorgente GitHub risponde 404 finché `config/contexts` non
è su `main`. Dettagli in [USAGE.md](USAGE.md#contesti-file-yaml).

## 📋 Pianificato

### Prossimi passi, in ordine di priorità
L'ordine è un giudizio (piccoli lavori che sbloccano o proteggono quelli successivi, poi i rischi, poi le prestazioni,
infine ciò che è grande). Quando un punto è fatto passa sotto "✅ Fatto" con la data.

1. **Chiave API in chiaro** (vedi sotto).
2. **Client MCP** - strumenti aggiunti senza ricompilare (database, browser, CI); si abbina ai contesti YAML (un contesto potrebbe elencare i server MCP che usa).
3. **Interfaccia per gestire la configurazione** - finestre di dialogo per le impostazioni (provider, modello, limiti,
   permessi, contesti, sorgente), scelta di file e cartelle con un browser, ricerca di cartelle. Da decidere se in
   console (Spectre.Console) o in Studio.
4. **Studio e kernel Jupyter alla pari della console** - il motore emette già streaming, velocità, allegati, riepilogo delle modifiche e richieste di permesso; manca la parte grafica (oggi in Studio e Jupyter i comandi che chiedono un permesso vengono rifiutati, salvo regole `Permissions:Allow`).

### Interfaccia grafica (Desktop/Web)
Nessuna UI grafica esiste oggi: solo la console (`RalfAI`, Spectre.Console) è implementata. Un
frontend Desktop o web (WinForms/WebView2, Blazor Hybrid o Server) resta un'idea aperta — se
verrà costruito, riuserà `AgentEngine`/`RalfAI.Core` così com'è, senza toccare la console.
Nessuna decisione presa su tecnologia o tempistiche.

### Irrobustire la gestione della API key
`appsettings.json` contiene oggi la API key Azure in chiaro in `RalfAI/appsettings.json`.
Rischioso se il repo viene condiviso o versionato. Il codice supporta già l'override via variabile
d'ambiente `RALFAI_AzureAIFoundry__ApiKey` (o l'equivalente per `OpenAI`/`Anthropic`) e un
`--config`/`RALFAI_CONFIG_PATH` che punta fuori dalla cartella d'installazione; manca solo il passo
di:
- rimuovere la key da `appsettings.json` (o sostituirla con un placeholder — vedi già
  `config.example.json`),
- documentare `dotnet user-secrets` come alternativa locale.

Rimandato su richiesta esplicita — non ancora prioritario.

## Non pianificato (idee aperte, non richieste esplicitamente)
- Altri context (`IAgentContext`) per stack diversi da .NET/Markdown/HTML (Python, Node, script di
  shell…): la factory è pronta ad accoglierli, ma nessuno è stato richiesto finora.
- Un frontend grafico (Desktop o web) e l'autenticazione/multi-utente che comporterebbe.
