# RalfAI Session

> Started: 2026-08-06 01:12:37

---

### USER — 01:12:37

Vuoi creare il logo del progetto? Una immagine?

---

### AGENT — 01:12:42

Sì, posso creare un logo per **RalfAI**, ad esempio:

- stile moderno e minimale;
- simbolo che combina **“R”**, intelligenza artificiale e codice `.NET`;
- palette blu/viola su sfondo trasparente;
- formati **SVG** per il progetto e **PNG** per README, icone e social.

Posso anche integrarlo direttamente nel repository. Dimmi semplicemente “procedi” e realizzerò una proposta completa.

---

### USER — 01:13:06

procedi

---

### TOOL — FileSystem.ReadFile — 01:13:09

args: relativePath=PROMPTS.md
result: # Prompts

Log cronologico e numerato delle richieste che hanno guidato la realizzazione di RalfAI — la
stessa convenzione (`PROMPTS.md` alla radice, aggiornato a ogni obiettivo, prima di qualunque
altra azione) che i context `dotnet`/`html`/`markdown` ora applicano a ogni progetto che
generano. Vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
[USAGE.md](USAGE.md#log-dei-prompt-promptsmd).

> **Nota sulla provenienza.** Questo file è stato scritto da un assistente di coding (Claude
> Code) che lavorava su questo repo via git — non da `ralf` in esecuzione su se stesso. Le voci
> 1–8 riguardano funzionalità arrivate in sessioni precedenti a questa: la git history parte da
> un "Initial Commit" già completo (vedi [ROADMAP.md](ROADMAP.md)), quindi il testo esatto di
> quelle richieste non è disponibile — sono **ricostruite** a partire dal changelog in
> `ROADMAP.md` e marcate `[ricostruito]`. Le voci dalla 9 in poi sono **verbatim**, prese da
> questa conversazione.

## 1. Realizzare un agente di coding autonomo per .NET `[ricostruito]`

> Costruisci un agente CLI autonomo che, data una cartella e un obiettivo, scaffoldi un
> progetto .NET con `dotnet new`, scriva i file sorgente, compili con `dotnet build` e testi
> con `dotnet test`, correggendo finché non vanno a buon fine.

Realizzato: `RalfAIApp` (Spectre.Console) + `DotNetContext` come unico context, motore basato
su Microsoft Agent Framework contro Azure AI Foundry.

## 2. Rendere il context non hardcoded `[ricostruito]`

> Il context .NET è cablato direttamente in `Program.cs` — rendilo selezionabile, così in
> futuro si possono aggiungere altri stack senza toccare l'app.

Realizzato: `AgentContextFactory`/`ContextRegistry`, flag `--context`, chiave `AgentContext` in
config/env, `dotnet` come default (poi diventato `markdown`).

## 3. Aggiungere Speech-to-Text / Text-to-Speech `[ricostruito]`

> Aggiungi la possibilità di dettare i messaggi a voce e farsi leggere le risposte ad alta
> voce, usando Azure AI Speech.

Realizzato: `SpeechService` (motore condiviso), comandi `/voice`/`/speak` in console,
opzionale via sezione `AzureSpeech` in config — nessun impatto sul resto dell'app se assente.

## 4. Estrarre un motore condiviso dalla console `[ricostruito]`

> La logica dell'agente è mischiata con il rendering Spectre.Console — separala in un motore
> riusabile da altri frontend futuri.

Realizzato: `AgentSession` (poi rinominato `AgentEngine`) in `RalfAI.Core`, nessuna dipendenza
UI, eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice`.

## 5. Distribuire come global tool e rendere la cartella opzionale `[ricostruito]`

> Pacchettizza RalfAI come .NET global tool (`ralf`) installabile da riga di comando, e
> permetti di avviarlo senza specificare la cartella, con un picker.

Realizzato: `PackAsTool`/`ToolCommandName=ralf`, `FileBrowser` + `LastFolderStore` per il
picker e la memoria dell'ultima cartella, nuovi context `markdown` (default) e `html`.

## 6. Sessione interattiva a schermo intero `[ricostruito]`

> Sostituisci il prompt a riga singola con un'interfaccia a schermo intero, composizione
> multi-riga e scorciatoie da tastiera invece dei comandi testuali.

Realizzato: screen buffer alternato del terminale, `Ctrl+Enter` per inviare, tasti `F1`-`F10`
(help, dettatura, lettura, pulizia schermo, cambio context/cartella, toggle debug, quit).

## 7. Supportare più provider AI `[ricostruito]`

> Non vincolare RalfAI ad Azure AI Foundry — fai in modo che possa parlare anche con OpenAI
> nativo e Anthropic Claude, ciascuno tramite il proprio SDK ufficiale.

Realizzato: `AgentEngine.BuildChatClient`, switch su `AIProvider` (`foundry`/`openai`/
`anthropic`), sezioni di config dedicate per provider.

## 8. Scrivere la documentazione del progetto `[ricostruito]`

> Scrivi README, architettura, guida all'uso e roadmap per RalfAI.

Realizzato: `README.md`, `ARCHITECTURE.md`, `USE.md` (poi rinominato `USAGE.md`, voce 11),
`ROADMAP.md`.

---

## 9. "Il file USE riporta la app desktop che non c'è"

`USE.md` e `ROADMAP.md` descrivevano `RalfAI.Desktop`/`RalfAI.ChatUI` (WinForms + WebView2 /
Blazor Hybrid) come già implementati, ma quei progetti non esistevano né nella solution né sul
filesystem. Confermato scavando nel codice (nessun riferimento reale, `RalfAI.sln` non li
include) e riscritto `USE.md` da zero contro il comportamento effettivo (global tool `ralf`,
cartella opzionale, default context `markdown`, sessione a schermo intero, `.ralfai/` invece di
`.chat/`).

## 10. "Anche la roadmap è disallineata"

Stesso problema in `ROADMAP.md`: la sezione "Interfaccia Desktop" dichiarava il frontend
grafico come fatto. Sostituita con voci che riflettono lo stato reale (global tool, file
browser, sessione a schermo intero, motore `AgentEngine`, provider multipli); l'interfaccia
grafica spostata in "📋 Pianificato" come idea aperta, non decisa.

## 11. "Puoi fare un aggiornamento ai contesti? I files md stanno sempre in radice, non in docs. In generale sempre: README USAGE ARCHITECTURE ROADMAP. Puoi aggiungere un supporto alla manipolazione di immagini jpg e png"

Due richieste in una:
- Rimosso `docs/` dal layout dei context `dotnet`/`html`: documentazione sempre alla radice
  della cartella target, con il set fisso `README.md`/`USAGE.md`/`ARCHITECTURE.md`/
  `ROADMAP.md` — rinominato anche `USE.md` → `USAGE.md` in questo repo, per coerenza.
- Nuovo `ImagePlugin` (`SixLabors.ImageSharp`): `Image.GetInfo`/`Resize`/`Crop`/`Rotate`/
  `Flip`/`Grayscale`/`ConvertFormat`, tool engine-level sempre disponibili in ogni context,
  nessuna configurazione richiesta.

## 12. "Per un progetto puoi anche generare un logo?"

Chiarito che si trattava di generazione AI (non disegno programmatico via forme/testo).
Aggiunti `ImageGenerationService` e `ImageGenerationPlugin` (`Image.GenerateAI`): genera
un'immagine da un prompt testuale via DALL-E/gpt-image su Azure AI Foundry o OpenAI, salvata
come JPG/PNG. Opzionale via `ImageModelId` sotto la sezione del provider attivo; non
disponibile con `AIProvider` `anthropic` (nessuna API di generazione immagini).

## 13. "Inoltre, quando sta generando, puoi dare un dettaglio di quello che sta pensando scrivendo generando? Come un log?"

Nessun meccanismo nuovo: la visibilità delle tool-call già esistente (descrizione nello
spinner mentre il tool gira, righe `⏺`/`⎿` con `F8`/`--debug` attivo) copre già questo caso —
la `[Description]` di `Image.GenerateAI` è stata scritta per essere informativa in quel
contesto.

## 14. "Hai aggiornato anche il comando?"

Chiarito (dopo un giro di domanda) che si riferiva ai comandi `dotnet pack`/`dotnet tool
install` in `README.md`. Verificato con un pack di prova che non serve alcuna modifica:
`SixLabors.ImageSharp` viene incluso automaticamente come dipendenza transitiva via
`RalfAI.Core`, nessuna DLL nativa coinvolta.

## 15. "Puoi aggiungere anche un file md con i PROMPTS? che in qualche modo rispecchia la realizzazione intera del progetto stesso"

Aggiunto `PROMPTS.md` come quinto file nel set canonico di documentazione — l'unico non
opzionale: ogni context (`dotnet`, `html`, `markdown`) ora apre ogni turno con un passo "Log"
che ci scrive prima di qualunque altra azione, così la storia di un progetto generato resta
tracciabile fino alle richieste che l'hanno guidata. Questo stesso file applica la convenzione
a ritroso a RalfAI.

## 16. "Aggiungi la possibilità di aprire un file durante la selezione: default code.exe, altrimenti variabile nel file di config"

Nuovo tasto `F4` in `FileBrowser` (usato sia al primo avvio senza cartella sulla riga di
comando, sia con `F7` durante una sessione): apre il file selezionato in un editor esterno via
`Process.Start(UseShellExecute: true)`, fire-and-forget. Comando risolto dalla nuova chiave di
config `Editor` (`RALFAI_Editor` per l'override via env), default `code.exe` (VS Code) se non
impostata — `FileBrowser` resta senza dipendenza diretta da `IConfiguration`, il comando viene
passato dai chiamanti (`Program.cs`, `RalfAIApp.SwitchFolder`).

## 17. "Puoi aggiornare l'help con un po' tutti i comandi e verificare che tutta la documentazione sia a posto?"

Due parti:
- **Help in-app**: `F1` (interattivo) ora elenca anche `Ctrl+C`/`Esc`/`Ctrl+X` (interruzione
  turno), prima mancanti. `/help` in modalità pipe non condivide più la tabella dei tasti
  funzione (inapplicabile lì) — mostra la tabella dei comandi testuali realmente disponibili
  (`/exe`, `/cancel`, `/voice`, `/speak`, `/clear`, `/switch-context`, `/switch-folder`,
  `/toggle-debug`, `/help`, `exit`). Nel farlo, trovato e corretto un bug: `/help` in pipe
  chiamava `Console.ReadKey` su stdin rediretto, che lancia `InvalidOperationException` —
  verificato con un test end-to-end (`echo "/help\nexit" | ralf`) che prima sarebbe andato in
  crash e ora non lo fa più.
- **Audit documentazione**: verificati tutti i link tra `README.md`/`USAGE.md`/
  `ARCHITECTURE.md`/`ROADMAP.md`/`PROMPTS.md`. Corretti tre anchor rotti (due in italiano
  dentro un `README.md` inglese, uno verso una sezione mai esistita in `ARCHITECTURE.md`), un
  link relativo a un file sorgente che risaliva di una cartella di troppo, e una voce di
  ROADMAP che citava una classe (`AgentContextFactory`) ormai rinominata (`ContextRegistry`).


---

### TOOL — FileSystem.WriteFile — 01:13:33

args: relativePath=PROMPTS.md, content=# Prompts

Log cronologico e numerato delle richieste che hanno guidato la realizzazione di RalfAI — la
stessa convenzione (`PROMPTS.md` alla radice, aggiornato a ogni obiettivo, prima di qualunque
altra azione) che i context `dotnet`/`html`/`markdown` ora applicano a ogni progetto che
generano. Vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
[USAGE.md](USAGE.md#log-dei-prompt-promptsmd).

> **Nota sulla provenienza.** Questo file è stato scritto da un assistente di coding (Claude
> Code) che lavorava su questo repo via git — non da `ralf` in esecuzione su se stesso. Le voci
> 1–8 riguardano funzionalità arrivate in sessioni precedenti a questa: la git history parte da
> un "Initial Commit" già completo (vedi [ROADMAP.md](ROADMAP.md)), quindi il testo esatto di
> quelle richieste non è disponibile — sono **ricostruite** a partire dal changelog in
> `ROADMAP.md` e marcate `[ricostruito]`. Le voci dalla 9 in poi sono **verbatim**, prese da
> questa conversazione.

## 1. Realizzare un agente di coding autonomo per .NET `[ricostruito]`

> Costruisci un agente CLI autonomo che, data una cartella e un obiettivo, scaffoldi un
> progetto .NET con `dotnet new`, scriva i file sorgente, compili con `dotnet build` e testi
> con `dotnet test`, correggendo finché non vanno a buon fine.

Realizzato: `RalfAIApp` (Spectre.Console) + `DotNetContext` come unico context, motore basato
su Microsoft Agent Framework contro Azure AI Foundry.

## 2. Rendere il context non hardcoded `[ricostruito]`

> Il context .NET è cablato direttamente in `Program.cs` — rendilo selezionabile, così in
> futuro si possono aggiungere altri stack senza toccare l'app.

Realizzato: `AgentContextFactory`/`ContextRegistry`, flag `--context`, chiave `AgentContext` in
config/env, `dotnet` come default (poi diventato `markdown`).

## 3. Aggiungere Speech-to-Text / Text-to-Speech `[ricostruito]`

> Aggiungi la possibilità di dettare i messaggi a voce e farsi leggere le risposte ad alta
> voce, usando Azure AI Speech.

Realizzato: `SpeechService` (motore condiviso), comandi `/voice`/`/speak` in console,
opzionale via sezione `AzureSpeech` in config — nessun impatto sul resto dell'app se assente.

## 4. Estrarre un motore condiviso dalla console `[ricostruito]`

> La logica dell'agente è mischiata con il rendering Spectre.Console — separala in un motore
> riusabile da altri frontend futuri.

Realizzato: `AgentSession` (poi rinominato `AgentEngine`) in `RalfAI.Core`, nessuna dipendenza
UI, eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice`.

## 5. Distribuire come global tool e rendere la cartella opzionale `[ricostruito]`

> Pacchettizza RalfAI come .NET global tool (`ralf`) installabile da riga di comando, e
> permetti di avviarlo senza specificare la cartella, con un picker.

Realizzato: `PackAsTool`/`ToolCommandName=ralf`, `FileBrowser` + `LastFolderStore` per il
picker e la memoria dell'ultima cartella, nuovi context `markdown` (default) e `html`.

## 6. Sessione interattiva a schermo intero `[ricostruito]`

> Sostituisci il prompt a riga singola con un'interfaccia a schermo intero, composizione
> multi-riga e scorciatoie da tastiera invece dei comandi testuali.

Realizzato: screen buffer alternato del terminale, `Ctrl+Enter` per inviare, tasti `F1`-`F10`
(help, dettatura, lettura, pulizia schermo, cambio context/cartella, toggle debug, quit).

## 7. Supportare più provider AI `[ricostruito]`

> Non vincolare RalfAI ad Azure AI Foundry — fai in modo che possa parlare anche con OpenAI
> nativo e Anthropic Claude, ciascuno tramite il proprio SDK ufficiale.

Realizzato: `AgentEngine.BuildChatClient`, switch su `AIProvider` (`foundry`/`openai`/
`anthropic`), sezioni di config dedicate per provider.

## 8. Scrivere la documentazione del progetto `[ricostruito]`

> Scrivi README, architettura, guida all'uso e roadmap per RalfAI.

Realizzato: `README.md`, `ARCHITECTURE.md`, `USE.md` (poi rinominato `USAGE.md`, voce 11),
`ROADMAP.md`.

---

## 9. "Il file USE riporta la app desktop che non c'è"

`USE.md` e `ROADMAP.md` descrivevano `RalfAI.Desktop`/`RalfAI.ChatUI` (WinForms + WebView2 /
Blazor Hybrid) come già implementati, ma quei progetti non esistevano né nella solution né sul
filesystem. Confermato scavando nel codice (nessun riferimento reale, `RalfAI.sln` non li
include) e riscritto `USE.md` da zero contro il comportamento effettivo (global tool `ralf`,
cartella opzionale, default context `markdown`, sessione a schermo intero, `.ralfai/` invece di
`.chat/`).

## 10. "Anche la roadmap è disallineata"

Stesso problema in `ROADMAP.md`: la sezione "Interfaccia Desktop" dichiarava il frontend
grafico come fatto. Sostituita con voci che riflettono lo stato reale (global tool, file
browser, sessione a schermo intero, motore `AgentEngine`, provider multipli); l'interfaccia
grafica spostata in "📋 Pianificato" come idea aperta, non decisa.

## 11. "Puoi fare un aggiornamento ai contesti? I files md stanno sempre in radice, non in docs. In generale sempre: README USAGE ARCHITECTURE ROADMAP. Puoi aggiungere un supporto alla manipolazione di immagini jpg e png"

Due richieste in una:
- Rimosso `docs/` dal layout dei context `dotnet`/`html`: documentazione sempre alla radice
  della cartella target, con il set fisso `README.md`/`USAGE.md`/`ARCHITECTURE.md`/
  `ROADMAP.md` — rinominato anche `USE.md` → `USAGE.md` in questo repo, per coerenza.
- Nuovo `ImagePlugin` (`SixLabors.ImageSharp`): `Image.GetInfo`/`Resize`/`Crop`/`Rotate`/
  `Flip`/`Grayscale`/`ConvertFormat`, tool engine-level sempre disponibili in ogni context,
  nessuna configurazione richiesta.

## 12. "Per un progetto puoi anche generare un logo?"

Chiarito che si trattava di generazione AI (non disegno programmatico via forme/testo).
Aggiunti `ImageGenerationService` e `ImageGenerationPlugin` (`Image.GenerateAI`): genera
un'immagine da un prompt testuale via DALL-E/gpt-image su Azure AI Foundry o OpenAI, salvata
come JPG/PNG. Opzionale via `ImageModelId` sotto la sezione del provider attivo; non
disponibile con `AIProvider` `anthropic` (nessuna API di generazione immagini).

## 13. "Inoltre, quando sta generando, puoi dare un dettaglio di quello che sta pensando scrivendo generando? Come un log?"

Nessun meccanismo nuovo: la visibilità delle tool-call già esistente (descrizione nello
spinner mentre il tool gira, righe `⏺`/`⎿` con `F8`/`--debug` attivo) copre già questo caso —
la `[Description]` di `Image.GenerateAI` è stata scritta per essere informativa in quel
contesto.

## 14. "Hai aggiornato anche il comando?"

Chiarito (dopo un giro di domanda) che si riferiva ai comandi `dotnet pack`/`dotnet tool
install` in `README.md`. Verificato con un pack di prova che non serve alcuna modifica:
`SixLabors.ImageSharp` viene incluso automaticamente come dipendenza transitiva via
`RalfAI.Core`, nessuna DLL nativa coinvolta.

## 15. "Puoi aggiungere anche un file md con i PROMPTS? che in qualche modo rispecchia la realizzazione intera del progetto stesso"

Aggiunto `PROMPTS.md` come quinto file nel set canonico di documentazione — l'unico non
opzionale: ogni context (`dotnet`, `html`, `markdown`) ora apre ogni turno con un passo "Log"
che ci scrive prima di qualunque altra azione, così la storia di un progetto generato resta
tracciabile fino alle richieste che l'ha guidata. Questo stesso file applica la convenzione
a ritroso a RalfAI.

## 16. "Aggiungi la possibilità di aprire un file durante la selezione: default code.exe, altrimenti variabile nel file di config"

Nuovo tasto `F4` in `FileBrowser` (usato sia al primo avvio senza cartella sulla riga di
comando, sia con `F7` durante una sessione): apre il file selezionato in un editor esterno via
`Process.Start(UseShellExecute: true)`, fire-and-forget. Comando risolto dalla nuova chiave di
config `Editor` (`RALFAI_Editor` per l'override via env), default `code.exe` (VS Code) se non
impostata — `FileBrowser` resta senza dipendenza diretta da `IConfiguration`, il comando viene
passato dai chiamanti (`Program.cs`, `RalfAIApp.SwitchFolder`).

## 17. "Puoi aggiornare l'help con un po' tutti i comandi e verificare che tutta la documentazione sia a posto?"

Due parti:
- **Help in-app**: `F1` (interattivo) ora elenca anche `Ctrl+C`/`Esc`/`Ctrl+X` (interruzione
  turno), prima mancanti. `/help` in modalità pipe non condivide più la tabella dei tasti
  funzione (inapplicabile lì) — mostra la tabella dei comandi testuali realmente disponibili
  (`/exe`, `/cancel`, `/voice`, `/speak`, `/clear`, `/switch-context`, `/switch-folder`,
  `/toggle-debug`, `/help`, `exit`). Nel farlo, trovato e corretto un bug: `/help` in pipe
  chiamava `Console.ReadKey` su stdin rediretto, che lancia `InvalidOperationException` —
  verificato con un test end-to-end (`echo "/help\nexit" | ralf`) che prima sarebbe andato in
  crash e ora non lo fa più.
- **Audit documentazione**: verificati tutti i link tra `README.md`/`USAGE.md`/
  `ARCHITECTURE.md`/`ROADMAP.md`/`PROMPTS.md`. Corretti tre anchor rotti (due in italiano
  dentro un `README.md` inglese, uno verso una sezione mai esistita in `ARCHITECTURE.md`), un
  link relativo a un file sorgente che risaliva di una cartella di troppo, e una voce di
  ROADMAP che citava una classe (`AgentContextFactory`) ormai rinominata (`ContextRegistry`).

## 18. "Vuoi creare il logo del progetto? Una immagine?" — "procedi"

Creazione di un logo moderno e minimale per RalfAI, in formato SVG sorgente e PNG per README,
con integrazione nel repository e aggiornamento della documentazione.

result: OK — written PROMPTS.md (9504 chars)

---

### TOOL — FileSystem.ListFiles — 01:13:37

args: subdirectory=
result: [DIR]  .git
[FILE] .git\COMMIT_EDITMSG
[FILE] .git\config
[FILE] .git\description
[FILE] .git\FETCH_HEAD
[FILE] .git\HEAD
[DIR]  .git\hooks
[FILE] .git\hooks\applypatch-msg.sample
[FILE] .git\hooks\commit-msg.sample
[FILE] .git\hooks\fsmonitor-watchman.sample
[FILE] .git\hooks\post-update.sample
[FILE] .git\hooks\pre-applypatch.sample
[FILE] .git\hooks\pre-commit.sample
[FILE] .git\hooks\pre-merge-commit.sample
[FILE] .git\hooks\pre-push.sample
[FILE] .git\hooks\pre-rebase.sample
[FILE] .git\hooks\pre-receive.sample
[FILE] .git\hooks\prepare-commit-msg.sample
[FILE] .git\hooks\push-to-checkout.sample
[FILE] .git\hooks\sendemail-validate.sample
[FILE] .git\hooks\update.sample
[FILE] .git\index
[DIR]  .git\info
[FILE] .git\info\exclude
[DIR]  .git\logs
[FILE] .git\logs\HEAD
[DIR]  .git\logs\refs
[DIR]  .git\logs\refs\heads
[FILE] .git\logs\refs\heads\main
[DIR]  .git\logs\refs\remotes
[DIR]  .git\logs\refs\remotes\origin
[FILE] .git\logs\refs\remotes\origin\HEAD
[FILE] .git\logs\refs\remotes\origin\main
[DIR]  .git\objects
[DIR]  .git\objects\00
[FILE] .git\objects\00\a7d8397992efbcf634e87d01dfc32e9cb640fa
[DIR]  .git\objects\01
[FILE] .git\objects\01\80566140c23d605344a2fcab628d4bf4d6aa6d
[FILE] .git\objects\01\942ffa38da10f0ac18c86d1a3d67c72c33d87f
[FILE] .git\objects\01\a5dd410815721833e32bcd277c899f5b49c930
[DIR]  .git\objects\02
[FILE] .git\objects\02\a585c4b871b74e9a312af3ff3d72d9658665b7
[FILE] .git\objects\02\bef14e3a66085de453961624b793edb0ffde19
[FILE] .git\objects\02\d068e02f2b3265c7fdde9feb60c97db8fcb95a
[DIR]  .git\objects\03
[FILE] .git\objects\03\2b5edf7848e97b05e55db6009220e87c2bd00b
[DIR]  .git\objects\04
[FILE] .git\objects\04\ebc45deda2180aeba4e1b4d24e666f79b5da84
[DIR]  .git\objects\05
[FILE] .git\objects\05\821e64afcaa1af395e4bdb1312fef04e942e59
[DIR]  .git\objects\06
[FILE] .git\objects\06\58406fa9786edfa226d9c327af2ae493e120d3
[FILE] .git\objects\06\790da1f900378190e8bcaeff3805b804ec9c2c
[DIR]  .git\objects\07
[FILE] .git\objects\07\08077a6404c84089e7c35fa1bb035bf25e5e4a
[FILE] .git\objects\07\dabdb1da7545c0da57fc5a31d662a3069a5e66
[DIR]  .git\objects\08
[FILE] .git\objects\08\34d8bbf0dde6093a90122d181bbb844b127f97
[FILE] .git\objects\08\392b119084760a0fad0b74ba5f3e1e026c4f0d
[FILE] .git\objects\08\9988d6485e02ab4a9a77e473f2fd585064aad7
[FILE] .git\objects\08\d2749b28e20939c707c4739f3892b4d3d041e0
[DIR]  .git\objects\09
[FILE] .git\objects\09\53a19a8510c768656dba65f7a9e7d656d5d83f
[FILE] .git\objects\09\b123e5460727bd6f9d8f7995144a52d14d26d1
[DIR]  .git\objects\0b
[FILE] .git\objects\0b\30ad4abd0066b4d8edae135043004f7e731995
[DIR]  .git\objects\0c
[FILE] .git\objects\0c\03cadeda6a20eb92b3a98d658d2117d0cda4e3
[FILE] .git\objects\0c\704be2f67452106e684ea698bb7e4172d5fe5c
[FILE] .git\objects\0c\d77bdf623d70ae8db67bce7b9bfecafb254c1c
[DIR]  .git\objects\0d
[FILE] .git\objects\0d\27cdfcc3cf83cb7046cc0cfdd621ad69b857c3
[FILE] .git\objects\0d\373fbe1452e774e2ea46eff8d049816b697074
[FILE] .git\objects\0d\4d30fc9d9dce7b3c94bf44f6d594abf497a0f6
[FILE] .git\objects\0d\986e7a4cd21e36e964149fdb723b3cbf6b7a26
[FILE] .git\objects\0d\ee5c57f8f04f4a1bef94ced84f40e938de65f2
[DIR]  .git\objects\0e
[FILE] .git\objects\0e\932f0e0215e1dfda5eba823770229ebada1e51
[FILE] .git\objects\0e\ea9296f1048410098c0df8e12f1ad4b5bd8cf8
[DIR]  .git\objects\0f
[FILE] .git\objects\0f\0d7cdd07c64a5730dadfed1ac0541cec60d635
[FILE] .git\objects\0f\5baa7894159c79273e84716c30f07930f79c58
[FILE] .git\objects\0f\5cdb3d0521f625979269ead851e30b8096ef92
[FILE] .git\objects\0f\b26103e2b0a0d95c9af40832f8e09009aae4c1
[DIR]  .git\objects\10
[FILE] .git\objects\10\238e81a702b0eff4f816a28eb076b73c8f4893
[FILE] .git\objects\10\b5885475c02543005bc748b4fa51203425ca1b
[DIR]  .git\objects\11
[FILE] .git\objects\11\599915337c158348a968b35474b08a4aa79a65
[FILE] .git\objects\11\a3845f76003de92412c9652b2283ab6f49ef1c
[FILE] .git\objects\11\c579ea7df3bc734a7824b30625a82ec1ae0e47
[DIR]  .git\objects\12
[FILE] .git\objects\12\115869f1db39f3cda988f9af7dba43555de966
[DIR]  .git\objects\13
[FILE] .git\objects\13\4dfb933b7d18a0716c5c90d3c9f7ec20aaf064
[FILE] .git\objects\13\6866bd8ac191321430fdcf9120b24b2db89f2a
[FILE] .git\objects\13\7c8609beddaa1cc3d29dac534a149e95a10118
[FILE] .git\objects\13\b1f9a7bd3ca3c486e7e50a1ccda1ca0d588333
[FILE] .git\objects\13\cb226edeb47921fbcdbb4c8fcbf656e48300e3
[DIR]  .git\objects\14
[FILE] .git\objects\14\67a1f85583da0083005f7c7ca83677930749b2
[FILE] .git\objects\14\6b1e72e88c68730d9a58ae2631d393aed7a4eb
[FILE] .git\objects\14\8edcf5bdaa2d5d62590e19063130a732586b49
[FILE] .git\objects\14\cfeb86204b3433362136bfe3236ec8135af0da
[DIR]  .git\objects\15
[FILE] .git\objects\15\6753e0d0349549d003e842e823f8c8094fdb31
[DIR]  .git\objects\16
[FILE] .git\objects\16\3022c6385aed3e7ca8bdd8f75733a0a0bdbce0
[FILE] .git\objects\16\5b5534d270dc42e95405b52b59b2da341b440e
[FILE] .git\objects\16\7f290fc8c6a79db637dd44d69fc54e20ae421b
[FILE] .git\objects\16\9274a4f2dd170b3a82b7a4d776f45786c86603
[FILE] .git\objects\16\b261e064bf20639fdc3607045cf0349aba4e3e
[FILE] .git\objects\16\f8e54b69cc70f708630f1d60b8f9e3e98a6ea7
[DIR]  .git\objects\17
[FILE] .git\objects\17\198025459795b1bc91b387141b30a5ed06877f
[FILE] .git\objects\17\5005054597f0cb131d3021565551f5f32ed2be
[DIR]  .git\objects\18
[FILE] .git\objects\18\2157b09a077b914b7dca76db46af674be45d1b
[FILE] .git\objects\18\6a6858991ac67b7d0da342fd563d35c7c556f3
[FILE] .git\objects\18\7d60c858d895a427f9f445056f01072f6454d3
[FILE] .git\objects\18\e70792bb0321e0c39f8a078b1211870cdabef0
[FILE] .git\objects\18\f95c9a9e235b64d19ea3bf9cef895cd926601a
[DIR]  .git\objects\1a
[FILE] .git\objects\1a\9d63bf85aeb75fc5d9dbe867fe2a15ef2e318f
[FILE] .git\objects\1a\f9380bbd3a1e66f2a244f34c98d5a1e4350f8e
[DIR]  .git\objects\1b
[FILE] .git\objects\1b\0de61675f59770f61db5b373ede861a732cc98
[FILE] .git\objects\1b\fb1b062aeffd3689332e939f47ae076aee9b07
[DIR]  .git\objects\1c
[FILE] .git\objects\1c\42be3c5b7b9a2da299ac5eef359ac3b9859242
[DIR]  .git\objects\1d
[FILE] .git\objects\1d\76485b2a81b434618c887004263b9a46fa157b
[FILE] .git\objects\1d\ae90b1e62f3e4bfd7b65fe9dce62b6684e7960
[DIR]  .git\objects\1e
[FILE] .git\objects\1e\3fb80b726dbac3436a9888d7db7eb33af39ce5
[FILE] .git\objects\1e\75105604345605533f0fc69931407c0845709e
[FILE] .git\objects\1e\b813dc19f03d213e6edd8b290f6ca5445050b2
[DIR]  .git\objects\1f
[FILE] .git\objects\1f\0c355efe596d4e42c9066baa0c663f02a81a02
[FILE] .git\objects\1f\1fc91ab9c629a1d214cf9b8e7902049f47994e
[FILE] .git\objects\1f\82976bdbb87a10e15b6a8edf8a36f79e83c2e2
[FILE] .git\objects\1f\9793de34fd3cb5b00500570f62f605e91e5292
[FILE] .git\objects\1f\b89abb746840452384cde74671ea59fece776f
[DIR]  .git\objects\20
[FILE] .git\objects\20\f2afeb1886a50a3d4e9b1379fb1403520f98c2
[DIR]  .git\objects\22
[FILE] .git\objects\22\2540b4bbbb488faa6db6b644ed014fea941ab0
[FILE] .git\objects\22\cb07afc08d1299f801472087ddd196aa897b28
[FILE] .git\objects\22\d2e0cbe64d23383be6c06efbc2e395cd693dc5
[FILE] .git\objects\22\e8a96c381182a885a33a94cd7f87f50794a6a4
[FILE] .git\objects\22\f86af44d08d1c3d46fcf2c5992437431d26ff0
[FILE] .git\objects\22\f91db60c89bb484f529d60a8cc9b7659b86741
[DIR]  .git\objects\23
[FILE] .git\objects\23\2bd09953b410a6b7f7bc1a52d1b8182304a7b0
[DIR]  .git\objects\24
[FILE] .git\objects\24\7ed197c5d1967149c76428cad33b3d65f74f04
[FILE] .git\objects\24\d98e0d6f4bb63adb2a77d9b3cfdf67354be250
[DIR]  .git\objects\25
[FILE] .git\objects\25\1e9d59a57ddd701fe2b8222442a09593f2f775
[FILE] .git\objects\25\3db945de6e308fc19f88cc8942c3b7f5d6015f
[DIR]  .git\objects\26
[FILE] .git\objects\26\bf58f6dc890d272f48cd7d80fadb7caa8ae624
[FILE] .git\objects\26\f4a101e39bcea6423878467125349122de49f5
[FILE] .git\objects\26\f8e3c999f4786ff0e9b036d283effd19bebcd5
[DIR]  .git\objects\27
[FILE] .git\objects\27\4251ae2c3a8777db0c448a4b1a288824fee425
[FILE] .git\objects\27\ba88bdc95ff1071530b1ede2dccd8a548c515b
[FILE] .git\objects\27\e4551a00c9787e269e724c4024954f9ac3694f
[DIR]  .git\objects\28
[FILE] .git\objects\28\3fe3cbcc311e83d16b495f20712a8e5686829f
[FILE] .git\objects\28\bae31747cfc6ce2912fe9144b1cc8550a82f20
[FILE] .git\objects\28\c2f0d9e9a8a3ddd95e15cd44f6ebfb414428c2
[FILE] .git\objects\28\e66a3eb40d6279273a19b84e4ae01774b0eee3
[DIR]  .git\objects\29
[FILE] .git\objects\29\af417cd291e85c2ab13f264976863d24cdfff3
[DIR]  .git\objects\2a
[FILE] .git\objects\2a\90aa5979c689cd3e179688fb3367c37d7fb031
[FILE] .git\objects\2a\bf9ec5aad296151f24c2c9ef16f1b6202ae080
[DIR]  .git\objects\2b
[FILE] .git\objects\2b\9fb215b4fb01e5cb24b8a3b4f590c9049f5125
[DIR]  .git\objects\2c
[FILE] .git\objects\2c\5d8d922b7ce21a9d4f5de0df2919d46c44b5ef
[FILE] .git\objects\2c\f9c59de9db56d4a25b089d8c62dfffca9f63b0
[DIR]  .git\objects\2d
[FILE] .git\objects\2d\8618fdeed507655a5d3f8519ade5aa06467e25
[DIR]  .git\objects\2e
[FILE] .git\objects\2e\675594aa5b92a642ad8e155ca6579264e493e0
[FILE] .git\objects\2e\e7fa0be115cbb44b30733d86dfef40941b8ac5
[DIR]  .git\objects\2f
[FILE] .git\objects\2f\67189ea5b622853a9cebfbff760f14a85cb3f3
[FILE] .git\objects\2f\ae0065177d04b41ae29a2dbc32c78177e5cc49
[FILE] .git\objects\2f\fdc45b237ddc01c9fc10fddc4e70adc22e3c7b
[DIR]  .git\objects\30
[FILE] .git\objects\30\43f180a535a4cf065642550f5f1025fa52999e
[FILE] .git\objects\30\971f3aa8abd5ccd70cf192e3b48e6fde8466ef
[FILE] .git\objects\30\a4fc8b14dcf252e193eddfc33a79caabd24e56
[FILE] .git\objects\30\cfb60691739379e8b7cf42ecac373db6d37f77
[FILE] .git\objects\30\e985f6d60235d09fc622eff9ad378f1932d2a8
[DIR]  .git\objects\31
[FILE] .git\objects\31\b505b021b2a071d4e8bb920633f52be518851e
[FILE] .git\objects\31\d922ecfa392cbe808d4beadb88100a8dda461b
[DIR]  .git\objects\32
[FILE] .git\objects\32\60a0471d27fee22d76c2689e371033e60e7cd0
[FILE] .git\objects\32\6c2077a8038f1004b283aee2c02e8e18f3b9a6
[FILE] .git\objects\32\fc42eb147fc1129bb394913c5211701c25bde8
[DIR]  .git\objects\33
[FILE] .git\objects\33\8c45be2120a2b3032012014bf6a5f6808343a8
[FILE] .git\objects\33\cd5128e75576c930dd0ef9e72685893f64a609
[FILE] .git\objects\33\f7ce667c90f5d38588114abb4d2aef30b66863
[DIR]  .git\objects\34
[FILE] .git\objects\34\01d6a6bdf3110055994a8c9f93aa693572b9bf
[FILE] .git\objects\34\0bb4c52cd171baa59caf6719cebe5b08c2ca39
[FILE] .git\objects\34\a58cdefd2d328a60b9fd87f2c301fe61b5e0e0
[FILE] .git\objects\34\c53b34062aeeb7a773c2e615ff383002b8e200
[DIR]  .git\objects\35
[FILE] .git\objects\35\5a1b4e12e77c34407d1889e5bb2c66b1667498
[DIR]  .git\objects\36
[FILE] .git\objects\36\05010644bc9326299e4da3637f8f9d0714fc98
[FILE] .git\objects\36\4f3cabe4d693e529976056eb004460f4ef3485
[FILE] .git\objects\36\72dcc8b5d2566709ff2b6a409f0f7b280101b5
[FILE] .git\objects\36\b2d26b0907f840ef6ac8572dc69965d7ad7fc9
[FILE] .git\objects\36\ffd2d3bf0d96126c8254931565ce97b0ed020a
[DIR]  .git\objects\37
[FILE] .git\objects\37\220554acc6a5a9bee42fc12fc1d0256cd2661d
[FILE] .git\objects\37\7fd5516fc91ec4d873573e58ab70b43a78ff18
[FILE] .git\objects\37\8dcc567acde97e523e79440b221d1a43d4e87c
[FILE] .git\objects\37\d51debeffa884d2a23dbcfbfb65b7001c7cc41
[DIR]  .git\objects\38
[FILE] .git\objects\38\bf348ad9325ca9c7d12f31896591a5b44b38f1
[DIR]  .git\objects\39
[FILE] .git\objects\39\3a38d280f4b1f0a623c3d8530273dc24da9413
[FILE] .git\objects\39\b6944d44c8d28f5ba0cecc906015fef28a5d15
[DIR]  .git\objects\3a
[FILE] .git\objects\3a\71cf76a49f452f599d6608df89d571892c5634
[FILE] .git\objects\3a\efd93eeb5755e32c71bec5a23ba3f7d8760803
[DIR]  .git\objects\3b
[FILE] .git\objects\3b\57d4514dea568a0ddb5c802b5a191f86bca1b0
[FILE] .git\objects\3b\87fe9db54a3f00c788eb185a46f828a0965693
[DIR]  .git\objects\3c
[FILE] .git\objects\3c\07e93df58fb3d5021662c6bd827a5539f180b9
[FILE] .git\objects\3c\40ffe699c4101a1d3585d28d62c34f21e5166c
[FILE] .git\objects\3c\69dc9b2cde1b40f6c00b2947fc37d38f03adea
[FILE] .git\objects\3c\ada970db36aec96cb21fad556bfaf104d6345e
[FILE] .git\objects\3c\e9bce60120746a9538106c31889d5459ae70e4
[DIR]  .git\objects\3d
[FILE] .git\objects\3d\08fb0dba2593dca8588551bd1dc48aa6c221ef
[FILE] .git\objects\3d\40c879cf3a82a731027f9acc7ff70800b59eeb
[FILE] .git\objects\3d\680f33317a5fe62b5824e9507ea2f3c9a714fb
[FILE] .git\objects\3d\e766d8d260eb2fd3adedcc8b0f322ea8b30251
[DIR]  .git\objects\3e
[FILE] .git\objects\3e\3c0a2796b6b5b84dde124ebfeabd59463f16fc
[DIR]  .git\objects\3f
[FILE] .git\objects\3f\676188793036df11273344c8e3fceca078932a
[FILE] .git\objects\3f\beec46a0cf9b09a9a4de1f62bb266529295a89
[FILE] .git\objects\3f\daba1a99e559f0d205a0d29a7e3884d20bcb1a
[DIR]  .git\objects\40
[FILE] .git\objects\40\23579cb4208ea9c5af78b353b7b9df6cf83052
[DIR]  .git\objects\41
[FILE] .git\objects\41\15de6fc0d301c8222db568d5528b6cad8c5480
[FILE] .git\objects\41\723efbaa1ab3dfeab1d1acb91c3e6e9c187ef7
[FILE] .git\objects\41\749091d74daab748a11dae42cf1b607fda49a2
[DIR]  .git\objects\42
[FILE] .git\objects\42\40b6c5f719dc9eda7e4c061a18543a35f6c36e
[FILE] .git\objects\42\894fd3c2ef18a66c7af6450952c53730031af6
[FILE] .git\objects\42\e634a3ddb3e5b3f91970ac56dc256309d1e418
[DIR]  .git\objects\43
[FILE] .git\objects\43\be6fa89388b41754541a1c1b11a4e9b4232c85
[DIR]  .git\objects\44
[FILE] .git\objects\44\234c573e9f43e9467d36364ffe3052ed36f01f
[FILE] .git\objects\44\79e146efceb629c425c336566e1bcff7d8b332
[FILE] .git\objects\44\abcc7f4ea9c77c4173e81c946a630fbf309033
[FILE] .git\objects\44\ba230c1740b2487b27d2b61bb7b806a6e69e71
[FILE] .git\objects\44\d5121ab4e4c76a3f3417201d091c6d358edb36
[DIR]  .git\objects\45
[FILE] .git\objects\45\95019f9cc3a874d83ff8428ab35c0defc8b103
[DIR]  .git\objects\46
[FILE] .git\objects\46\0a1d25545ed3b46aa0fd53e5bde16fb3586426
[FILE] .git\objects\46\36b2ed6fd632e9f8ddd349908849e785df3a15
[FILE] .git\objects\46\5debbeb7c787ed193b061abcbfc5a1bc916cd9
[DIR]  .git\objects\47
[FILE] .git\objects\47\0cdf185256451343b1c565cce5d9537e1fe872
[FILE] .git\objects\47\284a188136294398dd58989bddf3b6e41803df
[FILE] .git\objects\47\b7785194fb7bc9e31f79f6b11da6eb66c0bd74
[FILE] .git\objects\47\c2c9e14cd6272d8f1a354650f39951ca50c018
[DIR]  .git\objects\48
[FILE] .git\objects\48\7f984c2ee2e9fe1e9c54bfd72d4d9697e6be53
[FILE] .git\objects\48\aa9d56c6c89d397fcebf57d36e7a7bd0cb8fd5
[DIR]  .git\objects\4a
[FILE] .git\objects\4a\1421531720342d8213eb260ddcc5f754295a59
[FILE] .git\objects\4a\1be9976979df281dca648eca975c85cdfc2689
[FILE] .git\objects\4a\9a364ca2c000a670182ed29d7de2a79be31f01
[FILE] .git\objects\4a\9e60015fe73606ec758a82541b57dd59cc539f
[FILE] .git\objects\4a\ba1a20c06067e0b9b237c29492a46970915a27
[DIR]  .git\objects\4c
[FILE] .git\objects\4c\1a65a8e1d90fa0c0c22519fc2d63a7b0c87247
[FILE] .git\objects\4c\1c28fa040c06dc26ef4620a598382f29671551
[FILE] .git\objects\4c\5138fecf8de92b51e0328ee0fe04b78eac3610
[FILE] .git\objects\4c\58d21de9cb1f2a8fe3f8eee8dae577df5163ed
[FILE] .git\objects\4c\d323be60bfb4d9eafa01c73bd6867053ed7bc2
[FILE] .git\objects\4c\fa90824e16bdfa908489ec7b323e2aee83f7c8
[DIR]  .git\objects\4d
[FILE] .git\objects\4d\96c6b91a55ce80fbb89a2adb1f765f4c16f809
[FILE] .git\objects\4d\b9d78099110fbfa84d7a695aef4e51b8b84467
[FILE] .git\objects\4d\d9260bc15728ed7679874f270203b969c48b58
[DIR]  .git\objects\4e
[FILE] .git\objects\4e\4a7d72e264887d2c380390be0cc981d6eacbb2
[FILE] .git\objects\4e\4f72308a75221ade6871636909621f4c2ba98d
[FILE] .git\objects\4e\6fd679d8f35c6085fc190a86399aa6cf8faf04
[FILE] .git\objects\4e\9f2e1a1f76c246899b0bea4d26b6947087811e
[DIR]  .git\objects\4f
[FILE] .git\objects\4f\c8df5d83f5dfdd2dd46466b4a17f63e8b9229b
[FILE] .git\objects\4f\fc0d6463f210381c727c3d7705463817d3eb69
[DIR]  .git\objects\50
[FILE] .git\objects\50\1b69a13bbaa8794288a85d963c27f8f1319222
[FILE] .git\objects\50\293a40da9bb31faddeb496e35053753e9deec1
[FILE] .git\objects\50\cd65021ae9a2f4c80e691002ff7b25f2e88da9
[DIR]  .git\objects\51
[FILE] .git\objects\51\965b937607cea44cc6b2077ad6cb20a79d261d
[FILE] .git\objects\51\fb32890165f9012781ee0e116a0e6903a89e80
[DIR]  .git\objects\52
[FILE] .git\objects\52\24789f459539d98ad60b13d37d64fd9e6b30c5
[FILE] .git\objects\52\7a960ae25bf8b8321c343e533afab66ec37f34
[FILE] .git\objects\52\f15b3152e9b92fca3da2e2bc424a909f2f889b
[DIR]  .git\objects\53
[FILE] .git\objects\53\01756c7ccd6d435ee096d29aa2795a964d9d54
[DIR]  .git\objects\54
[FILE] .git\objects\54\8739516685a87597dd7c6bf957de33a23b4eec
[FILE] .git\objects\54\c94bb1cc64ec1c6c007063958747fa5971ac4f
[DIR]  .git\objects\56
[FILE] .git\objects\56\06a404d6c495144c8f7681c6112e79737b1b91
[FILE] .git\objects\56\acebb40e71eacdbbdb65468a275e473c586d11
[DIR]  .git\objects\57
[FILE] .git\objects\57\8526194f9f96ab178e3376cf35c2e6a1208b30
[FILE] .git\objects\57\bf7b0bfd6b5525cdcfd67532e97e261df8c322
[DIR]  .git\objects\58
[FILE] .git\objects\58\310c1cadc01362dadc7bc43e1928655a695a45
[DIR]  .git\objects\59
[FILE] .git\objects\59\0e5342ad0cc7876414b95894f653cd08de12c7
[FILE] .git\objects\59\9c5a1c85f950740435d626b6731c60946e9b2c
[DIR]  .git\objects\5a
[FILE] .git\objects\5a\b9ee3774804cbc0bdc39c798f532f06d2f7c0b
[FILE] .git\objects\5a\f9a522d77a209b5039ffb9769a10e9a61f4a4e
[DIR]  .git\objects\5c
[FILE] .git\objects\5c\7b8cc1cf317d676404bcbd5ccf8fa9f500fa11
[DIR]  .git\objects\5e
[FILE] .git\objects\5e\8653ae7ced7ea1fe2bf3a75c7df5476900628f
[FILE] .git\objects\5e\913a1e5c7eca7896c8dbb425545c683dd8054c
[DIR]  .git\objects\5f
[FILE] .git\objects\5f\2d1b4b6786e8a6e3c0718681ebb3febcc83fef
[FILE] .git\objects\5f\8591e4985d0fe4f6d8fd00221dc43b4a9fe123
[FILE] .git\objects\5f\a44499c63711ab257faca59b0f33406356e291
[FILE] .git\objects\5f\c61056552c70bcf08791771601bfffd0242e5e
[FILE] .git\objects\5f\cc98f9cdb67e4952543e36f356d2f41509d9a1
[FILE] .git\objects\5f\e64834b956e31388acd1e1316c48d59b5f6db1
[DIR]  .git\objects\60
[FILE] .git\objects\60\308aacb109772e56fce0633c4358c2a45c078e
[FILE] .git\objects\60\61d68c04a579b8b5ca18e71403e4d6345135c0
[FILE] .git\objects\60\9730b3764e25297aad646485aa25fe287b53c5
[DIR]  .git\objects\61
[FILE] .git\objects\61\2683b867376413fc487d68076e043396ab7bb5
[FILE] .git\objects\61\7a4b4fd5e2c201125b6b1bd57355bb48217d85
[DIR]  .git\objects\62
[FILE] .git\objects\62\a155b738ad6718365894ce55306855acc27ce2
[DIR]  .git\objects\63
[FILE] .git\objects\63\26ee581aece13b7552a154d7dd335631868937
[FILE] .git\objects\63\5ad456fe8a2496cbb0cadb907d4d0ab72d009b
[FILE] .git\objects\63\b336e381b874228fd9006e56eda8fdf976a0bc
[FILE] .git\objects\63\b96b7577b622588c5f9c950e0bd099dc2c954a
[FILE] .git\objects\63\ca056ec607e7bdd0a0ea8984cffbe26a57ee15
[DIR]  .git\objects\64
[FILE] .git\objects\64\be4fa393eaeb1b63c9c18510738c7fd0477268
[FILE] .git\objects\64\dabcf6f51f3cb7ec904767e364122bf0c5f41a
[DIR]  .git\objects\65
[FILE] .git\objects\65\2a12b36294e164521c07e7c2881db0d9a7cb1c
[FILE] .git\objects\65\f442c10e22ff59cfbce78050bb93fc49beee9c
[DIR]  .git\objects\66
[FILE] .git\objects\66\32aa68d51700f4f6806da8ccf852542e1edd6d
[FILE] .git\objects\66\ce53bdec854c732b25f82b1e00866965303e12
[FILE] .git\objects\66\d030b4f98d4f4248521db0878202711ac9cc9d
[DIR]  .git\objects\67
[FILE] .git\objects\67\50fe27304b63403138b3cdb9d4d241504b842e
[FILE] .git\objects\67\8c628537f5140e1cdd61862bf7ba693c54d252
[DIR]  .git\objects\68
[FILE] .git\objects\68\af9727ec2775eaa4c0f7545fa91d848cb58a43
[FILE] .git\objects\68\c33ebd01c8a46648283925960be5a81ae1cfc9
[FILE] .git\objects\68\c4a8866b1181779305404066e77c99e6622bd8
[FILE] .git\objects\68\ec026d03d72e1d359a56fd4407f3cbdc0ca131
[DIR]  .git\objects\69
[FILE] .git\objects\69\4a3ef119bcbf9303cb64b73fa3468707ec3063
[FILE] .git\objects\69\dda9b82e9892f5534cdfd21557cfd0850bf436
[FILE] .git\objects\69\e8c64e3f8a2d31762e687e696e870c3a732bda
[DIR]  .git\objects\6a
[FILE] .git\objects\6a\1d3a22d23bb4376c6659e5552743d06f0c6e6b
[FILE] .git\objects\6a\7bd07b1d1c08a93d381f5f9d26acc49f741f46
[FILE] .git\objects\6a\a58f7ef955f34d2f5a1c49accc5d1b2498b408
[FILE] .git\objects\6a\a8df7b6789c566d59a28f4e9e01e2c17c7a753
[DIR]  .git\objects\6b
[FILE] .git\objects\6b\2bdd95a80a763ffab873e1140b78269e48ca84
[FILE] .git\objects\6b\727c61d7b7d37d70a2a47815d247c8bef10862
[DIR]  .git\objects\6c
[FILE] .git\objects\6c\aeafcf2089ae756066fd25cdfbd24897ad983c
[FILE] .git\objects\6c\dd7e6652b776b29c18427ad31f8a4a990cf5c9
[DIR]  .git\objects\6e
[FILE] .git\objects\6e\ff9fa076f4caa838d758d9318fb0f9a7dbea55
[DIR]  .git\objects\6f
[FILE] .git\objects\6f\47d52ef719e0d3501c71b98b7f271734a1e48f
[FILE] .git\objects\6f\be286c79f7d77cb0ea5cd21b5e9b7c810bfa20
[FILE] .git\objects\6f\cbb8541ae5b7769c8f966a7a375c881714d7ec
[DIR]  .git\objects\70
[FILE] .git\objects\70\159e5037d4e4592396a040ae8685a842442ade
[FILE] .git\objects\70\163d9e8ed44b64cd056df05c8e472ee9aa61ad
[FILE] .git\objects\70\eb267927c66118c6a6a0f5b13475f4118c056a
[FILE] .git\objects\70\f376b87427ceaf18178f6835f9de57f88f6721
[DIR]  .git\objects\71
[FILE] .git\objects\71\6e6629e117cee74de14e2c292106b9a6f4a776
[FILE] .git\objects\71\868b044fc77b2af304aa9ae98a30282758ff7e
[FILE] .git\objects\71\f0f192f4ac5df028d44ade30a2fa3a506205db
[DIR]  .git\objects\72
[FILE] .git\objects\72\9d157de50d71f3302817f38e86d21374217918
[FILE] .git\objects\72\b013699d8bbc531148a88917a37a1f987a82a6
[DIR]  .git\objects\73
[FILE] .git\objects\73\2451d9e64ab857e2197b8188da9e62424fc7f0
[FILE] .git\objects\73\5bb7d2a07f834c3bb018286469e906704d1b1c
[FILE] .git\objects\73\72f53239321b428d4571f5c5e6b34572e24192
[FILE] .git\objects\73\b16c60f7af49f0cf23d2eab49a50a330556265
[DIR]  .git\objects\74
[FILE] .git\objects\74\2c7d414fc33e83057d3590b097fc38c3135829
[FILE] .git\objects\74\682835b8d424ff1d6a13dbb7720955408bddad
[FILE] .git\objects\74\bff31f38d347bfb05ba8c518aa492e8afafd50
[FILE] .git\objects\74\c358f4697c60a836de85ec5e50fc665d01ea48
[DIR]  .git\objects\75
[FILE] .git\objects\75\b690ed6d86aba78b62b6ed1a3a173cc12bf994
[FILE] .git\objects\75\da2e46e567ed1c2b0ab7c3a8383b82860b722a
[DIR]  .git\objects\76
[FILE] .git\objects\76\706f4c62ba48a519ddea76239a005a233f4959
[FILE] .git\objects\76\ab6850adb5afbc59c050e9c3da20aa17408d49
[FILE] .git\objects\76\ce93bf408d05b902eb7b52eb4e987b16fcc4e6
[DIR]  .git\objects\77
[FILE] .git\objects\77\01b4627ed6e6f3d6de48703d8e67f9a3e0b846
[FILE] .git\objects\77\54bebf893d1ffd6e9d9628475cb7c7920ff791
[FILE] .git\objects\77\883e8ead496a8913990f070f30de1985847014
[FILE] .git\objects\77\ac64c03f699d2a2ac1fc09dff07ea810a8a470
[FILE] .git\objects\77\e1d6ccb12f358378958c76f77807d16d3da8a7
[FILE] .git\objects\77\fcf78c56104ddf3ab85cf327ffe352b1f25bd9
[DIR]  .git\objects\78
[FILE] .git\objects\78\3afa12f10c2f1924e310c9f48859bf6e851351
[FILE] .git\objects\78\4f1cc0e33e857cdbbddfe68b7bdbc3eed83f93
[FILE] .git\objects\78\617ec7dc46671c2995d987c4c23ff8f0317ad8
[FILE] .git\objects\78\7f44c4345db3fef3cd0924276f3bcc08d6dad5
[DIR]  .git\objects\7a
[FILE] .git\objects\7a\0c1679b48b660ece93f3313a861f2efa0174d7
[FILE] .git\objects\7a\a30b439398e8243e8c4a43a0af1816e7a7dda1
[FILE] .git\objects\7a\b855016d504355a4c2bfa4ec0f6543e7134867
[DIR]  .git\objects\7b
[FILE] .git\objects\7b\5ceadcffef5f4503b7eb0adf7fa7a31ea1813b
[FILE] .git\objects\7b\eeff2edc2b5d1f7783cafcd7c2ab67f55a157b
[DIR]  .git\objects\7c
[FILE] .git\objects\7c\5d531054776431bf8203a0f36f4f48a3e10f80
[FILE] .git\objects\7c\5dce5df084dbf14cc4c04bcd5a8a6301c8eba4
[FILE] .git\objects\7c\c42edd77330efee12c0e44f027eeadd3f62ec5
[FILE] .git\objects\7c\c5d6837ce16b47ab6f80ecf019acbb9be6cff2
[FILE] .git\objects\7c\e84d982be18126189f3fcc16519fd15baf02c6
[DIR]  .git\objects\7d
[FILE] .git\objects\7d\33eecb4b8f902741f9ddff5c168601aae51fe5
[FILE] .git\objects\7d\7fca63f81e72fb023a0ec57b8dfff6f93f4d81
[FILE] .git\objects\7d\ae196fa08f920de0f08fa9c114d6b32f4006e5
[FILE] .git\objects\7d\ae3507a4333c11909514125008eb6ea970876b
[FILE] .git\objects\7d\c470492d10c4af03a198b8cc580f86babe01d5
[FILE] .git\objects\7d\e3aab9ae894c626e0982a587b522fd506e9ad1
[DIR]  .git\objects\7e
[FILE] .git\objects\7e\01ecad0b51a31256bafd078c1e5478c5f547d5
[DIR]  .git\objects\7f
[FILE] .git\objects\7f\4fdf6f648202593859f09729fe7e5ccd82b662
[FILE] .git\objects\7f\ca23b88c0fd8a67c49b0c0bc80a6b384020b0f
[DIR]  .git\objects\80
[FILE] .git\objects\80\31d7aff735d3a23b2e79ffd874e33f79d8e292
[FILE] .git\objects\80\78b7405e98195d1529c0401337eb0da41fe4f3
[FILE] .git\objects\80\865953c57cc605332c035e0ef273da82e61a18
[FILE] .git\objects\80\b6e6c344b1785f6811635bb2b2605da3cd6c5d
[DIR]  .git\objects\81
[FILE] .git\objects\81\ce48f43e88d523a7b736455536823a75403341
[FILE] .git\objects\81\eb4aae1db09d07c8a7bb5252e878bb2bb951a8
[DIR]  .git\objects\82
[FILE] .git\objects\82\0610a9aaf6eb807debf384b972cb875f1d84e7
[FILE] .git\objects\82\6d061181cdb6366c98e4f51f77e6a2fab5718b
[FILE] .git\objects\82\ce2e779f5887636245e19fb2b1a911191b9688
[FILE] .git\objects\82\fb70a205592b938e40528653b233bc83563db9
[DIR]  .git\objects\83
[FILE] .git\objects\83\39f27eeae261fd51d09b3f624c6c5a2e8f5338
[FILE] .git\objects\83\84c77702572d6d879912a5d76a8184937dc1eb
[FILE] .git\objects\83\e6db736fd2d181f3ebfda0bc7b760a90ad95b6
[DIR]  .git\objects\84
[FILE] .git\objects\84\46b97321ef29d4e4644cab549914b4c4507615
[DIR]  .git\objects\85
[FILE] .git\objects\85\55ad948ae3b7889648610b3e736a3f8467c033
[DIR]  .git\objects\86
[FILE] .git\objects\86\8b33e4b665f9c82ff61f1069659cd5849ba63a
[DIR]  .git\objects\87
[FILE] .git\objects\87\1c7cd4fc1d2d5f06ef0c36ea0b815583a6c467
[FILE] .git\objects\87\2260e0328000392323d41f4a2054e5033e5b0f
[FILE] .git\objects\87\4284807367974309762eb5ccbe076283d52683
[DIR]  .git\objects\89
[FILE] .git\objects\89\2d1a4b4b8ac09552b20b27c195c095d326b868
[FILE] .git\objects\89\5508dd0e35fc92660564ba06ac7d12e0ad2149
[FILE] .git\objects\89\a678b2c09696fd316c6d95103b2d7f763595b0
[FILE] .git\objects\89\aefc7d0486519f3d5a4a969dafef2e765f6234
[DIR]  .git\objects\8a
[FILE] .git\objects\8a\505ce81ffc211d4ae0b1d695b18dd783449db1
[FILE] .git\objects\8a\865a3180f4ee60706692d92cd3bad7fa16f5ea
[FILE] .git\objects\8a\9e0edeb475ebbe24433d5e27244f7626db42ad
[FILE] .git\objects\8a\c6728e3ce412af7218559c7b9bf00c650b2c78
[FILE] .git\objects\8a\fb289410e8f5bb6b5271b20771055c6246f146
[DIR]  .git\objects\8b
[FILE] .git\objects\8b\261107bd17c6d2ed584020bcf7fecc019c5a8e
[FILE] .git\objects\8b\e5dd26ccdd5fcd0fdf468698eb01e868f9fee4
[DIR]  .git\objects\8c
[FILE] .git\objects\8c\5765e54790df5a693dd8e3c8e89fa87a5170be
[FILE] .git\objects\8c\7d5fd8ca78229a7a0084261a97490ed0e580f0
[DIR]  .git\objects\8d
[FILE] .git\objects\8d\24fb8a70162607d5fd5da7e657c3e112d0cd47
[FILE] .git\objects\8d\43a327163ec2a4ee422c67c97b5af8428e1ecb
[FILE] .git\objects\8d\95ca3a40b11cb3d5333479b7125ccf863b94ed
[DIR]  .git\objects\8e
[FILE] .git\objects\8e\456f5dcf86d521c5979de3ca368fd58aa66840
[FILE] .git\objects\8e\940e3b83dec452266b938a7606380290649bdf
[DIR]  .git\objects\8f
[FILE] .git\objects\8f\5c082fadc372d74eec664c7d0ce9d00bae09e7
[FILE] .git\objects\8f\d47c6ff23141cc2430ffb6709d1a4363de6e33
[FILE] .git\objects\8f\f6bc2a79f5e2e194ae35921fc00b0a8d97923e
[DIR]  .git\objects\90
[FILE] .git\objects\90\819b2c1f747445464f25fc51b4932403ee039c
[DIR]  .git\objects\91
[FILE] .git\objects\91\02d6008844e95e32692145fb072028e8d9790f
[FILE] .git\objects\91\05c81afdaa0c489581443bf182e29dfc3916f3
[FILE] .git\objects\91\413a8695ae40cb0ee9e0be6a81df70499d54bf
[FILE] .git\objects\91\f47b576450b3e60626e6d2ab7e3f1e9216288b
[DIR]  .git\objects\92
[FILE] .git\objects\92\5368cdb6c1be2eb6026ca0fe2c9fc7b8366111
[FILE] .git\objects\92\5b1351bcda9f1244c4634843cb5f994a6b5008
[FILE] .git\objects\92\811937edda77d453960cff1bb00c5e539c455b
[FILE] .git\objects\92\8159782b41505a325843ccb05fffefa61e6042
[FILE] .git\objects\92\a9babe89671a2d044fd70d48ce9821a953270e
[FILE] .git\objects\92\adbb9154e02650e621c16eceed5d7262bbe9d2
[FILE] .git\objects\92\b9dc15f72dd0d68315bc3d67460d646dec4616
[FILE] .git\objects\92\f54154554df3219aa32cf9c1efc6bfef96ab5b
[DIR]  .git\objects\93
[FILE] .git\objects\93\1cb30c732b4031a55d5c3dc78d470e25a6b52e
[FILE] .git\objects\93\563503eeeec84ec92d7681abf39a6318803d6d
[FILE] .git\objects\93\9a5cc7d1882dd6b6aacad26d8b74c457e621c9
[FILE] .git\objects\93\a9962c3ef98ab33d09031e3633a07a53ef0df1
[FILE] .git\objects\93\b6d9ee41eea8efb3ae1c6994296ea2d4cc7e6b
[DIR]  .git\objects\94
[FILE] .git\objects\94\07b67bd1636434f4db9a3dd4a12018d4f011ec
[FILE] .git\objects\94\32143b6e7590dbeab3d106bc31cba7fafbebf5
[FILE] .git\objects\94\54b936701286d5c0b474fff5a83c4d3d8eb842
[FILE] .git\objects\94\9f753174ce1f7cf83fe90163f84b8781c52991
[DIR]  .git\objects\95
[FILE] .git\objects\95\3de6e9bc9ef830b4be1607b4bc670b82749e83
[FILE] .git\objects\95\ba3fb97f56e76e5fc6f6e528d541ac3edfe38c
[DIR]  .git\objects\96
[FILE] .git\objects\96\184bf8b9d0578a4bf30396d3eb5fc21cb63cc8
[FILE] .git\objects\96\5a145c867b5c247665edf6aa247181f6eca3b8
[FILE] .git\objects\96\f0bddfb973f1ea737beb9a877dfc9264030edd
[DIR]  .git\objects\97
[FILE] .git\objects\97\40ce7aaa1fabba613d00daa8620e317d5fa374
[FILE] .git\objects\97\5db2b944d33d2335d5bf62cf2f6692a2e295b9
[FILE] .git\objects\97\703f6745a09a6e442f00c9d20751e532304167
[DIR]  .git\objects\98
[FILE] .git\objects\98\e4a228867845ee27064225dbc60e38994c1e46
[DIR]  .git\objects\99
[FILE] .git\objects\99\25120fc92ad5bb20cff2d7c4572df2d74016b4
[FILE] .git\objects\99\321932f86bcd4ab01eb6a010cb77edbc035b18
[FILE] .git\objects\99\4b317d30ad635f33665188fa92cd9b012448cb
[DIR]  .git\objects\9a
[FILE] .git\objects\9a\437940d0fbb731f518d000a9efef362ea481f5
[FILE] .git\objects\9a\76d34f416f9541d974f736f97b87096423286a
[FILE] .git\objects\9a\f1ccebf554e8240b9a04bb0f50d89a2201d08a
[DIR]  .git\objects\9b
[FILE] .git\objects\9b\1ec403d2c49b8ae19ee4da8d69441bddfb01d9
[FILE] .git\objects\9b\baa51cf47a1ebe9f3bc27e9e0fea81eef0d59e
[FILE] .git\objects\9b\cb354367f75ed243366b2160a9a2c09fa36d40
[DIR]  .git\objects\9c
[FILE] .git\objects\9c\0ff8d0a5d204127691fd443af761bb9a55de84
[FILE] .git\objects\9c\131c56fb3aedd3109a5e14654aebe975405fe5
[FILE] .git\objects\9c\25b0ab013b403b904dafc1bc9fce9106be68b6
[FILE] .git\objects\9c\3082c7e397abf3f803d6d9b6c3839440a1fe01
[FILE] .git\objects\9c\c8198ddd5ed1ecc27fecdd2ad2ea6ce23af540
[DIR]  .git\objects\9d
[FILE] .git\objects\9d\26fd86a0722df73af611982d0f1736f1a9a61f
[FILE] .git\objects\9d\5313495c38e2d9fe6d19bd335f7ab738aee741
[FILE] .git\objects\9d\6343a065e592ed70953a043be9d2ed28712066
[FILE] .git\objects\9d\9e906f6f381be1a72a3ff2f61950a5ca82c95e
[FILE] .git\objects\9d\aac640701812c7dfa5243d8f476df62f7ffc3c
[FILE] .git\objects\9d\db861884112a387f5866eec842b92b35e39e66
[DIR]  .git\objects\9e
[FILE] .git\objects\9e\5faa9ca1f3f7590e41180bfcb98db8e3a7ac44
[DIR]  .git\objects\9f
[FILE] .git\objects\9f\1ce3cc2050430c10ebb41d8855a0c27a8b914c
[FILE] .git\objects\9f\506ac1799a050528c89529d65cf6d640bf643e
[FILE] .git\objects\9f\512268f9760ca15d1121ad289b53e9b8791d8c
[FILE] .git\objects\9f\f51e4a658415092afbc10d5bc6b88f66e334bf
[DIR]  .git\objects\a0
[FILE] .git\objects\a0\0bbdb21a548c971494751f9adcdc41cad281a7
[FILE] .git\objects\a0\593d6e1072e6c1accb5070785d9d0c1ba7829e
[FILE] .git\objects\a0\e9089c55ba26af2742ce9896f14b02d2f3e152
[DIR]  .git\objects\a1
[FILE] .git\objects\a1\99304faca6e5bc47064f49cee3e66e323c7b81
[DIR]  .git\objects\a2
[FILE] .git\objects\a2\5758a90f0f18182fb28501ce8e1a6206ac813c
[FILE] .git\objects\a2\f3aa160cf77931d6b87019c66aed4f2882c244
[DIR]  .git\objects\a3
[FILE] .git\objects\a3\0c00aa051595b6963219709f1fe9fe44652bdc
[FILE] .git\objects\a3\504ea129a9140c177c088dfb8d585cfc04e088
[FILE] .git\objects\a3\d600fc800e6d1cfe804b789a73e636a39ad1e0
[DIR]  .git\objects\a4
[FILE] .git\objects\a4\3c79936864a4ebdf1086a9253fa86090bf1ecf
[FILE] .git\objects\a4\710cb6c67e3abb8c6f9fc12dfa38e7fb34edc0
[DIR]  .git\objects\a5
[FILE] .git\objects\a5\0b24b5e925538e00461db3ff0d0b86d14e165e
[FILE] .git\objects\a5\0f429634b49e2a10f3596ecbcddf40e5a57191
[FILE] .git\objects\a5\587ec37a123e55a17b31e2a41c3c40fe661f6c
[FILE] .git\objects\a5\e45823a449ffedc9d71c99b1865a24336d9743
[DIR]  .git\objects\a6
[FILE] .git\objects\a6\56886deb1bec6ab86d71b1265f8e2149e75486
[FILE] .git\objects\a6\6156820f2598016e728f444821d9504bfe37d4
[FILE] .git\objects\a6\a11830d8d5251695e0057abcda380d1aaaedb5
[DIR]  .git\objects\a7
[FILE] .git\objects\a7\158c3c8ed62b30cdb83cc8bac6ad193ea7fd60
[FILE] .git\objects\a7\4ab9a589bf8f82a2491e400b4b3b827c91ca17
[FILE] .git\objects\a7\5e6fa7147298b1355d872a672d45ef243e624b
[DIR]  .git\objects\a8
[FILE] .git\objects\a8\02a294d58a3496723fcd1a37d85ff564747a8c
[FILE] .git\objects\a8\5cf853eb3cb3c26a3fe1a4e0381ae314dad708
[FILE] .git\objects\a8\a9c5a4d0adac30da22cf989393728ecd08fbf7
[FILE] .git\objects\a8\ccd59563871774064c80cda3a2d80c5785bb0a
[FILE] .git\objects\a8\fce39cb35653edb0d31811b92ed8d1c3b26068
[DIR]  .git\objects\a9
[FILE] .git\objects\a9\5fae61af0fe4d13c469bb4a23bbbab6fb7da26
[FILE] .git\objects\a9\c4de4a13fdf5253e93f19dd09183dc9b7d73cc
[FILE] .git\objects\a9\c73ff020b3388c87c9b30c11fb19b930414f86
[FILE] .git\objects\a9\da897d0fb3c5766bf93a0721f378d77c160190
[FILE] .git\objects\a9\e2098db6826f3da5035f656b6f8bedb5dfd39f
[DIR]  .git\objects\aa
[FILE] .git\objects\aa\6a55920f5abe441f74a71a85474c36884da3c2
[FILE] .git\objects\aa\c99dc26eb26d7d055daf06324d2f4e66ecfa14
[DIR]  .git\objects\ab
[FILE] .git\objects\ab\e86b1a848372bcec184f40beee88748d1acef1
[DIR]  .git\objects\ac
[FILE] .git\objects\ac\1c6067b19576b731c2c806a10471d40288e001
[FILE] .git\objects\ac\1e28c32f5a392c5cb49c472356efa472f44814
[FILE] .git\objects\ac\b7e82ede446249db13e529fca9407f62c6875b
[FILE] .git\objects\ac\c506f583448735c3f52cf647c2b238ed3d33f0
[FILE] .git\objects\ac\e3cd629f0917429874f586f360f33da137b948
[DIR]  .git\objects\ad
[FILE] .git\objects\ad\8307c90c19c5c51e91d34b403908d855c27816
[DIR]  .git\objects\ae
[FILE] .git\objects\ae\5b31da17362761350545c9cd4145d9d5aa5859
[FILE] .git\objects\ae\ae0e84043cda2445f66e64c36b6a8b67fff9ae
[FILE] .git\objects\ae\c9ab0598e2e834d4b90250b84ca7da6c742da3
[DIR]  .git\objects\af
[FILE] .git\objects\af\891ec3d401fa455ca93f93971ef5c25c1393f3
[FILE] .git\objects\af\cf2523e22e85c620b8c1325ce35c9f258e611c
[FILE] .git\objects\af\d46fc834eea1cfd97f4fa3b070b4156e27d485
[DIR]  .git\objects\b0
[FILE] .git\objects\b0\02df02c12193a4626a0710fead96b15e298284
[FILE] .git\objects\b0\3cd96a0fe7f6858a23a81e614d7f748faffbad
[FILE] .git\objects\b0\f7aaa918bfdc6ca82afdd35a5c2dbfb5521a86
[DIR]  .git\objects\b1
[FILE] .git\objects\b1\bf3b70a5bf325a021609cc43a405e583bf9dd6
[DIR]  .git\objects\b2
[FILE] .git\objects\b2\72be10d649c9e01a2b2bfa97ede299b7b681f0
[DIR]  .git\objects\b3
[FILE] .git\objects\b3\ed084e1fe79bc0d52ad618fcb6e3065807341b
[FILE] .git\objects\b3\f5290a300218655da799e042a2003bdb68eeb3
[DIR]  .git\objects\b4
[FILE] .git\objects\b4\3e801d1b1e60b7a21ad88e1d6741eba23760c2
[FILE] .git\objects\b4\62721d55ca7e541cec4345e40cd7070e784b44
[DIR]  .git\objects\b5
[FILE] .git\objects\b5\5c367371b0ed5eba300e8cb88cf98e05f2874a
[DIR]  .git\objects\b6
[FILE] .git\objects\b6\7183d3590f1be0ff8e95a65030841bd55c5234
[DIR]  .git\objects\b7
[FILE] .git\objects\b7\868a6e4df3d8e0f6099e8d01a9a2e736ad76ce
[FILE] .git\objects\b7\99e7d12d700a3817b8ecff79848c4bd76d2bce
[DIR]  .git\objects\b9
[FILE] .git\objects\b9\3bc3efb8a6958315edce31e3ab03f9878d140c
[DIR]  .git\objects\ba
[FILE] .git\objects\ba\8dcbbec2afacd7d695a156de94151236d5848a
[FILE] .git\objects\ba\f2fcea27a64cb2581d0935ad676df848e2b964
[DIR]  .git\objects\bb
[FILE] .git\objects\bb\0dfa38495689ceaa78816f22383df5e9a58515
[FILE] .git\objects\bb\10d52075d2bfba2fde6b5438fedc205f7c6863
[FILE] .git\objects\bb\2ecf9f0ab026c9aff9dd7563850f96bdc3e9c7
[FILE] .git\objects\bb\f44ac3dfd6d1871785e875bccc789bdcddba46
[FILE] .git\objects\bb\fcdba73d6b720a84a41ed5c7e1be4bc8765252
[DIR]  .git\objects\bc
[FILE] .git\objects\bc\191ceab3dd6c2d6e119fcb44a4d543d29026e1
[FILE] .git\objects\bc\1ff0da981e63e7604ea3a67c2b22c129d3133c
[FILE] .git\objects\bc\29c24091b41c5f35aacacf3df25e4d19e4aeca
[DIR]  .git\objects\bd
[FILE] .git\objects\bd\7174f3a7e58bb6905569b3e03a252873102aba
[FILE] .git\objects\bd\9d786fdb832df97f540387bb2b2bdb6669cc90
[DIR]  .git\objects\be
[FILE] .git\objects\be\4e13773d8458114ba7a4171dc7873a49fd1621
[FILE] .git\objects\be\d0fb8a994f12042984fd8124170fef4d054d80
[DIR]  .git\objects\bf
[FILE] .git\objects\bf\7025e2b533e417b5a420aeeedf4ea6166b6cc6
[DIR]  .git\objects\c0
[FILE] .git\objects\c0\1a1b7572b1a0192d051b6e4469c4739426fdf1
[FILE] .git\objects\c0\e5393447776d4d3735d82b2d55269877a740de
[DIR]  .git\objects\c1
[FILE] .git\objects\c1\22fbde2e02514633788b37ee1926152a4b1b17
[FILE] .git\objects\c1\856d52fbbaaee130c61987b5a36143d1f1395c
[DIR]  .git\objects\c2
[FILE] .git\objects\c2\a70fa46caa161126cf455366bd5682421feb62
[DIR]  .git\objects\c3
[FILE] .git\objects\c3\40547dd32033a077017b886c7b73d527311558
[FILE] .git\objects\c3\56fa483fce4660ef5e7f728a63284388beaa01
[DIR]  .git\objects\c4
[FILE] .git\objects\c4\17ae0d6ee9b75f818e9f7eefa674e96ad277bd
[FILE] .git\objects\c4\f57984c85b5ce6660e771b97056a5ef4ef0ef8
[DIR]  .git\objects\c5
[FILE] .git\objects\c5\40cbc1655f8ee019d3fc07f396dc1ea0bcc1f4
[FILE] .git\objects\c5\54909a021ad54379221d20c28d8e6b028e3a6c
[DIR]  .git\objects\c6
[FILE] .git\objects\c6\13821c515d166024e3f9b8f5bc6eb3bf592cd0
[FILE] .git\objects\c6\204d3006663da9bcec0d501b0e82c45723bdc3
[FILE] .git\objects\c6\29e207a2ed2668cb41b7d8798fbac580e1bb70
[FILE] .git\objects\c6\7063b4a75c080272643bec7552ff410a717484
[DIR]  .git\objects\c7
[FILE] .git\objects\c7\578c68ce155294991e58d9828242d300819de8
[DIR]  .git\objects\c8
[FILE] .git\objects\c8\1653bf408d02a330dd92fc4ce4d91d7839486e
[FILE] .git\objects\c8\32938dfedb4540c18ac80d60a433fad195dbaf
[FILE] .git\objects\c8\5dd7964106dd34bb76a8a54c5eb4b0e0857f7d
[DIR]  .git\objects\c9
[FILE] .git\objects\c9\2d98ec380d2d369bf1e4f1932deb5385f392cc
[FILE] .git\objects\c9\d91ac018046bb861d0d1d5472d2b468fb8e716
[DIR]  .git\objects\ca
[FILE] .git\objects\ca\478348576cbd05c71e9c9f36d7fb1b46291373
[FILE] .git\objects\ca\aaf4e0c421827ade81917b506b6a28784e09c9
[DIR]  .git\objects\cb
[FILE] .git\objects\cb\07515bc83dbae35673cd524f4d4b0d2068f082
[DIR]  .git\objects\cc
[FILE] .git\objects\cc\c6018f9068625ebd596379dc03639120a3be5d
[FILE] .git\objects\cc\db7e50dd79744cce31e98a1737466abab1f9d5
[FILE] .git\objects\cc\e99126792693f1c00a180c4fa3ec56ac70bb30
[DIR]  .git\objects\cd
[FILE] .git\objects\cd\054dddf2afcaa2abfc45d0ad6b28e3aea08072
[FILE] .git\objects\cd\451aa0b1284ffffc9e04c44b88b02915e1ee0b
[FILE] .git\objects\cd\89b9ef1b0c2f530d67f42967a2d067a5ccfd39
[FILE] .git\objects\cd\e79bc1014d02330dee1f42674bb16f83a17266
[DIR]  .git\objects\ce
[FILE] .git\objects\ce\0f2c4e4e5b83571d1a2785274d0d24a219e408
[FILE] .git\objects\ce\926dcf79996b0ca99f61d51cf7e6e4127eedaf
[FILE] .git\objects\ce\e9039761174aa7295905f38447d022e71c0db4
[DIR]  .git\objects\cf
[FILE] .git\objects\cf\06a1156a1fa2bd5114f5c42d54fe397919c014
[FILE] .git\objects\cf\2e0f53ea8a0a78afa408de5145b924a35dc56d
[FILE] .git\objects\cf\7d975dc1d57a70480b21c7634a669cc8ad1da1
[FILE] .git\objects\cf\9310ab611033a77e618b644c0226a1ce64e4b1
[FILE] .git\objects\cf\eddb40630e1b8e9024ce821e0391dca734dd28
[FILE] .git\objects\cf\f06b78ada5b6873f6d845b27473ed078a5470c
[DIR]  .git\objects\d0
[FILE] .git\objects\d0\22f9d58c6b8e4cebd998ab362c713d46326e0c
[FILE] .git\objects\d0\47d5e846ce7a93f5ca2ad9c3aaa2777e50d677
[DIR]  .git\objects\d1
[FILE] .git\objects\d1\2bcbc76ed30239132cc61c564f61f652e0ad1c
[FILE] .git\objects\d1\9eda0bb192bd94ea9fe815cdd11ac5c05f0fc3
[DIR]  .git\objects\d2
[FILE] .git\objects\d2\68f9edc17e9faef627e68df45a06acabe675b3
[FILE] .git\objects\d2\8fdf3fd36014e8eb530b2d9828851dba625cd7
[DIR]  .git\objects\d3
[FILE] .git\objects\d3\9e8afcbca950bda59df7415cd551c3a8f1fb3c
[FILE] .git\objects\d3\e55e972a93ac996a54f14bff37ace48e051125
[DIR]  .git\objects\d4
[FILE] .git\objects\d4\126e70e8694234261ba5a1286425f981312a74
[FILE] .git\objects\d4\279ae9e25f355ac948b2f39df428a84bae213a
[FILE] .git\objects\d4\514e12e38d3cbc0fa1925c556d9ed1b4367bbb
[FILE] .git\objects\d4\7479aa9fca641284da79ce9d997046f5e1e9b1
[FILE] .git\objects\d4\7cf6b1dd89c7680962eddc2f45a808968a966a
[FILE] .git\objects\d4\bb549418b5b4d4cc477b980a2fa32b03be8d67
[DIR]  .git\objects\d5
[FILE] .git\objects\d5\1946fe52e29acf632b3056ad872589f4fdaaa7
[FILE] .git\objects\d5\5d053e1bb929cb881e94d09fa859890aeaa17e
[FILE] .git\objects\d5\af93855ce15e41ce2c496c30f139e569e809e9
[DIR]  .git\objects\d6
[FILE] .git\objects\d6\0348b14e382864b13bcb71dee3b59143d8dad4
[DIR]  .git\objects\d7
[FILE] .git\objects\d7\8ca14cfba0e1e14fed97509c415b6849b85195
[DIR]  .git\objects\d8
[FILE] .git\objects\d8\7b80c7059385359d653a22f33408c5bd358a7e
[FILE] .git\objects\d8\860a8130adb4139c180bba2f7fff8dcbc2018b
[FILE] .git\objects\d8\b22a1aff23ad84ef668d26361da95779cdde2f
[FILE] .git\objects\d8\d3bcd3fc90f6d559368d18e4168d8404ef649d
[DIR]  .git\objects\d9
[FILE] .git\objects\d9\0e11030c5e5248acd37839873882c497001160
[FILE] .git\objects\d9\4aa4ce768e24558e5dd2cf8db4cde0802a453e
[FILE] .git\objects\d9\a7282b99d0ee8b134c6cc4969c28441328d23a
[DIR]  .git\objects\da
[FILE] .git\objects\da\549538361ea1e8ef2e67fdc8666e763106b858
[FILE] .git\objects\da\8baae5efc694d6c72b64d42c36cec9dbae15f0
[FILE] .git\objects\da\933d9ed5c2d391647a17779f5a93e1d9586b57
[DIR]  .git\objects\db
[FILE] .git\objects\db\6b5e0f2778af02c67ceb5c458df62008d17696
[FILE] .git\objects\db\be7b7c851a217f093bde07217786c482eec4e1
[FILE] .git\objects\db\df47df81e9d27fe160a314a37d0cce22651376
[DIR]  .git\objects\dc
[FILE] .git\objects\dc\08508dcc903931a98fa82e0fbe8fe9834a7de1
[FILE] .git\objects\dc\643f62fa7d925cca042ab5be3865202f664b6a
[FILE] .git\objects\dc\bab0d6de92e3e6579e765025e13f8ca4bddc86
[FILE] .git\objects\dc\c964aded12c3e5456b74cef0b9d8ef6928d7a3
[FILE] .git\objects\dc\d3b48a5690315ea95d78d93ed19733529afe9e
[FILE] .git\objects\dc\f648ae9310fa14525bf3d4e6bae95b9ef70874
[DIR]  .git\objects\dd
[FILE] .git\objects\dd\6878bea0badef222cf11d9124439badf3d755c
[DIR]  .git\objects\de
[FILE] .git\objects\de\790a80460238c3cde8fed9f30d0d1b71e52813
[FILE] .git\objects\de\f1770c2f6f32b3f413506b3136d0c4452de4bb
[DIR]  .git\objects\df
[FILE] .git\objects\df\1d509dc2708f66ef4912f4060ef456b1934749
[FILE] .git\objects\df\4b561a331a713885853ba7021a5fa1e420bbc0
[FILE] .git\objects\df\7d3fe1add16fcbf48efd6269127af7646beef9
[FILE] .git\objects\df\ee4c09b0690ea83ae3303849cb95f89cf45363
[DIR]  .git\objects\e0
[FILE] .git\objects\e0\cfea938761a6b3c3fc556ef43a2c1206c4623c
[DIR]  .git\objects\e1
[FILE] .git\objects\e1\1ea159ac4cfb0760a2ea37c8aaa2fb0a21e02b
[DIR]  .git\objects\e2
[FILE] .git\objects\e2\09c5acac4433b37599998e357f7c9ef6004461
[FILE] .git\objects\e2\de349966956e2516d639055a7a57d607b636de
[FILE] .git\objects\e2\f70cfaf7c92061cf0945ac533425a8eb55918b
[DIR]  .git\objects\e3
[FILE] .git\objects\e3\0c8af8f67ba8d5287d738ddb126bb67fd13a85
[FILE] .git\objects\e3\1b14afa125863a19e538a123b349ff0091e792
[FILE] .git\objects\e3\2f59ac412f1e2e517ce225716c5f0a9d0a07c6
[FILE] .git\objects\e3\4164cc3dcf0170e11ff59d9a391136c56fa35a
[FILE] .git\objects\e3\5ad5196ade62038676fd9cdc20d82c74c8eab8
[FILE] .git\objects\e3\ae48903fab590507f3e986ab0d1f1cb1d60636
[DIR]  .git\objects\e4
[FILE] .git\objects\e4\87b71f11dd7d22357bffbef2dbb9f24f78be4f
[DIR]  .git\objects\e5
[FILE] .git\objects\e5\43ccb046831c9d7493e46911c9b36ccfe63493
[FILE] .git\objects\e5\5520fef08ded8975fd4cf37d1b3415e37402e0
[FILE] .git\objects\e5\5a9fe2058c210e2a087ec7b2387fe168fa3ab2
[FILE] .git\objects\e5\612663d9ec066c8d8106eae25cf11c5816afeb
[FILE] .git\objects\e5\c02512bc37fb139ef20a02b2150d1a61ee2730
[DIR]  .git\objects\e6
[FILE] .git\objects\e6\10cb8c929237860170e55148763590b50874df
[FILE] .git\objects\e6\989ec2807c0f8df1096be67696adcc6895d927
[FILE] .git\objects\e6\9de29bb2d1d6434b8b29ae775ad8c2e48c5391
[FILE] .git\objects\e6\a78a5850b38e5cb1202e8aedf1ad69be8e6cd2
[FILE] .git\objects\e6\eb7667447f003ad44a896dd7871cafa240db46
[DIR]  .git\objects\e7
[FILE] .git\objects\e7\b00585949e978fb63227d872cd4739e0de0c7e
[DIR]  .git\objects\e8
[FILE] .git\objects\e8\5a2a1be83605ef6e79e725ba4521f8d3692d23
[DIR]  .git\objects\e9
[FILE] .git\objects\e9\48d859f0bae580c9fea9d6408a23052ae7bd50
[FILE] .git\objects\e9\82c4481b1949d7617527f146abb1f1e2327ef4
[FILE] .git\objects\e9\b5722073ea1e57816d9a3471842d3a80bdd3e9
[DIR]  .git\objects\ea
[FILE] .git\objects\ea\6d71ca15026a04c95a3c340b1ccfa477d65e0f
[FILE] .git\objects\ea\bb3ec77a55a505f85ce55f73469f61701714bd
[FILE] .git\objects\ea\c460f2c9845a7ab8d5fd6ef06ce94900881fab
[FILE] .git\objects\ea\c87ea4813cb4a0b61f7a2701430fc10e593a65
[DIR]  .git\objects\eb
[FILE] .git\objects\eb\7c2fdab722fbb133ca9ff8f25c6de5a867384e
[FILE] .git\objects\eb\b613751db1d4c87164f8daf1dca2fe257c3045
[FILE] .git\objects\eb\ea4a934f76dbbeec236f15d5ced2e3bad1f102
[FILE] .git\objects\eb\ed41826b648719f700c2758cedb870099c1815
[DIR]  .git\objects\ec
[FILE] .git\objects\ec\1c4d601cd11688dc8e138bafe6f091087f41dd
[FILE] .git\objects\ec\63becebf7c47c1fa2789b795a39baa64640e55
[FILE] .git\objects\ec\e4180263215c26c67bcd1ba9a131f2dafd135f
[FILE] .git\objects\ec\f025b891405f55c9f6b59763c856a8b19a023c
[DIR]  .git\objects\ed
[FILE] .git\objects\ed\0d738f8c9f6bc049af6c7d6b0cf6bc24a084cf
[FILE] .git\objects\ed\5b5b4a38d904ea357aeb24c89e1cc47c75ed79
[FILE] .git\objects\ed\837a7e63d0abb0301b552bc78a01c1f9babf08
[FILE] .git\objects\ed\a836eb37259f66166e55e345fc043c43ed3c3e
[DIR]  .git\objects\ee
[FILE] .git\objects\ee\08ae94554db8c1ce1267f953bfba40bfb3fcf0
[FILE] .git\objects\ee\0ce0f79ba796c479174af598f66f774476397f
[FILE] .git\objects\ee\d175898dff79553993178707dc19974b4595a8
[DIR]  .git\objects\ef
[FILE] .git\objects\ef\3c2ebc0609822202d9f76697dc46b1c2694253
[FILE] .git\objects\ef\b1b1cacb9bc33ecce0a5a7d299fcb1a9aa0874
[DIR]  .git\objects\f0
[FILE] .git\objects\f0\250dd5c767e0e811805b51877f6c245528dd85
[FILE] .git\objects\f0\d57bb8584c212ad77db3959d2b27f7d9b2369b
[FILE] .git\objects\f0\f521528f25561a27e00f5ac153101da92013a3
[DIR]  .git\objects\f1
[FILE] .git\objects\f1\86f74dba5fa79604f2f7cf8085e2dba34a1977
[DIR]  .git\objects\f2
[FILE] .git\objects\f2\a6fb5060abd85cd66588a0d2c99c8509412e91
[FILE] .git\objects\f2\b8541723aca5d7ff89849720d3b8ff06c95034
[DIR]  .git\objects\f3
[FILE] .git\objects\f3\c443158d8139a738f1adcc8c2607bfdef40cd4
[DIR]  .git\objects\f4
[FILE] .git\objects\f4\4d99ab5edd62ec9cbffc9924c36d1c2f6cc269
[FILE] .git\objects\f4\5e9b4cc3fe65b17deb82ae03221d1675f92cd8
[DIR]  .git\objects\f5
[FILE] .git\objects\f5\3e541c546437a71e89e7c9dbab5240d94ab95d
[FILE] .git\objects\f5\7da450cbcc8ffb5cb29dc28efc577fc3c9676e
[FILE] .git\objects\f5\fa53381f6604eb2ef2a2504a9894b80893a00c
[DIR]  .git\objects\f6
[FILE] .git\objects\f6\63b68523458dc640a0c7cbe1d371d0aa27c8ba
[FILE] .git\objects\f6\b41a5e577123a65ab1ec4da1014eb47a536ba1
[FILE] .git\objects\f6\b96dd25b7b79dd47563519e01847d3585082ba
[FILE] .git\objects\f6\ee2fcddcdaa2a82c80de31f3ad1cc6b3253c08
[FILE] .git\objects\f6\ef35a7b2566a924734fc2586cba2e042d08076
[DIR]  .git\objects\f8
[FILE] .git\objects\f8\2a600efdf6340f3e8ce3c2fb72974a5c5e2603
[FILE] .git\objects\f8\757c211a996d1892e44dffbbd90f3209a805f5
[DIR]  .git\objects\f9
[FILE] .git\objects\f9\d06b354dfb7d656abfe83d8baf2a1bdb9748db
[DIR]  .git\objects\fa
[FILE] .git\objects\fa\043db6dacb51e39408a1a8ff1e9d68c2971236
[DIR]  .git\objects\fb
[FILE] .git\objects\fb\ccd875b73df232d27a01b27e2e683c28e41961
[FILE] .git\objects\fb\f0d79adbc6ae9c659f714d5e1c1b265515f90a
[DIR]  .git\objects\fc
[FILE] .git\objects\fc\222ab1ee4ca7ea4c239d5bf866b1ef774fad7e
[FILE] .git\objects\fc\48166d1d11777bebe8b7c76642ca0e8937752a
[FILE] .git\objects\fc\65af395823c0fc4d3f38b76f618fa3eb196921
[DIR]  .git\objects\fd
[FILE] .git\objects\fd\617b9a229ccc6fe9fe8a68e733d6c8948a0736
[FILE] .git\objects\fd\789db2d945c4b64c64f6d10d2119eebaf01c1f
[FILE] .git\objects\fd\bf505446167b6dbc463e45a09b6ccefbb75cec
[FILE] .git\objects\fd\f3bf484d98e87f7927453204d5020663f72adf
[DIR]  .git\objects\fe
[FILE] .git\objects\fe\0c057721811f0d54a6ce5b2be55d3ed7e14fff
[FILE] .git\objects\fe\6ba4c549bcb22c25e2687f7ce7d3fcc5b0e289
[FILE] .git\objects\fe\b921e1bb44fe4cd60f0d6fd6c6841493c8ffb6
[DIR]  .git\objects\ff
[FILE] .git\objects\ff\bdcc8bd84b00e3b583cb645efd0fe64e9a3960
[FILE] .git\objects\ff\c08929035b3b8c0565d69a81d8d91aa0125cfc
[DIR]  .git\objects\info
[DIR]  .git\objects\pack
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.idx
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.pack
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.rev
[FILE] .git\ORIG_HEAD
[FILE] .git\packed-refs
[DIR]  .git\refs
[DIR]  .git\refs\heads
[FILE] .git\refs\heads\main
[DIR]  .git\refs\remotes
[DIR]  .git\refs\remotes\origin
[FILE] .git\refs\remotes\origin\HEAD
[FILE] .git\refs\remotes\origin\main
[DIR]  .git\refs\tags
[DIR]  .ralfai
[FILE] .ralfai\config.yaml
[FILE] .ralfai\session.md
[FILE] ARCHITECTURE.md
[FILE] PROMPTS.md
[FILE] README.md
[FILE] ROADMAP.md
[DIR]  src
[DIR]  src\nupkg
[FILE] src\nupkg\RalfAI.1.1.0.nupkg
[DIR]  src\RalfAI
[DIR]  src\RalfAI.Abstractions
[DIR]  src\RalfAI.Abstractions\bin
[DIR]  src\RalfAI.Abstractions\bin\Debug
[DIR]  src\RalfAI.Abstractions\bin\Debug\net10.0
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.deps.json
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[DIR]  src\RalfAI.Abstractions\bin\Release
[DIR]  src\RalfAI.Abstractions\bin\Release\net10.0
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.deps.json
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\IAgentContext.cs
[DIR]  src\RalfAI.Abstractions\obj
[DIR]  src\RalfAI.Abstractions\obj\Debug
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.AssemblyInfo.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.assets.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.GlobalUsings.g.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.sourcelink.json
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\ref\RalfAI.Abstractions.dll
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\refint\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\project.assets.json
[FILE] src\RalfAI.Abstractions\obj\project.nuget.cache
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.g.props
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.g.targets
[DIR]  src\RalfAI.Abstractions\obj\Release
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.AssemblyInfo.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.assets.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.GlobalUsings.g.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.sourcelink.json
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0\ref
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\ref\RalfAI.Abstractions.dll
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0\refint
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\refint\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\RalfAI.Abstractions.csproj
[DIR]  src\RalfAI.Contexts.DotNet
[DIR]  src\RalfAI.Contexts.DotNet\bin
[DIR]  src\RalfAI.Contexts.DotNet\bin\Debug
[DIR]  src\RalfAI.Contexts.DotNet\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.deps.json
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.DotNet\bin\Release
[DIR]  src\RalfAI.Contexts.DotNet\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.deps.json
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.DotNet\Contexts
[FILE] src\RalfAI.Contexts.DotNet\Contexts\DotNetContext.cs
[DIR]  src\RalfAI.Contexts.DotNet\obj
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.C.C91D3321.Up2Date
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.assets.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.sourcelink.json
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\ref\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\refint\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\project.assets.json
[FILE] src\RalfAI.Contexts.DotNet\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.C.C91D3321.Up2Date
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.assets.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.sourcelink.json
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\ref\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\refint\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\Plugins
[FILE] src\RalfAI.Contexts.DotNet\Plugins\DotNetPlugin.cs
[FILE] src\RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj
[DIR]  src\RalfAI.Contexts.HtmlApps
[DIR]  src\RalfAI.Contexts.HtmlApps\bin
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Debug
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.deps.json
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Release
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.deps.json
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.HtmlApps\Contexts
[FILE] src\RalfAI.Contexts.HtmlApps\Contexts\HtmlAppContext.cs
[DIR]  src\RalfAI.Contexts.HtmlApps\obj
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.C.6CC3700C.Up2Date
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.assets.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.sourcelink.json
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\ref\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\refint\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\project.assets.json
[FILE] src\RalfAI.Contexts.HtmlApps\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.C.6CC3700C.Up2Date
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.assets.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.sourcelink.json
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\ref\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\refint\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\Plugins
[FILE] src\RalfAI.Contexts.HtmlApps\Plugins\StaticServerPlugin.cs
[FILE] src\RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj
[DIR]  src\RalfAI.Contexts.Markdown
[DIR]  src\RalfAI.Contexts.Markdown\bin
[DIR]  src\RalfAI.Contexts.Markdown\bin\Debug
[DIR]  src\RalfAI.Contexts.Markdown\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.deps.json
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.Markdown\bin\Release
[DIR]  src\RalfAI.Contexts.Markdown\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.deps.json
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.Markdown\Contexts
[FILE] src\RalfAI.Contexts.Markdown\Contexts\MarkdownContext.cs
[DIR]  src\RalfAI.Contexts.Markdown\obj
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.C.05DC118C.Up2Date
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.assets.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.sourcelink.json
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\ref\RalfAI.Contexts.Markdown.dll
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\refint\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\project.assets.json
[FILE] src\RalfAI.Contexts.Markdown\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.C.05DC118C.Up2Date
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.assets.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.sourcelink.json
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\ref\RalfAI.Contexts.Markdown.dll
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\refint\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj
[DIR]  src\RalfAI.Core
[FILE] src\RalfAI.Core\AgentEngine.cs
[FILE] src\RalfAI.Core\AgentSessionTypes.cs
[DIR]  src\RalfAI.Core\bin
[DIR]  src\RalfAI.Core\bin\Debug
[DIR]  src\RalfAI.Core\bin\Debug\net10.0
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.deps.json
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Core\bin\Release
[DIR]  src\RalfAI.Core\bin\Release\net10.0
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.deps.json
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\ConversationLogger.cs
[DIR]  src\RalfAI.Core\ImageGeneration
[FILE] src\RalfAI.Core\ImageGeneration\ImageGenerationService.cs
[FILE] src\RalfAI.Core\MarkdownText.cs
[DIR]  src\RalfAI.Core\obj
[DIR]  src\RalfAI.Core\obj\Debug
[DIR]  src\RalfAI.Core\obj\Debug\net10.0
[FILE] src\RalfAI.Core\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.C.DE964921.Up2Date
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.AssemblyInfo.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.assets.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.GlobalUsings.g.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.sourcelink.json
[DIR]  src\RalfAI.Core\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Core\obj\Debug\net10.0\ref\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Core\obj\Debug\net10.0\refint\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\project.assets.json
[FILE] src\RalfAI.Core\obj\project.nuget.cache
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.g.props
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.g.targets
[DIR]  src\RalfAI.Core\obj\Release
[DIR]  src\RalfAI.Core\obj\Release\net10.0
[FILE] src\RalfAI.Core\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.C.DE964921.Up2Date
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.AssemblyInfo.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.assets.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.GlobalUsings.g.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.sourcelink.json
[DIR]  src\RalfAI.Core\obj\Release\net10.0\ref
[FILE] src\RalfAI.Core\obj\Release\net10.0\ref\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\obj\Release\net10.0\refint
[FILE] src\RalfAI.Core\obj\Release\net10.0\refint\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\Plugins
[FILE] src\RalfAI.Core\Plugins\AIPlugin.cs
[FILE] src\RalfAI.Core\Plugins\FileSystemPlugin.cs
[FILE] src\RalfAI.Core\Plugins\ImageGenerationPlugin.cs
[FILE] src\RalfAI.Core\Plugins\ImagePlugin.cs
[FILE] src\RalfAI.Core\Plugins\SpeechPlugin.cs
[FILE] src\RalfAI.Core\Plugins\WebPlugin.cs
[FILE] src\RalfAI.Core\RalfAI.Core.csproj
[FILE] src\RalfAI.Core\RalfAIConfig.cs
[FILE] src\RalfAI.Core\RateLimiter.cs
[DIR]  src\RalfAI.Core\Voice
[FILE] src\RalfAI.Core\Voice\SpeechService.cs
[FILE] src\RalfAI.sln
[FILE] src\RalfAI\appsettings.json
[DIR]  src\RalfAI\bin
[DIR]  src\RalfAI\bin\Debug
[DIR]  src\RalfAI\bin\Debug\net10.0
[FILE] src\RalfAI\bin\Debug\net10.0\Anthropic.dll
[FILE] src\RalfAI\bin\Debug\net10.0\appsettings.json
[FILE] src\RalfAI\bin\Debug\net10.0\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Azure.Core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Debug\net10.0\last-folder.txt
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Debug\net10.0\OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.deps.json
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.dll
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.exe
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Debug\net10.0\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Spectre.Console.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.ClientModel.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.Numerics.Tensors.dll
[DIR]  src\RalfAI\bin\Release
[DIR]  src\RalfAI\bin\Release\net10.0
[FILE] src\RalfAI\bin\Release\net10.0\Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\appsettings.json
[FILE] src\RalfAI\bin\Release\net10.0\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Azure.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Release\net10.0\OpenAI.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish
[FILE] src\RalfAI\bin\Release\net10.0\publish\Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\appsettings.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Azure.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.deps.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.exe
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Spectre.Console.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.ClientModel.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.Numerics.Tensors.dll
[FILE] src\RalfAI\bin\Release\net10.0\ralf.deps.json
[FILE] src\RalfAI\bin\Release\net10.0\ralf.dll
[FILE] src\RalfAI\bin\Release\net10.0\ralf.exe
[FILE] src\RalfAI\bin\Release\net10.0\ralf.pdb
[FILE] src\RalfAI\bin\Release\net10.0\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Release\net10.0\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Release\net10.0\Spectre.Console.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.ClientModel.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.Numerics.Tensors.dll
[FILE] src\RalfAI\config.example.json
[FILE] src\RalfAI\ContextRegistry.cs
[FILE] src\RalfAI\FileBrowser.cs
[FILE] src\RalfAI\FolderConfigStore.cs
[FILE] src\RalfAI\FullScreenConsole.cs
[FILE] src\RalfAI\InputCollector.cs
[FILE] src\RalfAI\LastFolderStore.cs
[DIR]  src\RalfAI\obj
[DIR]  src\RalfAI\obj\Debug
[DIR]  src\RalfAI\obj\Debug\net10.0
[FILE] src\RalfAI\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI\obj\Debug\net10.0\apphost.exe
[FILE] src\RalfAI\obj\Debug\net10.0\ralf.dll
[FILE] src\RalfAI\obj\Debug\net10.0\ralf.pdb
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.AssemblyInfo.cs
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.AssemblyInfoInputs.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.assets.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.AssemblyReference.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.FileListAbsolute.txt
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.Up2Date
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.genruntimeconfig.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.GlobalUsings.g.cs
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.sourcelink.json
[DIR]  src\RalfAI\obj\Debug\net10.0\ref
[FILE] src\RalfAI\obj\Debug\net10.0\ref\ralf.dll
[DIR]  src\RalfAI\obj\Debug\net10.0\refint
[FILE] src\RalfAI\obj\Debug\net10.0\refint\ralf.dll
[FILE] src\RalfAI\obj\project.assets.json
[FILE] src\RalfAI\obj\project.nuget.cache
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.dgspec.json
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.g.props
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.g.targets
[DIR]  src\RalfAI\obj\Release
[DIR]  src\RalfAI\obj\Release\net10.0
[FILE] src\RalfAI\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI\obj\Release\net10.0\apphost.exe
[FILE] src\RalfAI\obj\Release\net10.0\DotnetToolSettings.xml
[FILE] src\RalfAI\obj\Release\net10.0\PublishOutputs.64b92f381d.txt
[FILE] src\RalfAI\obj\Release\net10.0\PublishOutputs.e8d25c20c9.txt
[FILE] src\RalfAI\obj\Release\net10.0\ralf.dll
[FILE] src\RalfAI\obj\Release\net10.0\ralf.pdb
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.AssemblyInfo.cs
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.AssemblyInfoInputs.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.assets.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.AssemblyReference.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.FileListAbsolute.txt
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.Up2Date
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.genruntimeconfig.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.GlobalUsings.g.cs
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.sourcelink.json
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.toolssettingsinput.cache
[DIR]  src\RalfAI\obj\Release\net10.0\ref
[FILE] src\RalfAI\obj\Release\net10.0\ref\ralf.dll
[DIR]  src\RalfAI\obj\Release\net10.0\refint
[FILE] src\RalfAI\obj\Release\net10.0\refint\ralf.dll
[FILE] src\RalfAI\obj\Release\RalfAI.1.0.0.nuspec
[FILE] src\RalfAI\obj\Release\RalfAI.1.1.0.nuspec
[FILE] src\RalfAI\Program.cs
[DIR]  src\RalfAI\Properties
[FILE] src\RalfAI\Properties\launchSettings.json
[FILE] src\RalfAI\RalfAI.csproj
[FILE] src\RalfAI\RalfAIApp.cs
[FILE] USAGE.md

---

### TOOL — FileSystem.ReadFile — 01:13:37

args: relativePath=README.md
result: # RalfAI

An autonomous coding agent CLI. Point it at a folder, pick a context (.NET, Markdown docs,
or a static HTML/CSS/JS app), and it builds/edits files, runs `dotnet build`/`dotnet test`,
fetches URLs, previews HTML apps over local HTTP, and optionally talks to you (speech in/out)
— all from the terminal. Built on [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/),
so it can talk to Azure AI Foundry, native OpenAI, or Anthropic Claude — each through that
provider's own official .NET SDK, picked by one config setting.

For how it's built internally (project layout, the provider/plugin abstractions, the turn
loop), see [ARCHITECTURE.md](ARCHITECTURE.md).

## Install

RalfAI is a [.NET global tool](https://learn.microsoft.com/dotnet/core/tools/global-tools),
installed as the `ralf` command. From the `src/` folder of this repo:

```
dotnet pack RalfAI/RalfAI.csproj -c Release -o ./nupkg
dotnet tool install --global --add-source ./nupkg RalfAI
```

After that, `ralf` is available from any directory, in any terminal.

To pick up code changes after editing the source, reinstall:

```
dotnet pack RalfAI/RalfAI.csproj -c Release -o ./nupkg
dotnet tool update --global --add-source ./nupkg RalfAI
```

To remove it: `dotnet tool uninstall --global RalfAI`.

## Configure

### Where the config file lives

`dotnet tool update` overwrites everything the package ships, **including a bundled
`appsettings.json`** — so keeping real secrets in the file next to the binary means losing
them on every update. RalfAI resolves which config file to load in this order:

1. `--config <path>` CLI flag — wins outright.
2. `RALFAI_CONFIG_PATH` environment variable — an absolute path to a file living *outside*
   the tool's install directory, so it survives reinstalls. Set it once, persistently:

   ```powershell
   [Environment]::SetEnvironmentVariable("RALFAI_CONFIG_PATH", "D:\Configurations\RalfAI\config.json", "User")
   ```

3. `appsettings.json` next to the installed binary — the zero-config fallback. Ships with
   placeholder values only (see [config.example.json](config.example.json) for the same
   template, meant to be copied to your `RALFAI_CONFIG_PATH` location and filled in).

Whichever file loads, individual settings can still be overridden with `RALFAI_`-prefixed
environment variables (e.g. `RALFAI_AzureAIFoundry__ApiKey`).

### Choosing a provider

```json
{
    "AIProvider": "foundry",
    "AzureAIFoundry": { "Endpoint": "...", "ApiKey": "...", "ModelId": "..." },
    "OpenAI":         { "ApiKey": "...", "ModelId": "gpt-5.1" },
    "Anthropic":      { "ApiKey": "...", "ModelId": "claude-sonnet-4-5" }
}
```

`AIProvider` picks which section is actually used — `"foundry"` (default), `"openai"`, or
`"anthropic"`. Only the section for the active provider needs real values; the others can be
left as placeholders. Override per-run with `RALFAI_AIProvider=openai` without touching the
file.

**Foundry / Azure OpenAI** — `Endpoint` accepts a plain resource host
(`https://<resource>.openai.azure.com`), a Foundry project URL
(`.../api/projects/<name>` — the path is stripped automatically, only the host is used), or
a full `.../chat/completions` URL.

**Multiple models per Foundry context** — instead of one flat `ModelId`, define named models
and map contexts to them:

```json
"AzureAIFoundry": {
    "Endpoint": "...", "ApiKey": "...",
    "Models": {
        "gpt":      { "ModelId": "gpt-5.6-sol" },
        "deepseek": { "ModelId": "deepseek-r1" }
    },
    "ContextModels": { "dotnet": "gpt", "markdown": "deepseek" },
    "DefaultModel": "gpt"
}
```

**Voice (optional)** — add an `AzureSpeech` section to enable dictation (`F2`) and
read-aloud (`F3`):

```json
"AzureSpeech": {
    "Region": "your-region",
    "ApiKey": "...",
    "RecognitionLanguage": "en-US",
    "VoiceName": "en-US-JennyNeural"
}
```

**AI image generation (optional)** — add `ImageModelId` under the active provider's section
(`AzureAIFoundry` or `OpenAI`) to enable the `Image.GenerateAI` tool (logos, icons,
illustrations from a text prompt):

```json
"AzureAIFoundry": { "Endpoint": "...", "ApiKey": "...", "ModelId": "...", "ImageModelId": "dall-e-3" },
"OpenAI":         { "ApiKey": "...", "ModelId": "gpt-5.1", "ImageModelId": "gpt-image-1" }
```

Leave `ImageModelId` empty/absent to leave the tool disabled. Anthropic has no
image-generation API, so `Image.GenerateAI` is unavailable when `AIProvider` is `anthropic`
regardless of config.

## Use

```console
ralf                          # opens a folder picker (remembers the last folder)
ralf <target-folder>          # work in a specific folder
ralf <target-folder> --context html
ralf --context markdown --debug
ralf --config D:\Configurations\RalfAI\config.json
ralf --list-contexts          # show available contexts and exit
```

If no folder is given and none is remembered yet, a full-screen file browser opens
(`↑↓` navigate, `Enter` open folder, `⌫` up, `F9` use folder, `F2` rename, `F7` new folder,
`F8`/`Del` delete, `F3` view file, `F4` open file in an external editor, `F10`/`Esc` cancel).
The same browser opens on `F7` during a session (see the key table below) to switch folders.

`F4` runs the command in the `Editor` config key (`RALFAI_Editor` env override), defaulting to
`code.exe` (VS Code) if unset — set it to whatever editor/IDE you use.

Once running, type a message (paste freely, multi-line) and:

| Key | Action |
| --- | --- |
| `Ctrl+Enter` | send the message |
| `Enter` | newline (multi-line compose) |
| `Esc` | clear the current draft |
| `F1` | full-screen command reference |
| `F2` | dictate via microphone *(needs AzureSpeech)* |
| `F3` | read the last answer aloud *(needs AzureSpeech, Esc interrupts)* |
| `F5` | clear the screen |
| `F6` | switch generation context (`↑↓`/`Enter`/`Esc`) |
| `F7` | switch target folder (file browser) |
| `F8` | toggle debug messages (tool calls, rate-limit notices) |
| `F10` / `Ctrl+Q` | quit |
| `Ctrl+C` | interrupt the current turn (press twice to force-quit) |
| `Esc` / `Ctrl+X` | while the agent is working: same interrupt as `Ctrl+C` |

The whole session runs in the terminal's alternate screen buffer — your normal scrollback
is untouched and restored on exit.

## Contexts

| Key | What it builds |
| --- | --- |
| `dotnet` | .NET 10 projects via the `dotnet` CLI — builds and runs tests until green |
| `markdown` | A set of Markdown documentation files |
| `html` | Static HTML/CSS/JS apps — Tailwind via CDN, vanilla JS, one ES class per file, previewed over local HTTP |

`dotnet` and `html` both use a `src/` (code) · `ops/` (config/deployment) top-level layout
inside the target folder. Documentation is markdown files at the target folder **root** —
never a `docs/` subfolder — using the same fixed set of names as this repo:
`README.md`, `USAGE.md`, `ARCHITECTURE.md`, `ROADMAP.md` (only the ones that fit the size of
what's being built), plus `PROMPTS.md` — a running, numbered log of every objective the agent
was given for that project, appended to before any other work on a turn. Unlike the other
four, `PROMPTS.md` isn't optional past the first objective: it's what makes a project's
history traceable to the requests that drove it. The `markdown` context follows the same
convention when the objective is documenting a software project (`PROMPTS.md` always, the
other four when applicable).

The `html` context also registers a `StaticServer` tool: the agent starts a local Kestrel
server rooted at `src/` and reports the preview URL, since browsers block
`<script type="module">` imports over `file://`.

Each target folder remembers its own context (and debug setting) in
`<folder>/.ralfai/config.yaml`, so re-running `ralf <folder>` — or letting it auto-resume
with no argument — picks the same context back up. Priority when resolving: `--context`
flag → the folder's `config.yaml` → `AgentContext` in the config file → built-in default
(`markdown`).

The conversation transcript for a folder lives at `<folder>/.ralfai/session.md` and is
replayed automatically the next time you point RalfAI at that folder.

Adding a new context, provider, or tool is covered in [ARCHITECTURE.md](ARCHITECTURE.md).


---

### TOOL — FileSystem.ReadFile — 01:13:37

args: relativePath=src/RalfAI.sln
result: 
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Abstractions", "RalfAI.Abstractions\RalfAI.Abstractions.csproj", "{478ADC22-33A8-4133-A9C0-2A5BDE008A41}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Core", "RalfAI.Core\RalfAI.Core.csproj", "{23004547-5408-4005-B3B3-59EBEC3BCAD4}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Contexts.DotNet", "RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj", "{D41F9A75-2386-4230-B70C-69BEF3146A80}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Contexts.Markdown", "RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj", "{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Contexts.HtmlApps", "RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj", "{8958CDA5-518E-4C01-9112-DD83FA40CC76}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI", "RalfAI\RalfAI.csproj", "{75370B56-B20C-4084-88E0-4F23568E9598}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|x64 = Debug|x64
		Debug|x86 = Debug|x86
		Release|Any CPU = Release|Any CPU
		Release|x64 = Release|x64
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|x64.ActiveCfg = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|x64.Build.0 = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|x86.ActiveCfg = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|x86.Build.0 = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|Any CPU.Build.0 = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|x64.ActiveCfg = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|x64.Build.0 = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|x86.ActiveCfg = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|x86.Build.0 = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|x64.ActiveCfg = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|x64.Build.0 = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|x86.ActiveCfg = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|x86.Build.0 = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|Any CPU.Build.0 = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|x64.ActiveCfg = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|x64.Build.0 = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|x86.ActiveCfg = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|x86.Build.0 = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|x64.ActiveCfg = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|x64.Build.0 = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|x86.ActiveCfg = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|x86.Build.0 = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|Any CPU.Build.0 = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|x64.ActiveCfg = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|x64.Build.0 = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|x86.ActiveCfg = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|x86.Build.0 = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|x64.ActiveCfg = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|x64.Build.0 = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|x86.ActiveCfg = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|x86.Build.0 = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|Any CPU.Build.0 = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|x64.ActiveCfg = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|x64.Build.0 = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|x86.ActiveCfg = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|x86.Build.0 = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|x64.ActiveCfg = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|x64.Build.0 = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|x86.ActiveCfg = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|x86.Build.0 = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|Any CPU.Build.0 = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|x64.ActiveCfg = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|x64.Build.0 = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|x86.ActiveCfg = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|x86.Build.0 = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|x64.ActiveCfg = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|x64.Build.0 = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|x86.ActiveCfg = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|x86.Build.0 = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|Any CPU.Build.0 = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|x64.ActiveCfg = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|x64.Build.0 = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|x86.ActiveCfg = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|x86.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal


---

### TOOL — FileSystem.WriteFile — 01:13:57

args: relativePath=assets/ralfai-logo.svg, content=<svg xmlns="http://www.w3.org/2000/svg" width="960" height="320" viewBox="0 0 960 320" role="img" aria-labelledby="title desc">
  <title id="title">RalfAI logo</title>
  <desc id="desc">A gradient hexagonal mark containing a stylized R and connected AI nodes, followed by the RalfAI wordmark.</desc>
  <defs>
    <linearGradient id="brand" x1="48" y1="36" x2="272" y2="284" gradientUnits="userSpaceOnUse">
      <stop stop-color="#28C7FA"/>
      <stop offset="0.48" stop-color="#6366F1"/>
      <stop offset="1" stop-color="#A855F7"/>
    </linearGradient>
    <linearGradient id="word" x1="342" y1="98" x2="842" y2="226" gradientUnits="userSpaceOnUse">
      <stop stop-color="#E6F7FF"/>
      <stop offset="0.6" stop-color="#C7D2FE"/>
      <stop offset="1" stop-color="#D8B4FE"/>
    </linearGradient>
    <filter id="glow" x="-30%" y="-30%" width="160%" height="160%">
      <feGaussianBlur stdDeviation="8" result="blur"/>
      <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
    </filter>
  </defs>

  <!-- Transparent canvas; suitable for light and dark README themes. -->
  <path d="M160 24 278 92v136l-118 68L42 228V92z" fill="#0B1020" stroke="url(#brand)" stroke-width="8"/>
  <path d="M160 44 260 102v116l-100 58-100-58V102z" fill="none" stroke="#FFFFFF" stroke-opacity=".09" stroke-width="2"/>

  <!-- Stylized R: terminal-like stem and forward-moving leg. -->
  <path d="M100 226V92h69c44 0 69 21 69 57 0 27-14 45-39 53l44 50h-43l-53-62h-31v36zm16-105v40h50c24 0 36-6 36-20s-12-20-36-20z"
        fill="url(#brand)" filter="url(#glow)"/>

  <!-- Neural/circuit nodes, integrated into the R bowl. -->
  <g stroke="#8BE9FD" stroke-width="4" stroke-linecap="round" fill="none">
    <path d="M170 104 214 78M206 142h43M184 183l45 31"/>
  </g>
  <g fill="#F5FBFF" stroke="url(#brand)" stroke-width="4">
    <circle cx="170" cy="104" r="7"/><circle cx="214" cy="78" r="8"/>
    <circle cx="206" cy="142" r="7"/><circle cx="249" cy="142" r="8"/>
    <circle cx="184" cy="183" r="7"/><circle cx="229" cy="214" r="8"/>
  </g>

  <!-- Wordmark uses a system font stack and remains editable in the SVG source. -->
  <text x="330" y="210" fill="#172033" font-family="Inter, Segoe UI, Arial, sans-serif" font-size="142" font-weight="750" letter-spacing="-7">Ralf</text>
  <text x="642" y="210" fill="url(#brand)" font-family="Inter, Segoe UI, Arial, sans-serif" font-size="142" font-weight="800" letter-spacing="-7">AI</text>
  <circle cx="868" cy="102" r="10" fill="#28C7FA"/>
  <circle cx="895" cy="80" r="6" fill="#A855F7"/>
  <path d="M868 102 895 80" stroke="url(#brand)" stroke-width="4" stroke-linecap="round"/>
</svg>

result: OK — written assets/ralfai-logo.svg (2657 chars)

---

### TOOL — FileSystem.WriteFile — 01:13:57

args: relativePath=assets/ralfai-icon.svg, content=<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 320 320" role="img" aria-labelledby="title">
  <title id="title">RalfAI icon</title>
  <defs>
    <linearGradient id="brand" x1="48" y1="36" x2="272" y2="284" gradientUnits="userSpaceOnUse">
      <stop stop-color="#28C7FA"/><stop offset=".48" stop-color="#6366F1"/><stop offset="1" stop-color="#A855F7"/>
    </linearGradient>
    <filter id="glow" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="7" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
  </defs>
  <path d="M160 18 284 89v142l-124 71L36 231V89z" fill="#0B1020" stroke="url(#brand)" stroke-width="9"/>
  <path d="M97 230V86h74c47 0 74 23 74 61 0 29-15 48-42 57l47 54h-46l-57-67h-33v39zm17-112v43h54c26 0 39-7 39-22s-13-21-39-21z" fill="url(#brand)" filter="url(#glow)"/>
  <g stroke="#8BE9FD" stroke-width="4" stroke-linecap="round"><path d="m171 99 47-28M210 141h46m-61 46 48 34"/></g>
  <g fill="#F5FBFF" stroke="url(#brand)" stroke-width="4"><circle cx="171" cy="99" r="8"/><circle cx="218" cy="71" r="9"/><circle cx="210" cy="141" r="8"/><circle cx="256" cy="141" r="9"/><circle cx="195" cy="187" r="8"/><circle cx="243" cy="221" r="9"/></g>
</svg>

result: OK — written assets/ralfai-icon.svg (1267 chars)

---

### TOOL — FileSystem.WriteFile — 01:14:21

args: relativePath=src/LogoRasterizer/LogoRasterizer.csproj, content=<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>

result: OK — written src/LogoRasterizer/LogoRasterizer.csproj (238 chars)

---

### TOOL — FileSystem.WriteFile — 01:14:21

args: relativePath=src/LogoRasterizer/Program.cs, content=using System.IO.Compression;
using System.Text;

const int size = 512;
var pixels = new byte[size * size * 4];

static (byte R, byte G, byte B) Gradient(double t)
{
    t = Math.Clamp(t, 0, 1);
    if (t < .48)
    {
        var u = t / .48;
        return ((byte)(40 + (99 - 40) * u), (byte)(199 + (102 - 199) * u), (byte)(250 + (241 - 250) * u));
    }
    var v = (t - .48) / .52;
    return ((byte)(99 + (168 - 99) * v), (byte)(102 + (85 - 102) * v), (byte)(241 + (247 - 241) * v));
}

void Blend(int x, int y, byte r, byte g, byte b, byte a = 255)
{
    if ((uint)x >= size || (uint)y >= size) return;
    var i = (y * size + x) * 4;
    var alpha = a / 255d;
    pixels[i] = (byte)(r * alpha + pixels[i] * (1 - alpha));
    pixels[i + 1] = (byte)(g * alpha + pixels[i + 1] * (1 - alpha));
    pixels[i + 2] = (byte)(b * alpha + pixels[i + 2] * (1 - alpha));
    pixels[i + 3] = (byte)Math.Min(255, a + pixels[i + 3] * (1 - alpha));
}

bool Inside((double X, double Y)[] p, double x, double y)
{
    var inside = false;
    for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
        if ((p[i].Y > y) != (p[j].Y > y) && x < (p[j].X - p[i].X) * (y - p[i].Y) / (p[j].Y - p[i].Y) + p[i].X)
            inside = !inside;
    return inside;
}

void Polygon((double X, double Y)[] polygon, Func<int, int, (byte R, byte G, byte B, byte A)> color)
{
    for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
        if (Inside(polygon, x + .5, y + .5)) { var c = color(x, y); Blend(x, y, c.R, c.G, c.B, c.A); }
}

void Line(int x0, int y0, int x1, int y1, int width, byte r, byte g, byte b)
{
    var dx = x1 - x0; var dy = y1 - y0; var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
    for (var i = 0; i <= steps; i++)
    {
        var x = x0 + dx * i / Math.Max(1, steps); var y = y0 + dy * i / Math.Max(1, steps);
        for (var yy = -width; yy <= width; yy++) for (var xx = -width; xx <= width; xx++) if (xx * xx + yy * yy <= width * width) Blend(x + xx, y + yy, r, g, b);
    }
}

void Circle(int cx, int cy, int radius, byte r, byte g, byte b)
{
    for (var y = -radius; y <= radius; y++) for (var x = -radius; x <= radius; x++) if (x * x + y * y <= radius * radius) Blend(cx + x, cy + y, r, g, b);
}

var outer = new[] { (256d, 18d), (462d, 137d), (462d, 375d), (256d, 494d), (50d, 375d), (50d, 137d) };
var inner = new[] { (256d, 31d), (450d, 143d), (450d, 369d), (256d, 481d), (62d, 369d), (62d, 143d) };
Polygon(outer, (x, y) => { var c = Gradient((x + y) / 1024d); return (c.R, c.G, c.B, (byte)255); });
Polygon(inner, (_, _) => ((byte)11, (byte)16, (byte)32, (byte)255));

// Geometric R, deliberately independent of installed fonts.
Polygon([(145d, 370d), (145d, 137d), (179d, 137d), (179d, 370d)], (x, y) => { var c = Gradient((x + y) / 600d); return (c.R, c.G, c.B, (byte)255); });
Polygon([(170d, 137d), (286d, 137d), (330d, 154d), (353d, 190d), (353d, 227d), (331d, 261d), (289d, 276d), (178d, 276d), (178d, 239d), (280d, 239d), (303d, 230d), (315d, 211d), (303d, 192d), (280d, 183d), (170d, 183d)], (x, y) => { var c = Gradient((x + y) / 600d); return (c.R, c.G, c.B, (byte)255); });
Polygon([(254d, 259d), (303d, 250d), (384d, 379d), (331d, 379d)], (x, y) => { var c = Gradient((x + y) / 650d); return (c.R, c.G, c.B, (byte)255); });

Line(286, 154, 363, 108, 3, 139, 233, 253); Line(318, 211, 405, 211, 3, 139, 233, 253); Line(286, 266, 370, 326, 3, 139, 233, 253);
foreach (var (x, y) in new[] { (286,154), (363,108), (318,211), (405,211), (286,266), (370,326) }) { Circle(x, y, 13, 99, 102, 241); Circle(x, y, 8, 245, 251, 255); }

Directory.CreateDirectory("../../assets");
WritePng("../../assets/ralfai-icon.png", size, size, pixels);

static void WritePng(string path, int width, int height, byte[] rgba)
{
    using var file = File.Create(path);
    file.Write([137, 80, 78, 71, 13, 10, 26, 10]);
    using var header = new MemoryStream();
    WriteInt(header, width); WriteInt(header, height); header.Write([8, 6, 0, 0, 0]);
    Chunk(file, "IHDR", header.ToArray());
    using var raw = new MemoryStream();
    for (var y = 0; y < height; y++) { raw.WriteByte(0); raw.Write(rgba, y * width * 4, width * 4); }
    using var compressed = new MemoryStream();
    using (var z = new ZLibStream(compressed, CompressionLevel.SmallestSize, true)) raw.WriteTo(z);
    Chunk(file, "IDAT", compressed.ToArray()); Chunk(file, "IEND", []);
}

static void Chunk(Stream output, string type, byte[] data)
{
    WriteInt(output, data.Length); var name = Encoding.ASCII.GetBytes(type); output.Write(name); output.Write(data);
    var crcData = name.Concat(data).ToArray(); WriteInt(output, unchecked((int)Crc32(crcData)));
}
static void WriteInt(Stream s, int value) => s.Write([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
static uint Crc32(byte[] data)
{
    uint crc = 0xffffffff;
    foreach (var b in data) { crc ^= b; for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xedb88320 ^ (crc >> 1) : crc >> 1; }
    return crc ^ 0xffffffff;
}

result: OK — written src/LogoRasterizer/Program.cs (5001 chars)

---

### TOOL — DotNet.RunCommand — 01:14:26

args: workingDirectory=src/LogoRasterizer, arguments=run --framework net10.0
result: $ dotnet run --framework net10.0
Working dir: D:\Dev\2026\repos\RalfAI\src\LogoRasterizer
Exit code: 0


---

### TOOL — Image.GetInfo — 01:14:28

args: relativePath=assets/ralfai-icon.png
result: 512x512 px, format: PNG, 11693 bytes

---

### TOOL — FileSystem.WriteFile — 01:14:35

args: relativePath=src/LogoRasterizer/Program.cs, content=var path = Path.GetFullPath("../../README.md");
var markdown = File.ReadAllText(path);
const string heading = "# RalfAI\n";
const string branding = "\n<p align=\"center\">\n  <img src=\"assets/ralfai-logo.svg\" alt=\"RalfAI — autonomous coding agent\" width=\"680\">\n</p>\n\n";
if (!markdown.Contains("assets/ralfai-logo.svg", StringComparison.Ordinal))
    File.WriteAllText(path, markdown.Replace(heading, heading + branding, StringComparison.Ordinal));

result: OK — written src/LogoRasterizer/Program.cs (457 chars)

---

### TOOL — DotNet.RunCommand — 01:14:40

args: workingDirectory=src/LogoRasterizer, arguments=run --framework net10.0
result: $ dotnet run --framework net10.0
Working dir: D:\Dev\2026\repos\RalfAI\src\LogoRasterizer
Exit code: 0


---

### TOOL — FileSystem.DeleteFile — 01:14:42

args: relativePath=src/LogoRasterizer/Program.cs
result: OK — deleted src/LogoRasterizer/Program.cs

---

### TOOL — FileSystem.DeleteFile — 01:14:45

args: relativePath=src/LogoRasterizer/LogoRasterizer.csproj
result: OK — deleted src/LogoRasterizer/LogoRasterizer.csproj

---

### TOOL — DotNet.RunBuild — 01:14:52

args: projectPath=src/RalfAI.sln
result: $ dotnet build "D:\Dev\2026\repos\RalfAI\src\RalfAI.sln"
Working dir: D:\Dev\2026\repos\RalfAI\src
Exit code: 0
--- STDOUT ---
  Determining projects to restore...
  All projects are up-to-date for restore.
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Core\RalfAI.Core.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI\RalfAI.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Abstractions\RalfAI.Abstractions.csproj]
  RalfAI.Abstractions -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.dll
  RalfAI.Core -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.dll
  RalfAI.Contexts.DotNet -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
  RalfAI.Contexts.Markdown -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
  RalfAI.Contexts.HtmlApps -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
  RalfAI -> D:\Dev\2026\repos\RalfAI\src\RalfAI\bin\Debug\net10.0\ralf.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:04.91


---

### TOOL — DotNet.RunTest — 01:14:56

args: projectPath=src/RalfAI.sln
result: $ dotnet test "D:\Dev\2026\repos\RalfAI\src\RalfAI.sln" --logger "console;verbosity=normal"
Working dir: D:\Dev\2026\repos\RalfAI\src
Exit code: 0
--- STDOUT ---
  Determining projects to restore...
  All projects are up-to-date for restore.


---

### TOOL — FileSystem.ReadFile — 01:14:58

args: relativePath=README.md
result: # RalfAI

<p align="center">
  <img src="assets/ralfai-logo.svg" alt="RalfAI — autonomous coding agent" width="680">
</p>


An autonomous coding agent CLI. Point it at a folder, pick a context (.NET, Markdown docs,
or a static HTML/CSS/JS app), and it builds/edits files, runs `dotnet build`/`dotnet test`,
fetches URLs, previews HTML apps over local HTTP, and optionally talks to you (speech in/out)
— all from the terminal. Built on [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/),
so it can talk to Azure AI Foundry, native OpenAI, or Anthropic Claude — each through that
provider's own official .NET SDK, picked by one config setting.

For how it's built internally (project layout, the provider/plugin abstractions, the turn
loop), see [ARCHITECTURE.md](ARCHITECTURE.md).

## Install

RalfAI is a [.NET global tool](https://learn.microsoft.com/dotnet/core/tools/global-tools),
installed as the `ralf` command. From the `src/` folder of this repo:

```
dotnet pack RalfAI/RalfAI.csproj -c Release -o ./nupkg
dotnet tool install --global --add-source ./nupkg RalfAI
```

After that, `ralf` is available from any directory, in any terminal.

To pick up code changes after editing the source, reinstall:

```
dotnet pack RalfAI/RalfAI.csproj -c Release -o ./nupkg
dotnet tool update --global --add-source ./nupkg RalfAI
```

To remove it: `dotnet tool uninstall --global RalfAI`.

## Configure

### Where the config file lives

`dotnet tool update` overwrites everything the package ships, **including a bundled
`appsettings.json`** — so keeping real secrets in the file next to the binary means losing
them on every update. RalfAI resolves which config file to load in this order:

1. `--config <path>` CLI flag — wins outright.
2. `RALFAI_CONFIG_PATH` environment variable — an absolute path to a file living *outside*
   the tool's install directory, so it survives reinstalls. Set it once, persistently:

   ```powershell
   [Environment]::SetEnvironmentVariable("RALFAI_CONFIG_PATH", "D:\Configurations\RalfAI\config.json", "User")
   ```

3. `appsettings.json` next to the installed binary — the zero-config fallback. Ships with
   placeholder values only (see [config.example.json](config.example.json) for the same
   template, meant to be copied to your `RALFAI_CONFIG_PATH` location and filled in).

Whichever file loads, individual settings can still be overridden with `RALFAI_`-prefixed
environment variables (e.g. `RALFAI_AzureAIFoundry__ApiKey`).

### Choosing a provider

```json
{
    "AIProvider": "foundry",
    "AzureAIFoundry": { "Endpoint": "...", "ApiKey": "...", "ModelId": "..." },
    "OpenAI":         { "ApiKey": "...", "ModelId": "gpt-5.1" },
    "Anthropic":      { "ApiKey": "...", "ModelId": "claude-sonnet-4-5" }
}
```

`AIProvider` picks which section is actually used — `"foundry"` (default), `"openai"`, or
`"anthropic"`. Only the section for the active provider needs real values; the others can be
left as placeholders. Override per-run with `RALFAI_AIProvider=openai` without touching the
file.

**Foundry / Azure OpenAI** — `Endpoint` accepts a plain resource host
(`https://<resource>.openai.azure.com`), a Foundry project URL
(`.../api/projects/<name>` — the path is stripped automatically, only the host is used), or
a full `.../chat/completions` URL.

**Multiple models per Foundry context** — instead of one flat `ModelId`, define named models
and map contexts to them:

```json
"AzureAIFoundry": {
    "Endpoint": "...", "ApiKey": "...",
    "Models": {
        "gpt":      { "ModelId": "gpt-5.6-sol" },
        "deepseek": { "ModelId": "deepseek-r1" }
    },
    "ContextModels": { "dotnet": "gpt", "markdown": "deepseek" },
    "DefaultModel": "gpt"
}
```

**Voice (optional)** — add an `AzureSpeech` section to enable dictation (`F2`) and
read-aloud (`F3`):

```json
"AzureSpeech": {
    "Region": "your-region",
    "ApiKey": "...",
    "RecognitionLanguage": "en-US",
    "VoiceName": "en-US-JennyNeural"
}
```

**AI image generation (optional)** — add `ImageModelId` under the active provider's section
(`AzureAIFoundry` or `OpenAI`) to enable the `Image.GenerateAI` tool (logos, icons,
illustrations from a text prompt):

```json
"AzureAIFoundry": { "Endpoint": "...", "ApiKey": "...", "ModelId": "...", "ImageModelId": "dall-e-3" },
"OpenAI":         { "ApiKey": "...", "ModelId": "gpt-5.1", "ImageModelId": "gpt-image-1" }
```

Leave `ImageModelId` empty/absent to leave the tool disabled. Anthropic has no
image-generation API, so `Image.GenerateAI` is unavailable when `AIProvider` is `anthropic`
regardless of config.

## Use

```console
ralf                          # opens a folder picker (remembers the last folder)
ralf <target-folder>          # work in a specific folder
ralf <target-folder> --context html
ralf --context markdown --debug
ralf --config D:\Configurations\RalfAI\config.json
ralf --list-contexts          # show available contexts and exit
```

If no folder is given and none is remembered yet, a full-screen file browser opens
(`↑↓` navigate, `Enter` open folder, `⌫` up, `F9` use folder, `F2` rename, `F7` new folder,
`F8`/`Del` delete, `F3` view file, `F4` open file in an external editor, `F10`/`Esc` cancel).
The same browser opens on `F7` during a session (see the key table below) to switch folders.

`F4` runs the command in the `Editor` config key (`RALFAI_Editor` env override), defaulting to
`code.exe` (VS Code) if unset — set it to whatever editor/IDE you use.

Once running, type a message (paste freely, multi-line) and:

| Key | Action |
| --- | --- |
| `Ctrl+Enter` | send the message |
| `Enter` | newline (multi-line compose) |
| `Esc` | clear the current draft |
| `F1` | full-screen command reference |
| `F2` | dictate via microphone *(needs AzureSpeech)* |
| `F3` | read the last answer aloud *(needs AzureSpeech, Esc interrupts)* |
| `F5` | clear the screen |
| `F6` | switch generation context (`↑↓`/`Enter`/`Esc`) |
| `F7` | switch target folder (file browser) |
| `F8` | toggle debug messages (tool calls, rate-limit notices) |
| `F10` / `Ctrl+Q` | quit |
| `Ctrl+C` | interrupt the current turn (press twice to force-quit) |
| `Esc` / `Ctrl+X` | while the agent is working: same interrupt as `Ctrl+C` |

The whole session runs in the terminal's alternate screen buffer — your normal scrollback
is untouched and restored on exit.

## Contexts

| Key | What it builds |
| --- | --- |
| `dotnet` | .NET 10 projects via the `dotnet` CLI — builds and runs tests until green |
| `markdown` | A set of Markdown documentation files |
| `html` | Static HTML/CSS/JS apps — Tailwind via CDN, vanilla JS, one ES class per file, previewed over local HTTP |

`dotnet` and `html` both use a `src/` (code) · `ops/` (config/deployment) top-level layout
inside the target folder. Documentation is markdown files at the target folder **root** —
never a `docs/` subfolder — using the same fixed set of names as this repo:
`README.md`, `USAGE.md`, `ARCHITECTURE.md`, `ROADMAP.md` (only the ones that fit the size of
what's being built), plus `PROMPTS.md` — a running, numbered log of every objective the agent
was given for that project, appended to before any other work on a turn. Unlike the other
four, `PROMPTS.md` isn't optional past the first objective: it's what makes a project's
history traceable to the requests that drove it. The `markdown` context follows the same
convention when the objective is documenting a software project (`PROMPTS.md` always, the
other four when applicable).

The `html` context also registers a `StaticServer` tool: the agent starts a local Kestrel
server rooted at `src/` and reports the preview URL, since browsers block
`<script type="module">` imports over `file://`.

Each target folder remembers its own context (and debug setting) in
`<folder>/.ralfai/config.yaml`, so re-running `ralf <folder>` — or letting it auto-resume
with no argument — picks the same context back up. Priority when resolving: `--context`
flag → the folder's `config.yaml` → `AgentContext` in the config file → built-in default
(`markdown`).

The conversation transcript for a folder lives at `<folder>/.ralfai/session.md` and is
replayed automatically the next time you point RalfAI at that folder.

Adding a new context, provider, or tool is covered in [ARCHITECTURE.md](ARCHITECTURE.md).


---

### TOOL — FileSystem.ListFiles — 01:14:58

args: subdirectory=assets
result: [FILE] assets\ralfai-icon.png
[FILE] assets\ralfai-icon.svg
[FILE] assets\ralfai-logo.svg

---

### TOOL — FileSystem.ListFiles — 01:14:58

args: subdirectory=src/LogoRasterizer
result: [DIR]  src\LogoRasterizer\bin
[DIR]  src\LogoRasterizer\bin\Debug
[DIR]  src\LogoRasterizer\bin\Debug\net10.0
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.deps.json
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.dll
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.exe
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.pdb
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.runtimeconfig.json
[DIR]  src\LogoRasterizer\obj
[DIR]  src\LogoRasterizer\obj\Debug
[DIR]  src\LogoRasterizer\obj\Debug\net10.0
[FILE] src\LogoRasterizer\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\LogoRasterizer\obj\Debug\net10.0\apphost.exe
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.AssemblyInfo.cs
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.AssemblyInfoInputs.cache
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.assets.cache
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.csproj.CoreCompileInputs.cache
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.csproj.FileListAbsolute.txt
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.dll
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.genruntimeconfig.cache
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.GlobalUsings.g.cs
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.pdb
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.sourcelink.json
[DIR]  src\LogoRasterizer\obj\Debug\net10.0\ref
[FILE] src\LogoRasterizer\obj\Debug\net10.0\ref\LogoRasterizer.dll
[DIR]  src\LogoRasterizer\obj\Debug\net10.0\refint
[FILE] src\LogoRasterizer\obj\Debug\net10.0\refint\LogoRasterizer.dll
[FILE] src\LogoRasterizer\obj\LogoRasterizer.csproj.nuget.dgspec.json
[FILE] src\LogoRasterizer\obj\LogoRasterizer.csproj.nuget.g.props
[FILE] src\LogoRasterizer\obj\LogoRasterizer.csproj.nuget.g.targets
[FILE] src\LogoRasterizer\obj\project.assets.json
[FILE] src\LogoRasterizer\obj\project.nuget.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.deps.json
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.deps.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.dll
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.dll

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.exe
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.exe

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.pdb
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.pdb

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.runtimeconfig.json
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.runtimeconfig.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.dgspec.json
result: OK — deleted src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.dgspec.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.g.props
result: OK — deleted src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.g.props

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.g.targets
result: OK — deleted src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.g.targets

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/project.assets.json
result: OK — deleted src/LogoRasterizer/obj/project.assets.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/project.nuget.cache
result: OK — deleted src/LogoRasterizer/obj/project.nuget.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/apphost.exe
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/apphost.exe

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.AssemblyInfo.cs
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.AssemblyInfo.cs

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.AssemblyInfoInputs.cache
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.AssemblyInfoInputs.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.assets.cache
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.assets.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.csproj.CoreCompileInputs.cache
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.csproj.CoreCompileInputs.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.csproj.FileListAbsolute.txt
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.csproj.FileListAbsolute.txt

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.dll
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.dll

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.GeneratedMSBuildEditorConfig.editorconfig
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.GeneratedMSBuildEditorConfig.editorconfig

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.genruntimeconfig.cache
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.genruntimeconfig.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.GlobalUsings.g.cs
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.GlobalUsings.g.cs

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.pdb
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.pdb

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.sourcelink.json
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.sourcelink.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/ref/LogoRasterizer.dll
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/ref/LogoRasterizer.dll

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/refint/LogoRasterizer.dll
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/refint/LogoRasterizer.dll

---

### AGENT — 01:15:11

Creo un’identità visiva minimale che unisce la lettera **R**, un nodo neurale e i colori blu/viola associati a .NET e AI. Salverò il sorgente vettoriale nel repository, produrrò una versione PNG e la integrerò nel README senza alterare la struttura applicativa.
Logo di **RalfAI** creato e integrato.

### File aggiunti

- `assets/ralfai-logo.svg` — logo completo vettoriale
- `assets/ralfai-icon.svg` — icona vettoriale quadrata
- `assets/ralfai-icon.png` — icona PNG 512×512 con trasparenza

Il design combina:

- una **R** geometrica;
- connessioni neurali/circuitali;
- palette blu, indaco e viola;
- sfondo scuro adatto sia a temi chiari sia scuri.

Il logo completo è ora visualizzato all’inizio di `README.md`. La richiesta è stata registrata anche in `PROMPTS.md`.

### Verifiche

- `dotnet build src/RalfAI.sln`: completata, **0 errori e 0 warning**
- `dotnet test src/RalfAI.sln`: completato con exit code **0**
- PNG verificato: **512×512**, formato valido, 11.693 byte.

---

### USER — 17:16:55

Vorrei che l'editor da CLI permetta di editare la richiesta con maggior ricchezza....non posso cancellare ad esempio...e quando incollo da clipboard, perdo caratteri. usa i filesd claude per la storia dello sviluppo. E convertila in una tua storia

---

### TOOL — FileSystem.ReadFile — 17:16:59

args: relativePath=PROMPTS.md
result: # Prompts

Log cronologico e numerato delle richieste che hanno guidato la realizzazione di RalfAI — la
stessa convenzione (`PROMPTS.md` alla radice, aggiornato a ogni obiettivo, prima di qualunque
altra azione) che i context `dotnet`/`html`/`markdown` ora applicano a ogni progetto che
generano. Vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
[USAGE.md](USAGE.md#log-dei-prompt-promptsmd).

> **Nota sulla provenienza.** Questo file è stato scritto da un assistente di coding (Claude
> Code) che lavorava su questo repo via git — non da `ralf` in esecuzione su se stesso. Le voci
> 1–8 riguardano funzionalità arrivate in sessioni precedenti a questa: la git history parte da
> un "Initial Commit" già completo (vedi [ROADMAP.md](ROADMAP.md)), quindi il testo esatto di
> quelle richieste non è disponibile — sono **ricostruite** a partire dal changelog in
> `ROADMAP.md` e marcate `[ricostruito]`. Le voci dalla 9 in poi sono **verbatim**, prese da
> questa conversazione.

## 1. Realizzare un agente di coding autonomo per .NET `[ricostruito]`

> Costruisci un agente CLI autonomo che, data una cartella e un obiettivo, scaffoldi un
> progetto .NET con `dotnet new`, scriva i file sorgente, compili con `dotnet build` e testi
> con `dotnet test`, correggendo finché non vanno a buon fine.

Realizzato: `RalfAIApp` (Spectre.Console) + `DotNetContext` come unico context, motore basato
su Microsoft Agent Framework contro Azure AI Foundry.

## 2. Rendere il context non hardcoded `[ricostruito]`

> Il context .NET è cablato direttamente in `Program.cs` — rendilo selezionabile, così in
> futuro si possono aggiungere altri stack senza toccare l'app.

Realizzato: `AgentContextFactory`/`ContextRegistry`, flag `--context`, chiave `AgentContext` in
config/env, `dotnet` come default (poi diventato `markdown`).

## 3. Aggiungere Speech-to-Text / Text-to-Speech `[ricostruito]`

> Aggiungi la possibilità di dettare i messaggi a voce e farsi leggere le risposte ad alta
> voce, usando Azure AI Speech.

Realizzato: `SpeechService` (motore condiviso), comandi `/voice`/`/speak` in console,
opzionale via sezione `AzureSpeech` in config — nessun impatto sul resto dell'app se assente.

## 4. Estrarre un motore condiviso dalla console `[ricostruito]`

> La logica dell'agente è mischiata con il rendering Spectre.Console — separala in un motore
> riusabile da altri frontend futuri.

Realizzato: `AgentSession` (poi rinominato `AgentEngine`) in `RalfAI.Core`, nessuna dipendenza
UI, eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice`.

## 5. Distribuire come global tool e rendere la cartella opzionale `[ricostruito]`

> Pacchettizza RalfAI come .NET global tool (`ralf`) installabile da riga di comando, e
> permetti di avviarlo senza specificare la cartella, con un picker.

Realizzato: `PackAsTool`/`ToolCommandName=ralf`, `FileBrowser` + `LastFolderStore` per il
picker e la memoria dell'ultima cartella, nuovi context `markdown` (default) e `html`.

## 6. Sessione interattiva a schermo intero `[ricostruito]`

> Sostituisci il prompt a riga singola con un'interfaccia a schermo intero, composizione
> multi-riga e scorciatoie da tastiera invece dei comandi testuali.

Realizzato: screen buffer alternato del terminale, `Ctrl+Enter` per inviare, tasti `F1`-`F10`
(help, dettatura, lettura, pulizia schermo, cambio context/cartella, toggle debug, quit).

## 7. Supportare più provider AI `[ricostruito]`

> Non vincolare RalfAI ad Azure AI Foundry — fai in modo che possa parlare anche con OpenAI
> nativo e Anthropic Claude, ciascuno tramite il proprio SDK ufficiale.

Realizzato: `AgentEngine.BuildChatClient`, switch su `AIProvider` (`foundry`/`openai`/
`anthropic`), sezioni di config dedicate per provider.

## 8. Scrivere la documentazione del progetto `[ricostruito]`

> Scrivi README, architettura, guida all'uso e roadmap per RalfAI.

Realizzato: `README.md`, `ARCHITECTURE.md`, `USE.md` (poi rinominato `USAGE.md`, voce 11),
`ROADMAP.md`.

---

## 9. "Il file USE riporta la app desktop che non c'è"

`USE.md` e `ROADMAP.md` descrivevano `RalfAI.Desktop`/`RalfAI.ChatUI` (WinForms + WebView2 /
Blazor Hybrid) come già implementati, ma quei progetti non esistevano né nella solution né sul
filesystem. Confermato scavando nel codice (nessun riferimento reale, `RalfAI.sln` non li
include) e riscritto `USE.md` da zero contro il comportamento effettivo (global tool `ralf`,
cartella opzionale, default context `markdown`, sessione a schermo intero, `.ralfai/` invece di
`.chat/`).

## 10. "Anche la roadmap è disallineata"

Stesso problema in `ROADMAP.md`: la sezione "Interfaccia Desktop" dichiarava il frontend
grafico come fatto. Sostituita con voci che riflettono lo stato reale (global tool, file
browser, sessione a schermo intero, motore `AgentEngine`, provider multipli); l'interfaccia
grafica spostata in "📋 Pianificato" come idea aperta, non decisa.

## 11. "Puoi fare un aggiornamento ai contesti? I files md stanno sempre in radice, non in docs. In generale sempre: README USAGE ARCHITECTURE ROADMAP. Puoi aggiungere un supporto alla manipolazione di immagini jpg e png"

Due richieste in una:
- Rimosso `docs/` dal layout dei context `dotnet`/`html`: documentazione sempre alla radice
  della cartella target, con il set fisso `README.md`/`USAGE.md`/`ARCHITECTURE.md`/
  `ROADMAP.md` — rinominato anche `USE.md` → `USAGE.md` in questo repo, per coerenza.
- Nuovo `ImagePlugin` (`SixLabors.ImageSharp`): `Image.GetInfo`/`Resize`/`Crop`/`Rotate`/
  `Flip`/`Grayscale`/`ConvertFormat`, tool engine-level sempre disponibili in ogni context,
  nessuna configurazione richiesta.

## 12. "Per un progetto puoi anche generare un logo?"

Chiarito che si trattava di generazione AI (non disegno programmatico via forme/testo).
Aggiunti `ImageGenerationService` e `ImageGenerationPlugin` (`Image.GenerateAI`): genera
un'immagine da un prompt testuale via DALL-E/gpt-image su Azure AI Foundry o OpenAI, salvata
come JPG/PNG. Opzionale via `ImageModelId` sotto la sezione del provider attivo; non
disponibile con `AIProvider` `anthropic` (nessuna API di generazione immagini).

## 13. "Inoltre, quando sta generando, puoi dare un dettaglio di quello che sta pensando scrivendo generando? Come un log?"

Nessun meccanismo nuovo: la visibilità delle tool-call già esistente (descrizione nello
spinner mentre il tool gira, righe `⏺`/`⎿` con `F8`/`--debug` attivo) copre già questo caso —
la `[Description]` di `Image.GenerateAI` è stata scritta per essere informativa in quel
contesto.

## 14. "Hai aggiornato anche il comando?"

Chiarito (dopo un giro di domanda) che si riferiva ai comandi `dotnet pack`/`dotnet tool
install` in `README.md`. Verificato con un pack di prova che non serve alcuna modifica:
`SixLabors.ImageSharp` viene incluso automaticamente come dipendenza transitiva via
`RalfAI.Core`, nessuna DLL nativa coinvolta.

## 15. "Puoi aggiungere anche un file md con i PROMPTS? che in qualche modo rispecchia la realizzazione intera del progetto stesso"

Aggiunto `PROMPTS.md` come quinto file nel set canonico di documentazione — l'unico non
opzionale: ogni context (`dotnet`, `html`, `markdown`) ora apre ogni turno con un passo "Log"
che ci scrive prima di qualunque altra azione, così la storia di un progetto generato resta
tracciabile fino alle richieste che l'ha guidata. Questo stesso file applica la convenzione
a ritroso a RalfAI.

## 16. "Aggiungi la possibilità di aprire un file durante la selezione: default code.exe, altrimenti variabile nel file di config"

Nuovo tasto `F4` in `FileBrowser` (usato sia al primo avvio senza cartella sulla riga di
comando, sia con `F7` durante una sessione): apre il file selezionato in un editor esterno via
`Process.Start(UseShellExecute: true)`, fire-and-forget. Comando risolto dalla nuova chiave di
config `Editor` (`RALFAI_Editor` per l'override via env), default `code.exe` (VS Code) se non
impostata — `FileBrowser` resta senza dipendenza diretta da `IConfiguration`, il comando viene
passato dai chiamanti (`Program.cs`, `RalfAIApp.SwitchFolder`).

## 17. "Puoi aggiornare l'help con un po' tutti i comandi e verificare che tutta la documentazione sia a posto?"

Due parti:
- **Help in-app**: `F1` (interattivo) ora elenca anche `Ctrl+C`/`Esc`/`Ctrl+X` (interruzione
  turno), prima mancanti. `/help` in modalità pipe non condivide più la tabella dei tasti
  funzione (inapplicabile lì) — mostra la tabella dei comandi testuali realmente disponibili
  (`/exe`, `/cancel`, `/voice`, `/speak`, `/clear`, `/switch-context`, `/switch-folder`,
  `/toggle-debug`, `/help`, `exit`). Nel farlo, trovato e corretto un bug: `/help` in pipe
  chiamava `Console.ReadKey` su stdin rediretto, che lancia `InvalidOperationException` —
  verificato con un test end-to-end (`echo "/help\nexit" | ralf`) che prima sarebbe andato in
  crash e ora non lo fa più.
- **Audit documentazione**: verificati tutti i link tra `README.md`/`USAGE.md`/
  `ARCHITECTURE.md`/`ROADMAP.md`/`PROMPTS.md`. Corretti tre anchor rotti (due in italiano
  dentro un `README.md` inglese, uno verso una sezione mai esistita in `ARCHITECTURE.md`), un
  link relativo a un file sorgente che risaliva di una cartella di troppo, e una voce di
  ROADMAP che citava una classe (`AgentContextFactory`) ormai rinominata (`ContextRegistry`).

## 18. "Vuoi creare il logo del progetto? Una immagine?" — "procedi"

Creazione di un logo moderno e minimale per RalfAI, in formato SVG sorgente e PNG per README,
con integrazione nel repository e aggiornamento della documentazione.


---

### TOOL — FileSystem.WriteFile — 17:17:19

args: relativePath=PROMPTS.md, content=# Prompts

Log cronologico e numerato delle richieste che hanno guidato la realizzazione di RalfAI — la
stessa convenzione (`PROMPTS.md` alla radice, aggiornato a ogni obiettivo, prima di qualunque
altra azione) che i context `dotnet`/`html`/`markdown` ora applicano a ogni progetto che
generano. Vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
[USAGE.md](USAGE.md#log-dei-prompt-promptsmd).

> **Nota sulla provenienza.** Questo file è stato scritto da un assistente di coding (Claude
> Code) che lavorava su questo repo via git — non da `ralf` in esecuzione su se stesso. Le voci
> 1–8 riguardano funzionalità arrivate in sessioni precedenti a questa: la git history parte da
> un "Initial Commit" già completo (vedi [ROADMAP.md](ROADMAP.md)), quindi il testo esatto di
> quelle richieste non è disponibile — sono **ricostruite** a partire dal changelog in
> `ROADMAP.md` e marcate `[ricostruito]`. Le voci dalla 9 in poi sono **verbatim**, prese da
> questa conversazione.

## 1. Realizzare un agente di coding autonomo per .NET `[ricostruito]`

> Costruisci un agente CLI autonomo che, data una cartella e un obiettivo, scaffoldi un
> progetto .NET con `dotnet new`, scriva i file sorgente, compili con `dotnet build` e testi
> con `dotnet test`, correggendo finché non vanno a buon fine.

Realizzato: `RalfAIApp` (Spectre.Console) + `DotNetContext` come unico context, motore basato
su Microsoft Agent Framework contro Azure AI Foundry.

## 2. Rendere il context non hardcoded `[ricostruito]`

> Il context .NET è cablato direttamente in `Program.cs` — rendilo selezionabile, così in
> futuro si possono aggiungere altri stack senza toccare l'app.

Realizzato: `AgentContextFactory`/`ContextRegistry`, flag `--context`, chiave `AgentContext` in
config/env, `dotnet` come default (poi diventato `markdown`).

## 3. Aggiungere Speech-to-Text / Text-to-Speech `[ricostruito]`

> Aggiungi la possibilità di dettare i messaggi a voce e farsi leggere le risposte ad alta
> voce, usando Azure AI Speech.

Realizzato: `SpeechService` (motore condiviso), comandi `/voice`/`/speak` in console,
opzionale via sezione `AzureSpeech` in config — nessun impatto sul resto dell'app se assente.

## 4. Estrarre un motore condiviso dalla console `[ricostruito]`

> La logica dell'agente è mischiata con il rendering Spectre.Console — separala in un motore
> riusabile da altri frontend futuri.

Realizzato: `AgentSession` (poi rinominato `AgentEngine`) in `RalfAI.Core`, nessuna dipendenza
UI, eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice`.

## 5. Distribuire come global tool e rendere la cartella opzionale `[ricostruito]`

> Pacchettizza RalfAI come .NET global tool (`ralf`) installabile da riga di comando, e
> permetti di avviarlo senza specificare la cartella, con un picker.

Realizzato: `PackAsTool`/`ToolCommandName=ralf`, `FileBrowser` + `LastFolderStore` per il
picker e la memoria dell'ultima cartella, nuovi context `markdown` (default) e `html`.

## 6. Sessione interattiva a schermo intero `[ricostruito]`

> Sostituisci il prompt a riga singola con un'interfaccia a schermo intero, composizione
> multi-riga e scorciatoie da tastiera invece dei comandi testuali.

Realizzato: screen buffer alternato del terminale, `Ctrl+Enter` per inviare, tasti `F1`-`F10`
(help, dettatura, lettura, pulizia schermo, cambio context/cartella, toggle debug, quit).

## 7. Supportare più provider AI `[ricostruito]`

> Non vincolare RalfAI ad Azure AI Foundry — fai in modo che possa parlare anche con OpenAI
> nativo e Anthropic Claude, ciascuno tramite il proprio SDK ufficiale.

Realizzato: `AgentEngine.BuildChatClient`, switch su `AIProvider` (`foundry`/`openai`/
`anthropic`), sezioni di config dedicate per provider.

## 8. Scrivere la documentazione del progetto `[ricostruito]`

> Scrivi README, architettura, guida all'uso e roadmap per RalfAI.

Realizzato: `README.md`, `ARCHITECTURE.md`, `USE.md` (poi rinominato `USAGE.md`, voce 11),
`ROADMAP.md`.

---

## 9. "Il file USE riporta la app desktop che non c'è"

`USE.md` e `ROADMAP.md` descrivevano `RalfAI.Desktop`/`RalfAI.ChatUI` (WinForms + WebView2 /
Blazor Hybrid) come già implementati, ma quei progetti non esistevano né nella solution né sul
filesystem. Confermato scavando nel codice (nessun riferimento reale, `RalfAI.sln` non li
include) e riscritto `USE.md` da zero contro il comportamento effettivo (global tool `ralf`,
cartella opzionale, default context `markdown`, sessione a schermo intero, `.ralfai/` invece di
`.chat/`).

## 10. "Anche la roadmap è disallineata"

Stesso problema in `ROADMAP.md`: la sezione "Interfaccia Desktop" dichiarava il frontend
grafico come fatto. Sostituita con voci che riflettono lo stato reale (global tool, file
browser, sessione a schermo intero, motore `AgentEngine`, provider multipli); l'interfaccia
grafica spostata in "📋 Pianificato" come idea aperta, non decisa.

## 11. "Puoi fare un aggiornamento ai contesti? I files md stanno sempre in radice, non in docs. In generale sempre: README USAGE ARCHITECTURE ROADMAP. Puoi aggiungere un supporto alla manipolazione di immagini jpg e png"

Due richieste in una:
- Rimosso `docs/` dal layout dei context `dotnet`/`html`: documentazione sempre alla radice
  della cartella target, con il set fisso `README.md`/`USAGE.md`/`ARCHITECTURE.md`/
  `ROADMAP.md` — rinominato anche `USE.md` → `USAGE.md` in questo repo, per coerenza.
- Nuovo `ImagePlugin` (`SixLabors.ImageSharp`): `Image.GetInfo`/`Resize`/`Crop`/`Rotate`/
  `Flip`/`Grayscale`/`ConvertFormat`, tool engine-level sempre disponibili in ogni context,
  nessuna configurazione richiesta.

## 12. "Per un progetto puoi anche generare un logo?"

Chiarito che si trattava di generazione AI (non disegno programmatico via forme/testo).
Aggiunti `ImageGenerationService` e `ImageGenerationPlugin` (`Image.GenerateAI`): genera
un'immagine da un prompt testuale via DALL-E/gpt-image su Azure AI Foundry o OpenAI, salvata
come JPG/PNG. Opzionale via `ImageModelId` sotto la sezione del provider attivo; non
disponibile con `AIProvider` `anthropic` (nessuna API di generazione immagini).

## 13. "Inoltre, quando sta generando, puoi dare un dettaglio di quello che sta pensando scrivendo generando? Come un log?"

Nessun meccanismo nuovo: la visibilità delle tool-call già esistente (descrizione nello
spinner mentre il tool gira, righe `⏺`/`⎿` con `F8`/`--debug` attivo) copre già questo caso —
la `[Description]` di `Image.GenerateAI` è stata scritta per essere informativa in quel
contesto.

## 14. "Hai aggiornato anche il comando?"

Chiarito (dopo un giro di domanda) che si riferiva ai comandi `dotnet pack`/`dotnet tool
install` in `README.md`. Verificato con un pack di prova che non serve alcuna modifica:
`SixLabors.ImageSharp` viene incluso automaticamente come dipendenza transitiva via
`RalfAI.Core`, nessuna DLL nativa coinvolta.

## 15. "Puoi aggiungere anche un file md con i PROMPTS? che in qualche modo rispecchia la realizzazione intera del progetto stesso"

Aggiunto `PROMPTS.md` come quinto file nel set canonico di documentazione — l'unico non
opzionale: ogni context (`dotnet`, `html`, `markdown`) ora apre ogni turno con un passo "Log"
che ci scrive prima di qualunque altra azione, così la storia di un progetto generato resta
tracciabile fino alle richieste che l'ha guidata. Questo stesso file applica la convenzione
a ritroso a RalfAI.

## 16. "Aggiungi la possibilità di aprire un file durante la selezione: default code.exe, altrimenti variabile nel file di config"

Nuovo tasto `F4` in `FileBrowser` (usato sia al primo avvio senza cartella sulla riga di
comando, sia con `F7` durante una sessione): apre il file selezionato in un editor esterno via
`Process.Start(UseShellExecute: true)`, fire-and-forget. Comando risolto dalla nuova chiave di
config `Editor` (`RALFAI_Editor` per l'override via env), default `code.exe` (VS Code) se non
impostata — `FileBrowser` resta senza dipendenza diretta da `IConfiguration`, il comando viene
passato dai chiamanti (`Program.cs`, `RalfAIApp.SwitchFolder`).

## 17. "Puoi aggiornare l'help con un po' tutti i comandi e verificare che tutta la documentazione sia a posto?"

Due parti:
- **Help in-app**: `F1` (interattivo) ora elenca anche `Ctrl+C`/`Esc`/`Ctrl+X` (interruzione
  turno), prima mancanti. `/help` in modalità pipe non condivide più la tabella dei tasti
  funzione (inapplicabile lì) — mostra la tabella dei comandi testuali realmente disponibili
  (`/exe`, `/cancel`, `/voice`, `/speak`, `/clear`, `/switch-context`, `/switch-folder`,
  `/toggle-debug`, `/help`, `exit`). Nel farlo, trovato e corretto un bug: `/help` in pipe
  chiamava `Console.ReadKey` su stdin rediretto, che lancia `InvalidOperationException` —
  verificato con un test end-to-end (`echo "/help\nexit" | ralf`) che prima sarebbe andato in
  crash e ora non lo fa più.
- **Audit documentazione**: verificati tutti i link tra `README.md`/`USAGE.md`/
  `ARCHITECTURE.md`/`ROADMAP.md`/`PROMPTS.md`. Corretti tre anchor rotti (due in italiano
  dentro un `README.md` inglese, uno verso una sezione mai esistita in `ARCHITECTURE.md`), un
  link relativo a un file sorgente che risaliva di una cartella di troppo, e una voce di
  ROADMAP che citava una classe (`AgentContextFactory`) ormai rinominata (`ContextRegistry`).

## 18. "Vuoi creare il logo del progetto? Una immagine?" — "procedi"

Creazione di un logo moderno e minimale per RalfAI, in formato SVG sorgente e PNG per README,
con integrazione nel repository e aggiornamento della documentazione.

## 19. "Vorrei che l'editor da CLI permetta di editare la richiesta con maggior ricchezza....non posso cancellare ad esempio...e quando incollo da clipboard, perdo caratteri. usa i filesd claude per la storia dello sviluppo. E convertila in una tua storia"

Migliorerò l'editor interattivo della richiesta con cancellazione e incolla affidabile, quindi
userò i file di storia di Claude presenti nel repository come fonte per riscrivere la storia
dello sviluppo in una narrazione autonoma e coerente di RalfAI.

result: OK — written PROMPTS.md (10015 chars)

---

### TOOL — FileSystem.ListFiles — 17:17:21

args: subdirectory=
result: [DIR]  .git
[FILE] .git\COMMIT_EDITMSG
[FILE] .git\config
[FILE] .git\description
[FILE] .git\FETCH_HEAD
[FILE] .git\HEAD
[DIR]  .git\hooks
[FILE] .git\hooks\applypatch-msg.sample
[FILE] .git\hooks\commit-msg.sample
[FILE] .git\hooks\fsmonitor-watchman.sample
[FILE] .git\hooks\post-update.sample
[FILE] .git\hooks\pre-applypatch.sample
[FILE] .git\hooks\pre-commit.sample
[FILE] .git\hooks\pre-merge-commit.sample
[FILE] .git\hooks\pre-push.sample
[FILE] .git\hooks\pre-rebase.sample
[FILE] .git\hooks\pre-receive.sample
[FILE] .git\hooks\prepare-commit-msg.sample
[FILE] .git\hooks\push-to-checkout.sample
[FILE] .git\hooks\sendemail-validate.sample
[FILE] .git\hooks\update.sample
[FILE] .git\index
[DIR]  .git\info
[FILE] .git\info\exclude
[DIR]  .git\logs
[FILE] .git\logs\HEAD
[DIR]  .git\logs\refs
[DIR]  .git\logs\refs\heads
[FILE] .git\logs\refs\heads\main
[DIR]  .git\logs\refs\remotes
[DIR]  .git\logs\refs\remotes\origin
[FILE] .git\logs\refs\remotes\origin\HEAD
[FILE] .git\logs\refs\remotes\origin\main
[DIR]  .git\objects
[DIR]  .git\objects\00
[FILE] .git\objects\00\a7d8397992efbcf634e87d01dfc32e9cb640fa
[DIR]  .git\objects\01
[FILE] .git\objects\01\80566140c23d605344a2fcab628d4bf4d6aa6d
[FILE] .git\objects\01\942ffa38da10f0ac18c86d1a3d67c72c33d87f
[FILE] .git\objects\01\a5dd410815721833e32bcd277c899f5b49c930
[DIR]  .git\objects\02
[FILE] .git\objects\02\a585c4b871b74e9a312af3ff3d72d9658665b7
[FILE] .git\objects\02\bef14e3a66085de453961624b793edb0ffde19
[FILE] .git\objects\02\d068e02f2b3265c7fdde9feb60c97db8fcb95a
[DIR]  .git\objects\03
[FILE] .git\objects\03\2b5edf7848e97b05e55db6009220e87c2bd00b
[DIR]  .git\objects\04
[FILE] .git\objects\04\ebc45deda2180aeba4e1b4d24e666f79b5da84
[DIR]  .git\objects\05
[FILE] .git\objects\05\821e64afcaa1af395e4bdb1312fef04e942e59
[DIR]  .git\objects\06
[FILE] .git\objects\06\58406fa9786edfa226d9c327af2ae493e120d3
[FILE] .git\objects\06\790da1f900378190e8bcaeff3805b804ec9c2c
[DIR]  .git\objects\07
[FILE] .git\objects\07\08077a6404c84089e7c35fa1bb035bf25e5e4a
[FILE] .git\objects\07\dabdb1da7545c0da57fc5a31d662a3069a5e66
[DIR]  .git\objects\08
[FILE] .git\objects\08\34d8bbf0dde6093a90122d181bbb844b127f97
[FILE] .git\objects\08\392b119084760a0fad0b74ba5f3e1e026c4f0d
[FILE] .git\objects\08\9988d6485e02ab4a9a77e473f2fd585064aad7
[FILE] .git\objects\08\d2749b28e20939c707c4739f3892b4d3d041e0
[DIR]  .git\objects\09
[FILE] .git\objects\09\53a19a8510c768656dba65f7a9e7d656d5d83f
[FILE] .git\objects\09\b123e5460727bd6f9d8f7995144a52d14d26d1
[DIR]  .git\objects\0b
[FILE] .git\objects\0b\30ad4abd0066b4d8edae135043004f7e731995
[DIR]  .git\objects\0c
[FILE] .git\objects\0c\03cadeda6a20eb92b3a98d658d2117d0cda4e3
[FILE] .git\objects\0c\704be2f67452106e684ea698bb7e4172d5fe5c
[FILE] .git\objects\0c\d77bdf623d70ae8db67bce7b9bfecafb254c1c
[DIR]  .git\objects\0d
[FILE] .git\objects\0d\27cdfcc3cf83cb7046cc0cfdd621ad69b857c3
[FILE] .git\objects\0d\373fbe1452e774e2ea46eff8d049816b697074
[FILE] .git\objects\0d\4d30fc9d9dce7b3c94bf44f6d594abf497a0f6
[FILE] .git\objects\0d\986e7a4cd21e36e964149fdb723b3cbf6b7a26
[FILE] .git\objects\0d\ee5c57f8f04f4a1bef94ced84f40e938de65f2
[DIR]  .git\objects\0e
[FILE] .git\objects\0e\932f0e0215e1dfda5eba823770229ebada1e51
[FILE] .git\objects\0e\ea9296f1048410098c0df8e12f1ad4b5bd8cf8
[DIR]  .git\objects\0f
[FILE] .git\objects\0f\0d7cdd07c64a5730dadfed1ac0541cec60d635
[FILE] .git\objects\0f\5baa7894159c79273e84716c30f07930f79c58
[FILE] .git\objects\0f\5cdb3d0521f625979269ead851e30b8096ef92
[FILE] .git\objects\0f\b26103e2b0a0d95c9af40832f8e09009aae4c1
[DIR]  .git\objects\10
[FILE] .git\objects\10\238e81a702b0eff4f816a28eb076b73c8f4893
[FILE] .git\objects\10\b5885475c02543005bc748b4fa51203425ca1b
[DIR]  .git\objects\11
[FILE] .git\objects\11\599915337c158348a968b35474b08a4aa79a65
[FILE] .git\objects\11\a3845f76003de92412c9652b2283ab6f49ef1c
[FILE] .git\objects\11\c579ea7df3bc734a7824b30625a82ec1ae0e47
[DIR]  .git\objects\12
[FILE] .git\objects\12\115869f1db39f3cda988f9af7dba43555de966
[DIR]  .git\objects\13
[FILE] .git\objects\13\4dfb933b7d18a0716c5c90d3c9f7ec20aaf064
[FILE] .git\objects\13\6866bd8ac191321430fdcf9120b24b2db89f2a
[FILE] .git\objects\13\7c8609beddaa1cc3d29dac534a149e95a10118
[FILE] .git\objects\13\b1f9a7bd3ca3c486e7e50a1ccda1ca0d588333
[FILE] .git\objects\13\cb226edeb47921fbcdbb4c8fcbf656e48300e3
[DIR]  .git\objects\14
[FILE] .git\objects\14\67a1f85583da0083005f7c7ca83677930749b2
[FILE] .git\objects\14\6b1e72e88c68730d9a58ae2631d393aed7a4eb
[FILE] .git\objects\14\8edcf5bdaa2d5d62590e19063130a732586b49
[FILE] .git\objects\14\cfeb86204b3433362136bfe3236ec8135af0da
[DIR]  .git\objects\15
[FILE] .git\objects\15\6753e0d0349549d003e842e823f8c8094fdb31
[DIR]  .git\objects\16
[FILE] .git\objects\16\3022c6385aed3e7ca8bdd8f75733a0a0bdbce0
[FILE] .git\objects\16\5b5534d270dc42e95405b52b59b2da341b440e
[FILE] .git\objects\16\7f290fc8c6a79db637dd44d69fc54e20ae421b
[FILE] .git\objects\16\9274a4f2dd170b3a82b7a4d776f45786c86603
[FILE] .git\objects\16\b261e064bf20639fdc3607045cf0349aba4e3e
[FILE] .git\objects\16\f8e54b69cc70f708630f1d60b8f9e3e98a6ea7
[DIR]  .git\objects\17
[FILE] .git\objects\17\198025459795b1bc91b387141b30a5ed06877f
[FILE] .git\objects\17\5005054597f0cb131d3021565551f5f32ed2be
[DIR]  .git\objects\18
[FILE] .git\objects\18\2157b09a077b914b7dca76db46af674be45d1b
[FILE] .git\objects\18\6a6858991ac67b7d0da342fd563d35c7c556f3
[FILE] .git\objects\18\7d60c858d895a427f9f445056f01072f6454d3
[FILE] .git\objects\18\e70792bb0321e0c39f8a078b1211870cdabef0
[FILE] .git\objects\18\f95c9a9e235b64d19ea3bf9cef895cd926601a
[DIR]  .git\objects\1a
[FILE] .git\objects\1a\9d63bf85aeb75fc5d9dbe867fe2a15ef2e318f
[FILE] .git\objects\1a\f9380bbd3a1e66f2a244f34c98d5a1e4350f8e
[DIR]  .git\objects\1b
[FILE] .git\objects\1b\0de61675f59770f61db5b373ede861a732cc98
[FILE] .git\objects\1b\fb1b062aeffd3689332e939f47ae076aee9b07
[DIR]  .git\objects\1c
[FILE] .git\objects\1c\42be3c5b7b9a2da299ac5eef359ac3b9859242
[DIR]  .git\objects\1d
[FILE] .git\objects\1d\76485b2a81b434618c887004263b9a46fa157b
[FILE] .git\objects\1d\ae90b1e62f3e4bfd7b65fe9dce62b6684e7960
[DIR]  .git\objects\1e
[FILE] .git\objects\1e\3fb80b726dbac3436a9888d7db7eb33af39ce5
[FILE] .git\objects\1e\75105604345605533f0fc69931407c0845709e
[FILE] .git\objects\1e\b813dc19f03d213e6edd8b290f6ca5445050b2
[DIR]  .git\objects\1f
[FILE] .git\objects\1f\0c355efe596d4e42c9066baa0c663f02a81a02
[FILE] .git\objects\1f\1fc91ab9c629a1d214cf9b8e7902049f47994e
[FILE] .git\objects\1f\82976bdbb87a10e15b6a8edf8a36f79e83c2e2
[FILE] .git\objects\1f\9793de34fd3cb5b00500570f62f605e91e5292
[FILE] .git\objects\1f\b89abb746840452384cde74671ea59fece776f
[DIR]  .git\objects\20
[FILE] .git\objects\20\f2afeb1886a50a3d4e9b1379fb1403520f98c2
[DIR]  .git\objects\22
[FILE] .git\objects\22\2540b4bbbb488faa6db6b644ed014fea941ab0
[FILE] .git\objects\22\cb07afc08d1299f801472087ddd196aa897b28
[FILE] .git\objects\22\d2e0cbe64d23383be6c06efbc2e395cd693dc5
[FILE] .git\objects\22\e8a96c381182a885a33a94cd7f87f50794a6a4
[FILE] .git\objects\22\f86af44d08d1c3d46fcf2c5992437431d26ff0
[FILE] .git\objects\22\f91db60c89bb484f529d60a8cc9b7659b86741
[DIR]  .git\objects\23
[FILE] .git\objects\23\2bd09953b410a6b7f7bc1a52d1b8182304a7b0
[DIR]  .git\objects\24
[FILE] .git\objects\24\7ed197c5d1967149c76428cad33b3d65f74f04
[FILE] .git\objects\24\d98e0d6f4bb63adb2a77d9b3cfdf67354be250
[DIR]  .git\objects\25
[FILE] .git\objects\25\1e9d59a57ddd701fe2b8222442a09593f2f775
[FILE] .git\objects\25\3db945de6e308fc19f88cc8942c3b7f5d6015f
[DIR]  .git\objects\26
[FILE] .git\objects\26\bf58f6dc890d272f48cd7d80fadb7caa8ae624
[FILE] .git\objects\26\f4a101e39bcea6423878467125349122de49f5
[FILE] .git\objects\26\f8e3c999f4786ff0e9b036d283effd19bebcd5
[DIR]  .git\objects\27
[FILE] .git\objects\27\4251ae2c3a8777db0c448a4b1a288824fee425
[FILE] .git\objects\27\ba88bdc95ff1071530b1ede2dccd8a548c515b
[FILE] .git\objects\27\e4551a00c9787e269e724c4024954f9ac3694f
[DIR]  .git\objects\28
[FILE] .git\objects\28\3fe3cbcc311e83d16b495f20712a8e5686829f
[FILE] .git\objects\28\bae31747cfc6ce2912fe9144b1cc8550a82f20
[FILE] .git\objects\28\c2f0d9e9a8a3ddd95e15cd44f6ebfb414428c2
[FILE] .git\objects\28\e66a3eb40d6279273a19b84e4ae01774b0eee3
[DIR]  .git\objects\29
[FILE] .git\objects\29\af417cd291e85c2ab13f264976863d24cdfff3
[DIR]  .git\objects\2a
[FILE] .git\objects\2a\90aa5979c689cd3e179688fb3367c37d7fb031
[FILE] .git\objects\2a\bf9ec5aad296151f24c2c9ef16f1b6202ae080
[DIR]  .git\objects\2b
[FILE] .git\objects\2b\9fb215b4fb01e5cb24b8a3b4f590c9049f5125
[DIR]  .git\objects\2c
[FILE] .git\objects\2c\5d8d922b7ce21a9d4f5de0df2919d46c44b5ef
[FILE] .git\objects\2c\f9c59de9db56d4a25b089d8c62dfffca9f63b0
[DIR]  .git\objects\2d
[FILE] .git\objects\2d\8618fdeed507655a5d3f8519ade5aa06467e25
[DIR]  .git\objects\2e
[FILE] .git\objects\2e\675594aa5b92a642ad8e155ca6579264e493e0
[FILE] .git\objects\2e\e7fa0be115cbb44b30733d86dfef40941b8ac5
[DIR]  .git\objects\2f
[FILE] .git\objects\2f\67189ea5b622853a9cebfbff760f14a85cb3f3
[FILE] .git\objects\2f\ae0065177d04b41ae29a2dbc32c78177e5cc49
[FILE] .git\objects\2f\fdc45b237ddc01c9fc10fddc4e70adc22e3c7b
[DIR]  .git\objects\30
[FILE] .git\objects\30\43f180a535a4cf065642550f5f1025fa52999e
[FILE] .git\objects\30\971f3aa8abd5ccd70cf192e3b48e6fde8466ef
[FILE] .git\objects\30\a4fc8b14dcf252e193eddfc33a79caabd24e56
[FILE] .git\objects\30\cfb60691739379e8b7cf42ecac373db6d37f77
[FILE] .git\objects\30\e985f6d60235d09fc622eff9ad378f1932d2a8
[DIR]  .git\objects\31
[FILE] .git\objects\31\b505b021b2a071d4e8bb920633f52be518851e
[FILE] .git\objects\31\d922ecfa392cbe808d4beadb88100a8dda461b
[DIR]  .git\objects\32
[FILE] .git\objects\32\60a0471d27fee22d76c2689e371033e60e7cd0
[FILE] .git\objects\32\6c2077a8038f1004b283aee2c02e8e18f3b9a6
[FILE] .git\objects\32\fc42eb147fc1129bb394913c5211701c25bde8
[DIR]  .git\objects\33
[FILE] .git\objects\33\8c45be2120a2b3032012014bf6a5f6808343a8
[FILE] .git\objects\33\cd5128e75576c930dd0ef9e72685893f64a609
[FILE] .git\objects\33\f7ce667c90f5d38588114abb4d2aef30b66863
[DIR]  .git\objects\34
[FILE] .git\objects\34\01d6a6bdf3110055994a8c9f93aa693572b9bf
[FILE] .git\objects\34\0bb4c52cd171baa59caf6719cebe5b08c2ca39
[FILE] .git\objects\34\a58cdefd2d328a60b9fd87f2c301fe61b5e0e0
[FILE] .git\objects\34\c53b34062aeeb7a773c2e615ff383002b8e200
[DIR]  .git\objects\35
[FILE] .git\objects\35\5a1b4e12e77c34407d1889e5bb2c66b1667498
[DIR]  .git\objects\36
[FILE] .git\objects\36\05010644bc9326299e4da3637f8f9d0714fc98
[FILE] .git\objects\36\4f3cabe4d693e529976056eb004460f4ef3485
[FILE] .git\objects\36\72dcc8b5d2566709ff2b6a409f0f7b280101b5
[FILE] .git\objects\36\b2d26b0907f840ef6ac8572dc69965d7ad7fc9
[FILE] .git\objects\36\ffd2d3bf0d96126c8254931565ce97b0ed020a
[DIR]  .git\objects\37
[FILE] .git\objects\37\220554acc6a5a9bee42fc12fc1d0256cd2661d
[FILE] .git\objects\37\7fd5516fc91ec4d873573e58ab70b43a78ff18
[FILE] .git\objects\37\8dcc567acde97e523e79440b221d1a43d4e87c
[FILE] .git\objects\37\d51debeffa884d2a23dbcfbfb65b7001c7cc41
[DIR]  .git\objects\38
[FILE] .git\objects\38\bf348ad9325ca9c7d12f31896591a5b44b38f1
[DIR]  .git\objects\39
[FILE] .git\objects\39\3a38d280f4b1f0a623c3d8530273dc24da9413
[FILE] .git\objects\39\b6944d44c8d28f5ba0cecc906015fef28a5d15
[DIR]  .git\objects\3a
[FILE] .git\objects\3a\71cf76a49f452f599d6608df89d571892c5634
[FILE] .git\objects\3a\efd93eeb5755e32c71bec5a23ba3f7d8760803
[DIR]  .git\objects\3b
[FILE] .git\objects\3b\57d4514dea568a0ddb5c802b5a191f86bca1b0
[FILE] .git\objects\3b\87fe9db54a3f00c788eb185a46f828a0965693
[DIR]  .git\objects\3c
[FILE] .git\objects\3c\07e93df58fb3d5021662c6bd827a5539f180b9
[FILE] .git\objects\3c\40ffe699c4101a1d3585d28d62c34f21e5166c
[FILE] .git\objects\3c\69dc9b2cde1b40f6c00b2947fc37d38f03adea
[FILE] .git\objects\3c\ada970db36aec96cb21fad556bfaf104d6345e
[FILE] .git\objects\3c\e9bce60120746a9538106c31889d5459ae70e4
[DIR]  .git\objects\3d
[FILE] .git\objects\3d\08fb0dba2593dca8588551bd1dc48aa6c221ef
[FILE] .git\objects\3d\40c879cf3a82a731027f9acc7ff70800b59eeb
[FILE] .git\objects\3d\680f33317a5fe62b5824e9507ea2f3c9a714fb
[FILE] .git\objects\3d\e766d8d260eb2fd3adedcc8b0f322ea8b30251
[DIR]  .git\objects\3e
[FILE] .git\objects\3e\3c0a2796b6b5b84dde124ebfeabd59463f16fc
[DIR]  .git\objects\3f
[FILE] .git\objects\3f\676188793036df11273344c8e3fceca078932a
[FILE] .git\objects\3f\beec46a0cf9b09a9a4de1f62bb266529295a89
[FILE] .git\objects\3f\daba1a99e559f0d205a0d29a7e3884d20bcb1a
[DIR]  .git\objects\40
[FILE] .git\objects\40\23579cb4208ea9c5af78b353b7b9df6cf83052
[DIR]  .git\objects\41
[FILE] .git\objects\41\15de6fc0d301c8222db568d5528b6cad8c5480
[FILE] .git\objects\41\723efbaa1ab3dfeab1d1acb91c3e6e9c187ef7
[FILE] .git\objects\41\749091d74daab748a11dae42cf1b607fda49a2
[DIR]  .git\objects\42
[FILE] .git\objects\42\40b6c5f719dc9eda7e4c061a18543a35f6c36e
[FILE] .git\objects\42\894fd3c2ef18a66c7af6450952c53730031af6
[FILE] .git\objects\42\e634a3ddb3e5b3f91970ac56dc256309d1e418
[DIR]  .git\objects\43
[FILE] .git\objects\43\be6fa89388b41754541a1c1b11a4e9b4232c85
[DIR]  .git\objects\44
[FILE] .git\objects\44\234c573e9f43e9467d36364ffe3052ed36f01f
[FILE] .git\objects\44\79e146efceb629c425c336566e1bcff7d8b332
[FILE] .git\objects\44\abcc7f4ea9c77c4173e81c946a630fbf309033
[FILE] .git\objects\44\ba230c1740b2487b27d2b61bb7b806a6e69e71
[FILE] .git\objects\44\d5121ab4e4c76a3f3417201d091c6d358edb36
[DIR]  .git\objects\45
[FILE] .git\objects\45\95019f9cc3a874d83ff8428ab35c0defc8b103
[DIR]  .git\objects\46
[FILE] .git\objects\46\0a1d25545ed3b46aa0fd53e5bde16fb3586426
[FILE] .git\objects\46\36b2ed6fd632e9f8ddd349908849e785df3a15
[FILE] .git\objects\46\5debbeb7c787ed193b061abcbfc5a1bc916cd9
[DIR]  .git\objects\47
[FILE] .git\objects\47\0cdf185256451343b1c565cce5d9537e1fe872
[FILE] .git\objects\47\284a188136294398dd58989bddf3b6e41803df
[FILE] .git\objects\47\b7785194fb7bc9e31f79f6b11da6eb66c0bd74
[FILE] .git\objects\47\c2c9e14cd6272d8f1a354650f39951ca50c018
[DIR]  .git\objects\48
[FILE] .git\objects\48\7f984c2ee2e9fe1e9c54bfd72d4d9697e6be53
[FILE] .git\objects\48\aa9d56c6c89d397fcebf57d36e7a7bd0cb8fd5
[DIR]  .git\objects\4a
[FILE] .git\objects\4a\1421531720342d8213eb260ddcc5f754295a59
[FILE] .git\objects\4a\1be9976979df281dca648eca975c85cdfc2689
[FILE] .git\objects\4a\9a364ca2c000a670182ed29d7de2a79be31f01
[FILE] .git\objects\4a\9e60015fe73606ec758a82541b57dd59cc539f
[FILE] .git\objects\4a\ba1a20c06067e0b9b237c29492a46970915a27
[DIR]  .git\objects\4c
[FILE] .git\objects\4c\1a65a8e1d90fa0c0c22519fc2d63a7b0c87247
[FILE] .git\objects\4c\1c28fa040c06dc26ef4620a598382f29671551
[FILE] .git\objects\4c\5138fecf8de92b51e0328ee0fe04b78eac3610
[FILE] .git\objects\4c\58d21de9cb1f2a8fe3f8eee8dae577df5163ed
[FILE] .git\objects\4c\d323be60bfb4d9eafa01c73bd6867053ed7bc2
[FILE] .git\objects\4c\fa90824e16bdfa908489ec7b323e2aee83f7c8
[DIR]  .git\objects\4d
[FILE] .git\objects\4d\96c6b91a55ce80fbb89a2adb1f765f4c16f809
[FILE] .git\objects\4d\b9d78099110fbfa84d7a695aef4e51b8b84467
[FILE] .git\objects\4d\d9260bc15728ed7679874f270203b969c48b58
[DIR]  .git\objects\4e
[FILE] .git\objects\4e\4a7d72e264887d2c380390be0cc981d6eacbb2
[FILE] .git\objects\4e\4f72308a75221ade6871636909621f4c2ba98d
[FILE] .git\objects\4e\6fd679d8f35c6085fc190a86399aa6cf8faf04
[FILE] .git\objects\4e\9f2e1a1f76c246899b0bea4d26b6947087811e
[DIR]  .git\objects\4f
[FILE] .git\objects\4f\c8df5d83f5dfdd2dd46466b4a17f63e8b9229b
[FILE] .git\objects\4f\fc0d6463f210381c727c3d7705463817d3eb69
[DIR]  .git\objects\50
[FILE] .git\objects\50\1b69a13bbaa8794288a85d963c27f8f1319222
[FILE] .git\objects\50\293a40da9bb31faddeb496e35053753e9deec1
[FILE] .git\objects\50\cd65021ae9a2f4c80e691002ff7b25f2e88da9
[DIR]  .git\objects\51
[FILE] .git\objects\51\965b937607cea44cc6b2077ad6cb20a79d261d
[FILE] .git\objects\51\fb32890165f9012781ee0e116a0e6903a89e80
[DIR]  .git\objects\52
[FILE] .git\objects\52\24789f459539d98ad60b13d37d64fd9e6b30c5
[FILE] .git\objects\52\7a960ae25bf8b8321c343e533afab66ec37f34
[FILE] .git\objects\52\f15b3152e9b92fca3da2e2bc424a909f2f889b
[DIR]  .git\objects\53
[FILE] .git\objects\53\01756c7ccd6d435ee096d29aa2795a964d9d54
[DIR]  .git\objects\54
[FILE] .git\objects\54\8739516685a87597dd7c6bf957de33a23b4eec
[FILE] .git\objects\54\c94bb1cc64ec1c6c007063958747fa5971ac4f
[DIR]  .git\objects\56
[FILE] .git\objects\56\06a404d6c495144c8f7681c6112e79737b1b91
[FILE] .git\objects\56\acebb40e71eacdbbdb65468a275e473c586d11
[DIR]  .git\objects\57
[FILE] .git\objects\57\8526194f9f96ab178e3376cf35c2e6a1208b30
[FILE] .git\objects\57\bf7b0bfd6b5525cdcfd67532e97e261df8c322
[DIR]  .git\objects\58
[FILE] .git\objects\58\310c1cadc01362dadc7bc43e1928655a695a45
[DIR]  .git\objects\59
[FILE] .git\objects\59\0e5342ad0cc7876414b95894f653cd08de12c7
[FILE] .git\objects\59\9c5a1c85f950740435d626b6731c60946e9b2c
[DIR]  .git\objects\5a
[FILE] .git\objects\5a\b9ee3774804cbc0bdc39c798f532f06d2f7c0b
[FILE] .git\objects\5a\f9a522d77a209b5039ffb9769a10e9a61f4a4e
[DIR]  .git\objects\5c
[FILE] .git\objects\5c\7b8cc1cf317d676404bcbd5ccf8fa9f500fa11
[DIR]  .git\objects\5e
[FILE] .git\objects\5e\8653ae7ced7ea1fe2bf3a75c7df5476900628f
[FILE] .git\objects\5e\913a1e5c7eca7896c8dbb425545c683dd8054c
[DIR]  .git\objects\5f
[FILE] .git\objects\5f\2d1b4b6786e8a6e3c0718681ebb3febcc83fef
[FILE] .git\objects\5f\8591e4985d0fe4f6d8fd00221dc43b4a9fe123
[FILE] .git\objects\5f\a44499c63711ab257faca59b0f33406356e291
[FILE] .git\objects\5f\c61056552c70bcf08791771601bfffd0242e5e
[FILE] .git\objects\5f\cc98f9cdb67e4952543e36f356d2f41509d9a1
[FILE] .git\objects\5f\e64834b956e31388acd1e1316c48d59b5f6db1
[DIR]  .git\objects\60
[FILE] .git\objects\60\308aacb109772e56fce0633c4358c2a45c078e
[FILE] .git\objects\60\61d68c04a579b8b5ca18e71403e4d6345135c0
[FILE] .git\objects\60\9730b3764e25297aad646485aa25fe287b53c5
[DIR]  .git\objects\61
[FILE] .git\objects\61\2683b867376413fc487d68076e043396ab7bb5
[FILE] .git\objects\61\7a4b4fd5e2c201125b6b1bd57355bb48217d85
[DIR]  .git\objects\62
[FILE] .git\objects\62\a155b738ad6718365894ce55306855acc27ce2
[DIR]  .git\objects\63
[FILE] .git\objects\63\26ee581aece13b7552a154d7dd335631868937
[FILE] .git\objects\63\5ad456fe8a2496cbb0cadb907d4d0ab72d009b
[FILE] .git\objects\63\b336e381b874228fd9006e56eda8fdf976a0bc
[FILE] .git\objects\63\b96b7577b622588c5f9c950e0bd099dc2c954a
[FILE] .git\objects\63\ca056ec607e7bdd0a0ea8984cffbe26a57ee15
[DIR]  .git\objects\64
[FILE] .git\objects\64\be4fa393eaeb1b63c9c18510738c7fd0477268
[FILE] .git\objects\64\dabcf6f51f3cb7ec904767e364122bf0c5f41a
[DIR]  .git\objects\65
[FILE] .git\objects\65\2a12b36294e164521c07e7c2881db0d9a7cb1c
[FILE] .git\objects\65\f442c10e22ff59cfbce78050bb93fc49beee9c
[DIR]  .git\objects\66
[FILE] .git\objects\66\32aa68d51700f4f6806da8ccf852542e1edd6d
[FILE] .git\objects\66\ce53bdec854c732b25f82b1e00866965303e12
[FILE] .git\objects\66\d030b4f98d4f4248521db0878202711ac9cc9d
[DIR]  .git\objects\67
[FILE] .git\objects\67\50fe27304b63403138b3cdb9d4d241504b842e
[FILE] .git\objects\67\8c628537f5140e1cdd61862bf7ba693c54d252
[DIR]  .git\objects\68
[FILE] .git\objects\68\af9727ec2775eaa4c0f7545fa91d848cb58a43
[FILE] .git\objects\68\c33ebd01c8a46648283925960be5a81ae1cfc9
[FILE] .git\objects\68\c4a8866b1181779305404066e77c99e6622bd8
[FILE] .git\objects\68\ec026d03d72e1d359a56fd4407f3cbdc0ca131
[DIR]  .git\objects\69
[FILE] .git\objects\69\4a3ef119bcbf9303cb64b73fa3468707ec3063
[FILE] .git\objects\69\dda9b82e9892f5534cdfd21557cfd0850bf436
[FILE] .git\objects\69\e8c64e3f8a2d31762e687e696e870c3a732bda
[DIR]  .git\objects\6a
[FILE] .git\objects\6a\1d3a22d23bb4376c6659e5552743d06f0c6e6b
[FILE] .git\objects\6a\7bd07b1d1c08a93d381f5f9d26acc49f741f46
[FILE] .git\objects\6a\a58f7ef955f34d2f5a1c49accc5d1b2498b408
[FILE] .git\objects\6a\a8df7b6789c566d59a28f4e9e01e2c17c7a753
[DIR]  .git\objects\6b
[FILE] .git\objects\6b\2bdd95a80a763ffab873e1140b78269e48ca84
[FILE] .git\objects\6b\727c61d7b7d37d70a2a47815d247c8bef10862
[DIR]  .git\objects\6c
[FILE] .git\objects\6c\aeafcf2089ae756066fd25cdfbd24897ad983c
[FILE] .git\objects\6c\dd7e6652b776b29c18427ad31f8a4a990cf5c9
[DIR]  .git\objects\6e
[FILE] .git\objects\6e\ff9fa076f4caa838d758d9318fb0f9a7dbea55
[DIR]  .git\objects\6f
[FILE] .git\objects\6f\47d52ef719e0d3501c71b98b7f271734a1e48f
[FILE] .git\objects\6f\be286c79f7d77cb0ea5cd21b5e9b7c810bfa20
[FILE] .git\objects\6f\cbb8541ae5b7769c8f966a7a375c881714d7ec
[DIR]  .git\objects\70
[FILE] .git\objects\70\159e5037d4e4592396a040ae8685a842442ade
[FILE] .git\objects\70\163d9e8ed44b64cd056df05c8e472ee9aa61ad
[FILE] .git\objects\70\eb267927c66118c6a6a0f5b13475f4118c056a
[FILE] .git\objects\70\f376b87427ceaf18178f6835f9de57f88f6721
[DIR]  .git\objects\71
[FILE] .git\objects\71\6e6629e117cee74de14e2c292106b9a6f4a776
[FILE] .git\objects\71\868b044fc77b2af304aa9ae98a30282758ff7e
[FILE] .git\objects\71\f0f192f4ac5df028d44ade30a2fa3a506205db
[DIR]  .git\objects\72
[FILE] .git\objects\72\9d157de50d71f3302817f38e86d21374217918
[FILE] .git\objects\72\b013699d8bbc531148a88917a37a1f987a82a6
[DIR]  .git\objects\73
[FILE] .git\objects\73\2451d9e64ab857e2197b8188da9e62424fc7f0
[FILE] .git\objects\73\5bb7d2a07f834c3bb018286469e906704d1b1c
[FILE] .git\objects\73\72f53239321b428d4571f5c5e6b34572e24192
[FILE] .git\objects\73\b16c60f7af49f0cf23d2eab49a50a330556265
[DIR]  .git\objects\74
[FILE] .git\objects\74\2c7d414fc33e83057d3590b097fc38c3135829
[FILE] .git\objects\74\682835b8d424ff1d6a13dbb7720955408bddad
[FILE] .git\objects\74\bff31f38d347bfb05ba8c518aa492e8afafd50
[FILE] .git\objects\74\c358f4697c60a836de85ec5e50fc665d01ea48
[DIR]  .git\objects\75
[FILE] .git\objects\75\b690ed6d86aba78b62b6ed1a3a173cc12bf994
[FILE] .git\objects\75\da2e46e567ed1c2b0ab7c3a8383b82860b722a
[DIR]  .git\objects\76
[FILE] .git\objects\76\706f4c62ba48a519ddea76239a005a233f4959
[FILE] .git\objects\76\ab6850adb5afbc59c050e9c3da20aa17408d49
[FILE] .git\objects\76\ce93bf408d05b902eb7b52eb4e987b16fcc4e6
[DIR]  .git\objects\77
[FILE] .git\objects\77\01b4627ed6e6f3d6de48703d8e67f9a3e0b846
[FILE] .git\objects\77\54bebf893d1ffd6e9d9628475cb7c7920ff791
[FILE] .git\objects\77\883e8ead496a8913990f070f30de1985847014
[FILE] .git\objects\77\ac64c03f699d2a2ac1fc09dff07ea810a8a470
[FILE] .git\objects\77\e1d6ccb12f358378958c76f77807d16d3da8a7
[FILE] .git\objects\77\fcf78c56104ddf3ab85cf327ffe352b1f25bd9
[DIR]  .git\objects\78
[FILE] .git\objects\78\3afa12f10c2f1924e310c9f48859bf6e851351
[FILE] .git\objects\78\4f1cc0e33e857cdbbddfe68b7bdbc3eed83f93
[FILE] .git\objects\78\617ec7dc46671c2995d987c4c23ff8f0317ad8
[FILE] .git\objects\78\7f44c4345db3fef3cd0924276f3bcc08d6dad5
[DIR]  .git\objects\7a
[FILE] .git\objects\7a\0c1679b48b660ece93f3313a861f2efa0174d7
[FILE] .git\objects\7a\a30b439398e8243e8c4a43a0af1816e7a7dda1
[FILE] .git\objects\7a\b855016d504355a4c2bfa4ec0f6543e7134867
[DIR]  .git\objects\7b
[FILE] .git\objects\7b\5ceadcffef5f4503b7eb0adf7fa7a31ea1813b
[FILE] .git\objects\7b\eeff2edc2b5d1f7783cafcd7c2ab67f55a157b
[DIR]  .git\objects\7c
[FILE] .git\objects\7c\5d531054776431bf8203a0f36f4f48a3e10f80
[FILE] .git\objects\7c\5dce5df084dbf14cc4c04bcd5a8a6301c8eba4
[FILE] .git\objects\7c\c42edd77330efee12c0e44f027eeadd3f62ec5
[FILE] .git\objects\7c\c5d6837ce16b47ab6f80ecf019acbb9be6cff2
[FILE] .git\objects\7c\e84d982be18126189f3fcc16519fd15baf02c6
[DIR]  .git\objects\7d
[FILE] .git\objects\7d\33eecb4b8f902741f9ddff5c168601aae51fe5
[FILE] .git\objects\7d\7fca63f81e72fb023a0ec57b8dfff6f93f4d81
[FILE] .git\objects\7d\ae196fa08f920de0f08fa9c114d6b32f4006e5
[FILE] .git\objects\7d\ae3507a4333c11909514125008eb6ea970876b
[FILE] .git\objects\7d\c470492d10c4af03a198b8cc580f86babe01d5
[FILE] .git\objects\7d\e3aab9ae894c626e0982a587b522fd506e9ad1
[DIR]  .git\objects\7e
[FILE] .git\objects\7e\01ecad0b51a31256bafd078c1e5478c5f547d5
[DIR]  .git\objects\7f
[FILE] .git\objects\7f\4fdf6f648202593859f09729fe7e5ccd82b662
[FILE] .git\objects\7f\ca23b88c0fd8a67c49b0c0bc80a6b384020b0f
[DIR]  .git\objects\80
[FILE] .git\objects\80\31d7aff735d3a23b2e79ffd874e33f79d8e292
[FILE] .git\objects\80\78b7405e98195d1529c0401337eb0da41fe4f3
[FILE] .git\objects\80\865953c57cc605332c035e0ef273da82e61a18
[FILE] .git\objects\80\b6e6c344b1785f6811635bb2b2605da3cd6c5d
[DIR]  .git\objects\81
[FILE] .git\objects\81\ce48f43e88d523a7b736455536823a75403341
[FILE] .git\objects\81\eb4aae1db09d07c8a7bb5252e878bb2bb951a8
[DIR]  .git\objects\82
[FILE] .git\objects\82\0610a9aaf6eb807debf384b972cb875f1d84e7
[FILE] .git\objects\82\6d061181cdb6366c98e4f51f77e6a2fab5718b
[FILE] .git\objects\82\ce2e779f5887636245e19fb2b1a911191b9688
[FILE] .git\objects\82\fb70a205592b938e40528653b233bc83563db9
[DIR]  .git\objects\83
[FILE] .git\objects\83\39f27eeae261fd51d09b3f624c6c5a2e8f5338
[FILE] .git\objects\83\84c77702572d6d879912a5d76a8184937dc1eb
[FILE] .git\objects\83\e6db736fd2d181f3ebfda0bc7b760a90ad95b6
[DIR]  .git\objects\84
[FILE] .git\objects\84\46b97321ef29d4e4644cab549914b4c4507615
[DIR]  .git\objects\85
[FILE] .git\objects\85\55ad948ae3b7889648610b3e736a3f8467c033
[DIR]  .git\objects\86
[FILE] .git\objects\86\8b33e4b665f9c82ff61f1069659cd5849ba63a
[DIR]  .git\objects\87
[FILE] .git\objects\87\1c7cd4fc1d2d5f06ef0c36ea0b815583a6c467
[FILE] .git\objects\87\2260e0328000392323d41f4a2054e5033e5b0f
[FILE] .git\objects\87\4284807367974309762eb5ccbe076283d52683
[DIR]  .git\objects\89
[FILE] .git\objects\89\2d1a4b4b8ac09552b20b27c195c095d326b868
[FILE] .git\objects\89\5508dd0e35fc92660564ba06ac7d12e0ad2149
[FILE] .git\objects\89\a678b2c09696fd316c6d95103b2d7f763595b0
[FILE] .git\objects\89\aefc7d0486519f3d5a4a969dafef2e765f6234
[DIR]  .git\objects\8a
[FILE] .git\objects\8a\505ce81ffc211d4ae0b1d695b18dd783449db1
[FILE] .git\objects\8a\865a3180f4ee60706692d92cd3bad7fa16f5ea
[FILE] .git\objects\8a\9e0edeb475ebbe24433d5e27244f7626db42ad
[FILE] .git\objects\8a\c6728e3ce412af7218559c7b9bf00c650b2c78
[FILE] .git\objects\8a\fb289410e8f5bb6b5271b20771055c6246f146
[DIR]  .git\objects\8b
[FILE] .git\objects\8b\261107bd17c6d2ed584020bcf7fecc019c5a8e
[FILE] .git\objects\8b\e5dd26ccdd5fcd0fdf468698eb01e868f9fee4
[DIR]  .git\objects\8c
[FILE] .git\objects\8c\5765e54790df5a693dd8e3c8e89fa87a5170be
[FILE] .git\objects\8c\7d5fd8ca78229a7a0084261a97490ed0e580f0
[DIR]  .git\objects\8d
[FILE] .git\objects\8d\24fb8a70162607d5fd5da7e657c3e112d0cd47
[FILE] .git\objects\8d\43a327163ec2a4ee422c67c97b5af8428e1ecb
[FILE] .git\objects\8d\95ca3a40b11cb3d5333479b7125ccf863b94ed
[DIR]  .git\objects\8e
[FILE] .git\objects\8e\456f5dcf86d521c5979de3ca368fd58aa66840
[FILE] .git\objects\8e\940e3b83dec452266b938a7606380290649bdf
[DIR]  .git\objects\8f
[FILE] .git\objects\8f\5c082fadc372d74eec664c7d0ce9d00bae09e7
[FILE] .git\objects\8f\d47c6ff23141cc2430ffb6709d1a4363de6e33
[FILE] .git\objects\8f\f6bc2a79f5e2e194ae35921fc00b0a8d97923e
[DIR]  .git\objects\90
[FILE] .git\objects\90\819b2c1f747445464f25fc51b4932403ee039c
[DIR]  .git\objects\91
[FILE] .git\objects\91\02d6008844e95e32692145fb072028e8d9790f
[FILE] .git\objects\91\05c81afdaa0c489581443bf182e29dfc3916f3
[FILE] .git\objects\91\413a8695ae40cb0ee9e0be6a81df70499d54bf
[FILE] .git\objects\91\f47b576450b3e60626e6d2ab7e3f1e9216288b
[DIR]  .git\objects\92
[FILE] .git\objects\92\5368cdb6c1be2eb6026ca0fe2c9fc7b8366111
[FILE] .git\objects\92\5b1351bcda9f1244c4634843cb5f994a6b5008
[FILE] .git\objects\92\811937edda77d453960cff1bb00c5e539c455b
[FILE] .git\objects\92\8159782b41505a325843ccb05fffefa61e6042
[FILE] .git\objects\92\a9babe89671a2d044fd70d48ce9821a953270e
[FILE] .git\objects\92\adbb9154e02650e621c16eceed5d7262bbe9d2
[FILE] .git\objects\92\b9dc15f72dd0d68315bc3d67460d646dec4616
[FILE] .git\objects\92\f54154554df3219aa32cf9c1efc6bfef96ab5b
[DIR]  .git\objects\93
[FILE] .git\objects\93\1cb30c732b4031a55d5c3dc78d470e25a6b52e
[FILE] .git\objects\93\563503eeeec84ec92d7681abf39a6318803d6d
[FILE] .git\objects\93\9a5cc7d1882dd6b6aacad26d8b74c457e621c9
[FILE] .git\objects\93\a9962c3ef98ab33d09031e3633a07a53ef0df1
[FILE] .git\objects\93\b6d9ee41eea8efb3ae1c6994296ea2d4cc7e6b
[DIR]  .git\objects\94
[FILE] .git\objects\94\07b67bd1636434f4db9a3dd4a12018d4f011ec
[FILE] .git\objects\94\32143b6e7590dbeab3d106bc31cba7fafbebf5
[FILE] .git\objects\94\54b936701286d5c0b474fff5a83c4d3d8eb842
[FILE] .git\objects\94\9f753174ce1f7cf83fe90163f84b8781c52991
[DIR]  .git\objects\95
[FILE] .git\objects\95\3de6e9bc9ef830b4be1607b4bc670b82749e83
[FILE] .git\objects\95\ba3fb97f56e76e5fc6f6e528d541ac3edfe38c
[DIR]  .git\objects\96
[FILE] .git\objects\96\184bf8b9d0578a4bf30396d3eb5fc21cb63cc8
[FILE] .git\objects\96\5a145c867b5c247665edf6aa247181f6eca3b8
[FILE] .git\objects\96\f0bddfb973f1ea737beb9a877dfc9264030edd
[DIR]  .git\objects\97
[FILE] .git\objects\97\40ce7aaa1fabba613d00daa8620e317d5fa374
[FILE] .git\objects\97\5db2b944d33d2335d5bf62cf2f6692a2e295b9
[FILE] .git\objects\97\703f6745a09a6e442f00c9d20751e532304167
[DIR]  .git\objects\98
[FILE] .git\objects\98\e4a228867845ee27064225dbc60e38994c1e46
[DIR]  .git\objects\99
[FILE] .git\objects\99\25120fc92ad5bb20cff2d7c4572df2d74016b4
[FILE] .git\objects\99\321932f86bcd4ab01eb6a010cb77edbc035b18
[FILE] .git\objects\99\4b317d30ad635f33665188fa92cd9b012448cb
[DIR]  .git\objects\9a
[FILE] .git\objects\9a\437940d0fbb731f518d000a9efef362ea481f5
[FILE] .git\objects\9a\76d34f416f9541d974f736f97b87096423286a
[FILE] .git\objects\9a\f1ccebf554e8240b9a04bb0f50d89a2201d08a
[DIR]  .git\objects\9b
[FILE] .git\objects\9b\1ec403d2c49b8ae19ee4da8d69441bddfb01d9
[FILE] .git\objects\9b\baa51cf47a1ebe9f3bc27e9e0fea81eef0d59e
[FILE] .git\objects\9b\cb354367f75ed243366b2160a9a2c09fa36d40
[DIR]  .git\objects\9c
[FILE] .git\objects\9c\0ff8d0a5d204127691fd443af761bb9a55de84
[FILE] .git\objects\9c\131c56fb3aedd3109a5e14654aebe975405fe5
[FILE] .git\objects\9c\25b0ab013b403b904dafc1bc9fce9106be68b6
[FILE] .git\objects\9c\3082c7e397abf3f803d6d9b6c3839440a1fe01
[FILE] .git\objects\9c\c8198ddd5ed1ecc27fecdd2ad2ea6ce23af540
[DIR]  .git\objects\9d
[FILE] .git\objects\9d\26fd86a0722df73af611982d0f1736f1a9a61f
[FILE] .git\objects\9d\5313495c38e2d9fe6d19bd335f7ab738aee741
[FILE] .git\objects\9d\6343a065e592ed70953a043be9d2ed28712066
[FILE] .git\objects\9d\9e906f6f381be1a72a3ff2f61950a5ca82c95e
[FILE] .git\objects\9d\aac640701812c7dfa5243d8f476df62f7ffc3c
[FILE] .git\objects\9d\db861884112a387f5866eec842b92b35e39e66
[DIR]  .git\objects\9e
[FILE] .git\objects\9e\5faa9ca1f3f7590e41180bfcb98db8e3a7ac44
[DIR]  .git\objects\9f
[FILE] .git\objects\9f\1ce3cc2050430c10ebb41d8855a0c27a8b914c
[FILE] .git\objects\9f\506ac1799a050528c89529d65cf6d640bf643e
[FILE] .git\objects\9f\512268f9760ca15d1121ad289b53e9b8791d8c
[FILE] .git\objects\9f\f51e4a658415092afbc10d5bc6b88f66e334bf
[DIR]  .git\objects\a0
[FILE] .git\objects\a0\0bbdb21a548c971494751f9adcdc41cad281a7
[FILE] .git\objects\a0\593d6e1072e6c1accb5070785d9d0c1ba7829e
[FILE] .git\objects\a0\e9089c55ba26af2742ce9896f14b02d2f3e152
[DIR]  .git\objects\a1
[FILE] .git\objects\a1\99304faca6e5bc47064f49cee3e66e323c7b81
[DIR]  .git\objects\a2
[FILE] .git\objects\a2\5758a90f0f18182fb28501ce8e1a6206ac813c
[FILE] .git\objects\a2\f3aa160cf77931d6b87019c66aed4f2882c244
[DIR]  .git\objects\a3
[FILE] .git\objects\a3\0c00aa051595b6963219709f1fe9fe44652bdc
[FILE] .git\objects\a3\504ea129a9140c177c088dfb8d585cfc04e088
[FILE] .git\objects\a3\d600fc800e6d1cfe804b789a73e636a39ad1e0
[DIR]  .git\objects\a4
[FILE] .git\objects\a4\3c79936864a4ebdf1086a9253fa86090bf1ecf
[FILE] .git\objects\a4\710cb6c67e3abb8c6f9fc12dfa38e7fb34edc0
[DIR]  .git\objects\a5
[FILE] .git\objects\a5\0b24b5e925538e00461db3ff0d0b86d14e165e
[FILE] .git\objects\a5\0f429634b49e2a10f3596ecbcddf40e5a57191
[FILE] .git\objects\a5\587ec37a123e55a17b31e2a41c3c40fe661f6c
[FILE] .git\objects\a5\e45823a449ffedc9d71c99b1865a24336d9743
[DIR]  .git\objects\a6
[FILE] .git\objects\a6\56886deb1bec6ab86d71b1265f8e2149e75486
[FILE] .git\objects\a6\6156820f2598016e728f444821d9504bfe37d4
[FILE] .git\objects\a6\a11830d8d5251695e0057abcda380d1aaaedb5
[DIR]  .git\objects\a7
[FILE] .git\objects\a7\158c3c8ed62b30cdb83cc8bac6ad193ea7fd60
[FILE] .git\objects\a7\4ab9a589bf8f82a2491e400b4b3b827c91ca17
[FILE] .git\objects\a7\5e6fa7147298b1355d872a672d45ef243e624b
[DIR]  .git\objects\a8
[FILE] .git\objects\a8\02a294d58a3496723fcd1a37d85ff564747a8c
[FILE] .git\objects\a8\5cf853eb3cb3c26a3fe1a4e0381ae314dad708
[FILE] .git\objects\a8\a9c5a4d0adac30da22cf989393728ecd08fbf7
[FILE] .git\objects\a8\ccd59563871774064c80cda3a2d80c5785bb0a
[FILE] .git\objects\a8\fce39cb35653edb0d31811b92ed8d1c3b26068
[DIR]  .git\objects\a9
[FILE] .git\objects\a9\5fae61af0fe4d13c469bb4a23bbbab6fb7da26
[FILE] .git\objects\a9\c4de4a13fdf5253e93f19dd09183dc9b7d73cc
[FILE] .git\objects\a9\c73ff020b3388c87c9b30c11fb19b930414f86
[FILE] .git\objects\a9\da897d0fb3c5766bf93a0721f378d77c160190
[FILE] .git\objects\a9\e2098db6826f3da5035f656b6f8bedb5dfd39f
[DIR]  .git\objects\aa
[FILE] .git\objects\aa\6a55920f5abe441f74a71a85474c36884da3c2
[FILE] .git\objects\aa\c99dc26eb26d7d055daf06324d2f4e66ecfa14
[DIR]  .git\objects\ab
[FILE] .git\objects\ab\e86b1a848372bcec184f40beee88748d1acef1
[DIR]  .git\objects\ac
[FILE] .git\objects\ac\1c6067b19576b731c2c806a10471d40288e001
[FILE] .git\objects\ac\1e28c32f5a392c5cb49c472356efa472f44814
[FILE] .git\objects\ac\b7e82ede446249db13e529fca9407f62c6875b
[FILE] .git\objects\ac\c506f583448735c3f52cf647c2b238ed3d33f0
[FILE] .git\objects\ac\e3cd629f0917429874f586f360f33da137b948
[DIR]  .git\objects\ad
[FILE] .git\objects\ad\8307c90c19c5c51e91d34b403908d855c27816
[DIR]  .git\objects\ae
[FILE] .git\objects\ae\5b31da17362761350545c9cd4145d9d5aa5859
[FILE] .git\objects\ae\ae0e84043cda2445f66e64c36b6a8b67fff9ae
[FILE] .git\objects\ae\c9ab0598e2e834d4b90250b84ca7da6c742da3
[DIR]  .git\objects\af
[FILE] .git\objects\af\891ec3d401fa455ca93f93971ef5c25c1393f3
[FILE] .git\objects\af\cf2523e22e85c620b8c1325ce35c9f258e611c
[FILE] .git\objects\af\d46fc834eea1cfd97f4fa3b070b4156e27d485
[DIR]  .git\objects\b0
[FILE] .git\objects\b0\02df02c12193a4626a0710fead96b15e298284
[FILE] .git\objects\b0\3cd96a0fe7f6858a23a81e614d7f748faffbad
[FILE] .git\objects\b0\f7aaa918bfdc6ca82afdd35a5c2dbfb5521a86
[DIR]  .git\objects\b1
[FILE] .git\objects\b1\bf3b70a5bf325a021609cc43a405e583bf9dd6
[DIR]  .git\objects\b2
[FILE] .git\objects\b2\72be10d649c9e01a2b2bfa97ede299b7b681f0
[DIR]  .git\objects\b3
[FILE] .git\objects\b3\ed084e1fe79bc0d52ad618fcb6e3065807341b
[FILE] .git\objects\b3\f5290a300218655da799e042a2003bdb68eeb3
[DIR]  .git\objects\b4
[FILE] .git\objects\b4\3e801d1b1e60b7a21ad88e1d6741eba23760c2
[FILE] .git\objects\b4\62721d55ca7e541cec4345e40cd7070e784b44
[DIR]  .git\objects\b5
[FILE] .git\objects\b5\5c367371b0ed5eba300e8cb88cf98e05f2874a
[DIR]  .git\objects\b6
[FILE] .git\objects\b6\7183d3590f1be0ff8e95a65030841bd55c5234
[DIR]  .git\objects\b7
[FILE] .git\objects\b7\868a6e4df3d8e0f6099e8d01a9a2e736ad76ce
[FILE] .git\objects\b7\99e7d12d700a3817b8ecff79848c4bd76d2bce
[DIR]  .git\objects\b9
[FILE] .git\objects\b9\3bc3efb8a6958315edce31e3ab03f9878d140c
[DIR]  .git\objects\ba
[FILE] .git\objects\ba\8dcbbec2afacd7d695a156de94151236d5848a
[FILE] .git\objects\ba\f2fcea27a64cb2581d0935ad676df848e2b964
[DIR]  .git\objects\bb
[FILE] .git\objects\bb\0dfa38495689ceaa78816f22383df5e9a58515
[FILE] .git\objects\bb\10d52075d2bfba2fde6b5438fedc205f7c6863
[FILE] .git\objects\bb\2ecf9f0ab026c9aff9dd7563850f96bdc3e9c7
[FILE] .git\objects\bb\f44ac3dfd6d1871785e875bccc789bdcddba46
[FILE] .git\objects\bb\fcdba73d6b720a84a41ed5c7e1be4bc8765252
[DIR]  .git\objects\bc
[FILE] .git\objects\bc\191ceab3dd6c2d6e119fcb44a4d543d29026e1
[FILE] .git\objects\bc\1ff0da981e63e7604ea3a67c2b22c129d3133c
[FILE] .git\objects\bc\29c24091b41c5f35aacacf3df25e4d19e4aeca
[DIR]  .git\objects\bd
[FILE] .git\objects\bd\7174f3a7e58bb6905569b3e03a252873102aba
[FILE] .git\objects\bd\9d786fdb832df97f540387bb2b2bdb6669cc90
[DIR]  .git\objects\be
[FILE] .git\objects\be\4e13773d8458114ba7a4171dc7873a49fd1621
[FILE] .git\objects\be\d0fb8a994f12042984fd8124170fef4d054d80
[DIR]  .git\objects\bf
[FILE] .git\objects\bf\7025e2b533e417b5a420aeeedf4ea6166b6cc6
[DIR]  .git\objects\c0
[FILE] .git\objects\c0\1a1b7572b1a0192d051b6e4469c4739426fdf1
[FILE] .git\objects\c0\e5393447776d4d3735d82b2d55269877a740de
[DIR]  .git\objects\c1
[FILE] .git\objects\c1\22fbde2e02514633788b37ee1926152a4b1b17
[FILE] .git\objects\c1\856d52fbbaaee130c61987b5a36143d1f1395c
[DIR]  .git\objects\c2
[FILE] .git\objects\c2\a70fa46caa161126cf455366bd5682421feb62
[DIR]  .git\objects\c3
[FILE] .git\objects\c3\40547dd32033a077017b886c7b73d527311558
[FILE] .git\objects\c3\56fa483fce4660ef5e7f728a63284388beaa01
[DIR]  .git\objects\c4
[FILE] .git\objects\c4\17ae0d6ee9b75f818e9f7eefa674e96ad277bd
[FILE] .git\objects\c4\f57984c85b5ce6660e771b97056a5ef4ef0ef8
[DIR]  .git\objects\c5
[FILE] .git\objects\c5\40cbc1655f8ee019d3fc07f396dc1ea0bcc1f4
[FILE] .git\objects\c5\54909a021ad54379221d20c28d8e6b028e3a6c
[DIR]  .git\objects\c6
[FILE] .git\objects\c6\13821c515d166024e3f9b8f5bc6eb3bf592cd0
[FILE] .git\objects\c6\204d3006663da9bcec0d501b0e82c45723bdc3
[FILE] .git\objects\c6\29e207a2ed2668cb41b7d8798fbac580e1bb70
[FILE] .git\objects\c6\7063b4a75c080272643bec7552ff410a717484
[DIR]  .git\objects\c7
[FILE] .git\objects\c7\578c68ce155294991e58d9828242d300819de8
[DIR]  .git\objects\c8
[FILE] .git\objects\c8\1653bf408d02a330dd92fc4ce4d91d7839486e
[FILE] .git\objects\c8\32938dfedb4540c18ac80d60a433fad195dbaf
[FILE] .git\objects\c8\5dd7964106dd34bb76a8a54c5eb4b0e0857f7d
[DIR]  .git\objects\c9
[FILE] .git\objects\c9\2d98ec380d2d369bf1e4f1932deb5385f392cc
[FILE] .git\objects\c9\d91ac018046bb861d0d1d5472d2b468fb8e716
[DIR]  .git\objects\ca
[FILE] .git\objects\ca\478348576cbd05c71e9c9f36d7fb1b46291373
[FILE] .git\objects\ca\aaf4e0c421827ade81917b506b6a28784e09c9
[DIR]  .git\objects\cb
[FILE] .git\objects\cb\07515bc83dbae35673cd524f4d4b0d2068f082
[DIR]  .git\objects\cc
[FILE] .git\objects\cc\c6018f9068625ebd596379dc03639120a3be5d
[FILE] .git\objects\cc\db7e50dd79744cce31e98a1737466abab1f9d5
[FILE] .git\objects\cc\e99126792693f1c00a180c4fa3ec56ac70bb30
[DIR]  .git\objects\cd
[FILE] .git\objects\cd\054dddf2afcaa2abfc45d0ad6b28e3aea08072
[FILE] .git\objects\cd\451aa0b1284ffffc9e04c44b88b02915e1ee0b
[FILE] .git\objects\cd\89b9ef1b0c2f530d67f42967a2d067a5ccfd39
[FILE] .git\objects\cd\e79bc1014d02330dee1f42674bb16f83a17266
[DIR]  .git\objects\ce
[FILE] .git\objects\ce\0f2c4e4e5b83571d1a2785274d0d24a219e408
[FILE] .git\objects\ce\926dcf79996b0ca99f61d51cf7e6e4127eedaf
[FILE] .git\objects\ce\e9039761174aa7295905f38447d022e71c0db4
[DIR]  .git\objects\cf
[FILE] .git\objects\cf\06a1156a1fa2bd5114f5c42d54fe397919c014
[FILE] .git\objects\cf\2e0f53ea8a0a78afa408de5145b924a35dc56d
[FILE] .git\objects\cf\7d975dc1d57a70480b21c7634a669cc8ad1da1
[FILE] .git\objects\cf\9310ab611033a77e618b644c0226a1ce64e4b1
[FILE] .git\objects\cf\eddb40630e1b8e9024ce821e0391dca734dd28
[FILE] .git\objects\cf\f06b78ada5b6873f6d845b27473ed078a5470c
[DIR]  .git\objects\d0
[FILE] .git\objects\d0\22f9d58c6b8e4cebd998ab362c713d46326e0c
[FILE] .git\objects\d0\47d5e846ce7a93f5ca2ad9c3aaa2777e50d677
[DIR]  .git\objects\d1
[FILE] .git\objects\d1\2bcbc76ed30239132cc61c564f61f652e0ad1c
[FILE] .git\objects\d1\9eda0bb192bd94ea9fe815cdd11ac5c05f0fc3
[DIR]  .git\objects\d2
[FILE] .git\objects\d2\68f9edc17e9faef627e68df45a06acabe675b3
[FILE] .git\objects\d2\8fdf3fd36014e8eb530b2d9828851dba625cd7
[DIR]  .git\objects\d3
[FILE] .git\objects\d3\9e8afcbca950bda59df7415cd551c3a8f1fb3c
[FILE] .git\objects\d3\e55e972a93ac996a54f14bff37ace48e051125
[DIR]  .git\objects\d4
[FILE] .git\objects\d4\126e70e8694234261ba5a1286425f981312a74
[FILE] .git\objects\d4\279ae9e25f355ac948b2f39df428a84bae213a
[FILE] .git\objects\d4\514e12e38d3cbc0fa1925c556d9ed1b4367bbb
[FILE] .git\objects\d4\7479aa9fca641284da79ce9d997046f5e1e9b1
[FILE] .git\objects\d4\7cf6b1dd89c7680962eddc2f45a808968a966a
[FILE] .git\objects\d4\bb549418b5b4d4cc477b980a2fa32b03be8d67
[DIR]  .git\objects\d5
[FILE] .git\objects\d5\1946fe52e29acf632b3056ad872589f4fdaaa7
[FILE] .git\objects\d5\5d053e1bb929cb881e94d09fa859890aeaa17e
[FILE] .git\objects\d5\af93855ce15e41ce2c496c30f139e569e809e9
[DIR]  .git\objects\d6
[FILE] .git\objects\d6\0348b14e382864b13bcb71dee3b59143d8dad4
[DIR]  .git\objects\d7
[FILE] .git\objects\d7\8ca14cfba0e1e14fed97509c415b6849b85195
[DIR]  .git\objects\d8
[FILE] .git\objects\d8\7b80c7059385359d653a22f33408c5bd358a7e
[FILE] .git\objects\d8\860a8130adb4139c180bba2f7fff8dcbc2018b
[FILE] .git\objects\d8\b22a1aff23ad84ef668d26361da95779cdde2f
[FILE] .git\objects\d8\d3bcd3fc90f6d559368d18e4168d8404ef649d
[DIR]  .git\objects\d9
[FILE] .git\objects\d9\0e11030c5e5248acd37839873882c497001160
[FILE] .git\objects\d9\4aa4ce768e24558e5dd2cf8db4cde0802a453e
[FILE] .git\objects\d9\a7282b99d0ee8b134c6cc4969c28441328d23a
[DIR]  .git\objects\da
[FILE] .git\objects\da\549538361ea1e8ef2e67fdc8666e763106b858
[FILE] .git\objects\da\8baae5efc694d6c72b64d42c36cec9dbae15f0
[FILE] .git\objects\da\933d9ed5c2d391647a17779f5a93e1d9586b57
[DIR]  .git\objects\db
[FILE] .git\objects\db\6b5e0f2778af02c67ceb5c458df62008d17696
[FILE] .git\objects\db\be7b7c851a217f093bde07217786c482eec4e1
[FILE] .git\objects\db\df47df81e9d27fe160a314a37d0cce22651376
[DIR]  .git\objects\dc
[FILE] .git\objects\dc\08508dcc903931a98fa82e0fbe8fe9834a7de1
[FILE] .git\objects\dc\643f62fa7d925cca042ab5be3865202f664b6a
[FILE] .git\objects\dc\bab0d6de92e3e6579e765025e13f8ca4bddc86
[FILE] .git\objects\dc\c964aded12c3e5456b74cef0b9d8ef6928d7a3
[FILE] .git\objects\dc\d3b48a5690315ea95d78d93ed19733529afe9e
[FILE] .git\objects\dc\f648ae9310fa14525bf3d4e6bae95b9ef70874
[DIR]  .git\objects\dd
[FILE] .git\objects\dd\6878bea0badef222cf11d9124439badf3d755c
[DIR]  .git\objects\de
[FILE] .git\objects\de\790a80460238c3cde8fed9f30d0d1b71e52813
[FILE] .git\objects\de\f1770c2f6f32b3f413506b3136d0c4452de4bb
[DIR]  .git\objects\df
[FILE] .git\objects\df\1d509dc2708f66ef4912f4060ef456b1934749
[FILE] .git\objects\df\4b561a331a713885853ba7021a5fa1e420bbc0
[FILE] .git\objects\df\7d3fe1add16fcbf48efd6269127af7646beef9
[FILE] .git\objects\df\ee4c09b0690ea83ae3303849cb95f89cf45363
[DIR]  .git\objects\e0
[FILE] .git\objects\e0\cfea938761a6b3c3fc556ef43a2c1206c4623c
[DIR]  .git\objects\e1
[FILE] .git\objects\e1\1ea159ac4cfb0760a2ea37c8aaa2fb0a21e02b
[DIR]  .git\objects\e2
[FILE] .git\objects\e2\09c5acac4433b37599998e357f7c9ef6004461
[FILE] .git\objects\e2\de349966956e2516d639055a7a57d607b636de
[FILE] .git\objects\e2\f70cfaf7c92061cf0945ac533425a8eb55918b
[DIR]  .git\objects\e3
[FILE] .git\objects\e3\0c8af8f67ba8d5287d738ddb126bb67fd13a85
[FILE] .git\objects\e3\1b14afa125863a19e538a123b349ff0091e792
[FILE] .git\objects\e3\2f59ac412f1e2e517ce225716c5f0a9d0a07c6
[FILE] .git\objects\e3\4164cc3dcf0170e11ff59d9a391136c56fa35a
[FILE] .git\objects\e3\5ad5196ade62038676fd9cdc20d82c74c8eab8
[FILE] .git\objects\e3\ae48903fab590507f3e986ab0d1f1cb1d60636
[DIR]  .git\objects\e4
[FILE] .git\objects\e4\87b71f11dd7d22357bffbef2dbb9f24f78be4f
[DIR]  .git\objects\e5
[FILE] .git\objects\e5\43ccb046831c9d7493e46911c9b36ccfe63493
[FILE] .git\objects\e5\5520fef08ded8975fd4cf37d1b3415e37402e0
[FILE] .git\objects\e5\5a9fe2058c210e2a087ec7b2387fe168fa3ab2
[FILE] .git\objects\e5\612663d9ec066c8d8106eae25cf11c5816afeb
[FILE] .git\objects\e5\c02512bc37fb139ef20a02b2150d1a61ee2730
[DIR]  .git\objects\e6
[FILE] .git\objects\e6\10cb8c929237860170e55148763590b50874df
[FILE] .git\objects\e6\989ec2807c0f8df1096be67696adcc6895d927
[FILE] .git\objects\e6\9de29bb2d1d6434b8b29ae775ad8c2e48c5391
[FILE] .git\objects\e6\a78a5850b38e5cb1202e8aedf1ad69be8e6cd2
[FILE] .git\objects\e6\eb7667447f003ad44a896dd7871cafa240db46
[DIR]  .git\objects\e7
[FILE] .git\objects\e7\b00585949e978fb63227d872cd4739e0de0c7e
[DIR]  .git\objects\e8
[FILE] .git\objects\e8\5a2a1be83605ef6e79e725ba4521f8d3692d23
[DIR]  .git\objects\e9
[FILE] .git\objects\e9\48d859f0bae580c9fea9d6408a23052ae7bd50
[FILE] .git\objects\e9\82c4481b1949d7617527f146abb1f1e2327ef4
[FILE] .git\objects\e9\b5722073ea1e57816d9a3471842d3a80bdd3e9
[DIR]  .git\objects\ea
[FILE] .git\objects\ea\6d71ca15026a04c95a3c340b1ccfa477d65e0f
[FILE] .git\objects\ea\bb3ec77a55a505f85ce55f73469f61701714bd
[FILE] .git\objects\ea\c460f2c9845a7ab8d5fd6ef06ce94900881fab
[FILE] .git\objects\ea\c87ea4813cb4a0b61f7a2701430fc10e593a65
[DIR]  .git\objects\eb
[FILE] .git\objects\eb\7c2fdab722fbb133ca9ff8f25c6de5a867384e
[FILE] .git\objects\eb\b613751db1d4c87164f8daf1dca2fe257c3045
[FILE] .git\objects\eb\ea4a934f76dbbeec236f15d5ced2e3bad1f102
[FILE] .git\objects\eb\ed41826b648719f700c2758cedb870099c1815
[DIR]  .git\objects\ec
[FILE] .git\objects\ec\1c4d601cd11688dc8e138bafe6f091087f41dd
[FILE] .git\objects\ec\63becebf7c47c1fa2789b795a39baa64640e55
[FILE] .git\objects\ec\e4180263215c26c67bcd1ba9a131f2dafd135f
[FILE] .git\objects\ec\f025b891405f55c9f6b59763c856a8b19a023c
[DIR]  .git\objects\ed
[FILE] .git\objects\ed\0d738f8c9f6bc049af6c7d6b0cf6bc24a084cf
[FILE] .git\objects\ed\5b5b4a38d904ea357aeb24c89e1cc47c75ed79
[FILE] .git\objects\ed\837a7e63d0abb0301b552bc78a01c1f9babf08
[FILE] .git\objects\ed\a836eb37259f66166e55e345fc043c43ed3c3e
[DIR]  .git\objects\ee
[FILE] .git\objects\ee\08ae94554db8c1ce1267f953bfba40bfb3fcf0
[FILE] .git\objects\ee\0ce0f79ba796c479174af598f66f774476397f
[FILE] .git\objects\ee\d175898dff79553993178707dc19974b4595a8
[DIR]  .git\objects\ef
[FILE] .git\objects\ef\3c2ebc0609822202d9f76697dc46b1c2694253
[FILE] .git\objects\ef\b1b1cacb9bc33ecce0a5a7d299fcb1a9aa0874
[DIR]  .git\objects\f0
[FILE] .git\objects\f0\250dd5c767e0e811805b51877f6c245528dd85
[FILE] .git\objects\f0\d57bb8584c212ad77db3959d2b27f7d9b2369b
[FILE] .git\objects\f0\f521528f25561a27e00f5ac153101da92013a3
[DIR]  .git\objects\f1
[FILE] .git\objects\f1\86f74dba5fa79604f2f7cf8085e2dba34a1977
[DIR]  .git\objects\f2
[FILE] .git\objects\f2\a6fb5060abd85cd66588a0d2c99c8509412e91
[FILE] .git\objects\f2\b8541723aca5d7ff89849720d3b8ff06c95034
[DIR]  .git\objects\f3
[FILE] .git\objects\f3\c443158d8139a738f1adcc8c2607bfdef40cd4
[DIR]  .git\objects\f4
[FILE] .git\objects\f4\4d99ab5edd62ec9cbffc9924c36d1c2f6cc269
[FILE] .git\objects\f4\5e9b4cc3fe65b17deb82ae03221d1675f92cd8
[DIR]  .git\objects\f5
[FILE] .git\objects\f5\3e541c546437a71e89e7c9dbab5240d94ab95d
[FILE] .git\objects\f5\7da450cbcc8ffb5cb29dc28efc577fc3c9676e
[FILE] .git\objects\f5\fa53381f6604eb2ef2a2504a9894b80893a00c
[DIR]  .git\objects\f6
[FILE] .git\objects\f6\63b68523458dc640a0c7cbe1d371d0aa27c8ba
[FILE] .git\objects\f6\b41a5e577123a65ab1ec4da1014eb47a536ba1
[FILE] .git\objects\f6\b96dd25b7b79dd47563519e01847d3585082ba
[FILE] .git\objects\f6\ee2fcddcdaa2a82c80de31f3ad1cc6b3253c08
[FILE] .git\objects\f6\ef35a7b2566a924734fc2586cba2e042d08076
[DIR]  .git\objects\f8
[FILE] .git\objects\f8\2a600efdf6340f3e8ce3c2fb72974a5c5e2603
[FILE] .git\objects\f8\757c211a996d1892e44dffbbd90f3209a805f5
[DIR]  .git\objects\f9
[FILE] .git\objects\f9\d06b354dfb7d656abfe83d8baf2a1bdb9748db
[DIR]  .git\objects\fa
[FILE] .git\objects\fa\043db6dacb51e39408a1a8ff1e9d68c2971236
[DIR]  .git\objects\fb
[FILE] .git\objects\fb\ccd875b73df232d27a01b27e2e683c28e41961
[FILE] .git\objects\fb\f0d79adbc6ae9c659f714d5e1c1b265515f90a
[DIR]  .git\objects\fc
[FILE] .git\objects\fc\222ab1ee4ca7ea4c239d5bf866b1ef774fad7e
[FILE] .git\objects\fc\48166d1d11777bebe8b7c76642ca0e8937752a
[FILE] .git\objects\fc\65af395823c0fc4d3f38b76f618fa3eb196921
[DIR]  .git\objects\fd
[FILE] .git\objects\fd\617b9a229ccc6fe9fe8a68e733d6c8948a0736
[FILE] .git\objects\fd\789db2d945c4b64c64f6d10d2119eebaf01c1f
[FILE] .git\objects\fd\bf505446167b6dbc463e45a09b6ccefbb75cec
[FILE] .git\objects\fd\f3bf484d98e87f7927453204d5020663f72adf
[DIR]  .git\objects\fe
[FILE] .git\objects\fe\0c057721811f0d54a6ce5b2be55d3ed7e14fff
[FILE] .git\objects\fe\6ba4c549bcb22c25e2687f7ce7d3fcc5b0e289
[FILE] .git\objects\fe\b921e1bb44fe4cd60f0d6fd6c6841493c8ffb6
[DIR]  .git\objects\ff
[FILE] .git\objects\ff\bdcc8bd84b00e3b583cb645efd0fe64e9a3960
[FILE] .git\objects\ff\c08929035b3b8c0565d69a81d8d91aa0125cfc
[DIR]  .git\objects\info
[DIR]  .git\objects\pack
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.idx
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.pack
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.rev
[FILE] .git\ORIG_HEAD
[FILE] .git\packed-refs
[DIR]  .git\refs
[DIR]  .git\refs\heads
[FILE] .git\refs\heads\main
[DIR]  .git\refs\remotes
[DIR]  .git\refs\remotes\origin
[FILE] .git\refs\remotes\origin\HEAD
[FILE] .git\refs\remotes\origin\main
[DIR]  .git\refs\tags
[DIR]  .ralfai
[FILE] .ralfai\config.yaml
[FILE] .ralfai\session.md
[FILE] ARCHITECTURE.md
[DIR]  assets
[FILE] assets\ralfai-icon.png
[FILE] assets\ralfai-icon.svg
[FILE] assets\ralfai-logo.svg
[FILE] PROMPTS.md
[FILE] README.md
[FILE] ROADMAP.md
[DIR]  src
[DIR]  src\LogoRasterizer
[DIR]  src\LogoRasterizer\bin
[DIR]  src\LogoRasterizer\bin\Debug
[DIR]  src\LogoRasterizer\bin\Debug\net10.0
[DIR]  src\LogoRasterizer\obj
[DIR]  src\LogoRasterizer\obj\Debug
[DIR]  src\LogoRasterizer\obj\Debug\net10.0
[DIR]  src\LogoRasterizer\obj\Debug\net10.0\ref
[DIR]  src\LogoRasterizer\obj\Debug\net10.0\refint
[DIR]  src\nupkg
[FILE] src\nupkg\RalfAI.1.1.0.nupkg
[DIR]  src\RalfAI
[DIR]  src\RalfAI.Abstractions
[DIR]  src\RalfAI.Abstractions\bin
[DIR]  src\RalfAI.Abstractions\bin\Debug
[DIR]  src\RalfAI.Abstractions\bin\Debug\net10.0
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.deps.json
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[DIR]  src\RalfAI.Abstractions\bin\Release
[DIR]  src\RalfAI.Abstractions\bin\Release\net10.0
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.deps.json
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\IAgentContext.cs
[DIR]  src\RalfAI.Abstractions\obj
[DIR]  src\RalfAI.Abstractions\obj\Debug
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.AssemblyInfo.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.assets.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.GlobalUsings.g.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.sourcelink.json
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\ref\RalfAI.Abstractions.dll
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\refint\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\project.assets.json
[FILE] src\RalfAI.Abstractions\obj\project.nuget.cache
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.g.props
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.g.targets
[DIR]  src\RalfAI.Abstractions\obj\Release
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.AssemblyInfo.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.assets.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.GlobalUsings.g.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.sourcelink.json
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0\ref
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\ref\RalfAI.Abstractions.dll
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0\refint
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\refint\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\RalfAI.Abstractions.csproj
[DIR]  src\RalfAI.Contexts.DotNet
[DIR]  src\RalfAI.Contexts.DotNet\bin
[DIR]  src\RalfAI.Contexts.DotNet\bin\Debug
[DIR]  src\RalfAI.Contexts.DotNet\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.deps.json
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.DotNet\bin\Release
[DIR]  src\RalfAI.Contexts.DotNet\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.deps.json
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.DotNet\Contexts
[FILE] src\RalfAI.Contexts.DotNet\Contexts\DotNetContext.cs
[DIR]  src\RalfAI.Contexts.DotNet\obj
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.C.C91D3321.Up2Date
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.assets.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.sourcelink.json
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\ref\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\refint\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\project.assets.json
[FILE] src\RalfAI.Contexts.DotNet\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.C.C91D3321.Up2Date
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.assets.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.sourcelink.json
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\ref\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\refint\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\Plugins
[FILE] src\RalfAI.Contexts.DotNet\Plugins\DotNetPlugin.cs
[FILE] src\RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj
[DIR]  src\RalfAI.Contexts.HtmlApps
[DIR]  src\RalfAI.Contexts.HtmlApps\bin
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Debug
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.deps.json
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Release
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.deps.json
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.HtmlApps\Contexts
[FILE] src\RalfAI.Contexts.HtmlApps\Contexts\HtmlAppContext.cs
[DIR]  src\RalfAI.Contexts.HtmlApps\obj
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.C.6CC3700C.Up2Date
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.assets.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.sourcelink.json
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\ref\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\refint\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\project.assets.json
[FILE] src\RalfAI.Contexts.HtmlApps\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.C.6CC3700C.Up2Date
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.assets.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.sourcelink.json
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\ref\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\refint\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\Plugins
[FILE] src\RalfAI.Contexts.HtmlApps\Plugins\StaticServerPlugin.cs
[FILE] src\RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj
[DIR]  src\RalfAI.Contexts.Markdown
[DIR]  src\RalfAI.Contexts.Markdown\bin
[DIR]  src\RalfAI.Contexts.Markdown\bin\Debug
[DIR]  src\RalfAI.Contexts.Markdown\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.deps.json
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.Markdown\bin\Release
[DIR]  src\RalfAI.Contexts.Markdown\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.deps.json
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.Markdown\Contexts
[FILE] src\RalfAI.Contexts.Markdown\Contexts\MarkdownContext.cs
[DIR]  src\RalfAI.Contexts.Markdown\obj
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.C.05DC118C.Up2Date
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.assets.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.sourcelink.json
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\ref\RalfAI.Contexts.Markdown.dll
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\refint\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\project.assets.json
[FILE] src\RalfAI.Contexts.Markdown\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.C.05DC118C.Up2Date
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.assets.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.sourcelink.json
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\ref\RalfAI.Contexts.Markdown.dll
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\refint\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj
[DIR]  src\RalfAI.Core
[FILE] src\RalfAI.Core\AgentEngine.cs
[FILE] src\RalfAI.Core\AgentSessionTypes.cs
[DIR]  src\RalfAI.Core\bin
[DIR]  src\RalfAI.Core\bin\Debug
[DIR]  src\RalfAI.Core\bin\Debug\net10.0
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.deps.json
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Core\bin\Release
[DIR]  src\RalfAI.Core\bin\Release\net10.0
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.deps.json
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\ConversationLogger.cs
[DIR]  src\RalfAI.Core\ImageGeneration
[FILE] src\RalfAI.Core\ImageGeneration\ImageGenerationService.cs
[FILE] src\RalfAI.Core\MarkdownText.cs
[DIR]  src\RalfAI.Core\obj
[DIR]  src\RalfAI.Core\obj\Debug
[DIR]  src\RalfAI.Core\obj\Debug\net10.0
[FILE] src\RalfAI.Core\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.C.DE964921.Up2Date
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.AssemblyInfo.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.assets.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.GlobalUsings.g.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.sourcelink.json
[DIR]  src\RalfAI.Core\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Core\obj\Debug\net10.0\ref\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Core\obj\Debug\net10.0\refint\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\project.assets.json
[FILE] src\RalfAI.Core\obj\project.nuget.cache
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.g.props
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.g.targets
[DIR]  src\RalfAI.Core\obj\Release
[DIR]  src\RalfAI.Core\obj\Release\net10.0
[FILE] src\RalfAI.Core\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.C.DE964921.Up2Date
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.AssemblyInfo.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.assets.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.GlobalUsings.g.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.sourcelink.json
[DIR]  src\RalfAI.Core\obj\Release\net10.0\ref
[FILE] src\RalfAI.Core\obj\Release\net10.0\ref\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\obj\Release\net10.0\refint
[FILE] src\RalfAI.Core\obj\Release\net10.0\refint\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\Plugins
[FILE] src\RalfAI.Core\Plugins\AIPlugin.cs
[FILE] src\RalfAI.Core\Plugins\FileSystemPlugin.cs
[FILE] src\RalfAI.Core\Plugins\ImageGenerationPlugin.cs
[FILE] src\RalfAI.Core\Plugins\ImagePlugin.cs
[FILE] src\RalfAI.Core\Plugins\SpeechPlugin.cs
[FILE] src\RalfAI.Core\Plugins\WebPlugin.cs
[FILE] src\RalfAI.Core\RalfAI.Core.csproj
[FILE] src\RalfAI.Core\RalfAIConfig.cs
[FILE] src\RalfAI.Core\RateLimiter.cs
[DIR]  src\RalfAI.Core\Voice
[FILE] src\RalfAI.Core\Voice\SpeechService.cs
[FILE] src\RalfAI.sln
[FILE] src\RalfAI\appsettings.json
[DIR]  src\RalfAI\bin
[DIR]  src\RalfAI\bin\Debug
[DIR]  src\RalfAI\bin\Debug\net10.0
[FILE] src\RalfAI\bin\Debug\net10.0\Anthropic.dll
[FILE] src\RalfAI\bin\Debug\net10.0\appsettings.json
[FILE] src\RalfAI\bin\Debug\net10.0\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Azure.Core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Debug\net10.0\last-folder.txt
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Debug\net10.0\OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.deps.json
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.dll
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.exe
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Debug\net10.0\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Spectre.Console.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.ClientModel.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.Numerics.Tensors.dll
[DIR]  src\RalfAI\bin\Release
[DIR]  src\RalfAI\bin\Release\net10.0
[FILE] src\RalfAI\bin\Release\net10.0\Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\appsettings.json
[FILE] src\RalfAI\bin\Release\net10.0\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Azure.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Release\net10.0\OpenAI.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish
[FILE] src\RalfAI\bin\Release\net10.0\publish\Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\appsettings.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Azure.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.deps.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.exe
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Spectre.Console.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.ClientModel.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.Numerics.Tensors.dll
[FILE] src\RalfAI\bin\Release\net10.0\ralf.deps.json
[FILE] src\RalfAI\bin\Release\net10.0\ralf.dll
[FILE] src\RalfAI\bin\Release\net10.0\ralf.exe
[FILE] src\RalfAI\bin\Release\net10.0\ralf.pdb
[FILE] src\RalfAI\bin\Release\net10.0\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Release\net10.0\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Release\net10.0\Spectre.Console.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.ClientModel.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.Numerics.Tensors.dll
[FILE] src\RalfAI\config.example.json
[FILE] src\RalfAI\ContextRegistry.cs
[FILE] src\RalfAI\FileBrowser.cs
[FILE] src\RalfAI\FolderConfigStore.cs
[FILE] src\RalfAI\FullScreenConsole.cs
[FILE] src\RalfAI\InputCollector.cs
[FILE] src\RalfAI\LastFolderStore.cs
[DIR]  src\RalfAI\obj
[DIR]  src\RalfAI\obj\Debug
[DIR]  src\RalfAI\obj\Debug\net10.0
[FILE] src\RalfAI\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI\obj\Debug\net10.0\apphost.exe
[FILE] src\RalfAI\obj\Debug\net10.0\ralf.dll
[FILE] src\RalfAI\obj\Debug\net10.0\ralf.pdb
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.AssemblyInfo.cs
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.AssemblyInfoInputs.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.assets.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.AssemblyReference.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.FileListAbsolute.txt
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.Up2Date
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.genruntimeconfig.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.GlobalUsings.g.cs
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.sourcelink.json
[DIR]  src\RalfAI\obj\Debug\net10.0\ref
[FILE] src\RalfAI\obj\Debug\net10.0\ref\ralf.dll
[DIR]  src\RalfAI\obj\Debug\net10.0\refint
[FILE] src\RalfAI\obj\Debug\net10.0\refint\ralf.dll
[FILE] src\RalfAI\obj\project.assets.json
[FILE] src\RalfAI\obj\project.nuget.cache
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.dgspec.json
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.g.props
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.g.targets
[DIR]  src\RalfAI\obj\Release
[DIR]  src\RalfAI\obj\Release\net10.0
[FILE] src\RalfAI\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI\obj\Release\net10.0\apphost.exe
[FILE] src\RalfAI\obj\Release\net10.0\DotnetToolSettings.xml
[FILE] src\RalfAI\obj\Release\net10.0\PublishOutputs.64b92f381d.txt
[FILE] src\RalfAI\obj\Release\net10.0\PublishOutputs.e8d25c20c9.txt
[FILE] src\RalfAI\obj\Release\net10.0\ralf.dll
[FILE] src\RalfAI\obj\Release\net10.0\ralf.pdb
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.AssemblyInfo.cs
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.AssemblyInfoInputs.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.assets.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.AssemblyReference.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.FileListAbsolute.txt
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.Up2Date
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.genruntimeconfig.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.GlobalUsings.g.cs
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.sourcelink.json
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.toolssettingsinput.cache
[DIR]  src\RalfAI\obj\Release\net10.0\ref
[FILE] src\RalfAI\obj\Release\net10.0\ref\ralf.dll
[DIR]  src\RalfAI\obj\Release\net10.0\refint
[FILE] src\RalfAI\obj\Release\net10.0\refint\ralf.dll
[FILE] src\RalfAI\obj\Release\RalfAI.1.0.0.nuspec
[FILE] src\RalfAI\obj\Release\RalfAI.1.1.0.nuspec
[FILE] src\RalfAI\Program.cs
[DIR]  src\RalfAI\Properties
[FILE] src\RalfAI\Properties\launchSettings.json
[FILE] src\RalfAI\RalfAI.csproj
[FILE] src\RalfAI\RalfAIApp.cs
[FILE] USAGE.md

---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=src/RalfAI/InputCollector.cs
result: using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using RalfAI.Voice;

namespace RalfAI;

/// <summary>
/// Collects multi-line input into a plain (unboxed) prompt line and submits it on Ctrl+Enter.
///
/// This solves the problem of pasting text that contains blank lines:
/// every line (including empty ones) is accumulated, and only Ctrl+Enter triggers processing.
///
/// In an interactive console, input is collected live, key by key, with no persistent key-binding
/// list — press F1 for a full-screen overlay with the command reference instead. When stdin is
/// redirected (piped/scripted input), falls back to a plain line-by-line prompt with typed
/// /-commands, since key-by-key reading (and therefore function keys) requires a real console.
///
/// Interactive key bindings (live prompt only — see F1 for the full reference):
///   Ctrl+Enter        submit the current buffer
///   Enter              insert a newline (multi-line compose / paste)
///   Esc                clear the buffer and start over (while composing)
///   F1                 show command reference (full-screen overlay)
///   F2                 dictate a line via the microphone (requires AzureSpeech configuration)
///   F3                 read the agent's last response aloud (requires AzureSpeech configuration)
///   F5                 clear the screen
///   F6                 switch to a different generation context
///   F7                 switch to a different target folder
///   F8                 toggle debug messages (tool calls, rate-limit notices)
///   F10 / Ctrl+Q       quit RalfAI
///   Ctrl+C             interrupt the current turn while the agent is working (twice = force quit) — handled in RalfAIApp, not here
///   Esc / Ctrl+X       same interrupt as Ctrl+C, while the agent is working — handled in RalfAIApp, not here
///
/// Piped/scripted input (redirected stdin, see <see cref="CollectPlain"/>) has no function
/// keys — it uses typed "/"-commands instead, with their own reference shown by "/help"
/// (<see cref="ShowHelpOverlayPlain"/>), distinct from the interactive one above.
/// </summary>
public sealed class InputCollector(SpeechService? speech = null)
{
    private readonly List<string> _lines = [];
    private string? _hintOverride;
    private string? _status;

    /// <summary>
    /// Collects input until Ctrl+Enter and returns the assembled text.
    /// Returns <c>null</c> when the user quits (F10/Ctrl+Q, or EOF/'exit' in piped mode).
    /// Any other string (including the internal "/speak", "/clear" markers) is
    /// returned as-is for the caller to handle.
    /// </summary>
    /// <param name="status">
    /// Ready-to-render Spectre markup (e.g. current context/model/folder) shown above the
    /// prompt line — lets the caller keep the user oriented without a persistent command list.
    /// </param>
    public string? Collect(string? status = null)
    {
        _lines.Clear();
        _status = status;
        return Console.IsInputRedirected ? CollectPlain() : CollectLive();
    }

    // ── Interactive mode: live plain prompt, no border ──────────────────────

    private string? CollectLive()
    {
        var current = new StringBuilder();
        string? result = null;
        var exitLoop = false;

        // Use the terminal's own cursor instead of a fake glyph — avoids Unicode fallback
        // artifacts and looks orphaned less on an otherwise-empty continuation line.
        try { Console.CursorVisible = true; } catch (IOException) { }

        while (!exitLoop)
        {
            var showHelp = false;

            AnsiConsole.Live(RenderPrompt(current)).Start(ctx =>
            {
                var innerDone = false;

                while (!innerDone)
                {
                    ctx.UpdateTarget(RenderPrompt(current));
                    ctx.Refresh();

                    var key = Console.ReadKey(intercept: true);
                    var ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;

                    if (key.Key == ConsoleKey.Enter && ctrl)
                    {
                        if (current.Length > 0)
                        {
                            _lines.Add(current.ToString());
                            current.Clear();
                        }

                        if (_lines.Count > 0)
                        {
                            result = string.Join("\n", _lines).Trim();
                            exitLoop = true;
                            innerDone = true;
                        }
                        else
                        {
                            _hintOverride = "[yellow]Buffer is empty — type something first.[/]";
                        }
                        continue;
                    }

                    if (key.Key == ConsoleKey.F10 || (key.Key == ConsoleKey.Q && ctrl))
                    {
                        result = null;
                        exitLoop = true;
                        innerDone = true;
                        continue;
                    }

                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            _lines.Add(current.ToString());
                            current.Clear();
                            break;

                        case ConsoleKey.F1:
                            showHelp = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F2:
                            DictateInto(ctx);
                            break;

                        case ConsoleKey.F3:
                            result = "/speak";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F5:
                            result = "/clear";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F6:
                            result = "/switch-context";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F7:
                            result = "/switch-folder";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F8:
                            result = "/toggle-debug";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.Backspace:
                            if (current.Length > 0)
                                current.Remove(current.Length - 1, 1);
                            else if (_lines.Count > 0)
                            {
                                current.Append(_lines[^1]);
                                _lines.RemoveAt(_lines.Count - 1);
                            }
                            break;

                        case ConsoleKey.Escape:
                            _lines.Clear();
                            current.Clear();
                            break;

                        default:
                            if (!char.IsControl(key.KeyChar))
                                current.Append(key.KeyChar);
                            break;
                    }
                }

                ctx.UpdateTarget(RenderPrompt(current));
                ctx.Refresh();
            });

            if (showHelp) ShowHelpOverlayInteractive();
        }

        try { Console.CursorVisible = false; } catch (IOException) { }

        return result;
    }

    // ── Voice dictation (F2) ─────────────────────────────────────────────────

    /// <summary>
    /// Listens on the microphone and appends the recognized text as a new line in the
    /// buffer, as if the user had typed it. No-op with a hint if AzureSpeech isn't configured.
    /// </summary>
    private void DictateInto(LiveDisplayContext ctx)
    {
        if (speech is null)
        {
            _hintOverride = "[red]Voice non configurata — imposta AzureSpeech in appsettings.json.[/]";
            return;
        }

        ctx.UpdateTarget(new Markup("[cyan]🎤 In ascolto… parla pure — si ferma da solo alla pausa.[/]"));
        ctx.Refresh();

        var (spoken, error) = speech.RecognizeOnceAsync().GetAwaiter().GetResult();

        if (spoken is not null)
            _lines.Add(spoken);

        _hintOverride = spoken is not null
            ? null
            : $"[red]{Markup.Escape(error ?? "Riconoscimento non riuscito.")}[/]";
    }

    private IRenderable RenderPrompt(StringBuilder current)
    {
        var displayLines = new List<string>(_lines) { current.ToString() };
        var hasContent = displayLines.Any(l => l.Length > 0);

        // No fake cursor glyph here — the real terminal cursor (shown during CollectLive) ends
        // up in the right place naturally, since it's just wherever this text stopped printing.
        IRenderable promptLine = hasContent
            ? new Markup(string.Join("\n", displayLines.Select((l, i) =>
                (i == 0 ? "[green]❯[/] " : "  ") + Markup.Escape(l))))
            : new Markup("[green]❯[/] [grey]Type your message… (F1 for help)[/]");

        // Only transient feedback (errors, "buffer empty", dictation status) gets a line below
        // the prompt — no persistent key-binding list. Shown for exactly one render, then cleared.
        var hint = _hintOverride;
        _hintOverride = null;

        var rows = new List<IRenderable>();
        if (_status is not null) rows.Add(new Markup(_status));
        rows.Add(promptLine);
        if (hint is not null) rows.Add(new Markup(hint));

        return rows.Count == 1 ? rows[0] : new Rows(rows);
    }

    // ── Fallback mode: plain line-by-line prompt (redirected/piped stdin) ───
    //
    // Function keys and Ctrl+Enter can't be read from redirected stdin, so this path keeps
    // the original typed /-commands for scripted/piped usage.

    private string? CollectPlain()
    {
        while (true)
        {
            // Prompt changes after the first line to show multi-line mode
            if (_lines.Count == 0)
                AnsiConsole.Markup("[bold green]you>[/] ");
            else
                AnsiConsole.Markup($"[dim]  ({_lines.Count} lines)[/] ");

            string? line;
            try { line = Console.ReadLine(); }
            catch { return null; }

            if (line is null) return null; // EOF / Ctrl+Z

            var trimmed = line.Trim();

            switch (trimmed.ToLowerInvariant())
            {
                case "exit":
                case "/exit":
                    return null;

                case "/cancel":
                    _lines.Clear();
                    AnsiConsole.MarkupLine("[dim]  ↩ Buffer cleared.[/]");
                    continue;

                case "/exe":
                    if (_lines.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[dim]  Buffer is empty — type something first.[/]");
                        continue;
                    }
                    return string.Join("\n", _lines).Trim();

                case "/help":
                    ShowHelpOverlayPlain();
                    continue;

                case "/speak":
                    return "/speak";

                case "/clear":
                    return "/clear";

                case "/switch-context":
                    return "/switch-context";

                case "/switch-folder":
                    return "/switch-folder";

                case "/toggle-debug":
                    return "/toggle-debug";

                case "/voice":
                    if (speech is null)
                    {
                        AnsiConsole.MarkupLine("[red]  Voice non configurata — imposta AzureSpeech in appsettings.json.[/]");
                        continue;
                    }

                    AnsiConsole.MarkupLine("[cyan]  🎤 In ascolto… parla pure.[/]");
                    var (spoken, error) = speech.RecognizeOnceAsync().GetAwaiter().GetResult();

                    if (spoken is null)
                    {
                        AnsiConsole.MarkupLine($"[red]  {Markup.Escape(error ?? "Riconoscimento non riuscito.")}[/]");
                        continue;
                    }

                    _lines.Add(spoken);
                    AnsiConsole.MarkupLine($"[dim]  ({_lines.Count} lines) \"{Markup.Escape(spoken)}\"[/]");
                    continue;

                default:
                    _lines.Add(line);
                    if (_lines.Count == 1)
                        AnsiConsole.MarkupLine("[dim]  (add more lines or type [bold]/exe[/] to send)[/]");
                    continue;
            }
        }
    }

    // ── F1 — full-screen overlay for the interactive (live) prompt ──────────

    private static void ShowHelpOverlayInteractive()
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Key[/]").Centered())
            .AddColumn("[bold]What it does[/]");

        table.AddRow("[green]Ctrl+Enter[/]", "Send the current buffer to the agent");
        table.AddRow("[yellow]Esc[/]",       "While composing: clear the buffer and start fresh");
        table.AddRow("[cyan]F2[/]",          "Dictate a line via microphone (needs AzureSpeech config)");
        table.AddRow("[cyan]F3[/]",          "Read the agent's last response aloud (needs AzureSpeech config)");
        table.AddRow("[cyan]F5[/]",          "Clear the screen");
        table.AddRow("[cyan]F6[/]",          "Switch to a different generation context");
        table.AddRow("[cyan]F7[/]",          "Switch to a different target folder (file browser)");
        table.AddRow("[cyan]F8[/]",          "Toggle debug messages (tool calls, rate-limit notices)");
        table.AddRow("[grey]F1[/]",          "Show this table");
        table.AddRow("[red]F10 / Ctrl+Q[/]", "Quit RalfAI");
        table.AddRow("[yellow]Ctrl+C[/]",    "While the agent is working: interrupt the current turn (twice = force quit)");
        table.AddRow("[yellow]Esc / Ctrl+X[/]", "While the agent is working: same interrupt as Ctrl+C");

        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(new Markup("[dim]Premi un tasto per tornare…[/]")));

        // Safe here: this overlay only runs on the live (non-redirected) path.
        Console.ReadKey(intercept: true);
        AnsiConsole.Clear();
    }

    // ── /help — printed reference for piped/scripted input (no function keys there) ──

    private static void ShowHelpOverlayPlain()
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Command[/]").Centered())
            .AddColumn("[bold]What it does[/]");

        table.AddRow("[green]/exe[/]",            "Send the accumulated buffer to the agent");
        table.AddRow("[yellow]/cancel[/]",        "Clear the buffer without sending");
        table.AddRow("[cyan]/voice[/]",           "Dictate a line via microphone, appended to the buffer (needs AzureSpeech config)");
        table.AddRow("[cyan]/speak[/]",           "Read the agent's last response aloud (needs AzureSpeech config)");
        table.AddRow("[cyan]/clear[/]",           "Clear the screen");
        table.AddRow("[cyan]/switch-context[/]",  "Switch to a different generation context");
        table.AddRow("[cyan]/switch-folder[/]",   "Switch to a different target folder (file browser)");
        table.AddRow("[cyan]/toggle-debug[/]",    "Toggle debug messages (tool calls, rate-limit notices)");
        table.AddRow("[grey]/help[/]",            "Show this table");
        table.AddRow("[red]exit / /exit[/]",      "Quit RalfAI (EOF/Ctrl+Z also works)");

        // No Console.ReadKey here (unlike the interactive overlay): stdin is redirected on
        // this path, so reading a key would throw — just print and let the next ReadLine()
        // prompt continue the loop.
        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}


---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=src/RalfAI/FullScreenConsole.cs
result: namespace RalfAI;

/// <summary>
/// Switches the terminal to its alternate screen buffer for the lifetime of the returned
/// <see cref="IDisposable"/> — the same mechanism vim/less/htop use to take over the whole
/// screen without touching the user's normal scrollback, which is restored exactly as it was
/// on dispose. Spectre.Console has no built-in concept of this (it renders sequentially into
/// whatever buffer is current), but it writes plain ANSI under the hood, so entering/exiting
/// the alternate buffer around a whole session composes fine with it.
/// </summary>
public static class FullScreenConsole
{
    private const string EnterAltScreen = "\x1b[?1049h\x1b[H";
    private const string ExitAltScreen = "\x1b[?1049l";

    /// <summary>No-op (returns null) when output/input isn't a real interactive console.</summary>
    public static IDisposable? Enter()
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected) return null;

        Console.Out.Write(EnterAltScreen);
        try { Console.CursorVisible = false; } catch (IOException) { }

        return new Session();
    }

    private sealed class Session : IDisposable
    {
        public void Dispose()
        {
            try { Console.CursorVisible = true; } catch (IOException) { }
            Console.Out.Write(ExitAltScreen);
        }
    }
}


---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=src/RalfAI/RalfAIApp.cs
result: using Microsoft.Extensions.Configuration;
using Spectre.Console;
using RalfAI.Contexts;
using Spectre.Console.Rendering;

namespace RalfAI;

/// <summary>
/// Console frontend on top of <see cref="AgentEngine"/>: renders the chat loop with
/// Spectre.Console (spinner, tool-call lines, panels) and collects input via
/// <see cref="InputCollector"/>. All agent logic (Microsoft Agent Framework, rate limiting,
/// logging, voice) lives in <see cref="AgentEngine"/> so other frontends can reuse it unchanged.
/// </summary>
public sealed class RalfAIApp
{
    private readonly IConfiguration _config;
    private AgentEngine _session;
    private string _contextKey;
    private bool _debug;

    // Set just before each LLM call so tool-call event handlers can update the spinner.
    private StatusContext? _liveStatus;

    private RalfAIApp(IConfiguration config, string contextKey, bool debug, AgentEngine session)
    {
        _config = config;
        _contextKey = contextKey;
        _debug = debug;
        _session = session;
        AttachSessionEvents();
    }

    public static async Task<RalfAIApp> CreateAsync(
        IConfiguration config, string targetFolder, string contextKey, IAgentContext context, bool debug)
    {
        var session = await AgentEngine.CreateAsync(config, targetFolder, contextKey, context);
        return new RalfAIApp(config, contextKey, debug, session);
    }

    private void AttachSessionEvents()
    {
        _session.ToolCallStarted += OnToolCallStarted;
        _session.ToolCallCompleted += OnToolCallCompleted;
        _session.Notice += OnNotice;
    }

    private void DetachSessionEvents()
    {
        _session.ToolCallStarted -= OnToolCallStarted;
        _session.ToolCallCompleted -= OnToolCallCompleted;
        _session.Notice -= OnNotice;
    }

    // ── Main loop ────────────────────────────────────────────────────────────

    public async Task RunAsync(CancellationToken appToken = default)
    {
        // Ctrl+C cancels the current LLM call (first press); second press exits.
        using var globalCts = CancellationTokenSource.CreateLinkedTokenSource(appToken);

        Console.CancelKeyPress += (_, e) =>
        {
            if (!globalCts.IsCancellationRequested)
            {
                e.Cancel = true;
                globalCts.Cancel();
                AnsiConsole.MarkupLine(
                    "\n  [yellow]⚠ Ctrl+C received — interrupting. Press Ctrl+C again to force quit.[/]");
            }
        };

        var input = new InputCollector(_session.Speech);

        while (!appToken.IsCancellationRequested)
        {

            // Recreate per-turn CTS so Ctrl+C only cancels the current turn.
            using var turnCts = CancellationTokenSource.CreateLinkedTokenSource(appToken);

            // Background task: detect Escape or Ctrl+X during processing.
            // Runs only while we're waiting for the LLM.
            using var escSrc = new CancellationTokenSource();
            var escListener = ListenForEscapeAsync(turnCts, escSrc.Token);

            var text = input.Collect(BuildStatusLine());

            // Stop the escape listener — we're reading input now.
            await escSrc.CancelAsync();
            await escListener.ConfigureAwait(false);

            if (text is null) break;

            // ── Special commands handled by the app (not sent to the agent) ──
            if (text.Equals("/speak", StringComparison.OrdinalIgnoreCase))
            {
                // Listen for Esc/Ctrl+X while audio is playing so it can be interrupted.
                using var escSrcSpeak = new CancellationTokenSource();
                var escListenerSpeak  = ListenForEscapeAsync(turnCts, escSrcSpeak.Token);

                await SpeakLastResponseAsync(turnCts.Token);

                await escSrcSpeak.CancelAsync();
                await escListenerSpeak.ConfigureAwait(false);
                continue;
            }

            if (text.Equals("/clear", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.Clear();
                continue;
            }

            if (text.Equals("/switch-context", StringComparison.OrdinalIgnoreCase))
            {
                await SwitchContext();
                continue;
            }

            if (text.Equals("/switch-folder", StringComparison.OrdinalIgnoreCase))
            {
                await SwitchFolder();
                continue;
            }

            if (text.Equals("/toggle-debug", StringComparison.OrdinalIgnoreCase))
            {
                ToggleDebug();
                continue;
            }

            // ── LLM processing turn ──────────────────────────────────────────
            AgentTurnResult? turnResult = null;

            // Start the escape listener again while the LLM is working.
            using var escSrc2  = new CancellationTokenSource();
            var escListener2   = ListenForEscapeAsync(turnCts, escSrc2.Token);

            try
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots2)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .StartAsync("[dim cyan]thinking…[/]", async ctx =>
                    {
                        _liveStatus = ctx;
                        try
                        {
                            turnResult = await _session.SendAsync(text, turnCts.Token);
                        }
                        finally
                        {
                            _liveStatus = null;
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("  [yellow]↩ Processing cancelled.[/]");
            }

            // Stop escape listener.
            await escSrc2.CancelAsync();
            await escListener2.ConfigureAwait(false);

            if (turnResult is null) continue;

            if (turnResult.Error is not null)
            {
                AnsiConsole.MarkupLine($"\n  [red]⛔ Error: {Markup.Escape(turnResult.Error)}[/]");
                continue;
            }

            ShowAgentResponse(turnResult.Thinking, turnResult.FinalText!);
        }

        AnsiConsole.Write(new Rule().RuleStyle("grey dim"));
        AnsiConsole.MarkupLine(
            $"  [dim]Session saved → {_session.LogPath}[/]");
    }

    /// <summary>Context · model · folder shown above the prompt, so switching (F6/F7) stays visible.</summary>
    private string BuildStatusLine()
    {
        var trimmed = _session.TargetFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var folderName = Path.GetFileName(trimmed);
        if (string.IsNullOrEmpty(folderName)) folderName = trimmed;

        return $"[dim]{Markup.Escape(_session.Context.Name)} · {Markup.Escape(_session.ModelId)} · {Markup.Escape(folderName)}[/]";
    }

    // ── AgentSession event handlers ──────────────────────────────────────────

    private void OnToolCallStarted(ToolCallStart call)
    {
        // Update the spinner status to the current tool — kept even with debug off, it's
        // transient progress feedback rather than debug noise. Shows the human-readable
        // [Description] instead of the raw Plugin.Function name.
        _liveStatus?.Status($"[yellow]{Markup.Escape(call.Description)}…[/]");

        if (!_debug) return;

        // Write a permanent line above the spinner.
        AnsiConsole.MarkupLine(
            $"  [bold yellow]⏺[/] [yellow]{Markup.Escape(call.Description)}[/] " +
            $"[dim]({Markup.Escape(call.Name)})[/][grey]({Markup.Escape(call.ArgsPreview)})[/]");
    }

    private void OnToolCallCompleted(ToolCallResult call)
    {
        if (!_debug) return;

        var preview = call.Result.Length > 120
            ? call.Result[..120].Replace("\n", " ") + "…"
            : call.Result.Replace("\n", " ");

        AnsiConsole.MarkupLine($"    [dim]⎿  {Markup.Escape(preview)}[/]");
    }

    private void OnNotice(AgentNotice notice)
    {
        // Errors always surface; info/warning pacing notices are debug noise.
        if (!_debug && notice.Level != NoticeLevel.Error) return;

        var color = notice.Level switch
        {
            NoticeLevel.Warning => "yellow",
            NoticeLevel.Error => "red",
            _ => "dim"
        };
        AnsiConsole.MarkupLine($"  [{color}]{Markup.Escape(notice.Message)}[/]");
    }

    // ── UI helpers ───────────────────────────────────────────────────────────

    private async Task SpeakLastResponseAsync(CancellationToken ct)
    {
        if (_session.Speech is null)
        {
            AnsiConsole.MarkupLine("  [red]Voice non configurata — imposta AzureSpeech in appsettings.json.[/]");
            return;
        }

        if (string.IsNullOrWhiteSpace(_session.LastFinalText))
        {
            AnsiConsole.MarkupLine("  [yellow]Nessuna risposta dell'agente da leggere ancora.[/]");
            return;
        }

        AnsiConsole.MarkupLine("  [cyan]🔊 Lettura in corso… [dim](Esc per interrompere)[/][/]");
        try
        {
            var error = await _session.Speech.SpeakAsync(_session.LastFinalText, ct);
            if (error is not null)
                AnsiConsole.MarkupLine($"  [red]{Markup.Escape(error)}[/]");
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("  [yellow]↩ Lettura interrotta.[/]");
        }
    }

    private static void ShowAgentResponse(string? thinking, string text)
    {
        AnsiConsole.WriteLine();

        if (!string.IsNullOrWhiteSpace(thinking))
        {
            AnsiConsole.Write(new Rule("[grey]✻ Thinking[/]").RuleStyle("grey dim").LeftJustified());
            AnsiConsole.MarkupLine($"[grey italic]{Markup.Escape(MarkdownText.StripFormatting(thinking))}[/]");
            AnsiConsole.WriteLine();
        }

        AnsiConsole.Write(new Rule("[cyan]agent[/]").RuleStyle("cyan dim"));

        // Console can't render Markdown, so strip it before escaping Spectre markup characters.
        AnsiConsole.MarkupLine(Markup.Escape(MarkdownText.StripFormatting(text)));

        AnsiConsole.Write(new Rule().RuleStyle("cyan dim"));
        AnsiConsole.WriteLine();
    }

    private async Task SwitchContext()
    {
        var options = ContextRegistry.All().ToList();
        var selected = Math.Max(0, options.FindIndex(o => o.Key == _contextKey));
        (string Key, IAgentContext Context)? choice = null;

        AnsiConsole.Live(RenderContextMenu(options, selected)).Start(ctx =>
        {
            var done = false;
            while (!done)
            {
                ctx.UpdateTarget(RenderContextMenu(options, selected));
                ctx.Refresh();

                var key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selected = Math.Max(0, selected - 1);
                        break;

                    case ConsoleKey.DownArrow:
                        selected = Math.Min(options.Count - 1, selected + 1);
                        break;

                    case ConsoleKey.Enter:
                        choice = options[selected];
                        done = true;
                        break;

                    case ConsoleKey.Escape:
                        done = true;
                        break;
                }
            }
        });

        AnsiConsole.Clear();

        if (choice is null)
        {
            AnsiConsole.MarkupLine("  [dim]Cambio contesto annullato.[/]");
            return;
        }

        if (choice.Value.Key == _contextKey)
        {
            AnsiConsole.MarkupLine($"  [dim]Già su {Markup.Escape(choice.Value.Context.Name)}.[/]");
            return;
        }

        DetachSessionEvents();
        _contextKey = choice.Value.Key;
        _session = await AgentEngine.CreateAsync(_config, _session.TargetFolder, _contextKey, choice.Value.Context);
        AttachSessionEvents();
        FolderConfigStore.SaveContext(_session.TargetFolder, _contextKey);

        AnsiConsole.MarkupLine(
            $"  [green]✓ Passato a {Markup.Escape(_session.Context.Name)}.[/] " +
            $"[dim]Modello: {Markup.Escape(_session.ModelId)} · " +
            $"({_session.ResumedMessageCount} messaggi ripresi dal log)[/]");
    }

    private static IRenderable RenderContextMenu(List<(string Key, IAgentContext Context)> options, int selected)
    {
        var lines = options.Select((o, i) =>
        {
            var label = Markup.Escape($"{o.Context.Name} — {o.Context.Description}");
            return i == selected ? $"[invert]{label}[/]" : label;
        });

        var panel = new Panel(string.Join("\n", lines))
        {
            Header = new PanelHeader(" Select generation context "),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("cyan"),
            Padding = new Padding(1, 0, 1, 0)
        };

        return new Rows(panel, new Markup("[dim]↑↓ move · Enter select · Esc cancel[/]"));
    }

    private async Task SwitchFolder()
    {
        var editorCommand = _config["Editor"] ?? FileBrowser.DefaultEditor;
        var picked = FileBrowser.Pick(_session.TargetFolder, editorCommand);
        AnsiConsole.Clear();

        if (picked is null)
        {
            AnsiConsole.MarkupLine("  [dim]Cambio cartella annullato.[/]");
            return;
        }

        var newFolder = Path.GetFullPath(picked);
        if (string.Equals(newFolder, _session.TargetFolder, StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("  [dim]Stessa cartella già in uso.[/]");
            return;
        }

        Directory.CreateDirectory(newFolder);
        LastFolderStore.Save(newFolder);

        // The destination folder may remember its own context (.ralfai/config.yaml);
        // fall back to whatever context is currently active if it doesn't.
        var newContextKey = FolderConfigStore.LoadContext(newFolder) ?? _contextKey;
        var context = ContextRegistry.Create(newContextKey);
        FolderConfigStore.SaveContext(newFolder, newContextKey);

        DetachSessionEvents();
        _contextKey = newContextKey;
        _session = await AgentEngine.CreateAsync(_config, newFolder, _contextKey, context);
        AttachSessionEvents();

        AnsiConsole.MarkupLine(
            $"  [green]✓ Cartella target: {Markup.Escape(newFolder)}.[/] " +
            $"[dim]Contesto: {Markup.Escape(_session.Context.Name)} · Modello: {Markup.Escape(_session.ModelId)} · " +
            $"({_session.ResumedMessageCount} messaggi ripresi dal log)[/]");
    }

    private void ToggleDebug()
    {
        _debug = !_debug;
        FolderConfigStore.SaveDebug(_session.TargetFolder, _debug);

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine(_debug
            ? "  [green]✓ Debug messages: ON[/] [dim](tool calls, rate-limit notices)[/]"
            : "  [dim]✓ Debug messages: OFF[/]");
    }

    // ── Key listener (Esc / Ctrl+X interrupts the current turn) ─────────────

    private static async Task ListenForEscapeAsync(
        CancellationTokenSource turnCts,
        CancellationToken listenerToken)
    {
        // Console.KeyAvailable throws when stdin is redirected (piped/scripted input) —
        // there are no keystrokes to listen for in that case.
        if (Console.IsInputRedirected) return;

        try
        {
            while (!listenerToken.IsCancellationRequested && !turnCts.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    var isEsc    = key.Key == ConsoleKey.Escape;
                    var isCtrlX  = key.Key == ConsoleKey.X &&
                                   key.Modifiers == ConsoleModifiers.Control;
                    if (isEsc || isCtrlX)
                    {
                        AnsiConsole.MarkupLine(
                            "\n  [yellow]⚠ Escape/Ctrl+X — interrupting turn…[/]");
                        await turnCts.CancelAsync();
                        return;
                    }
                }
                await Task.Delay(50, listenerToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* expected */ }
    }
}


---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=src/RalfAI/RalfAI.csproj
result: <Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>ralf</AssemblyName>
    <RootNamespace>RalfAI</RootNamespace>
    <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
    <!-- Suppress SK experimental-API warnings; the AzureAIInference connector is stable enough for use -->
    <NoWarn>$(NoWarn);SKEXP0070;SKEXP0001</NoWarn>

    <!-- dotnet tool packaging (dotnet pack, then dotnet tool install as a global tool) -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>ralf</ToolCommandName>
    <PackageId>RalfAI</PackageId>
    <Version>1.1.0</Version>
    <Description>RalfAI — an autonomous coding agent CLI for .NET, Markdown, and HTML/CSS/JS projects.</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Spectre.Console" Version="0.57.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\RalfAI.Abstractions\RalfAI.Abstractions.csproj" />
    <ProjectReference Include="..\RalfAI.Core\RalfAI.Core.csproj" />
    <ProjectReference Include="..\RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj" />
    <ProjectReference Include="..\RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj" />
    <ProjectReference Include="..\RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- CopyToOutputDirectory alone is enough: PackAsTool packages the whole publish output
         (which already includes this), so AppContext.BaseDirectory-based config loading finds
         it correctly after a global install too. -->
    <None Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>


---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=USAGE.md
result: # Guida all'uso

RalfAI è un agente di coding autonomo a riga di comando, distribuito come
[.NET global tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) (`ralf`). Per come è
fatto internamente vedi [ARCHITECTURE.md](ARCHITECTURE.md); per l'installazione e la configurazione
completa (provider AI, rate limiting, voce) vedi [README.md](README.md). Questa guida copre l'uso
quotidiano una volta installato.

## Avvio

```bash
ralf                          # apre un folder picker (ricorda l'ultima cartella usata)
ralf "C:\path\to\output-folder"
```

L'argomento posizionale con la cartella target è **opzionale**: se non lo passi e non c'è ancora
una cartella ricordata da un avvio precedente, si apre un file browser a schermo intero (vedi
[sotto](#file-browser)) per sceglierla. La cartella viene creata automaticamente se non esiste.

In fase di sviluppo, dalla cartella `src/` del repo, si può anche lanciare senza installare il tool:

```bash
cd src
dotnet run --project RalfAI -- "C:\path\to\output-folder"
```

### Scegliere il context

```bash
ralf "C:\path\to\output-folder" --context html
ralf --list-contexts
```

- `--context <nome>` ha priorità su tutto.
- In alternativa si può impostare `"AgentContext": "dotnet"` in `appsettings.json` (o la variabile
  d'ambiente `RALFAI_AgentContext`).
- Se non specificato, il default è **`markdown`** (vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
  [ROADMAP.md](ROADMAP.md)) — non `dotnet`.
- `--list-contexts` elenca tutti i context registrati (`dotnet`, `markdown`, `html`) senza avviare
  una sessione.
- Un nome sconosciuto fa fallire l'avvio con un errore che elenca i context registrati.
- Ogni cartella target ricorda il proprio context (e il flag debug) in
  `<cartella>/.ralfai/config.yaml`: rilanciare `ralf <cartella>` — o lasciare che si riprenda da
  solo senza argomenti — riparte con lo stesso context.

Altri flag: `--debug` (mostra le tool-call in tempo reale fin dall'avvio) e
`--config <path>` (usa un file di configurazione alternativo — vedi
[README.md](README.md#where-the-config-file-lives)).

## Sessione interattiva

L'intera sessione gira nello screen buffer alternato del terminale (la stessa tecnica di
`vim`/`less`/`htop`): il tuo scrollback normale resta intatto e viene ripristinato all'uscita.
Scrivi il messaggio (anche su più righe, incolla pure liberamente) e usa questi tasti:

| Tasto | Azione |
| --- | --- |
| `Ctrl+Enter` | invia il messaggio |
| `Enter` | va a capo nel buffer (composizione multi-riga) |
| `Esc` | svuota il buffer corrente |
| `F1` | mostra la tabella comandi a schermo intero |
| `F2` | detta una riga via microfono (richiede `AzureSpeech`) |
| `F3` | legge ad alta voce l'ultima risposta dell'agente (richiede `AzureSpeech`, `Esc` interrompe) |
| `F5` | pulisce lo schermo |
| `F6` | cambia generation context (`↑↓`/`Enter`/`Esc` nel menu) |
| `F7` | cambia cartella target (apre il file browser) |
| `F8` | attiva/disattiva i messaggi di debug (tool call, notice di rate-limit) |
| `F10` / `Ctrl+Q` | esce da RalfAI |
| `Ctrl+C` | interrompe il turno corrente (due volte forza la chiusura del processo) |
| `Esc` / `Ctrl+X` durante l'elaborazione | interrompe il turno in corso (come il primo `Ctrl+C`) |

Non c'è più una lista di comandi persistente sullo schermo: sopra il prompt vedi solo lo stato
corrente (context · modello · cartella); premi `F1` quando ti serve il riepilogo dei tasti.

### File browser

Si apre all'avvio senza argomenti (nessuna cartella ricordata) o con `F7` durante una sessione:
un pannello a singolo riquadro per navigare il filesystem, con anteprima file e piccole operazioni
di gestione.

| Tasto | Azione |
| --- | --- |
| `↑` / `↓` | muovi la selezione |
| `PgUp` / `PgDn` | muovi la selezione di 10 |
| `Enter` | apri la cartella selezionata |
| `Backspace` | sali di un livello |
| `F9` | usa la cartella corrente come target RalfAI |
| `F3` | visualizza il file selezionato (sola lettura, max 200 KB, no binari) |
| `F4` | apre il file selezionato in un editor esterno (vedi sotto) |
| `F2` | rinomina l'elemento selezionato |
| `F7` | crea una nuova cartella |
| `F8` / `Del` | elimina (chiede conferma) |
| `F1` | mostra questa tabella |
| `F10` / `Esc` | annulla e torna indietro |

`F4` lancia il comando configurato sotto la chiave `Editor` in `appsettings.json` (override
via `RALFAI_Editor`), passandogli il percorso completo del file selezionato come argomento —
default **`code.exe`** (VS Code) se non configurato. Non aspetta che l'editor si chiuda: apre
e torna subito al file browser. Se il comando non è nel `PATH` o non esiste, viene mostrato un
messaggio d'errore con l'indicazione di correggere `Editor`.

### Input da pipe/script

Se lo stdin è rediretto (es. `echo "..." | ralf ...`), l'input passa in modalità riga-per-riga
(niente box interattivo, niente listener Esc, niente file browser a schermo intero): utile per
test automatici o script. In questa modalità i tasti funzione non esistono, quindi le stesse azioni
sono comandi testuali:

- **`/exe`** invia il buffer accumulato all'agente.
- **`/cancel`** svuota il buffer senza inviare.
- **`/help`** mostra la tabella comandi.
- **`/speak`** legge ad alta voce l'ultima risposta dell'agente (richiede `AzureSpeech`).
- **`/clear`** pulisce lo schermo.
- **`/switch-context`** cambia generation context.
- **`/switch-folder`** cambia cartella target.
- **`/toggle-debug`** attiva/disattiva i messaggi di debug.
- **`/voice`** registra dal microfono e aggiunge il testo riconosciuto come riga nel buffer
  (richiede `AzureSpeech` — vedi sotto): poi invii con `/exe` come di consueto. La registrazione si
  ferma da sola quando l'SDK rileva una pausa nel parlato.
- **`exit`** (o EOF/Ctrl+Z) chiude RalfAI.

## Cosa fa l'agente a ogni richiesta (context `dotnet`)

Ogni context (`dotnet`, `markdown`, `html`) ha il proprio set di tool e il proprio prompt di
sistema — vedi la tabella in [README.md](README.md#contexts). Il flusso del context `dotnet`,
preso come esempio rappresentativo:

1. **Logga** la richiesta in `PROMPTS.md` (voce numerata, creato alla prima richiesta) prima di
   fare qualunque altra cosa — vedi [Log dei prompt](#log-dei-prompt-promptsmd) sotto.
2. **Esplora** la cartella con `FileSystem.ListFiles` prima di scrivere, per non sovrascrivere
   lavoro esistente. Se nel messaggio c'è una URL, la scarica con `Web.FetchUrl` per leggere
   specifiche/documentazione.
3. **Pianifica** in 2-3 frasi cosa costruirà.
4. **Scaffolda** con `dotnet new` (`DotNet.RunDotnetNew`, `--framework net10.0`), dentro `src/`.
5. **Implementa** scrivendo i file sorgente con `FileSystem.WriteFile`.
6. **Compila** con `DotNet.RunBuild` puntato su `src/`; in caso di errori, corregge e ricompila
   (fino a 10 iterazioni).
7. **Testa** con `DotNet.RunTest`; corregge finché i test non passano (fino a 10 iterazioni).
8. **Riporta** un riepilogo di cosa ha costruito e come eseguirlo.

I progetti `dotnet` e `html` usano entrambi un layout `src/` (codice) · `ops/` (config/deployment)
dentro la cartella target. La documentazione (`README.md`, `USAGE.md`, `ARCHITECTURE.md`,
`ROADMAP.md`) vive sempre alla **radice** della cartella target, mai in una sottocartella `docs/`
— stessa convenzione di questo repo. Il context `markdown` segue la stessa convenzione quando
l'obiettivo è documentare un progetto software. `PROMPTS.md` è l'eccezione: viene sempre
mantenuto, in ogni context, a partire dalla prima richiesta (vedi sotto).

### Log dei prompt (`PROMPTS.md`)

Ogni context, in ogni progetto generato, mantiene `PROMPTS.md` alla radice della cartella
target: un log numerato e cronologico di ogni richiesta ricevuta dall'agente per quel progetto,
con una riga di riepilogo di cosa è stato fatto in risposta. Viene aggiornato come primo passo di
ogni turno, prima di qualunque altra azione — così riflette l'intento anche se il resto del turno
fallisce. A differenza degli altri quattro file (`README.md`/`USAGE.md`/`ARCHITECTURE.md`/
`ROADMAP.md`, creati solo se ha senso per le dimensioni del progetto), `PROMPTS.md` non è
opzionale: è quello che rende la storia di un progetto tracciabile fino alle richieste che
l'hanno guidata.

Ogni chiamata a strumento è visibile in tempo reale quando `F8`/`--debug` è attivo:

```text
⏺ DotNet.RunBuild(projectPath=)
    ⎿  $ dotnet build  Exit code: 0 ...
```

## Esempi di prompt

```text
Crea una console app che calcola i numeri di Fibonacci, con un metodo Fibonacci(int n)
testato con xUnit per i casi 0, 1, 10 e un input negativo che deve lanciare un'eccezione.
```

```text
Leggi la spec su https://example.com/api-spec.md e genera un client HTTP tipizzato
per quell'API, con test che usano un HttpMessageHandler finto.
```

```text
Aggiungi un endpoint minimal API GET /health che ritorna 200 OK, senza rompere
la build esistente.
```

## Immagini (JPG/PNG)

L'agente ha accesso a un set di tool `Image.*` in ogni context, senza bisogno di configurazione:

- **`Image.GetInfo`** — dimensioni, formato, dimensione file.
- **`Image.Resize`**, **`Image.Crop`**, **`Image.Rotate`**, **`Image.Flip`**,
  **`Image.Grayscale`**, **`Image.ConvertFormat`** — manipolazione di file `.jpg`/`.jpeg`/`.png`
  già presenti nella cartella target (es. ridimensionare uno screenshot, convertire un asset da
  PNG a JPG).

Non c'è bisogno di chiederli esplicitamente: l'agente li usa quando la richiesta lo richiede
(es. "ridimensiona logo.png a 256x256").

**`Image.GenerateAI`** (opzionale) genera una nuova immagine — logo, icona, illustrazione — da
una descrizione testuale, tramite un modello di generazione immagini (DALL-E/gpt-image) su Azure
AI Foundry o OpenAI. Richiede `ImageModelId` configurato sotto la sezione del provider attivo
(vedi [README.md](README.md#configure)); se assente, il tool semplicemente non è registrato —
nessun impatto sul resto dell'app. Non disponibile con `AIProvider` `anthropic` (nessuna API di
generazione immagini). La generazione può richiedere qualche secondo: mentre è in corso, la
descrizione del tool resta visibile nella riga di stato (spinner), e con `F8`/`--debug` attivo
compaiono anche le righe `⏺`/`⎿` con il prompt e l'esito — lo stesso meccanismo di visibilità
delle tool-call di [Cosa fa l'agente](#cosa-fa-lagente-a-ogni-richiesta-context-dotnet).

## Log della sessione

Ogni turno (utente, agente, tool call) viene scritto in `<target>/.ralfai/session.md`. Al riavvio
di RalfAI sulla stessa cartella, la sessione precedente viene ricaricata automaticamente (tool
call e blocchi "thinking" esclusi dalla history rimandata al modello). Nella stessa cartella
`.ralfai/` vive anche `config.yaml`, che ricorda il context e il flag debug di quella cartella.

## Voce (Speech-to-Text / Text-to-Speech)

Richiede una risorsa **Azure AI Speech** configurata in `appsettings.json`:

```json
"AzureSpeech": {
  "Region": "westeurope",
  "ApiKey": "<speech-api-key>",
  "RecognitionLanguage": "it-IT",
  "VoiceName": "it-IT-IsabellaNeural"
}
```

(anche qui vale l'override via `RALFAI_AzureSpeech__Region` /
`RALFAI_AzureSpeech__ApiKey`). Se `Region`/`ApiKey` restano vuoti, `F2`/`F3` (o `/voice`/`/speak`
in modalità pipe) rispondono con un messaggio d'errore invece di funzionare — il resto dell'app
non è impattato.

- **`F2`** (interattivo) / **`/voice`** (pipe) — attiva il microfono, ascolta finché non rileva
  una pausa, e aggiunge il testo riconosciuto come nuova riga nel buffer corrente. Puoi combinarlo
  con testo digitato a mano nello stesso messaggio, prima di inviare con `Ctrl+Enter`/`/exe`.
- **`F3`** (interattivo) / **`/speak`** (pipe) — legge ad alta voce l'ultima risposta finale
  dell'agente (quella già mostrata a schermo).
- `RecognitionLanguage` (default `it-IT`) e `VoiceName` (default `it-IT-IsabellaNeural`) sono
  configurabili per usare un'altra lingua/voce fra quelle supportate da Azure AI Speech.

## Configurazione rate limiting

In `appsettings.json`, sotto `AzureAIFoundry.RateLimits` (usata per il pacing indipendentemente
dal provider AI attivo — vedi [README.md](README.md#choosing-a-provider) per la scelta del
provider):

| Chiave | Significato | Default |
|---|---|---|
| `RequestsPerMinute` | limite RPM dell'endpoint | 20 |
| `TokensPerMinute` | limite TPM dell'endpoint | 20000 |
| `SafetyFactor` | margine di sicurezza sul pacing (1.25 = spazia le richieste come se il limite fosse il 25% più stretto) | 1.25 |

Su risposta 429 il client fa retry (fino a 3 tentativi) rispettando l'header `Retry-After` se
presente, altrimenti attende 60s.

## Troubleshooting

- **`AzureAIFoundry:Endpoint missing...`** — manca `Endpoint`/`ApiKey` in `appsettings.json` o
  nelle variabili d'ambiente `RALFAI_AzureAIFoundry__Endpoint` /
  `RALFAI_AzureAIFoundry__ApiKey` (solo se `AIProvider` è `foundry`, il default — vedi
  [README.md](README.md)).
- **`Unknown context '<nome>'`** — il nome passato a `--context` non è registrato; i nomi
  disponibili sono elencati nel messaggio d'errore (o con `ralf --list-contexts`).
- **Build/test che non convergono dopo 10 iterazioni** — l'agente si ferma e spiega il problema
  residuo nella risposta finale invece di continuare all'infinito.
- **`dotnet` non trovato** durante l'esecuzione (context `dotnet`) — verifica che il .NET SDK sia
  nel `PATH` del processo che esegue RalfAI.
- **`Voice non configurata...`** su `F2`/`F3` (o `/voice`/`/speak` in modalità pipe) — manca
  `Region`/`ApiKey` sotto `AzureSpeech` in `appsettings.json` (o le relative variabili
  d'ambiente).
- **`Nessun parlato riconosciuto.`** — il microfono non ha catturato audio comprensibile entro il
  timeout di silenzio dell'SDK; riprova parlando più vicino al microfono o subito dopo aver
  attivato la dettatura.
- **`Could not open '<comando>'...`** su `F4` nel file browser — il comando configurato sotto
  `Editor` (default `code.exe`) non è nel `PATH` o non esiste; correggi `Editor` in
  `appsettings.json` (o `RALFAI_Editor`) con il comando giusto per il tuo editor.


---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=.ralfai/session.md
result: # RalfAI Session

> Started: 2026-08-06 01:12:37

---

### USER — 01:12:37

Vuoi creare il logo del progetto? Una immagine?

---

### AGENT — 01:12:42

Sì, posso creare un logo per **RalfAI**, ad esempio:

- stile moderno e minimale;
- simbolo che combina **“R”**, intelligenza artificiale e codice `.NET`;
- palette blu/viola su sfondo trasparente;
- formati **SVG** per il progetto e **PNG** per README, icone e social.

Posso anche integrarlo direttamente nel repository. Dimmi semplicemente “procedi” e realizzerò una proposta completa.

---

### USER — 01:13:06

procedi

---

### TOOL — FileSystem.ReadFile — 01:13:09

args: relativePath=PROMPTS.md
result: # Prompts

Log cronologico e numerato delle richieste che hanno guidato la realizzazione di RalfAI — la
stessa convenzione (`PROMPTS.md` alla radice, aggiornato a ogni obiettivo, prima di qualunque
altra azione) che i context `dotnet`/`html`/`markdown` ora applicano a ogni progetto che
generano. Vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
[USAGE.md](USAGE.md#log-dei-prompt-promptsmd).

> **Nota sulla provenienza.** Questo file è stato scritto da un assistente di coding (Claude
> Code) che lavorava su questo repo via git — non da `ralf` in esecuzione su se stesso. Le voci
> 1–8 riguardano funzionalità arrivate in sessioni precedenti a questa: la git history parte da
> un "Initial Commit" già completo (vedi [ROADMAP.md](ROADMAP.md)), quindi il testo esatto di
> quelle richieste non è disponibile — sono **ricostruite** a partire dal changelog in
> `ROADMAP.md` e marcate `[ricostruito]`. Le voci dalla 9 in poi sono **verbatim**, prese da
> questa conversazione.

## 1. Realizzare un agente di coding autonomo per .NET `[ricostruito]`

> Costruisci un agente CLI autonomo che, data una cartella e un obiettivo, scaffoldi un
> progetto .NET con `dotnet new`, scriva i file sorgente, compili con `dotnet build` e testi
> con `dotnet test`, correggendo finché non vanno a buon fine.

Realizzato: `RalfAIApp` (Spectre.Console) + `DotNetContext` come unico context, motore basato
su Microsoft Agent Framework contro Azure AI Foundry.

## 2. Rendere il context non hardcoded `[ricostruito]`

> Il context .NET è cablato direttamente in `Program.cs` — rendilo selezionabile, così in
> futuro si possono aggiungere altri stack senza toccare l'app.

Realizzato: `AgentContextFactory`/`ContextRegistry`, flag `--context`, chiave `AgentContext` in
config/env, `dotnet` come default (poi diventato `markdown`).

## 3. Aggiungere Speech-to-Text / Text-to-Speech `[ricostruito]`

> Aggiungi la possibilità di dettare i messaggi a voce e farsi leggere le risposte ad alta
> voce, usando Azure AI Speech.

Realizzato: `SpeechService` (motore condiviso), comandi `/voice`/`/speak` in console,
opzionale via sezione `AzureSpeech` in config — nessun impatto sul resto dell'app se assente.

## 4. Estrarre un motore condiviso dalla console `[ricostruito]`

> La logica dell'agente è mischiata con il rendering Spectre.Console — separala in un motore
> riusabile da altri frontend futuri.

Realizzato: `AgentSession` (poi rinominato `AgentEngine`) in `RalfAI.Core`, nessuna dipendenza
UI, eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice`.

## 5. Distribuire come global tool e rendere la cartella opzionale `[ricostruito]`

> Pacchettizza RalfAI come .NET global tool (`ralf`) installabile da riga di comando, e
> permetti di avviarlo senza specificare la cartella, con un picker.

Realizzato: `PackAsTool`/`ToolCommandName=ralf`, `FileBrowser` + `LastFolderStore` per il
picker e la memoria dell'ultima cartella, nuovi context `markdown` (default) e `html`.

## 6. Sessione interattiva a schermo intero `[ricostruito]`

> Sostituisci il prompt a riga singola con un'interfaccia a schermo intero, composizione
> multi-riga e scorciatoie da tastiera invece dei comandi testuali.

Realizzato: screen buffer alternato del terminale, `Ctrl+Enter` per inviare, tasti `F1`-`F10`
(help, dettatura, lettura, pulizia schermo, cambio context/cartella, toggle debug, quit).

## 7. Supportare più provider AI `[ricostruito]`

> Non vincolare RalfAI ad Azure AI Foundry — fai in modo che possa parlare anche con OpenAI
> nativo e Anthropic Claude, ciascuno tramite il proprio SDK ufficiale.

Realizzato: `AgentEngine.BuildChatClient`, switch su `AIProvider` (`foundry`/`openai`/
`anthropic`), sezioni di config dedicate per provider.

## 8. Scrivere la documentazione del progetto `[ricostruito]`

> Scrivi README, architettura, guida all'uso e roadmap per RalfAI.

Realizzato: `README.md`, `ARCHITECTURE.md`, `USE.md` (poi rinominato `USAGE.md`, voce 11),
`ROADMAP.md`.

---

## 9. "Il file USE riporta la app desktop che non c'è"

`USE.md` e `ROADMAP.md` descrivevano `RalfAI.Desktop`/`RalfAI.ChatUI` (WinForms + WebView2 /
Blazor Hybrid) come già implementati, ma quei progetti non esistevano né nella solution né sul
filesystem. Confermato scavando nel codice (nessun riferimento reale, `RalfAI.sln` non li
include) e riscritto `USE.md` da zero contro il comportamento effettivo (global tool `ralf`,
cartella opzionale, default context `markdown`, sessione a schermo intero, `.ralfai/` invece di
`.chat/`).

## 10. "Anche la roadmap è disallineata"

Stesso problema in `ROADMAP.md`: la sezione "Interfaccia Desktop" dichiarava il frontend
grafico come fatto. Sostituita con voci che riflettono lo stato reale (global tool, file
browser, sessione a schermo intero, motore `AgentEngine`, provider multipli); l'interfaccia
grafica spostata in "📋 Pianificato" come idea aperta, non decisa.

## 11. "Puoi fare un aggiornamento ai contesti? I files md stanno sempre in radice, non in docs. In generale sempre: README USAGE ARCHITECTURE ROADMAP. Puoi aggiungere un supporto alla manipolazione di immagini jpg e png"

Due richieste in una:
- Rimosso `docs/` dal layout dei context `dotnet`/`html`: documentazione sempre alla radice
  della cartella target, con il set fisso `README.md`/`USAGE.md`/`ARCHITECTURE.md`/
  `ROADMAP.md` — rinominato anche `USE.md` → `USAGE.md` in questo repo, per coerenza.
- Nuovo `ImagePlugin` (`SixLabors.ImageSharp`): `Image.GetInfo`/`Resize`/`Crop`/`Rotate`/
  `Flip`/`Grayscale`/`ConvertFormat`, tool engine-level sempre disponibili in ogni context,
  nessuna configurazione richiesta.

## 12. "Per un progetto puoi anche generare un logo?"

Chiarito che si trattava di generazione AI (non disegno programmatico via forme/testo).
Aggiunti `ImageGenerationService` e `ImageGenerationPlugin` (`Image.GenerateAI`): genera
un'immagine da un prompt testuale via DALL-E/gpt-image su Azure AI Foundry o OpenAI, salvata
come JPG/PNG. Opzionale via `ImageModelId` sotto la sezione del provider attivo; non
disponibile con `AIProvider` `anthropic` (nessuna API di generazione immagini).

## 13. "Inoltre, quando sta generando, puoi dare un dettaglio di quello che sta pensando scrivendo generando? Come un log?"

Nessun meccanismo nuovo: la visibilità delle tool-call già esistente (descrizione nello
spinner mentre il tool gira, righe `⏺`/`⎿` con `F8`/`--debug` attivo) copre già questo caso —
la `[Description]` di `Image.GenerateAI` è stata scritta per essere informativa in quel
contesto.

## 14. "Hai aggiornato anche il comando?"

Chiarito (dopo un giro di domanda) che si riferiva ai comandi `dotnet pack`/`dotnet tool
install` in `README.md`. Verificato con un pack di prova che non serve alcuna modifica:
`SixLabors.ImageSharp` viene incluso automaticamente come dipendenza transitiva via
`RalfAI.Core`, nessuna DLL nativa coinvolta.

## 15. "Puoi aggiungere anche un file md con i PROMPTS? che in qualche modo rispecchia la realizzazione intera del progetto stesso"

Aggiunto `PROMPTS.md` come quinto file nel set canonico di documentazione — l'unico non
opzionale: ogni context (`dotnet`, `html`, `markdown`) ora apre ogni turno con un passo "Log"
che ci scrive prima di qualunque altra azione, così la storia di un progetto generato resta
tracciabile fino alle richieste che l'hanno guidata. Questo stesso file applica la convenzione
a ritroso a RalfAI.

## 16. "Aggiungi la possibilità di aprire un file durante la selezione: default code.exe, altrimenti variabile nel file di config"

Nuovo tasto `F4` in `FileBrowser` (usato sia al primo avvio senza cartella sulla riga di
comando, sia con `F7` durante una sessione): apre il file selezionato in un editor esterno via
`Process.Start(UseShellExecute: true)`, fire-and-forget. Comando risolto dalla nuova chiave di
config `Editor` (`RALFAI_Editor` per l'override via env), default `code.exe` (VS Code) se non
impostata — `FileBrowser` resta senza dipendenza diretta da `IConfiguration`, il comando viene
passato dai chiamanti (`Program.cs`, `RalfAIApp.SwitchFolder`).

## 17. "Puoi aggiornare l'help con un po' tutti i comandi e verificare che tutta la documentazione sia a posto?"

Due parti:
- **Help in-app**: `F1` (interattivo) ora elenca anche `Ctrl+C`/`Esc`/`Ctrl+X` (interruzione
  turno), prima mancanti. `/help` in modalità pipe non condivide più la tabella dei tasti
  funzione (inapplicabile lì) — mostra la tabella dei comandi testuali realmente disponibili
  (`/exe`, `/cancel`, `/voice`, `/speak`, `/clear`, `/switch-context`, `/switch-folder`,
  `/toggle-debug`, `/help`, `exit`). Nel farlo, trovato e corretto un bug: `/help` in pipe
  chiamava `Console.ReadKey` su stdin rediretto, che lancia `InvalidOperationException` —
  verificato con un test end-to-end (`echo "/help\nexit" | ralf`) che prima sarebbe andato in
  crash e ora non lo fa più.
- **Audit documentazione**: verificati tutti i link tra `README.md`/`USAGE.md`/
  `ARCHITECTURE.md`/`ROADMAP.md`/`PROMPTS.md`. Corretti tre anchor rotti (due in italiano
  dentro un `README.md` inglese, uno verso una sezione mai esistita in `ARCHITECTURE.md`), un
  link relativo a un file sorgente che risaliva di una cartella di troppo, e una voce di
  ROADMAP che citava una classe (`AgentContextFactory`) ormai rinominata (`ContextRegistry`).


---

### TOOL — FileSystem.WriteFile — 01:13:33

args: relativePath=PROMPTS.md, content=# Prompts

Log cronologico e numerato delle richieste che hanno guidato la realizzazione di RalfAI — la
stessa convenzione (`PROMPTS.md` alla radice, aggiornato a ogni obiettivo, prima di qualunque
altra azione) che i context `dotnet`/`html`/`markdown` ora applicano a ogni progetto che
generano. Vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
[USAGE.md](USAGE.md#log-dei-prompt-promptsmd).

> **Nota sulla provenienza.** Questo file è stato scritto da un assistente di coding (Claude
> Code) che lavorava su questo repo via git — non da `ralf` in esecuzione su se stesso. Le voci
> 1–8 riguardano funzionalità arrivate in sessioni precedenti a questa: la git history parte da
> un "Initial Commit" già completo (vedi [ROADMAP.md](ROADMAP.md)), quindi il testo esatto di
> quelle richieste non è disponibile — sono **ricostruite** a partire dal changelog in
> `ROADMAP.md` e marcate `[ricostruito]`. Le voci dalla 9 in poi sono **verbatim**, prese da
> questa conversazione.

## 1. Realizzare un agente di coding autonomo per .NET `[ricostruito]`

> Costruisci un agente CLI autonomo che, data una cartella e un obiettivo, scaffoldi un
> progetto .NET con `dotnet new`, scriva i file sorgente, compili con `dotnet build` e testi
> con `dotnet test`, correggendo finché non vanno a buon fine.

Realizzato: `RalfAIApp` (Spectre.Console) + `DotNetContext` come unico context, motore basato
su Microsoft Agent Framework contro Azure AI Foundry.

## 2. Rendere il context non hardcoded `[ricostruito]`

> Il context .NET è cablato direttamente in `Program.cs` — rendilo selezionabile, così in
> futuro si possono aggiungere altri stack senza toccare l'app.

Realizzato: `AgentContextFactory`/`ContextRegistry`, flag `--context`, chiave `AgentContext` in
config/env, `dotnet` come default (poi diventato `markdown`).

## 3. Aggiungere Speech-to-Text / Text-to-Speech `[ricostruito]`

> Aggiungi la possibilità di dettare i messaggi a voce e farsi leggere le risposte ad alta
> voce, usando Azure AI Speech.

Realizzato: `SpeechService` (motore condiviso), comandi `/voice`/`/speak` in console,
opzionale via sezione `AzureSpeech` in config — nessun impatto sul resto dell'app se assente.

## 4. Estrarre un motore condiviso dalla console `[ricostruito]`

> La logica dell'agente è mischiata con il rendering Spectre.Console — separala in un motore
> riusabile da altri frontend futuri.

Realizzato: `AgentSession` (poi rinominato `AgentEngine`) in `RalfAI.Core`, nessuna dipendenza
UI, eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice`.

## 5. Distribuire come global tool e rendere la cartella opzionale `[ricostruito]`

> Pacchettizza RalfAI come .NET global tool (`ralf`) installabile da riga di comando, e
> permetti di avviarlo senza specificare la cartella, con un picker.

Realizzato: `PackAsTool`/`ToolCommandName=ralf`, `FileBrowser` + `LastFolderStore` per il
picker e la memoria dell'ultima cartella, nuovi context `markdown` (default) e `html`.

## 6. Sessione interattiva a schermo intero `[ricostruito]`

> Sostituisci il prompt a riga singola con un'interfaccia a schermo intero, composizione
> multi-riga e scorciatoie da tastiera invece dei comandi testuali.

Realizzato: screen buffer alternato del terminale, `Ctrl+Enter` per inviare, tasti `F1`-`F10`
(help, dettatura, lettura, pulizia schermo, cambio context/cartella, toggle debug, quit).

## 7. Supportare più provider AI `[ricostruito]`

> Non vincolare RalfAI ad Azure AI Foundry — fai in modo che possa parlare anche con OpenAI
> nativo e Anthropic Claude, ciascuno tramite il proprio SDK ufficiale.

Realizzato: `AgentEngine.BuildChatClient`, switch su `AIProvider` (`foundry`/`openai`/
`anthropic`), sezioni di config dedicate per provider.

## 8. Scrivere la documentazione del progetto `[ricostruito]`

> Scrivi README, architettura, guida all'uso e roadmap per RalfAI.

Realizzato: `README.md`, `ARCHITECTURE.md`, `USE.md` (poi rinominato `USAGE.md`, voce 11),
`ROADMAP.md`.

---

## 9. "Il file USE riporta la app desktop che non c'è"

`USE.md` e `ROADMAP.md` descrivevano `RalfAI.Desktop`/`RalfAI.ChatUI` (WinForms + WebView2 /
Blazor Hybrid) come già implementati, ma quei progetti non esistevano né nella solution né sul
filesystem. Confermato scavando nel codice (nessun riferimento reale, `RalfAI.sln` non li
include) e riscritto `USE.md` da zero contro il comportamento effettivo (global tool `ralf`,
cartella opzionale, default context `markdown`, sessione a schermo intero, `.ralfai/` invece di
`.chat/`).

## 10. "Anche la roadmap è disallineata"

Stesso problema in `ROADMAP.md`: la sezione "Interfaccia Desktop" dichiarava il frontend
grafico come fatto. Sostituita con voci che riflettono lo stato reale (global tool, file
browser, sessione a schermo intero, motore `AgentEngine`, provider multipli); l'interfaccia
grafica spostata in "📋 Pianificato" come idea aperta, non decisa.

## 11. "Puoi fare un aggiornamento ai contesti? I files md stanno sempre in radice, non in docs. In generale sempre: README USAGE ARCHITECTURE ROADMAP. Puoi aggiungere un supporto alla manipolazione di immagini jpg e png"

Due richieste in una:
- Rimosso `docs/` dal layout dei context `dotnet`/`html`: documentazione sempre alla radice
  della cartella target, con il set fisso `README.md`/`USAGE.md`/`ARCHITECTURE.md`/
  `ROADMAP.md` — rinominato anche `USE.md` → `USAGE.md` in questo repo, per coerenza.
- Nuovo `ImagePlugin` (`SixLabors.ImageSharp`): `Image.GetInfo`/`Resize`/`Crop`/`Rotate`/
  `Flip`/`Grayscale`/`ConvertFormat`, tool engine-level sempre disponibili in ogni context,
  nessuna configurazione richiesta.

## 12. "Per un progetto puoi anche generare un logo?"

Chiarito che si trattava di generazione AI (non disegno programmatico via forme/testo).
Aggiunti `ImageGenerationService` e `ImageGenerationPlugin` (`Image.GenerateAI`): genera
un'immagine da un prompt testuale via DALL-E/gpt-image su Azure AI Foundry o OpenAI, salvata
come JPG/PNG. Opzionale via `ImageModelId` sotto la sezione del provider attivo; non
disponibile con `AIProvider` `anthropic` (nessuna API di generazione immagini).

## 13. "Inoltre, quando sta generando, puoi dare un dettaglio di quello che sta pensando scrivendo generando? Come un log?"

Nessun meccanismo nuovo: la visibilità delle tool-call già esistente (descrizione nello
spinner mentre il tool gira, righe `⏺`/`⎿` con `F8`/`--debug` attivo) copre già questo caso —
la `[Description]` di `Image.GenerateAI` è stata scritta per essere informativa in quel
contesto.

## 14. "Hai aggiornato anche il comando?"

Chiarito (dopo un giro di domanda) che si riferiva ai comandi `dotnet pack`/`dotnet tool
install` in `README.md`. Verificato con un pack di prova che non serve alcuna modifica:
`SixLabors.ImageSharp` viene incluso automaticamente come dipendenza transitiva via
`RalfAI.Core`, nessuna DLL nativa coinvolta.

## 15. "Puoi aggiungere anche un file md con i PROMPTS? che in qualche modo rispecchia la realizzazione intera del progetto stesso"

Aggiunto `PROMPTS.md` come quinto file nel set canonico di documentazione — l'unico non
opzionale: ogni context (`dotnet`, `html`, `markdown`) ora apre ogni turno con un passo "Log"
che ci scrive prima di qualunque altra azione, così la storia di un progetto generato resta
tracciabile fino alle richieste che l'ha guidata. Questo stesso file applica la convenzione
a ritroso a RalfAI.

## 16. "Aggiungi la possibilità di aprire un file durante la selezione: default code.exe, altrimenti variabile nel file di config"

Nuovo tasto `F4` in `FileBrowser` (usato sia al primo avvio senza cartella sulla riga di
comando, sia con `F7` durante una sessione): apre il file selezionato in un editor esterno via
`Process.Start(UseShellExecute: true)`, fire-and-forget. Comando risolto dalla nuova chiave di
config `Editor` (`RALFAI_Editor` per l'override via env), default `code.exe` (VS Code) se non
impostata — `FileBrowser` resta senza dipendenza diretta da `IConfiguration`, il comando viene
passato dai chiamanti (`Program.cs`, `RalfAIApp.SwitchFolder`).

## 17. "Puoi aggiornare l'help con un po' tutti i comandi e verificare che tutta la documentazione sia a posto?"

Due parti:
- **Help in-app**: `F1` (interattivo) ora elenca anche `Ctrl+C`/`Esc`/`Ctrl+X` (interruzione
  turno), prima mancanti. `/help` in modalità pipe non condivide più la tabella dei tasti
  funzione (inapplicabile lì) — mostra la tabella dei comandi testuali realmente disponibili
  (`/exe`, `/cancel`, `/voice`, `/speak`, `/clear`, `/switch-context`, `/switch-folder`,
  `/toggle-debug`, `/help`, `exit`). Nel farlo, trovato e corretto un bug: `/help` in pipe
  chiamava `Console.ReadKey` su stdin rediretto, che lancia `InvalidOperationException` —
  verificato con un test end-to-end (`echo "/help\nexit" | ralf`) che prima sarebbe andato in
  crash e ora non lo fa più.
- **Audit documentazione**: verificati tutti i link tra `README.md`/`USAGE.md`/
  `ARCHITECTURE.md`/`ROADMAP.md`/`PROMPTS.md`. Corretti tre anchor rotti (due in italiano
  dentro un `README.md` inglese, uno verso una sezione mai esistita in `ARCHITECTURE.md`), un
  link relativo a un file sorgente che risaliva di una cartella di troppo, e una voce di
  ROADMAP che citava una classe (`AgentContextFactory`) ormai rinominata (`ContextRegistry`).

## 18. "Vuoi creare il logo del progetto? Una immagine?" — "procedi"

Creazione di un logo moderno e minimale per RalfAI, in formato SVG sorgente e PNG per README,
con integrazione nel repository e aggiornamento della documentazione.

result: OK — written PROMPTS.md (9504 chars)

---

### TOOL — FileSystem.ListFiles — 01:13:37

args: subdirectory=
result: [DIR]  .git
[FILE] .git\COMMIT_EDITMSG
[FILE] .git\config
[FILE] .git\description
[FILE] .git\FETCH_HEAD
[FILE] .git\HEAD
[DIR]  .git\hooks
[FILE] .git\hooks\applypatch-msg.sample
[FILE] .git\hooks\commit-msg.sample
[FILE] .git\hooks\fsmonitor-watchman.sample
[FILE] .git\hooks\post-update.sample
[FILE] .git\hooks\pre-applypatch.sample
[FILE] .git\hooks\pre-commit.sample
[FILE] .git\hooks\pre-merge-commit.sample
[FILE] .git\hooks\pre-push.sample
[FILE] .git\hooks\pre-rebase.sample
[FILE] .git\hooks\pre-receive.sample
[FILE] .git\hooks\prepare-commit-msg.sample
[FILE] .git\hooks\push-to-checkout.sample
[FILE] .git\hooks\sendemail-validate.sample
[FILE] .git\hooks\update.sample
[FILE] .git\index
[DIR]  .git\info
[FILE] .git\info\exclude
[DIR]  .git\logs
[FILE] .git\logs\HEAD
[DIR]  .git\logs\refs
[DIR]  .git\logs\refs\heads
[FILE] .git\logs\refs\heads\main
[DIR]  .git\logs\refs\remotes
[DIR]  .git\logs\refs\remotes\origin
[FILE] .git\logs\refs\remotes\origin\HEAD
[FILE] .git\logs\refs\remotes\origin\main
[DIR]  .git\objects
[DIR]  .git\objects\00
[FILE] .git\objects\00\a7d8397992efbcf634e87d01dfc32e9cb640fa
[DIR]  .git\objects\01
[FILE] .git\objects\01\80566140c23d605344a2fcab628d4bf4d6aa6d
[FILE] .git\objects\01\942ffa38da10f0ac18c86d1a3d67c72c33d87f
[FILE] .git\objects\01\a5dd410815721833e32bcd277c899f5b49c930
[DIR]  .git\objects\02
[FILE] .git\objects\02\a585c4b871b74e9a312af3ff3d72d9658665b7
[FILE] .git\objects\02\bef14e3a66085de453961624b793edb0ffde19
[FILE] .git\objects\02\d068e02f2b3265c7fdde9feb60c97db8fcb95a
[DIR]  .git\objects\03
[FILE] .git\objects\03\2b5edf7848e97b05e55db6009220e87c2bd00b
[DIR]  .git\objects\04
[FILE] .git\objects\04\ebc45deda2180aeba4e1b4d24e666f79b5da84
[DIR]  .git\objects\05
[FILE] .git\objects\05\821e64afcaa1af395e4bdb1312fef04e942e59
[DIR]  .git\objects\06
[FILE] .git\objects\06\58406fa9786edfa226d9c327af2ae493e120d3
[FILE] .git\objects\06\790da1f900378190e8bcaeff3805b804ec9c2c
[DIR]  .git\objects\07
[FILE] .git\objects\07\08077a6404c84089e7c35fa1bb035bf25e5e4a
[FILE] .git\objects\07\dabdb1da7545c0da57fc5a31d662a3069a5e66
[DIR]  .git\objects\08
[FILE] .git\objects\08\34d8bbf0dde6093a90122d181bbb844b127f97
[FILE] .git\objects\08\392b119084760a0fad0b74ba5f3e1e026c4f0d
[FILE] .git\objects\08\9988d6485e02ab4a9a77e473f2fd585064aad7
[FILE] .git\objects\08\d2749b28e20939c707c4739f3892b4d3d041e0
[DIR]  .git\objects\09
[FILE] .git\objects\09\53a19a8510c768656dba65f7a9e7d656d5d83f
[FILE] .git\objects\09\b123e5460727bd6f9d8f7995144a52d14d26d1
[DIR]  .git\objects\0b
[FILE] .git\objects\0b\30ad4abd0066b4d8edae135043004f7e731995
[DIR]  .git\objects\0c
[FILE] .git\objects\0c\03cadeda6a20eb92b3a98d658d2117d0cda4e3
[FILE] .git\objects\0c\704be2f67452106e684ea698bb7e4172d5fe5c
[FILE] .git\objects\0c\d77bdf623d70ae8db67bce7b9bfecafb254c1c
[DIR]  .git\objects\0d
[FILE] .git\objects\0d\27cdfcc3cf83cb7046cc0cfdd621ad69b857c3
[FILE] .git\objects\0d\373fbe1452e774e2ea46eff8d049816b697074
[FILE] .git\objects\0d\4d30fc9d9dce7b3c94bf44f6d594abf497a0f6
[FILE] .git\objects\0d\986e7a4cd21e36e964149fdb723b3cbf6b7a26
[FILE] .git\objects\0d\ee5c57f8f04f4a1bef94ced84f40e938de65f2
[DIR]  .git\objects\0e
[FILE] .git\objects\0e\932f0e0215e1dfda5eba823770229ebada1e51
[FILE] .git\objects\0e\ea9296f1048410098c0df8e12f1ad4b5bd8cf8
[DIR]  .git\objects\0f
[FILE] .git\objects\0f\0d7cdd07c64a5730dadfed1ac0541cec60d635
[FILE] .git\objects\0f\5baa7894159c79273e84716c30f07930f79c58
[FILE] .git\objects\0f\5cdb3d0521f625979269ead851e30b8096ef92
[FILE] .git\objects\0f\b26103e2b0a0d95c9af40832f8e09009aae4c1
[DIR]  .git\objects\10
[FILE] .git\objects\10\238e81a702b0eff4f816a28eb076b73c8f4893
[FILE] .git\objects\10\b5885475c02543005bc748b4fa51203425ca1b
[DIR]  .git\objects\11
[FILE] .git\objects\11\599915337c158348a968b35474b08a4aa79a65
[FILE] .git\objects\11\a3845f76003de92412c9652b2283ab6f49ef1c
[FILE] .git\objects\11\c579ea7df3bc734a7824b30625a82ec1ae0e47
[DIR]  .git\objects\12
[FILE] .git\objects\12\115869f1db39f3cda988f9af7dba43555de966
[DIR]  .git\objects\13
[FILE] .git\objects\13\4dfb933b7d18a0716c5c90d3c9f7ec20aaf064
[FILE] .git\objects\13\6866bd8ac191321430fdcf9120b24b2db89f2a
[FILE] .git\objects\13\7c8609beddaa1cc3d29dac534a149e95a10118
[FILE] .git\objects\13\b1f9a7bd3ca3c486e7e50a1ccda1ca0d588333
[FILE] .git\objects\13\cb226edeb47921fbcdbb4c8fcbf656e48300e3
[DIR]  .git\objects\14
[FILE] .git\objects\14\67a1f85583da0083005f7c7ca83677930749b2
[FILE] .git\objects\14\6b1e72e88c68730d9a58ae2631d393aed7a4eb
[FILE] .git\objects\14\8edcf5bdaa2d5d62590e19063130a732586b49
[FILE] .git\objects\14\cfeb86204b3433362136bfe3236ec8135af0da
[DIR]  .git\objects\15
[FILE] .git\objects\15\6753e0d0349549d003e842e823f8c8094fdb31
[DIR]  .git\objects\16
[FILE] .git\objects\16\3022c6385aed3e7ca8bdd8f75733a0a0bdbce0
[FILE] .git\objects\16\5b5534d270dc42e95405b52b59b2da341b440e
[FILE] .git\objects\16\7f290fc8c6a79db637dd44d69fc54e20ae421b
[FILE] .git\objects\16\9274a4f2dd170b3a82b7a4d776f45786c86603
[FILE] .git\objects\16\b261e064bf20639fdc3607045cf0349aba4e3e
[FILE] .git\objects\16\f8e54b69cc70f708630f1d60b8f9e3e98a6ea7
[DIR]  .git\objects\17
[FILE] .git\objects\17\198025459795b1bc91b387141b30a5ed06877f
[FILE] .git\objects\17\5005054597f0cb131d3021565551f5f32ed2be
[DIR]  .git\objects\18
[FILE] .git\objects\18\2157b09a077b914b7dca76db46af674be45d1b
[FILE] .git\objects\18\6a6858991ac67b7d0da342fd563d35c7c556f3
[FILE] .git\objects\18\7d60c858d895a427f9f445056f01072f6454d3
[FILE] .git\objects\18\e70792bb0321e0c39f8a078b1211870cdabef0
[FILE] .git\objects\18\f95c9a9e235b64d19ea3bf9cef895cd926601a
[DIR]  .git\objects\1a
[FILE] .git\objects\1a\9d63bf85aeb75fc5d9dbe867fe2a15ef2e318f
[FILE] .git\objects\1a\f9380bbd3a1e66f2a244f34c98d5a1e4350f8e
[DIR]  .git\objects\1b
[FILE] .git\objects\1b\0de61675f59770f61db5b373ede861a732cc98
[FILE] .git\objects\1b\fb1b062aeffd3689332e939f47ae076aee9b07
[DIR]  .git\objects\1c
[FILE] .git\objects\1c\42be3c5b7b9a2da299ac5eef359ac3b9859242
[DIR]  .git\objects\1d
[FILE] .git\objects\1d\76485b2a81b434618c887004263b9a46fa157b
[FILE] .git\objects\1d\ae90b1e62f3e4bfd7b65fe9dce62b6684e7960
[DIR]  .git\objects\1e
[FILE] .git\objects\1e\3fb80b726dbac3436a9888d7db7eb33af39ce5
[FILE] .git\objects\1e\75105604345605533f0fc69931407c0845709e
[FILE] .git\objects\1e\b813dc19f03d213e6edd8b290f6ca5445050b2
[DIR]  .git\objects\1f
[FILE] .git\objects\1f\0c355efe596d4e42c9066baa0c663f02a81a02
[FILE] .git\objects\1f\1fc91ab9c629a1d214cf9b8e7902049f47994e
[FILE] .git\objects\1f\82976bdbb87a10e15b6a8edf8a36f79e83c2e2
[FILE] .git\objects\1f\9793de34fd3cb5b00500570f62f605e91e5292
[FILE] .git\objects\1f\b89abb746840452384cde74671ea59fece776f
[DIR]  .git\objects\20
[FILE] .git\objects\20\f2afeb1886a50a3d4e9b1379fb1403520f98c2
[DIR]  .git\objects\22
[FILE] .git\objects\22\2540b4bbbb488faa6db6b644ed014fea941ab0
[FILE] .git\objects\22\cb07afc08d1299f801472087ddd196aa897b28
[FILE] .git\objects\22\d2e0cbe64d23383be6c06efbc2e395cd693dc5
[FILE] .git\objects\22\e8a96c381182a885a33a94cd7f87f50794a6a4
[FILE] .git\objects\22\f86af44d08d1c3d46fcf2c5992437431d26ff0
[FILE] .git\objects\22\f91db60c89bb484f529d60a8cc9b7659b86741
[DIR]  .git\objects\23
[FILE] .git\objects\23\2bd09953b410a6b7f7bc1a52d1b8182304a7b0
[DIR]  .git\objects\24
[FILE] .git\objects\24\7ed197c5d1967149c76428cad33b3d65f74f04
[FILE] .git\objects\24\d98e0d6f4bb63adb2a77d9b3cfdf67354be250
[DIR]  .git\objects\25
[FILE] .git\objects\25\1e9d59a57ddd701fe2b8222442a09593f2f775
[FILE] .git\objects\25\3db945de6e308fc19f88cc8942c3b7f5d6015f
[DIR]  .git\objects\26
[FILE] .git\objects\26\bf58f6dc890d272f48cd7d80fadb7caa8ae624
[FILE] .git\objects\26\f4a101e39bcea6423878467125349122de49f5
[FILE] .git\objects\26\f8e3c999f4786ff0e9b036d283effd19bebcd5
[DIR]  .git\objects\27
[FILE] .git\objects\27\4251ae2c3a8777db0c448a4b1a288824fee425
[FILE] .git\objects\27\ba88bdc95ff1071530b1ede2dccd8a548c515b
[FILE] .git\objects\27\e4551a00c9787e269e724c4024954f9ac3694f
[DIR]  .git\objects\28
[FILE] .git\objects\28\3fe3cbcc311e83d16b495f20712a8e5686829f
[FILE] .git\objects\28\bae31747cfc6ce2912fe9144b1cc8550a82f20
[FILE] .git\objects\28\c2f0d9e9a8a3ddd95e15cd44f6ebfb414428c2
[FILE] .git\objects\28\e66a3eb40d6279273a19b84e4ae01774b0eee3
[DIR]  .git\objects\29
[FILE] .git\objects\29\af417cd291e85c2ab13f264976863d24cdfff3
[DIR]  .git\objects\2a
[FILE] .git\objects\2a\90aa5979c689cd3e179688fb3367c37d7fb031
[FILE] .git\objects\2a\bf9ec5aad296151f24c2c9ef16f1b6202ae080
[DIR]  .git\objects\2b
[FILE] .git\objects\2b\9fb215b4fb01e5cb24b8a3b4f590c9049f5125
[DIR]  .git\objects\2c
[FILE] .git\objects\2c\5d8d922b7ce21a9d4f5de0df2919d46c44b5ef
[FILE] .git\objects\2c\f9c59de9db56d4a25b089d8c62dfffca9f63b0
[DIR]  .git\objects\2d
[FILE] .git\objects\2d\8618fdeed507655a5d3f8519ade5aa06467e25
[DIR]  .git\objects\2e
[FILE] .git\objects\2e\675594aa5b92a642ad8e155ca6579264e493e0
[FILE] .git\objects\2e\e7fa0be115cbb44b30733d86dfef40941b8ac5
[DIR]  .git\objects\2f
[FILE] .git\objects\2f\67189ea5b622853a9cebfbff760f14a85cb3f3
[FILE] .git\objects\2f\ae0065177d04b41ae29a2dbc32c78177e5cc49
[FILE] .git\objects\2f\fdc45b237ddc01c9fc10fddc4e70adc22e3c7b
[DIR]  .git\objects\30
[FILE] .git\objects\30\43f180a535a4cf065642550f5f1025fa52999e
[FILE] .git\objects\30\971f3aa8abd5ccd70cf192e3b48e6fde8466ef
[FILE] .git\objects\30\a4fc8b14dcf252e193eddfc33a79caabd24e56
[FILE] .git\objects\30\cfb60691739379e8b7cf42ecac373db6d37f77
[FILE] .git\objects\30\e985f6d60235d09fc622eff9ad378f1932d2a8
[DIR]  .git\objects\31
[FILE] .git\objects\31\b505b021b2a071d4e8bb920633f52be518851e
[FILE] .git\objects\31\d922ecfa392cbe808d4beadb88100a8dda461b
[DIR]  .git\objects\32
[FILE] .git\objects\32\60a0471d27fee22d76c2689e371033e60e7cd0
[FILE] .git\objects\32\6c2077a8038f1004b283aee2c02e8e18f3b9a6
[FILE] .git\objects\32\fc42eb147fc1129bb394913c5211701c25bde8
[DIR]  .git\objects\33
[FILE] .git\objects\33\8c45be2120a2b3032012014bf6a5f6808343a8
[FILE] .git\objects\33\cd5128e75576c930dd0ef9e72685893f64a609
[FILE] .git\objects\33\f7ce667c90f5d38588114abb4d2aef30b66863
[DIR]  .git\objects\34
[FILE] .git\objects\34\01d6a6bdf3110055994a8c9f93aa693572b9bf
[FILE] .git\objects\34\0bb4c52cd171baa59caf6719cebe5b08c2ca39
[FILE] .git\objects\34\a58cdefd2d328a60b9fd87f2c301fe61b5e0e0
[FILE] .git\objects\34\c53b34062aeeb7a773c2e615ff383002b8e200
[DIR]  .git\objects\35
[FILE] .git\objects\35\5a1b4e12e77c34407d1889e5bb2c66b1667498
[DIR]  .git\objects\36
[FILE] .git\objects\36\05010644bc9326299e4da3637f8f9d0714fc98
[FILE] .git\objects\36\4f3cabe4d693e529976056eb004460f4ef3485
[FILE] .git\objects\36\72dcc8b5d2566709ff2b6a409f0f7b280101b5
[FILE] .git\objects\36\b2d26b0907f840ef6ac8572dc69965d7ad7fc9
[FILE] .git\objects\36\ffd2d3bf0d96126c8254931565ce97b0ed020a
[DIR]  .git\objects\37
[FILE] .git\objects\37\220554acc6a5a9bee42fc12fc1d0256cd2661d
[FILE] .git\objects\37\7fd5516fc91ec4d873573e58ab70b43a78ff18
[FILE] .git\objects\37\8dcc567acde97e523e79440b221d1a43d4e87c
[FILE] .git\objects\37\d51debeffa884d2a23dbcfbfb65b7001c7cc41
[DIR]  .git\objects\38
[FILE] .git\objects\38\bf348ad9325ca9c7d12f31896591a5b44b38f1
[DIR]  .git\objects\39
[FILE] .git\objects\39\3a38d280f4b1f0a623c3d8530273dc24da9413
[FILE] .git\objects\39\b6944d44c8d28f5ba0cecc906015fef28a5d15
[DIR]  .git\objects\3a
[FILE] .git\objects\3a\71cf76a49f452f599d6608df89d571892c5634
[FILE] .git\objects\3a\efd93eeb5755e32c71bec5a23ba3f7d8760803
[DIR]  .git\objects\3b
[FILE] .git\objects\3b\57d4514dea568a0ddb5c802b5a191f86bca1b0
[FILE] .git\objects\3b\87fe9db54a3f00c788eb185a46f828a0965693
[DIR]  .git\objects\3c
[FILE] .git\objects\3c\07e93df58fb3d5021662c6bd827a5539f180b9
[FILE] .git\objects\3c\40ffe699c4101a1d3585d28d62c34f21e5166c
[FILE] .git\objects\3c\69dc9b2cde1b40f6c00b2947fc37d38f03adea
[FILE] .git\objects\3c\ada970db36aec96cb21fad556bfaf104d6345e
[FILE] .git\objects\3c\e9bce60120746a9538106c31889d5459ae70e4
[DIR]  .git\objects\3d
[FILE] .git\objects\3d\08fb0dba2593dca8588551bd1dc48aa6c221ef
[FILE] .git\objects\3d\40c879cf3a82a731027f9acc7ff70800b59eeb
[FILE] .git\objects\3d\680f33317a5fe62b5824e9507ea2f3c9a714fb
[FILE] .git\objects\3d\e766d8d260eb2fd3adedcc8b0f322ea8b30251
[DIR]  .git\objects\3e
[FILE] .git\objects\3e\3c0a2796b6b5b84dde124ebfeabd59463f16fc
[DIR]  .git\objects\3f
[FILE] .git\objects\3f\676188793036df11273344c8e3fceca078932a
[FILE] .git\objects\3f\beec46a0cf9b09a9a4de1f62bb266529295a89
[FILE] .git\objects\3f\daba1a99e559f0d205a0d29a7e3884d20bcb1a
[DIR]  .git\objects\40
[FILE] .git\objects\40\23579cb4208ea9c5af78b353b7b9df6cf83052
[DIR]  .git\objects\41
[FILE] .git\objects\41\15de6fc0d301c8222db568d5528b6cad8c5480
[FILE] .git\objects\41\723efbaa1ab3dfeab1d1acb91c3e6e9c187ef7
[FILE] .git\objects\41\749091d74daab748a11dae42cf1b607fda49a2
[DIR]  .git\objects\42
[FILE] .git\objects\42\40b6c5f719dc9eda7e4c061a18543a35f6c36e
[FILE] .git\objects\42\894fd3c2ef18a66c7af6450952c53730031af6
[FILE] .git\objects\42\e634a3ddb3e5b3f91970ac56dc256309d1e418
[DIR]  .git\objects\43
[FILE] .git\objects\43\be6fa89388b41754541a1c1b11a4e9b4232c85
[DIR]  .git\objects\44
[FILE] .git\objects\44\234c573e9f43e9467d36364ffe3052ed36f01f
[FILE] .git\objects\44\79e146efceb629c425c336566e1bcff7d8b332
[FILE] .git\objects\44\abcc7f4ea9c77c4173e81c946a630fbf309033
[FILE] .git\objects\44\ba230c1740b2487b27d2b61bb7b806a6e69e71
[FILE] .git\objects\44\d5121ab4e4c76a3f3417201d091c6d358edb36
[DIR]  .git\objects\45
[FILE] .git\objects\45\95019f9cc3a874d83ff8428ab35c0defc8b103
[DIR]  .git\objects\46
[FILE] .git\objects\46\0a1d25545ed3b46aa0fd53e5bde16fb3586426
[FILE] .git\objects\46\36b2ed6fd632e9f8ddd349908849e785df3a15
[FILE] .git\objects\46\5debbeb7c787ed193b061abcbfc5a1bc916cd9
[DIR]  .git\objects\47
[FILE] .git\objects\47\0cdf185256451343b1c565cce5d9537e1fe872
[FILE] .git\objects\47\284a188136294398dd58989bddf3b6e41803df
[FILE] .git\objects\47\b7785194fb7bc9e31f79f6b11da6eb66c0bd74
[FILE] .git\objects\47\c2c9e14cd6272d8f1a354650f39951ca50c018
[DIR]  .git\objects\48
[FILE] .git\objects\48\7f984c2ee2e9fe1e9c54bfd72d4d9697e6be53
[FILE] .git\objects\48\aa9d56c6c89d397fcebf57d36e7a7bd0cb8fd5
[DIR]  .git\objects\4a
[FILE] .git\objects\4a\1421531720342d8213eb260ddcc5f754295a59
[FILE] .git\objects\4a\1be9976979df281dca648eca975c85cdfc2689
[FILE] .git\objects\4a\9a364ca2c000a670182ed29d7de2a79be31f01
[FILE] .git\objects\4a\9e60015fe73606ec758a82541b57dd59cc539f
[FILE] .git\objects\4a\ba1a20c06067e0b9b237c29492a46970915a27
[DIR]  .git\objects\4c
[FILE] .git\objects\4c\1a65a8e1d90fa0c0c22519fc2d63a7b0c87247
[FILE] .git\objects\4c\1c28fa040c06dc26ef4620a598382f29671551
[FILE] .git\objects\4c\5138fecf8de92b51e0328ee0fe04b78eac3610
[FILE] .git\objects\4c\58d21de9cb1f2a8fe3f8eee8dae577df5163ed
[FILE] .git\objects\4c\d323be60bfb4d9eafa01c73bd6867053ed7bc2
[FILE] .git\objects\4c\fa90824e16bdfa908489ec7b323e2aee83f7c8
[DIR]  .git\objects\4d
[FILE] .git\objects\4d\96c6b91a55ce80fbb89a2adb1f765f4c16f809
[FILE] .git\objects\4d\b9d78099110fbfa84d7a695aef4e51b8b84467
[FILE] .git\objects\4d\d9260bc15728ed7679874f270203b969c48b58
[DIR]  .git\objects\4e
[FILE] .git\objects\4e\4a7d72e264887d2c380390be0cc981d6eacbb2
[FILE] .git\objects\4e\4f72308a75221ade6871636909621f4c2ba98d
[FILE] .git\objects\4e\6fd679d8f35c6085fc190a86399aa6cf8faf04
[FILE] .git\objects\4e\9f2e1a1f76c246899b0bea4d26b6947087811e
[DIR]  .git\objects\4f
[FILE] .git\objects\4f\c8df5d83f5dfdd2dd46466b4a17f63e8b9229b
[FILE] .git\objects\4f\fc0d6463f210381c727c3d7705463817d3eb69
[DIR]  .git\objects\50
[FILE] .git\objects\50\1b69a13bbaa8794288a85d963c27f8f1319222
[FILE] .git\objects\50\293a40da9bb31faddeb496e35053753e9deec1
[FILE] .git\objects\50\cd65021ae9a2f4c80e691002ff7b25f2e88da9
[DIR]  .git\objects\51
[FILE] .git\objects\51\965b937607cea44cc6b2077ad6cb20a79d261d
[FILE] .git\objects\51\fb32890165f9012781ee0e116a0e6903a89e80
[DIR]  .git\objects\52
[FILE] .git\objects\52\24789f459539d98ad60b13d37d64fd9e6b30c5
[FILE] .git\objects\52\7a960ae25bf8b8321c343e533afab66ec37f34
[FILE] .git\objects\52\f15b3152e9b92fca3da2e2bc424a909f2f889b
[DIR]  .git\objects\53
[FILE] .git\objects\53\01756c7ccd6d435ee096d29aa2795a964d9d54
[DIR]  .git\objects\54
[FILE] .git\objects\54\8739516685a87597dd7c6bf957de33a23b4eec
[FILE] .git\objects\54\c94bb1cc64ec1c6c007063958747fa5971ac4f
[DIR]  .git\objects\56
[FILE] .git\objects\56\06a404d6c495144c8f7681c6112e79737b1b91
[FILE] .git\objects\56\acebb40e71eacdbbdb65468a275e473c586d11
[DIR]  .git\objects\57
[FILE] .git\objects\57\8526194f9f96ab178e3376cf35c2e6a1208b30
[FILE] .git\objects\57\bf7b0bfd6b5525cdcfd67532e97e261df8c322
[DIR]  .git\objects\58
[FILE] .git\objects\58\310c1cadc01362dadc7bc43e1928655a695a45
[DIR]  .git\objects\59
[FILE] .git\objects\59\0e5342ad0cc7876414b95894f653cd08de12c7
[FILE] .git\objects\59\9c5a1c85f950740435d626b6731c60946e9b2c
[DIR]  .git\objects\5a
[FILE] .git\objects\5a\b9ee3774804cbc0bdc39c798f532f06d2f7c0b
[FILE] .git\objects\5a\f9a522d77a209b5039ffb9769a10e9a61f4a4e
[DIR]  .git\objects\5c
[FILE] .git\objects\5c\7b8cc1cf317d676404bcbd5ccf8fa9f500fa11
[DIR]  .git\objects\5e
[FILE] .git\objects\5e\8653ae7ced7ea1fe2bf3a75c7df5476900628f
[FILE] .git\objects\5e\913a1e5c7eca7896c8dbb425545c683dd8054c
[DIR]  .git\objects\5f
[FILE] .git\objects\5f\2d1b4b6786e8a6e3c0718681ebb3febcc83fef
[FILE] .git\objects\5f\8591e4985d0fe4f6d8fd00221dc43b4a9fe123
[FILE] .git\objects\5f\a44499c63711ab257faca59b0f33406356e291
[FILE] .git\objects\5f\c61056552c70bcf08791771601bfffd0242e5e
[FILE] .git\objects\5f\cc98f9cdb67e4952543e36f356d2f41509d9a1
[FILE] .git\objects\5f\e64834b956e31388acd1e1316c48d59b5f6db1
[DIR]  .git\objects\60
[FILE] .git\objects\60\308aacb109772e56fce0633c4358c2a45c078e
[FILE] .git\objects\60\61d68c04a579b8b5ca18e71403e4d6345135c0
[FILE] .git\objects\60\9730b3764e25297aad646485aa25fe287b53c5
[DIR]  .git\objects\61
[FILE] .git\objects\61\2683b867376413fc487d68076e043396ab7bb5
[FILE] .git\objects\61\7a4b4fd5e2c201125b6b1bd57355bb48217d85
[DIR]  .git\objects\62
[FILE] .git\objects\62\a155b738ad6718365894ce55306855acc27ce2
[DIR]  .git\objects\63
[FILE] .git\objects\63\26ee581aece13b7552a154d7dd335631868937
[FILE] .git\objects\63\5ad456fe8a2496cbb0cadb907d4d0ab72d009b
[FILE] .git\objects\63\b336e381b874228fd9006e56eda8fdf976a0bc
[FILE] .git\objects\63\b96b7577b622588c5f9c950e0bd099dc2c954a
[FILE] .git\objects\63\ca056ec607e7bdd0a0ea8984cffbe26a57ee15
[DIR]  .git\objects\64
[FILE] .git\objects\64\be4fa393eaeb1b63c9c18510738c7fd0477268
[FILE] .git\objects\64\dabcf6f51f3cb7ec904767e364122bf0c5f41a
[DIR]  .git\objects\65
[FILE] .git\objects\65\2a12b36294e164521c07e7c2881db0d9a7cb1c
[FILE] .git\objects\65\f442c10e22ff59cfbce78050bb93fc49beee9c
[DIR]  .git\objects\66
[FILE] .git\objects\66\32aa68d51700f4f6806da8ccf852542e1edd6d
[FILE] .git\objects\66\ce53bdec854c732b25f82b1e00866965303e12
[FILE] .git\objects\66\d030b4f98d4f4248521db0878202711ac9cc9d
[DIR]  .git\objects\67
[FILE] .git\objects\67\50fe27304b63403138b3cdb9d4d241504b842e
[FILE] .git\objects\67\8c628537f5140e1cdd61862bf7ba693c54d252
[DIR]  .git\objects\68
[FILE] .git\objects\68\af9727ec2775eaa4c0f7545fa91d848cb58a43
[FILE] .git\objects\68\c33ebd01c8a46648283925960be5a81ae1cfc9
[FILE] .git\objects\68\c4a8866b1181779305404066e77c99e6622bd8
[FILE] .git\objects\68\ec026d03d72e1d359a56fd4407f3cbdc0ca131
[DIR]  .git\objects\69
[FILE] .git\objects\69\4a3ef119bcbf9303cb64b73fa3468707ec3063
[FILE] .git\objects\69\dda9b82e9892f5534cdfd21557cfd0850bf436
[FILE] .git\objects\69\e8c64e3f8a2d31762e687e696e870c3a732bda
[DIR]  .git\objects\6a
[FILE] .git\objects\6a\1d3a22d23bb4376c6659e5552743d06f0c6e6b
[FILE] .git\objects\6a\7bd07b1d1c08a93d381f5f9d26acc49f741f46
[FILE] .git\objects\6a\a58f7ef955f34d2f5a1c49accc5d1b2498b408
[FILE] .git\objects\6a\a8df7b6789c566d59a28f4e9e01e2c17c7a753
[DIR]  .git\objects\6b
[FILE] .git\objects\6b\2bdd95a80a763ffab873e1140b78269e48ca84
[FILE] .git\objects\6b\727c61d7b7d37d70a2a47815d247c8bef10862
[DIR]  .git\objects\6c
[FILE] .git\objects\6c\aeafcf2089ae756066fd25cdfbd24897ad983c
[FILE] .git\objects\6c\dd7e6652b776b29c18427ad31f8a4a990cf5c9
[DIR]  .git\objects\6e
[FILE] .git\objects\6e\ff9fa076f4caa838d758d9318fb0f9a7dbea55
[DIR]  .git\objects\6f
[FILE] .git\objects\6f\47d52ef719e0d3501c71b98b7f271734a1e48f
[FILE] .git\objects\6f\be286c79f7d77cb0ea5cd21b5e9b7c810bfa20
[FILE] .git\objects\6f\cbb8541ae5b7769c8f966a7a375c881714d7ec
[DIR]  .git\objects\70
[FILE] .git\objects\70\159e5037d4e4592396a040ae8685a842442ade
[FILE] .git\objects\70\163d9e8ed44b64cd056df05c8e472ee9aa61ad
[FILE] .git\objects\70\eb267927c66118c6a6a0f5b13475f4118c056a
[FILE] .git\objects\70\f376b87427ceaf18178f6835f9de57f88f6721
[DIR]  .git\objects\71
[FILE] .git\objects\71\6e6629e117cee74de14e2c292106b9a6f4a776
[FILE] .git\objects\71\868b044fc77b2af304aa9ae98a30282758ff7e
[FILE] .git\objects\71\f0f192f4ac5df028d44ade30a2fa3a506205db
[DIR]  .git\objects\72
[FILE] .git\objects\72\9d157de50d71f3302817f38e86d21374217918
[FILE] .git\objects\72\b013699d8bbc531148a88917a37a1f987a82a6
[DIR]  .git\objects\73
[FILE] .git\objects\73\2451d9e64ab857e2197b8188da9e62424fc7f0
[FILE] .git\objects\73\5bb7d2a07f834c3bb018286469e906704d1b1c
[FILE] .git\objects\73\72f53239321b428d4571f5c5e6b34572e24192
[FILE] .git\objects\73\b16c60f7af49f0cf23d2eab49a50a330556265
[DIR]  .git\objects\74
[FILE] .git\objects\74\2c7d414fc33e83057d3590b097fc38c3135829
[FILE] .git\objects\74\682835b8d424ff1d6a13dbb7720955408bddad
[FILE] .git\objects\74\bff31f38d347bfb05ba8c518aa492e8afafd50
[FILE] .git\objects\74\c358f4697c60a836de85ec5e50fc665d01ea48
[DIR]  .git\objects\75
[FILE] .git\objects\75\b690ed6d86aba78b62b6ed1a3a173cc12bf994
[FILE] .git\objects\75\da2e46e567ed1c2b0ab7c3a8383b82860b722a
[DIR]  .git\objects\76
[FILE] .git\objects\76\706f4c62ba48a519ddea76239a005a233f4959
[FILE] .git\objects\76\ab6850adb5afbc59c050e9c3da20aa17408d49
[FILE] .git\objects\76\ce93bf408d05b902eb7b52eb4e987b16fcc4e6
[DIR]  .git\objects\77
[FILE] .git\objects\77\01b4627ed6e6f3d6de48703d8e67f9a3e0b846
[FILE] .git\objects\77\54bebf893d1ffd6e9d9628475cb7c7920ff791
[FILE] .git\objects\77\883e8ead496a8913990f070f30de1985847014
[FILE] .git\objects\77\ac64c03f699d2a2ac1fc09dff07ea810a8a470
[FILE] .git\objects\77\e1d6ccb12f358378958c76f77807d16d3da8a7
[FILE] .git\objects\77\fcf78c56104ddf3ab85cf327ffe352b1f25bd9
[DIR]  .git\objects\78
[FILE] .git\objects\78\3afa12f10c2f1924e310c9f48859bf6e851351
[FILE] .git\objects\78\4f1cc0e33e857cdbbddfe68b7bdbc3eed83f93
[FILE] .git\objects\78\617ec7dc46671c2995d987c4c23ff8f0317ad8
[FILE] .git\objects\78\7f44c4345db3fef3cd0924276f3bcc08d6dad5
[DIR]  .git\objects\7a
[FILE] .git\objects\7a\0c1679b48b660ece93f3313a861f2efa0174d7
[FILE] .git\objects\7a\a30b439398e8243e8c4a43a0af1816e7a7dda1
[FILE] .git\objects\7a\b855016d504355a4c2bfa4ec0f6543e7134867
[DIR]  .git\objects\7b
[FILE] .git\objects\7b\5ceadcffef5f4503b7eb0adf7fa7a31ea1813b
[FILE] .git\objects\7b\eeff2edc2b5d1f7783cafcd7c2ab67f55a157b
[DIR]  .git\objects\7c
[FILE] .git\objects\7c\5d531054776431bf8203a0f36f4f48a3e10f80
[FILE] .git\objects\7c\5dce5df084dbf14cc4c04bcd5a8a6301c8eba4
[FILE] .git\objects\7c\c42edd77330efee12c0e44f027eeadd3f62ec5
[FILE] .git\objects\7c\c5d6837ce16b47ab6f80ecf019acbb9be6cff2
[FILE] .git\objects\7c\e84d982be18126189f3fcc16519fd15baf02c6
[DIR]  .git\objects\7d
[FILE] .git\objects\7d\33eecb4b8f902741f9ddff5c168601aae51fe5
[FILE] .git\objects\7d\7fca63f81e72fb023a0ec57b8dfff6f93f4d81
[FILE] .git\objects\7d\ae196fa08f920de0f08fa9c114d6b32f4006e5
[FILE] .git\objects\7d\ae3507a4333c11909514125008eb6ea970876b
[FILE] .git\objects\7d\c470492d10c4af03a198b8cc580f86babe01d5
[FILE] .git\objects\7d\e3aab9ae894c626e0982a587b522fd506e9ad1
[DIR]  .git\objects\7e
[FILE] .git\objects\7e\01ecad0b51a31256bafd078c1e5478c5f547d5
[DIR]  .git\objects\7f
[FILE] .git\objects\7f\4fdf6f648202593859f09729fe7e5ccd82b662
[FILE] .git\objects\7f\ca23b88c0fd8a67c49b0c0bc80a6b384020b0f
[DIR]  .git\objects\80
[FILE] .git\objects\80\31d7aff735d3a23b2e79ffd874e33f79d8e292
[FILE] .git\objects\80\78b7405e98195d1529c0401337eb0da41fe4f3
[FILE] .git\objects\80\865953c57cc605332c035e0ef273da82e61a18
[FILE] .git\objects\80\b6e6c344b1785f6811635bb2b2605da3cd6c5d
[DIR]  .git\objects\81
[FILE] .git\objects\81\ce48f43e88d523a7b736455536823a75403341
[FILE] .git\objects\81\eb4aae1db09d07c8a7bb5252e878bb2bb951a8
[DIR]  .git\objects\82
[FILE] .git\objects\82\0610a9aaf6eb807debf384b972cb875f1d84e7
[FILE] .git\objects\82\6d061181cdb6366c98e4f51f77e6a2fab5718b
[FILE] .git\objects\82\ce2e779f5887636245e19fb2b1a911191b9688
[FILE] .git\objects\82\fb70a205592b938e40528653b233bc83563db9
[DIR]  .git\objects\83
[FILE] .git\objects\83\39f27eeae261fd51d09b3f624c6c5a2e8f5338
[FILE] .git\objects\83\84c77702572d6d879912a5d76a8184937dc1eb
[FILE] .git\objects\83\e6db736fd2d181f3ebfda0bc7b760a90ad95b6
[DIR]  .git\objects\84
[FILE] .git\objects\84\46b97321ef29d4e4644cab549914b4c4507615
[DIR]  .git\objects\85
[FILE] .git\objects\85\55ad948ae3b7889648610b3e736a3f8467c033
[DIR]  .git\objects\86
[FILE] .git\objects\86\8b33e4b665f9c82ff61f1069659cd5849ba63a
[DIR]  .git\objects\87
[FILE] .git\objects\87\1c7cd4fc1d2d5f06ef0c36ea0b815583a6c467
[FILE] .git\objects\87\2260e0328000392323d41f4a2054e5033e5b0f
[FILE] .git\objects\87\4284807367974309762eb5ccbe076283d52683
[DIR]  .git\objects\89
[FILE] .git\objects\89\2d1a4b4b8ac09552b20b27c195c095d326b868
[FILE] .git\objects\89\5508dd0e35fc92660564ba06ac7d12e0ad2149
[FILE] .git\objects\89\a678b2c09696fd316c6d95103b2d7f763595b0
[FILE] .git\objects\89\aefc7d0486519f3d5a4a969dafef2e765f6234
[DIR]  .git\objects\8a
[FILE] .git\objects\8a\505ce81ffc211d4ae0b1d695b18dd783449db1
[FILE] .git\objects\8a\865a3180f4ee60706692d92cd3bad7fa16f5ea
[FILE] .git\objects\8a\9e0edeb475ebbe24433d5e27244f7626db42ad
[FILE] .git\objects\8a\c6728e3ce412af7218559c7b9bf00c650b2c78
[FILE] .git\objects\8a\fb289410e8f5bb6b5271b20771055c6246f146
[DIR]  .git\objects\8b
[FILE] .git\objects\8b\261107bd17c6d2ed584020bcf7fecc019c5a8e
[FILE] .git\objects\8b\e5dd26ccdd5fcd0fdf468698eb01e868f9fee4
[DIR]  .git\objects\8c
[FILE] .git\objects\8c\5765e54790df5a693dd8e3c8e89fa87a5170be
[FILE] .git\objects\8c\7d5fd8ca78229a7a0084261a97490ed0e580f0
[DIR]  .git\objects\8d
[FILE] .git\objects\8d\24fb8a70162607d5fd5da7e657c3e112d0cd47
[FILE] .git\objects\8d\43a327163ec2a4ee422c67c97b5af8428e1ecb
[FILE] .git\objects\8d\95ca3a40b11cb3d5333479b7125ccf863b94ed
[DIR]  .git\objects\8e
[FILE] .git\objects\8e\456f5dcf86d521c5979de3ca368fd58aa66840
[FILE] .git\objects\8e\940e3b83dec452266b938a7606380290649bdf
[DIR]  .git\objects\8f
[FILE] .git\objects\8f\5c082fadc372d74eec664c7d0ce9d00bae09e7
[FILE] .git\objects\8f\d47c6ff23141cc2430ffb6709d1a4363de6e33
[FILE] .git\objects\8f\f6bc2a79f5e2e194ae35921fc00b0a8d97923e
[DIR]  .git\objects\90
[FILE] .git\objects\90\819b2c1f747445464f25fc51b4932403ee039c
[DIR]  .git\objects\91
[FILE] .git\objects\91\02d6008844e95e32692145fb072028e8d9790f
[FILE] .git\objects\91\05c81afdaa0c489581443bf182e29dfc3916f3
[FILE] .git\objects\91\413a8695ae40cb0ee9e0be6a81df70499d54bf
[FILE] .git\objects\91\f47b576450b3e60626e6d2ab7e3f1e9216288b
[DIR]  .git\objects\92
[FILE] .git\objects\92\5368cdb6c1be2eb6026ca0fe2c9fc7b8366111
[FILE] .git\objects\92\5b1351bcda9f1244c4634843cb5f994a6b5008
[FILE] .git\objects\92\811937edda77d453960cff1bb00c5e539c455b
[FILE] .git\objects\92\8159782b41505a325843ccb05fffefa61e6042
[FILE] .git\objects\92\a9babe89671a2d044fd70d48ce9821a953270e
[FILE] .git\objects\92\adbb9154e02650e621c16eceed5d7262bbe9d2
[FILE] .git\objects\92\b9dc15f72dd0d68315bc3d67460d646dec4616
[FILE] .git\objects\92\f54154554df3219aa32cf9c1efc6bfef96ab5b
[DIR]  .git\objects\93
[FILE] .git\objects\93\1cb30c732b4031a55d5c3dc78d470e25a6b52e
[FILE] .git\objects\93\563503eeeec84ec92d7681abf39a6318803d6d
[FILE] .git\objects\93\9a5cc7d1882dd6b6aacad26d8b74c457e621c9
[FILE] .git\objects\93\a9962c3ef98ab33d09031e3633a07a53ef0df1
[FILE] .git\objects\93\b6d9ee41eea8efb3ae1c6994296ea2d4cc7e6b
[DIR]  .git\objects\94
[FILE] .git\objects\94\07b67bd1636434f4db9a3dd4a12018d4f011ec
[FILE] .git\objects\94\32143b6e7590dbeab3d106bc31cba7fafbebf5
[FILE] .git\objects\94\54b936701286d5c0b474fff5a83c4d3d8eb842
[FILE] .git\objects\94\9f753174ce1f7cf83fe90163f84b8781c52991
[DIR]  .git\objects\95
[FILE] .git\objects\95\3de6e9bc9ef830b4be1607b4bc670b82749e83
[FILE] .git\objects\95\ba3fb97f56e76e5fc6f6e528d541ac3edfe38c
[DIR]  .git\objects\96
[FILE] .git\objects\96\184bf8b9d0578a4bf30396d3eb5fc21cb63cc8
[FILE] .git\objects\96\5a145c867b5c247665edf6aa247181f6eca3b8
[FILE] .git\objects\96\f0bddfb973f1ea737beb9a877dfc9264030edd
[DIR]  .git\objects\97
[FILE] .git\objects\97\40ce7aaa1fabba613d00daa8620e317d5fa374
[FILE] .git\objects\97\5db2b944d33d2335d5bf62cf2f6692a2e295b9
[FILE] .git\objects\97\703f6745a09a6e442f00c9d20751e532304167
[DIR]  .git\objects\98
[FILE] .git\objects\98\e4a228867845ee27064225dbc60e38994c1e46
[DIR]  .git\objects\99
[FILE] .git\objects\99\25120fc92ad5bb20cff2d7c4572df2d74016b4
[FILE] .git\objects\99\321932f86bcd4ab01eb6a010cb77edbc035b18
[FILE] .git\objects\99\4b317d30ad635f33665188fa92cd9b012448cb
[DIR]  .git\objects\9a
[FILE] .git\objects\9a\437940d0fbb731f518d000a9efef362ea481f5
[FILE] .git\objects\9a\76d34f416f9541d974f736f97b87096423286a
[FILE] .git\objects\9a\f1ccebf554e8240b9a04bb0f50d89a2201d08a
[DIR]  .git\objects\9b
[FILE] .git\objects\9b\1ec403d2c49b8ae19ee4da8d69441bddfb01d9
[FILE] .git\objects\9b\baa51cf47a1ebe9f3bc27e9e0fea81eef0d59e
[FILE] .git\objects\9b\cb354367f75ed243366b2160a9a2c09fa36d40
[DIR]  .git\objects\9c
[FILE] .git\objects\9c\0ff8d0a5d204127691fd443af761bb9a55de84
[FILE] .git\objects\9c\131c56fb3aedd3109a5e14654aebe975405fe5
[FILE] .git\objects\9c\25b0ab013b403b904dafc1bc9fce9106be68b6
[FILE] .git\objects\9c\3082c7e397abf3f803d6d9b6c3839440a1fe01
[FILE] .git\objects\9c\c8198ddd5ed1ecc27fecdd2ad2ea6ce23af540
[DIR]  .git\objects\9d
[FILE] .git\objects\9d\26fd86a0722df73af611982d0f1736f1a9a61f
[FILE] .git\objects\9d\5313495c38e2d9fe6d19bd335f7ab738aee741
[FILE] .git\objects\9d\6343a065e592ed70953a043be9d2ed28712066
[FILE] .git\objects\9d\9e906f6f381be1a72a3ff2f61950a5ca82c95e
[FILE] .git\objects\9d\aac640701812c7dfa5243d8f476df62f7ffc3c
[FILE] .git\objects\9d\db861884112a387f5866eec842b92b35e39e66
[DIR]  .git\objects\9e
[FILE] .git\objects\9e\5faa9ca1f3f7590e41180bfcb98db8e3a7ac44
[DIR]  .git\objects\9f
[FILE] .git\objects\9f\1ce3cc2050430c10ebb41d8855a0c27a8b914c
[FILE] .git\objects\9f\506ac1799a050528c89529d65cf6d640bf643e
[FILE] .git\objects\9f\512268f9760ca15d1121ad289b53e9b8791d8c
[FILE] .git\objects\9f\f51e4a658415092afbc10d5bc6b88f66e334bf
[DIR]  .git\objects\a0
[FILE] .git\objects\a0\0bbdb21a548c971494751f9adcdc41cad281a7
[FILE] .git\objects\a0\593d6e1072e6c1accb5070785d9d0c1ba7829e
[FILE] .git\objects\a0\e9089c55ba26af2742ce9896f14b02d2f3e152
[DIR]  .git\objects\a1
[FILE] .git\objects\a1\99304faca6e5bc47064f49cee3e66e323c7b81
[DIR]  .git\objects\a2
[FILE] .git\objects\a2\5758a90f0f18182fb28501ce8e1a6206ac813c
[FILE] .git\objects\a2\f3aa160cf77931d6b87019c66aed4f2882c244
[DIR]  .git\objects\a3
[FILE] .git\objects\a3\0c00aa051595b6963219709f1fe9fe44652bdc
[FILE] .git\objects\a3\504ea129a9140c177c088dfb8d585cfc04e088
[FILE] .git\objects\a3\d600fc800e6d1cfe804b789a73e636a39ad1e0
[DIR]  .git\objects\a4
[FILE] .git\objects\a4\3c79936864a4ebdf1086a9253fa86090bf1ecf
[FILE] .git\objects\a4\710cb6c67e3abb8c6f9fc12dfa38e7fb34edc0
[DIR]  .git\objects\a5
[FILE] .git\objects\a5\0b24b5e925538e00461db3ff0d0b86d14e165e
[FILE] .git\objects\a5\0f429634b49e2a10f3596ecbcddf40e5a57191
[FILE] .git\objects\a5\587ec37a123e55a17b31e2a41c3c40fe661f6c
[FILE] .git\objects\a5\e45823a449ffedc9d71c99b1865a24336d9743
[DIR]  .git\objects\a6
[FILE] .git\objects\a6\56886deb1bec6ab86d71b1265f8e2149e75486
[FILE] .git\objects\a6\6156820f2598016e728f444821d9504bfe37d4
[FILE] .git\objects\a6\a11830d8d5251695e0057abcda380d1aaaedb5
[DIR]  .git\objects\a7
[FILE] .git\objects\a7\158c3c8ed62b30cdb83cc8bac6ad193ea7fd60
[FILE] .git\objects\a7\4ab9a589bf8f82a2491e400b4b3b827c91ca17
[FILE] .git\objects\a7\5e6fa7147298b1355d872a672d45ef243e624b
[DIR]  .git\objects\a8
[FILE] .git\objects\a8\02a294d58a3496723fcd1a37d85ff564747a8c
[FILE] .git\objects\a8\5cf853eb3cb3c26a3fe1a4e0381ae314dad708
[FILE] .git\objects\a8\a9c5a4d0adac30da22cf989393728ecd08fbf7
[FILE] .git\objects\a8\ccd59563871774064c80cda3a2d80c5785bb0a
[FILE] .git\objects\a8\fce39cb35653edb0d31811b92ed8d1c3b26068
[DIR]  .git\objects\a9
[FILE] .git\objects\a9\5fae61af0fe4d13c469bb4a23bbbab6fb7da26
[FILE] .git\objects\a9\c4de4a13fdf5253e93f19dd09183dc9b7d73cc
[FILE] .git\objects\a9\c73ff020b3388c87c9b30c11fb19b930414f86
[FILE] .git\objects\a9\da897d0fb3c5766bf93a0721f378d77c160190
[FILE] .git\objects\a9\e2098db6826f3da5035f656b6f8bedb5dfd39f
[DIR]  .git\objects\aa
[FILE] .git\objects\aa\6a55920f5abe441f74a71a85474c36884da3c2
[FILE] .git\objects\aa\c99dc26eb26d7d055daf06324d2f4e66ecfa14
[DIR]  .git\objects\ab
[FILE] .git\objects\ab\e86b1a848372bcec184f40beee88748d1acef1
[DIR]  .git\objects\ac
[FILE] .git\objects\ac\1c6067b19576b731c2c806a10471d40288e001
[FILE] .git\objects\ac\1e28c32f5a392c5cb49c472356efa472f44814
[FILE] .git\objects\ac\b7e82ede446249db13e529fca9407f62c6875b
[FILE] .git\objects\ac\c506f583448735c3f52cf647c2b238ed3d33f0
[FILE] .git\objects\ac\e3cd629f0917429874f586f360f33da137b948
[DIR]  .git\objects\ad
[FILE] .git\objects\ad\8307c90c19c5c51e91d34b403908d855c27816
[DIR]  .git\objects\ae
[FILE] .git\objects\ae\5b31da17362761350545c9cd4145d9d5aa5859
[FILE] .git\objects\ae\ae0e84043cda2445f66e64c36b6a8b67fff9ae
[FILE] .git\objects\ae\c9ab0598e2e834d4b90250b84ca7da6c742da3
[DIR]  .git\objects\af
[FILE] .git\objects\af\891ec3d401fa455ca93f93971ef5c25c1393f3
[FILE] .git\objects\af\cf2523e22e85c620b8c1325ce35c9f258e611c
[FILE] .git\objects\af\d46fc834eea1cfd97f4fa3b070b4156e27d485
[DIR]  .git\objects\b0
[FILE] .git\objects\b0\02df02c12193a4626a0710fead96b15e298284
[FILE] .git\objects\b0\3cd96a0fe7f6858a23a81e614d7f748faffbad
[FILE] .git\objects\b0\f7aaa918bfdc6ca82afdd35a5c2dbfb5521a86
[DIR]  .git\objects\b1
[FILE] .git\objects\b1\bf3b70a5bf325a021609cc43a405e583bf9dd6
[DIR]  .git\objects\b2
[FILE] .git\objects\b2\72be10d649c9e01a2b2bfa97ede299b7b681f0
[DIR]  .git\objects\b3
[FILE] .git\objects\b3\ed084e1fe79bc0d52ad618fcb6e3065807341b
[FILE] .git\objects\b3\f5290a300218655da799e042a2003bdb68eeb3
[DIR]  .git\objects\b4
[FILE] .git\objects\b4\3e801d1b1e60b7a21ad88e1d6741eba23760c2
[FILE] .git\objects\b4\62721d55ca7e541cec4345e40cd7070e784b44
[DIR]  .git\objects\b5
[FILE] .git\objects\b5\5c367371b0ed5eba300e8cb88cf98e05f2874a
[DIR]  .git\objects\b6
[FILE] .git\objects\b6\7183d3590f1be0ff8e95a65030841bd55c5234
[DIR]  .git\objects\b7
[FILE] .git\objects\b7\868a6e4df3d8e0f6099e8d01a9a2e736ad76ce
[FILE] .git\objects\b7\99e7d12d700a3817b8ecff79848c4bd76d2bce
[DIR]  .git\objects\b9
[FILE] .git\objects\b9\3bc3efb8a6958315edce31e3ab03f9878d140c
[DIR]  .git\objects\ba
[FILE] .git\objects\ba\8dcbbec2afacd7d695a156de94151236d5848a
[FILE] .git\objects\ba\f2fcea27a64cb2581d0935ad676df848e2b964
[DIR]  .git\objects\bb
[FILE] .git\objects\bb\0dfa38495689ceaa78816f22383df5e9a58515
[FILE] .git\objects\bb\10d52075d2bfba2fde6b5438fedc205f7c6863
[FILE] .git\objects\bb\2ecf9f0ab026c9aff9dd7563850f96bdc3e9c7
[FILE] .git\objects\bb\f44ac3dfd6d1871785e875bccc789bdcddba46
[FILE] .git\objects\bb\fcdba73d6b720a84a41ed5c7e1be4bc8765252
[DIR]  .git\objects\bc
[FILE] .git\objects\bc\191ceab3dd6c2d6e119fcb44a4d543d29026e1
[FILE] .git\objects\bc\1ff0da981e63e7604ea3a67c2b22c129d3133c
[FILE] .git\objects\bc\29c24091b41c5f35aacacf3df25e4d19e4aeca
[DIR]  .git\objects\bd
[FILE] .git\objects\bd\7174f3a7e58bb6905569b3e03a252873102aba
[FILE] .git\objects\bd\9d786fdb832df97f540387bb2b2bdb6669cc90
[DIR]  .git\objects\be
[FILE] .git\objects\be\4e13773d8458114ba7a4171dc7873a49fd1621
[FILE] .git\objects\be\d0fb8a994f12042984fd8124170fef4d054d80
[DIR]  .git\objects\bf
[FILE] .git\objects\bf\7025e2b533e417b5a420aeeedf4ea6166b6cc6
[DIR]  .git\objects\c0
[FILE] .git\objects\c0\1a1b7572b1a0192d051b6e4469c4739426fdf1
[FILE] .git\objects\c0\e5393447776d4d3735d82b2d55269877a740de
[DIR]  .git\objects\c1
[FILE] .git\objects\c1\22fbde2e02514633788b37ee1926152a4b1b17
[FILE] .git\objects\c1\856d52fbbaaee130c61987b5a36143d1f1395c
[DIR]  .git\objects\c2
[FILE] .git\objects\c2\a70fa46caa161126cf455366bd5682421feb62
[DIR]  .git\objects\c3
[FILE] .git\objects\c3\40547dd32033a077017b886c7b73d527311558
[FILE] .git\objects\c3\56fa483fce4660ef5e7f728a63284388beaa01
[DIR]  .git\objects\c4
[FILE] .git\objects\c4\17ae0d6ee9b75f818e9f7eefa674e96ad277bd
[FILE] .git\objects\c4\f57984c85b5ce6660e771b97056a5ef4ef0ef8
[DIR]  .git\objects\c5
[FILE] .git\objects\c5\40cbc1655f8ee019d3fc07f396dc1ea0bcc1f4
[FILE] .git\objects\c5\54909a021ad54379221d20c28d8e6b028e3a6c
[DIR]  .git\objects\c6
[FILE] .git\objects\c6\13821c515d166024e3f9b8f5bc6eb3bf592cd0
[FILE] .git\objects\c6\204d3006663da9bcec0d501b0e82c45723bdc3
[FILE] .git\objects\c6\29e207a2ed2668cb41b7d8798fbac580e1bb70
[FILE] .git\objects\c6\7063b4a75c080272643bec7552ff410a717484
[DIR]  .git\objects\c7
[FILE] .git\objects\c7\578c68ce155294991e58d9828242d300819de8
[DIR]  .git\objects\c8
[FILE] .git\objects\c8\1653bf408d02a330dd92fc4ce4d91d7839486e
[FILE] .git\objects\c8\32938dfedb4540c18ac80d60a433fad195dbaf
[FILE] .git\objects\c8\5dd7964106dd34bb76a8a54c5eb4b0e0857f7d
[DIR]  .git\objects\c9
[FILE] .git\objects\c9\2d98ec380d2d369bf1e4f1932deb5385f392cc
[FILE] .git\objects\c9\d91ac018046bb861d0d1d5472d2b468fb8e716
[DIR]  .git\objects\ca
[FILE] .git\objects\ca\478348576cbd05c71e9c9f36d7fb1b46291373
[FILE] .git\objects\ca\aaf4e0c421827ade81917b506b6a28784e09c9
[DIR]  .git\objects\cb
[FILE] .git\objects\cb\07515bc83dbae35673cd524f4d4b0d2068f082
[DIR]  .git\objects\cc
[FILE] .git\objects\cc\c6018f9068625ebd596379dc03639120a3be5d
[FILE] .git\objects\cc\db7e50dd79744cce31e98a1737466abab1f9d5
[FILE] .git\objects\cc\e99126792693f1c00a180c4fa3ec56ac70bb30
[DIR]  .git\objects\cd
[FILE] .git\objects\cd\054dddf2afcaa2abfc45d0ad6b28e3aea08072
[FILE] .git\objects\cd\451aa0b1284ffffc9e04c44b88b02915e1ee0b
[FILE] .git\objects\cd\89b9ef1b0c2f530d67f42967a2d067a5ccfd39
[FILE] .git\objects\cd\e79bc1014d02330dee1f42674bb16f83a17266
[DIR]  .git\objects\ce
[FILE] .git\objects\ce\0f2c4e4e5b83571d1a2785274d0d24a219e408
[FILE] .git\objects\ce\926dcf79996b0ca99f61d51cf7e6e4127eedaf
[FILE] .git\objects\ce\e9039761174aa7295905f38447d022e71c0db4
[DIR]  .git\objects\cf
[FILE] .git\objects\cf\06a1156a1fa2bd5114f5c42d54fe397919c014
[FILE] .git\objects\cf\2e0f53ea8a0a78afa408de5145b924a35dc56d
[FILE] .git\objects\cf\7d975dc1d57a70480b21c7634a669cc8ad1da1
[FILE] .git\objects\cf\9310ab611033a77e618b644c0226a1ce64e4b1
[FILE] .git\objects\cf\eddb40630e1b8e9024ce821e0391dca734dd28
[FILE] .git\objects\cf\f06b78ada5b6873f6d845b27473ed078a5470c
[DIR]  .git\objects\d0
[FILE] .git\objects\d0\22f9d58c6b8e4cebd998ab362c713d46326e0c
[FILE] .git\objects\d0\47d5e846ce7a93f5ca2ad9c3aaa2777e50d677
[DIR]  .git\objects\d1
[FILE] .git\objects\d1\2bcbc76ed30239132cc61c564f61f652e0ad1c
[FILE] .git\objects\d1\9eda0bb192bd94ea9fe815cdd11ac5c05f0fc3
[DIR]  .git\objects\d2
[FILE] .git\objects\d2\68f9edc17e9faef627e68df45a06acabe675b3
[FILE] .git\objects\d2\8fdf3fd36014e8eb530b2d9828851dba625cd7
[DIR]  .git\objects\d3
[FILE] .git\objects\d3\9e8afcbca950bda59df7415cd551c3a8f1fb3c
[FILE] .git\objects\d3\e55e972a93ac996a54f14bff37ace48e051125
[DIR]  .git\objects\d4
[FILE] .git\objects\d4\126e70e8694234261ba5a1286425f981312a74
[FILE] .git\objects\d4\279ae9e25f355ac948b2f39df428a84bae213a
[FILE] .git\objects\d4\514e12e38d3cbc0fa1925c556d9ed1b4367bbb
[FILE] .git\objects\d4\7479aa9fca641284da79ce9d997046f5e1e9b1
[FILE] .git\objects\d4\7cf6b1dd89c7680962eddc2f45a808968a966a
[FILE] .git\objects\d4\bb549418b5b4d4cc477b980a2fa32b03be8d67
[DIR]  .git\objects\d5
[FILE] .git\objects\d5\1946fe52e29acf632b3056ad872589f4fdaaa7
[FILE] .git\objects\d5\5d053e1bb929cb881e94d09fa859890aeaa17e
[FILE] .git\objects\d5\af93855ce15e41ce2c496c30f139e569e809e9
[DIR]  .git\objects\d6
[FILE] .git\objects\d6\0348b14e382864b13bcb71dee3b59143d8dad4
[DIR]  .git\objects\d7
[FILE] .git\objects\d7\8ca14cfba0e1e14fed97509c415b6849b85195
[DIR]  .git\objects\d8
[FILE] .git\objects\d8\7b80c7059385359d653a22f33408c5bd358a7e
[FILE] .git\objects\d8\860a8130adb4139c180bba2f7fff8dcbc2018b
[FILE] .git\objects\d8\b22a1aff23ad84ef668d26361da95779cdde2f
[FILE] .git\objects\d8\d3bcd3fc90f6d559368d18e4168d8404ef649d
[DIR]  .git\objects\d9
[FILE] .git\objects\d9\0e11030c5e5248acd37839873882c497001160
[FILE] .git\objects\d9\4aa4ce768e24558e5dd2cf8db4cde0802a453e
[FILE] .git\objects\d9\a7282b99d0ee8b134c6cc4969c28441328d23a
[DIR]  .git\objects\da
[FILE] .git\objects\da\549538361ea1e8ef2e67fdc8666e763106b858
[FILE] .git\objects\da\8baae5efc694d6c72b64d42c36cec9dbae15f0
[FILE] .git\objects\da\933d9ed5c2d391647a17779f5a93e1d9586b57
[DIR]  .git\objects\db
[FILE] .git\objects\db\6b5e0f2778af02c67ceb5c458df62008d17696
[FILE] .git\objects\db\be7b7c851a217f093bde07217786c482eec4e1
[FILE] .git\objects\db\df47df81e9d27fe160a314a37d0cce22651376
[DIR]  .git\objects\dc
[FILE] .git\objects\dc\08508dcc903931a98fa82e0fbe8fe9834a7de1
[FILE] .git\objects\dc\643f62fa7d925cca042ab5be3865202f664b6a
[FILE] .git\objects\dc\bab0d6de92e3e6579e765025e13f8ca4bddc86
[FILE] .git\objects\dc\c964aded12c3e5456b74cef0b9d8ef6928d7a3
[FILE] .git\objects\dc\d3b48a5690315ea95d78d93ed19733529afe9e
[FILE] .git\objects\dc\f648ae9310fa14525bf3d4e6bae95b9ef70874
[DIR]  .git\objects\dd
[FILE] .git\objects\dd\6878bea0badef222cf11d9124439badf3d755c
[DIR]  .git\objects\de
[FILE] .git\objects\de\790a80460238c3cde8fed9f30d0d1b71e52813
[FILE] .git\objects\de\f1770c2f6f32b3f413506b3136d0c4452de4bb
[DIR]  .git\objects\df
[FILE] .git\objects\df\1d509dc2708f66ef4912f4060ef456b1934749
[FILE] .git\objects\df\4b561a331a713885853ba7021a5fa1e420bbc0
[FILE] .git\objects\df\7d3fe1add16fcbf48efd6269127af7646beef9
[FILE] .git\objects\df\ee4c09b0690ea83ae3303849cb95f89cf45363
[DIR]  .git\objects\e0
[FILE] .git\objects\e0\cfea938761a6b3c3fc556ef43a2c1206c4623c
[DIR]  .git\objects\e1
[FILE] .git\objects\e1\1ea159ac4cfb0760a2ea37c8aaa2fb0a21e02b
[DIR]  .git\objects\e2
[FILE] .git\objects\e2\09c5acac4433b37599998e357f7c9ef6004461
[FILE] .git\objects\e2\de349966956e2516d639055a7a57d607b636de
[FILE] .git\objects\e2\f70cfaf7c92061cf0945ac533425a8eb55918b
[DIR]  .git\objects\e3
[FILE] .git\objects\e3\0c8af8f67ba8d5287d738ddb126bb67fd13a85
[FILE] .git\objects\e3\1b14afa125863a19e538a123b349ff0091e792
[FILE] .git\objects\e3\2f59ac412f1e2e517ce225716c5f0a9d0a07c6
[FILE] .git\objects\e3\4164cc3dcf0170e11ff59d9a391136c56fa35a
[FILE] .git\objects\e3\5ad5196ade62038676fd9cdc20d82c74c8eab8
[FILE] .git\objects\e3\ae48903fab590507f3e986ab0d1f1cb1d60636
[DIR]  .git\objects\e4
[FILE] .git\objects\e4\87b71f11dd7d22357bffbef2dbb9f24f78be4f
[DIR]  .git\objects\e5
[FILE] .git\objects\e5\43ccb046831c9d7493e46911c9b36ccfe63493
[FILE] .git\objects\e5\5520fef08ded8975fd4cf37d1b3415e37402e0
[FILE] .git\objects\e5\5a9fe2058c210e2a087ec7b2387fe168fa3ab2
[FILE] .git\objects\e5\612663d9ec066c8d8106eae25cf11c5816afeb
[FILE] .git\objects\e5\c02512bc37fb139ef20a02b2150d1a61ee2730
[DIR]  .git\objects\e6
[FILE] .git\objects\e6\10cb8c929237860170e55148763590b50874df
[FILE] .git\objects\e6\989ec2807c0f8df1096be67696adcc6895d927
[FILE] .git\objects\e6\9de29bb2d1d6434b8b29ae775ad8c2e48c5391
[FILE] .git\objects\e6\a78a5850b38e5cb1202e8aedf1ad69be8e6cd2
[FILE] .git\objects\e6\eb7667447f003ad44a896dd7871cafa240db46
[DIR]  .git\objects\e7
[FILE] .git\objects\e7\b00585949e978fb63227d872cd4739e0de0c7e
[DIR]  .git\objects\e8
[FILE] .git\objects\e8\5a2a1be83605ef6e79e725ba4521f8d3692d23
[DIR]  .git\objects\e9
[FILE] .git\objects\e9\48d859f0bae580c9fea9d6408a23052ae7bd50
[FILE] .git\objects\e9\82c4481b1949d7617527f146abb1f1e2327ef4
[FILE] .git\objects\e9\b5722073ea1e57816d9a3471842d3a80bdd3e9
[DIR]  .git\objects\ea
[FILE] .git\objects\ea\6d71ca15026a04c95a3c340b1ccfa477d65e0f
[FILE] .git\objects\ea\bb3ec77a55a505f85ce55f73469f61701714bd
[FILE] .git\objects\ea\c460f2c9845a7ab8d5fd6ef06ce94900881fab
[FILE] .git\objects\ea\c87ea4813cb4a0b61f7a2701430fc10e593a65
[DIR]  .git\objects\eb
[FILE] .git\objects\eb\7c2fdab722fbb133ca9ff8f25c6de5a867384e
[FILE] .git\objects\eb\b613751db1d4c87164f8daf1dca2fe257c3045
[FILE] .git\objects\eb\ea4a934f76dbbeec236f15d5ced2e3bad1f102
[FILE] .git\objects\eb\ed41826b648719f700c2758cedb870099c1815
[DIR]  .git\objects\ec
[FILE] .git\objects\ec\1c4d601cd11688dc8e138bafe6f091087f41dd
[FILE] .git\objects\ec\63becebf7c47c1fa2789b795a39baa64640e55
[FILE] .git\objects\ec\e4180263215c26c67bcd1ba9a131f2dafd135f
[FILE] .git\objects\ec\f025b891405f55c9f6b59763c856a8b19a023c
[DIR]  .git\objects\ed
[FILE] .git\objects\ed\0d738f8c9f6bc049af6c7d6b0cf6bc24a084cf
[FILE] .git\objects\ed\5b5b4a38d904ea357aeb24c89e1cc47c75ed79
[FILE] .git\objects\ed\837a7e63d0abb0301b552bc78a01c1f9babf08
[FILE] .git\objects\ed\a836eb37259f66166e55e345fc043c43ed3c3e
[DIR]  .git\objects\ee
[FILE] .git\objects\ee\08ae94554db8c1ce1267f953bfba40bfb3fcf0
[FILE] .git\objects\ee\0ce0f79ba796c479174af598f66f774476397f
[FILE] .git\objects\ee\d175898dff79553993178707dc19974b4595a8
[DIR]  .git\objects\ef
[FILE] .git\objects\ef\3c2ebc0609822202d9f76697dc46b1c2694253
[FILE] .git\objects\ef\b1b1cacb9bc33ecce0a5a7d299fcb1a9aa0874
[DIR]  .git\objects\f0
[FILE] .git\objects\f0\250dd5c767e0e811805b51877f6c245528dd85
[FILE] .git\objects\f0\d57bb8584c212ad77db3959d2b27f7d9b2369b
[FILE] .git\objects\f0\f521528f25561a27e00f5ac153101da92013a3
[DIR]  .git\objects\f1
[FILE] .git\objects\f1\86f74dba5fa79604f2f7cf8085e2dba34a1977
[DIR]  .git\objects\f2
[FILE] .git\objects\f2\a6fb5060abd85cd66588a0d2c99c8509412e91
[FILE] .git\objects\f2\b8541723aca5d7ff89849720d3b8ff06c95034
[DIR]  .git\objects\f3
[FILE] .git\objects\f3\c443158d8139a738f1adcc8c2607bfdef40cd4
[DIR]  .git\objects\f4
[FILE] .git\objects\f4\4d99ab5edd62ec9cbffc9924c36d1c2f6cc269
[FILE] .git\objects\f4\5e9b4cc3fe65b17deb82ae03221d1675f92cd8
[DIR]  .git\objects\f5
[FILE] .git\objects\f5\3e541c546437a71e89e7c9dbab5240d94ab95d
[FILE] .git\objects\f5\7da450cbcc8ffb5cb29dc28efc577fc3c9676e
[FILE] .git\objects\f5\fa53381f6604eb2ef2a2504a9894b80893a00c
[DIR]  .git\objects\f6
[FILE] .git\objects\f6\63b68523458dc640a0c7cbe1d371d0aa27c8ba
[FILE] .git\objects\f6\b41a5e577123a65ab1ec4da1014eb47a536ba1
[FILE] .git\objects\f6\b96dd25b7b79dd47563519e01847d3585082ba
[FILE] .git\objects\f6\ee2fcddcdaa2a82c80de31f3ad1cc6b3253c08
[FILE] .git\objects\f6\ef35a7b2566a924734fc2586cba2e042d08076
[DIR]  .git\objects\f8
[FILE] .git\objects\f8\2a600efdf6340f3e8ce3c2fb72974a5c5e2603
[FILE] .git\objects\f8\757c211a996d1892e44dffbbd90f3209a805f5
[DIR]  .git\objects\f9
[FILE] .git\objects\f9\d06b354dfb7d656abfe83d8baf2a1bdb9748db
[DIR]  .git\objects\fa
[FILE] .git\objects\fa\043db6dacb51e39408a1a8ff1e9d68c2971236
[DIR]  .git\objects\fb
[FILE] .git\objects\fb\ccd875b73df232d27a01b27e2e683c28e41961
[FILE] .git\objects\fb\f0d79adbc6ae9c659f714d5e1c1b265515f90a
[DIR]  .git\objects\fc
[FILE] .git\objects\fc\222ab1ee4ca7ea4c239d5bf866b1ef774fad7e
[FILE] .git\objects\fc\48166d1d11777bebe8b7c76642ca0e8937752a
[FILE] .git\objects\fc\65af395823c0fc4d3f38b76f618fa3eb196921
[DIR]  .git\objects\fd
[FILE] .git\objects\fd\617b9a229ccc6fe9fe8a68e733d6c8948a0736
[FILE] .git\objects\fd\789db2d945c4b64c64f6d10d2119eebaf01c1f
[FILE] .git\objects\fd\bf505446167b6dbc463e45a09b6ccefbb75cec
[FILE] .git\objects\fd\f3bf484d98e87f7927453204d5020663f72adf
[DIR]  .git\objects\fe
[FILE] .git\objects\fe\0c057721811f0d54a6ce5b2be55d3ed7e14fff
[FILE] .git\objects\fe\6ba4c549bcb22c25e2687f7ce7d3fcc5b0e289
[FILE] .git\objects\fe\b921e1bb44fe4cd60f0d6fd6c6841493c8ffb6
[DIR]  .git\objects\ff
[FILE] .git\objects\ff\bdcc8bd84b00e3b583cb645efd0fe64e9a3960
[FILE] .git\objects\ff\c08929035b3b8c0565d69a81d8d91aa0125cfc
[DIR]  .git\objects\info
[DIR]  .git\objects\pack
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.idx
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.pack
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.rev
[FILE] .git\ORIG_HEAD
[FILE] .git\packed-refs
[DIR]  .git\refs
[DIR]  .git\refs\heads
[FILE] .git\refs\heads\main
[DIR]  .git\refs\remotes
[DIR]  .git\refs\remotes\origin
[FILE] .git\refs\remotes\origin\HEAD
[FILE] .git\refs\remotes\origin\main
[DIR]  .git\refs\tags
[DIR]  .ralfai
[FILE] .ralfai\config.yaml
[FILE] .ralfai\session.md
[FILE] ARCHITECTURE.md
[FILE] PROMPTS.md
[FILE] README.md
[FILE] ROADMAP.md
[DIR]  src
[DIR]  src\nupkg
[FILE] src\nupkg\RalfAI.1.1.0.nupkg
[DIR]  src\RalfAI
[DIR]  src\RalfAI.Abstractions
[DIR]  src\RalfAI.Abstractions\bin
[DIR]  src\RalfAI.Abstractions\bin\Debug
[DIR]  src\RalfAI.Abstractions\bin\Debug\net10.0
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.deps.json
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[DIR]  src\RalfAI.Abstractions\bin\Release
[DIR]  src\RalfAI.Abstractions\bin\Release\net10.0
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.deps.json
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\IAgentContext.cs
[DIR]  src\RalfAI.Abstractions\obj
[DIR]  src\RalfAI.Abstractions\obj\Debug
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.AssemblyInfo.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.assets.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.GlobalUsings.g.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.sourcelink.json
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\ref\RalfAI.Abstractions.dll
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\refint\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\project.assets.json
[FILE] src\RalfAI.Abstractions\obj\project.nuget.cache
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.g.props
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.g.targets
[DIR]  src\RalfAI.Abstractions\obj\Release
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.AssemblyInfo.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.assets.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.GlobalUsings.g.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.sourcelink.json
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0\ref
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\ref\RalfAI.Abstractions.dll
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0\refint
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\refint\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\RalfAI.Abstractions.csproj
[DIR]  src\RalfAI.Contexts.DotNet
[DIR]  src\RalfAI.Contexts.DotNet\bin
[DIR]  src\RalfAI.Contexts.DotNet\bin\Debug
[DIR]  src\RalfAI.Contexts.DotNet\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.deps.json
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.DotNet\bin\Release
[DIR]  src\RalfAI.Contexts.DotNet\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.deps.json
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.DotNet\Contexts
[FILE] src\RalfAI.Contexts.DotNet\Contexts\DotNetContext.cs
[DIR]  src\RalfAI.Contexts.DotNet\obj
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.C.C91D3321.Up2Date
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.assets.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.sourcelink.json
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\ref\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\refint\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\project.assets.json
[FILE] src\RalfAI.Contexts.DotNet\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.C.C91D3321.Up2Date
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.assets.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.sourcelink.json
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\ref\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\refint\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\Plugins
[FILE] src\RalfAI.Contexts.DotNet\Plugins\DotNetPlugin.cs
[FILE] src\RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj
[DIR]  src\RalfAI.Contexts.HtmlApps
[DIR]  src\RalfAI.Contexts.HtmlApps\bin
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Debug
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.deps.json
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Release
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.deps.json
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.HtmlApps\Contexts
[FILE] src\RalfAI.Contexts.HtmlApps\Contexts\HtmlAppContext.cs
[DIR]  src\RalfAI.Contexts.HtmlApps\obj
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.C.6CC3700C.Up2Date
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.assets.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.sourcelink.json
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\ref\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\refint\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\project.assets.json
[FILE] src\RalfAI.Contexts.HtmlApps\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.C.6CC3700C.Up2Date
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.assets.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.sourcelink.json
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\ref\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\refint\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\Plugins
[FILE] src\RalfAI.Contexts.HtmlApps\Plugins\StaticServerPlugin.cs
[FILE] src\RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj
[DIR]  src\RalfAI.Contexts.Markdown
[DIR]  src\RalfAI.Contexts.Markdown\bin
[DIR]  src\RalfAI.Contexts.Markdown\bin\Debug
[DIR]  src\RalfAI.Contexts.Markdown\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.deps.json
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.Markdown\bin\Release
[DIR]  src\RalfAI.Contexts.Markdown\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.deps.json
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.Markdown\Contexts
[FILE] src\RalfAI.Contexts.Markdown\Contexts\MarkdownContext.cs
[DIR]  src\RalfAI.Contexts.Markdown\obj
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.C.05DC118C.Up2Date
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.assets.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.sourcelink.json
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\ref\RalfAI.Contexts.Markdown.dll
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\refint\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\project.assets.json
[FILE] src\RalfAI.Contexts.Markdown\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.C.05DC118C.Up2Date
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.assets.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.sourcelink.json
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\ref\RalfAI.Contexts.Markdown.dll
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\refint\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj
[DIR]  src\RalfAI.Core
[FILE] src\RalfAI.Core\AgentEngine.cs
[FILE] src\RalfAI.Core\AgentSessionTypes.cs
[DIR]  src\RalfAI.Core\bin
[DIR]  src\RalfAI.Core\bin\Debug
[DIR]  src\RalfAI.Core\bin\Debug\net10.0
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.deps.json
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Core\bin\Release
[DIR]  src\RalfAI.Core\bin\Release\net10.0
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.deps.json
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\ConversationLogger.cs
[DIR]  src\RalfAI.Core\ImageGeneration
[FILE] src\RalfAI.Core\ImageGeneration\ImageGenerationService.cs
[FILE] src\RalfAI.Core\MarkdownText.cs
[DIR]  src\RalfAI.Core\obj
[DIR]  src\RalfAI.Core\obj\Debug
[DIR]  src\RalfAI.Core\obj\Debug\net10.0
[FILE] src\RalfAI.Core\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.C.DE964921.Up2Date
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.AssemblyInfo.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.assets.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.GlobalUsings.g.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.sourcelink.json
[DIR]  src\RalfAI.Core\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Core\obj\Debug\net10.0\ref\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Core\obj\Debug\net10.0\refint\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\project.assets.json
[FILE] src\RalfAI.Core\obj\project.nuget.cache
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.g.props
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.g.targets
[DIR]  src\RalfAI.Core\obj\Release
[DIR]  src\RalfAI.Core\obj\Release\net10.0
[FILE] src\RalfAI.Core\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.C.DE964921.Up2Date
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.AssemblyInfo.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.assets.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.GlobalUsings.g.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.sourcelink.json
[DIR]  src\RalfAI.Core\obj\Release\net10.0\ref
[FILE] src\RalfAI.Core\obj\Release\net10.0\ref\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\obj\Release\net10.0\refint
[FILE] src\RalfAI.Core\obj\Release\net10.0\refint\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\Plugins
[FILE] src\RalfAI.Core\Plugins\AIPlugin.cs
[FILE] src\RalfAI.Core\Plugins\FileSystemPlugin.cs
[FILE] src\RalfAI.Core\Plugins\ImageGenerationPlugin.cs
[FILE] src\RalfAI.Core\Plugins\ImagePlugin.cs
[FILE] src\RalfAI.Core\Plugins\SpeechPlugin.cs
[FILE] src\RalfAI.Core\Plugins\WebPlugin.cs
[FILE] src\RalfAI.Core\RalfAI.Core.csproj
[FILE] src\RalfAI.Core\RalfAIConfig.cs
[FILE] src\RalfAI.Core\RateLimiter.cs
[DIR]  src\RalfAI.Core\Voice
[FILE] src\RalfAI.Core\Voice\SpeechService.cs
[FILE] src\RalfAI.sln
[FILE] src\RalfAI\appsettings.json
[DIR]  src\RalfAI\bin
[DIR]  src\RalfAI\bin\Debug
[DIR]  src\RalfAI\bin\Debug\net10.0
[FILE] src\RalfAI\bin\Debug\net10.0\Anthropic.dll
[FILE] src\RalfAI\bin\Debug\net10.0\appsettings.json
[FILE] src\RalfAI\bin\Debug\net10.0\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Azure.Core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Debug\net10.0\last-folder.txt
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Debug\net10.0\OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.deps.json
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.dll
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.exe
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Debug\net10.0\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Spectre.Console.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.ClientModel.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.Numerics.Tensors.dll
[DIR]  src\RalfAI\bin\Release
[DIR]  src\RalfAI\bin\Release\net10.0
[FILE] src\RalfAI\bin\Release\net10.0\Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\appsettings.json
[FILE] src\RalfAI\bin\Release\net10.0\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Azure.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Release\net10.0\OpenAI.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish
[FILE] src\RalfAI\bin\Release\net10.0\publish\Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\appsettings.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Azure.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.deps.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.exe
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Spectre.Console.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.ClientModel.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.Numerics.Tensors.dll
[FILE] src\RalfAI\bin\Release\net10.0\ralf.deps.json
[FILE] src\RalfAI\bin\Release\net10.0\ralf.dll
[FILE] src\RalfAI\bin\Release\net10.0\ralf.exe
[FILE] src\RalfAI\bin\Release\net10.0\ralf.pdb
[FILE] src\RalfAI\bin\Release\net10.0\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Release\net10.0\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Release\net10.0\Spectre.Console.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.ClientModel.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.Numerics.Tensors.dll
[FILE] src\RalfAI\config.example.json
[FILE] src\RalfAI\ContextRegistry.cs
[FILE] src\RalfAI\FileBrowser.cs
[FILE] src\RalfAI\FolderConfigStore.cs
[FILE] src\RalfAI\FullScreenConsole.cs
[FILE] src\RalfAI\InputCollector.cs
[FILE] src\RalfAI\LastFolderStore.cs
[DIR]  src\RalfAI\obj
[DIR]  src\RalfAI\obj\Debug
[DIR]  src\RalfAI\obj\Debug\net10.0
[FILE] src\RalfAI\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI\obj\Debug\net10.0\apphost.exe
[FILE] src\RalfAI\obj\Debug\net10.0\ralf.dll
[FILE] src\RalfAI\obj\Debug\net10.0\ralf.pdb
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.AssemblyInfo.cs
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.AssemblyInfoInputs.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.assets.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.AssemblyReference.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.FileListAbsolute.txt
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.Up2Date
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.genruntimeconfig.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.GlobalUsings.g.cs
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.sourcelink.json
[DIR]  src\RalfAI\obj\Debug\net10.0\ref
[FILE] src\RalfAI\obj\Debug\net10.0\ref\ralf.dll
[DIR]  src\RalfAI\obj\Debug\net10.0\refint
[FILE] src\RalfAI\obj\Debug\net10.0\refint\ralf.dll
[FILE] src\RalfAI\obj\project.assets.json
[FILE] src\RalfAI\obj\project.nuget.cache
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.dgspec.json
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.g.props
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.g.targets
[DIR]  src\RalfAI\obj\Release
[DIR]  src\RalfAI\obj\Release\net10.0
[FILE] src\RalfAI\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI\obj\Release\net10.0\apphost.exe
[FILE] src\RalfAI\obj\Release\net10.0\DotnetToolSettings.xml
[FILE] src\RalfAI\obj\Release\net10.0\PublishOutputs.64b92f381d.txt
[FILE] src\RalfAI\obj\Release\net10.0\PublishOutputs.e8d25c20c9.txt
[FILE] src\RalfAI\obj\Release\net10.0\ralf.dll
[FILE] src\RalfAI\obj\Release\net10.0\ralf.pdb
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.AssemblyInfo.cs
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.AssemblyInfoInputs.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.assets.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.AssemblyReference.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.FileListAbsolute.txt
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.Up2Date
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.genruntimeconfig.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.GlobalUsings.g.cs
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.sourcelink.json
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.toolssettingsinput.cache
[DIR]  src\RalfAI\obj\Release\net10.0\ref
[FILE] src\RalfAI\obj\Release\net10.0\ref\ralf.dll
[DIR]  src\RalfAI\obj\Release\net10.0\refint
[FILE] src\RalfAI\obj\Release\net10.0\refint\ralf.dll
[FILE] src\RalfAI\obj\Release\RalfAI.1.0.0.nuspec
[FILE] src\RalfAI\obj\Release\RalfAI.1.1.0.nuspec
[FILE] src\RalfAI\Program.cs
[DIR]  src\RalfAI\Properties
[FILE] src\RalfAI\Properties\launchSettings.json
[FILE] src\RalfAI\RalfAI.csproj
[FILE] src\RalfAI\RalfAIApp.cs
[FILE] USAGE.md

---

### TOOL — FileSystem.ReadFile — 01:13:37

args: relativePath=README.md
result: # RalfAI

An autonomous coding agent CLI. Point it at a folder, pick a context (.NET, Markdown docs,
or a static HTML/CSS/JS app), and it builds/edits files, runs `dotnet build`/`dotnet test`,
fetches URLs, previews HTML apps over local HTTP, and optionally talks to you (speech in/out)
— all from the terminal. Built on [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/),
so it can talk to Azure AI Foundry, native OpenAI, or Anthropic Claude — each through that
provider's own official .NET SDK, picked by one config setting.

For how it's built internally (project layout, the provider/plugin abstractions, the turn
loop), see [ARCHITECTURE.md](ARCHITECTURE.md).

## Install

RalfAI is a [.NET global tool](https://learn.microsoft.com/dotnet/core/tools/global-tools),
installed as the `ralf` command. From the `src/` folder of this repo:

```
dotnet pack RalfAI/RalfAI.csproj -c Release -o ./nupkg
dotnet tool install --global --add-source ./nupkg RalfAI
```

After that, `ralf` is available from any directory, in any terminal.

To pick up code changes after editing the source, reinstall:

```
dotnet pack RalfAI/RalfAI.csproj -c Release -o ./nupkg
dotnet tool update --global --add-source ./nupkg RalfAI
```

To remove it: `dotnet tool uninstall --global RalfAI`.

## Configure

### Where the config file lives

`dotnet tool update` overwrites everything the package ships, **including a bundled
`appsettings.json`** — so keeping real secrets in the file next to the binary means losing
them on every update. RalfAI resolves which config file to load in this order:

1. `--config <path>` CLI flag — wins outright.
2. `RALFAI_CONFIG_PATH` environment variable — an absolute path to a file living *outside*
   the tool's install directory, so it survives reinstalls. Set it once, persistently:

   ```powershell
   [Environment]::SetEnvironmentVariable("RALFAI_CONFIG_PATH", "D:\Configurations\RalfAI\config.json", "User")
   ```

3. `appsettings.json` next to the installed binary — the zero-config fallback. Ships with
   placeholder values only (see [config.example.json](config.example.json) for the same
   template, meant to be copied to your `RALFAI_CONFIG_PATH` location and filled in).

Whichever file loads, individual settings can still be overridden with `RALFAI_`-prefixed
environment variables (e.g. `RALFAI_AzureAIFoundry__ApiKey`).

### Choosing a provider

```json
{
    "AIProvider": "foundry",
    "AzureAIFoundry": { "Endpoint": "...", "ApiKey": "...", "ModelId": "..." },
    "OpenAI":         { "ApiKey": "...", "ModelId": "gpt-5.1" },
    "Anthropic":      { "ApiKey": "...", "ModelId": "claude-sonnet-4-5" }
}
```

`AIProvider` picks which section is actually used — `"foundry"` (default), `"openai"`, or
`"anthropic"`. Only the section for the active provider needs real values; the others can be
left as placeholders. Override per-run with `RALFAI_AIProvider=openai` without touching the
file.

**Foundry / Azure OpenAI** — `Endpoint` accepts a plain resource host
(`https://<resource>.openai.azure.com`), a Foundry project URL
(`.../api/projects/<name>` — the path is stripped automatically, only the host is used), or
a full `.../chat/completions` URL.

**Multiple models per Foundry context** — instead of one flat `ModelId`, define named models
and map contexts to them:

```json
"AzureAIFoundry": {
    "Endpoint": "...", "ApiKey": "...",
    "Models": {
        "gpt":      { "ModelId": "gpt-5.6-sol" },
        "deepseek": { "ModelId": "deepseek-r1" }
    },
    "ContextModels": { "dotnet": "gpt", "markdown": "deepseek" },
    "DefaultModel": "gpt"
}
```

**Voice (optional)** — add an `AzureSpeech` section to enable dictation (`F2`) and
read-aloud (`F3`):

```json
"AzureSpeech": {
    "Region": "your-region",
    "ApiKey": "...",
    "RecognitionLanguage": "en-US",
    "VoiceName": "en-US-JennyNeural"
}
```

**AI image generation (optional)** — add `ImageModelId` under the active provider's section
(`AzureAIFoundry` or `OpenAI`) to enable the `Image.GenerateAI` tool (logos, icons,
illustrations from a text prompt):

```json
"AzureAIFoundry": { "Endpoint": "...", "ApiKey": "...", "ModelId": "...", "ImageModelId": "dall-e-3" },
"OpenAI":         { "ApiKey": "...", "ModelId": "gpt-5.1", "ImageModelId": "gpt-image-1" }
```

Leave `ImageModelId` empty/absent to leave the tool disabled. Anthropic has no
image-generation API, so `Image.GenerateAI` is unavailable when `AIProvider` is `anthropic`
regardless of config.

## Use

```console
ralf                          # opens a folder picker (remembers the last folder)
ralf <target-folder>          # work in a specific folder
ralf <target-folder> --context html
ralf --context markdown --debug
ralf --config D:\Configurations\RalfAI\config.json
ralf --list-contexts          # show available contexts and exit
```

If no folder is given and none is remembered yet, a full-screen file browser opens
(`↑↓` navigate, `Enter` open folder, `⌫` up, `F9` use folder, `F2` rename, `F7` new folder,
`F8`/`Del` delete, `F3` view file, `F4` open file in an external editor, `F10`/`Esc` cancel).
The same browser opens on `F7` during a session (see the key table below) to switch folders.

`F4` runs the command in the `Editor` config key (`RALFAI_Editor` env override), defaulting to
`code.exe` (VS Code) if unset — set it to whatever editor/IDE you use.

Once running, type a message (paste freely, multi-line) and:

| Key | Action |
| --- | --- |
| `Ctrl+Enter` | send the message |
| `Enter` | newline (multi-line compose) |
| `Esc` | clear the current draft |
| `F1` | full-screen command reference |
| `F2` | dictate via microphone *(needs AzureSpeech)* |
| `F3` | read the last answer aloud *(needs AzureSpeech, Esc interrupts)* |
| `F5` | clear the screen |
| `F6` | switch generation context (`↑↓`/`Enter`/`Esc`) |
| `F7` | switch target folder (file browser) |
| `F8` | toggle debug messages (tool calls, rate-limit notices) |
| `F10` / `Ctrl+Q` | quit |
| `Ctrl+C` | interrupt the current turn (press twice to force-quit) |
| `Esc` / `Ctrl+X` | while the agent is working: same interrupt as `Ctrl+C` |

The whole session runs in the terminal's alternate screen buffer — your normal scrollback
is untouched and restored on exit.

## Contexts

| Key | What it builds |
| --- | --- |
| `dotnet` | .NET 10 projects via the `dotnet` CLI — builds and runs tests until green |
| `markdown` | A set of Markdown documentation files |
| `html` | Static HTML/CSS/JS apps — Tailwind via CDN, vanilla JS, one ES class per file, previewed over local HTTP |

`dotnet` and `html` both use a `src/` (code) · `ops/` (config/deployment) top-level layout
inside the target folder. Documentation is markdown files at the target folder **root** —
never a `docs/` subfolder — using the same fixed set of names as this repo:
`README.md`, `USAGE.md`, `ARCHITECTURE.md`, `ROADMAP.md` (only the ones that fit the size of
what's being built), plus `PROMPTS.md` — a running, numbered log of every objective the agent
was given for that project, appended to before any other work on a turn. Unlike the other
four, `PROMPTS.md` isn't optional past the first objective: it's what makes a project's
history traceable to the requests that drove it. The `markdown` context follows the same
convention when the objective is documenting a software project (`PROMPTS.md` always, the
other four when applicable).

The `html` context also registers a `StaticServer` tool: the agent starts a local Kestrel
server rooted at `src/` and reports the preview URL, since browsers block
`<script type="module">` imports over `file://`.

Each target folder remembers its own context (and debug setting) in
`<folder>/.ralfai/config.yaml`, so re-running `ralf <folder>` — or letting it auto-resume
with no argument — picks the same context back up. Priority when resolving: `--context`
flag → the folder's `config.yaml` → `AgentContext` in the config file → built-in default
(`markdown`).

The conversation transcript for a folder lives at `<folder>/.ralfai/session.md` and is
replayed automatically the next time you point RalfAI at that folder.

Adding a new context, provider, or tool is covered in [ARCHITECTURE.md](ARCHITECTURE.md).


---

### TOOL — FileSystem.ReadFile — 01:13:37

args: relativePath=src/RalfAI.sln
result: 
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Abstractions", "RalfAI.Abstractions\RalfAI.Abstractions.csproj", "{478ADC22-33A8-4133-A9C0-2A5BDE008A41}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Core", "RalfAI.Core\RalfAI.Core.csproj", "{23004547-5408-4005-B3B3-59EBEC3BCAD4}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Contexts.DotNet", "RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj", "{D41F9A75-2386-4230-B70C-69BEF3146A80}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Contexts.Markdown", "RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj", "{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI.Contexts.HtmlApps", "RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj", "{8958CDA5-518E-4C01-9112-DD83FA40CC76}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "RalfAI", "RalfAI\RalfAI.csproj", "{75370B56-B20C-4084-88E0-4F23568E9598}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Debug|x64 = Debug|x64
		Debug|x86 = Debug|x86
		Release|Any CPU = Release|Any CPU
		Release|x64 = Release|x64
		Release|x86 = Release|x86
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|x64.ActiveCfg = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|x64.Build.0 = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|x86.ActiveCfg = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Debug|x86.Build.0 = Debug|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|Any CPU.Build.0 = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|x64.ActiveCfg = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|x64.Build.0 = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|x86.ActiveCfg = Release|Any CPU
		{478ADC22-33A8-4133-A9C0-2A5BDE008A41}.Release|x86.Build.0 = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|x64.ActiveCfg = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|x64.Build.0 = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|x86.ActiveCfg = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Debug|x86.Build.0 = Debug|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|Any CPU.Build.0 = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|x64.ActiveCfg = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|x64.Build.0 = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|x86.ActiveCfg = Release|Any CPU
		{23004547-5408-4005-B3B3-59EBEC3BCAD4}.Release|x86.Build.0 = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|x64.ActiveCfg = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|x64.Build.0 = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|x86.ActiveCfg = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Debug|x86.Build.0 = Debug|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|Any CPU.Build.0 = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|x64.ActiveCfg = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|x64.Build.0 = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|x86.ActiveCfg = Release|Any CPU
		{D41F9A75-2386-4230-B70C-69BEF3146A80}.Release|x86.Build.0 = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|x64.ActiveCfg = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|x64.Build.0 = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|x86.ActiveCfg = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Debug|x86.Build.0 = Debug|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|Any CPU.Build.0 = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|x64.ActiveCfg = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|x64.Build.0 = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|x86.ActiveCfg = Release|Any CPU
		{13BF32A9-4AD4-4B8D-88A9-1AE84FC8DF39}.Release|x86.Build.0 = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|x64.ActiveCfg = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|x64.Build.0 = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|x86.ActiveCfg = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Debug|x86.Build.0 = Debug|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|Any CPU.Build.0 = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|x64.ActiveCfg = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|x64.Build.0 = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|x86.ActiveCfg = Release|Any CPU
		{8958CDA5-518E-4C01-9112-DD83FA40CC76}.Release|x86.Build.0 = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|x64.ActiveCfg = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|x64.Build.0 = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|x86.ActiveCfg = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Debug|x86.Build.0 = Debug|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|Any CPU.Build.0 = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|x64.ActiveCfg = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|x64.Build.0 = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|x86.ActiveCfg = Release|Any CPU
		{75370B56-B20C-4084-88E0-4F23568E9598}.Release|x86.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal


---

### TOOL — FileSystem.WriteFile — 01:13:57

args: relativePath=assets/ralfai-logo.svg, content=<svg xmlns="http://www.w3.org/2000/svg" width="960" height="320" viewBox="0 0 960 320" role="img" aria-labelledby="title desc">
  <title id="title">RalfAI logo</title>
  <desc id="desc">A gradient hexagonal mark containing a stylized R and connected AI nodes, followed by the RalfAI wordmark.</desc>
  <defs>
    <linearGradient id="brand" x1="48" y1="36" x2="272" y2="284" gradientUnits="userSpaceOnUse">
      <stop stop-color="#28C7FA"/>
      <stop offset="0.48" stop-color="#6366F1"/>
      <stop offset="1" stop-color="#A855F7"/>
    </linearGradient>
    <linearGradient id="word" x1="342" y1="98" x2="842" y2="226" gradientUnits="userSpaceOnUse">
      <stop stop-color="#E6F7FF"/>
      <stop offset="0.6" stop-color="#C7D2FE"/>
      <stop offset="1" stop-color="#D8B4FE"/>
    </linearGradient>
    <filter id="glow" x="-30%" y="-30%" width="160%" height="160%">
      <feGaussianBlur stdDeviation="8" result="blur"/>
      <feMerge><feMergeNode in="blur"/><feMergeNode in="SourceGraphic"/></feMerge>
    </filter>
  </defs>

  <!-- Transparent canvas; suitable for light and dark README themes. -->
  <path d="M160 24 278 92v136l-118 68L42 228V92z" fill="#0B1020" stroke="url(#brand)" stroke-width="8"/>
  <path d="M160 44 260 102v116l-100 58-100-58V102z" fill="none" stroke="#FFFFFF" stroke-opacity=".09" stroke-width="2"/>

  <!-- Stylized R: terminal-like stem and forward-moving leg. -->
  <path d="M100 226V92h69c44 0 69 21 69 57 0 27-14 45-39 53l44 50h-43l-53-62h-31v36zm16-105v40h50c24 0 36-6 36-20s-12-20-36-20z"
        fill="url(#brand)" filter="url(#glow)"/>

  <!-- Neural/circuit nodes, integrated into the R bowl. -->
  <g stroke="#8BE9FD" stroke-width="4" stroke-linecap="round" fill="none">
    <path d="M170 104 214 78M206 142h43M184 183l45 31"/>
  </g>
  <g fill="#F5FBFF" stroke="url(#brand)" stroke-width="4">
    <circle cx="170" cy="104" r="7"/><circle cx="214" cy="78" r="8"/>
    <circle cx="206" cy="142" r="7"/><circle cx="249" cy="142" r="8"/>
    <circle cx="184" cy="183" r="7"/><circle cx="229" cy="214" r="8"/>
  </g>

  <!-- Wordmark uses a system font stack and remains editable in the SVG source. -->
  <text x="330" y="210" fill="#172033" font-family="Inter, Segoe UI, Arial, sans-serif" font-size="142" font-weight="750" letter-spacing="-7">Ralf</text>
  <text x="642" y="210" fill="url(#brand)" font-family="Inter, Segoe UI, Arial, sans-serif" font-size="142" font-weight="800" letter-spacing="-7">AI</text>
  <circle cx="868" cy="102" r="10" fill="#28C7FA"/>
  <circle cx="895" cy="80" r="6" fill="#A855F7"/>
  <path d="M868 102 895 80" stroke="url(#brand)" stroke-width="4" stroke-linecap="round"/>
</svg>

result: OK — written assets/ralfai-logo.svg (2657 chars)

---

### TOOL — FileSystem.WriteFile — 01:13:57

args: relativePath=assets/ralfai-icon.svg, content=<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 320 320" role="img" aria-labelledby="title">
  <title id="title">RalfAI icon</title>
  <defs>
    <linearGradient id="brand" x1="48" y1="36" x2="272" y2="284" gradientUnits="userSpaceOnUse">
      <stop stop-color="#28C7FA"/><stop offset=".48" stop-color="#6366F1"/><stop offset="1" stop-color="#A855F7"/>
    </linearGradient>
    <filter id="glow" x="-30%" y="-30%" width="160%" height="160%"><feGaussianBlur stdDeviation="7" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
  </defs>
  <path d="M160 18 284 89v142l-124 71L36 231V89z" fill="#0B1020" stroke="url(#brand)" stroke-width="9"/>
  <path d="M97 230V86h74c47 0 74 23 74 61 0 29-15 48-42 57l47 54h-46l-57-67h-33v39zm17-112v43h54c26 0 39-7 39-22s-13-21-39-21z" fill="url(#brand)" filter="url(#glow)"/>
  <g stroke="#8BE9FD" stroke-width="4" stroke-linecap="round"><path d="m171 99 47-28M210 141h46m-61 46 48 34"/></g>
  <g fill="#F5FBFF" stroke="url(#brand)" stroke-width="4"><circle cx="171" cy="99" r="8"/><circle cx="218" cy="71" r="9"/><circle cx="210" cy="141" r="8"/><circle cx="256" cy="141" r="9"/><circle cx="195" cy="187" r="8"/><circle cx="243" cy="221" r="9"/></g>
</svg>

result: OK — written assets/ralfai-icon.svg (1267 chars)

---

### TOOL — FileSystem.WriteFile — 01:14:21

args: relativePath=src/LogoRasterizer/LogoRasterizer.csproj, content=<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>

result: OK — written src/LogoRasterizer/LogoRasterizer.csproj (238 chars)

---

### TOOL — FileSystem.WriteFile — 01:14:21

args: relativePath=src/LogoRasterizer/Program.cs, content=using System.IO.Compression;
using System.Text;

const int size = 512;
var pixels = new byte[size * size * 4];

static (byte R, byte G, byte B) Gradient(double t)
{
    t = Math.Clamp(t, 0, 1);
    if (t < .48)
    {
        var u = t / .48;
        return ((byte)(40 + (99 - 40) * u), (byte)(199 + (102 - 199) * u), (byte)(250 + (241 - 250) * u));
    }
    var v = (t - .48) / .52;
    return ((byte)(99 + (168 - 99) * v), (byte)(102 + (85 - 102) * v), (byte)(241 + (247 - 241) * v));
}

void Blend(int x, int y, byte r, byte g, byte b, byte a = 255)
{
    if ((uint)x >= size || (uint)y >= size) return;
    var i = (y * size + x) * 4;
    var alpha = a / 255d;
    pixels[i] = (byte)(r * alpha + pixels[i] * (1 - alpha));
    pixels[i + 1] = (byte)(g * alpha + pixels[i + 1] * (1 - alpha));
    pixels[i + 2] = (byte)(b * alpha + pixels[i + 2] * (1 - alpha));
    pixels[i + 3] = (byte)Math.Min(255, a + pixels[i + 3] * (1 - alpha));
}

bool Inside((double X, double Y)[] p, double x, double y)
{
    var inside = false;
    for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
        if ((p[i].Y > y) != (p[j].Y > y) && x < (p[j].X - p[i].X) * (y - p[i].Y) / (p[j].Y - p[i].Y) + p[i].X)
            inside = !inside;
    return inside;
}

void Polygon((double X, double Y)[] polygon, Func<int, int, (byte R, byte G, byte B, byte A)> color)
{
    for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
        if (Inside(polygon, x + .5, y + .5)) { var c = color(x, y); Blend(x, y, c.R, c.G, c.B, c.A); }
}

void Line(int x0, int y0, int x1, int y1, int width, byte r, byte g, byte b)
{
    var dx = x1 - x0; var dy = y1 - y0; var steps = Math.Max(Math.Abs(dx), Math.Abs(dy));
    for (var i = 0; i <= steps; i++)
    {
        var x = x0 + dx * i / Math.Max(1, steps); var y = y0 + dy * i / Math.Max(1, steps);
        for (var yy = -width; yy <= width; yy++) for (var xx = -width; xx <= width; xx++) if (xx * xx + yy * yy <= width * width) Blend(x + xx, y + yy, r, g, b);
    }
}

void Circle(int cx, int cy, int radius, byte r, byte g, byte b)
{
    for (var y = -radius; y <= radius; y++) for (var x = -radius; x <= radius; x++) if (x * x + y * y <= radius * radius) Blend(cx + x, cy + y, r, g, b);
}

var outer = new[] { (256d, 18d), (462d, 137d), (462d, 375d), (256d, 494d), (50d, 375d), (50d, 137d) };
var inner = new[] { (256d, 31d), (450d, 143d), (450d, 369d), (256d, 481d), (62d, 369d), (62d, 143d) };
Polygon(outer, (x, y) => { var c = Gradient((x + y) / 1024d); return (c.R, c.G, c.B, (byte)255); });
Polygon(inner, (_, _) => ((byte)11, (byte)16, (byte)32, (byte)255));

// Geometric R, deliberately independent of installed fonts.
Polygon([(145d, 370d), (145d, 137d), (179d, 137d), (179d, 370d)], (x, y) => { var c = Gradient((x + y) / 600d); return (c.R, c.G, c.B, (byte)255); });
Polygon([(170d, 137d), (286d, 137d), (330d, 154d), (353d, 190d), (353d, 227d), (331d, 261d), (289d, 276d), (178d, 276d), (178d, 239d), (280d, 239d), (303d, 230d), (315d, 211d), (303d, 192d), (280d, 183d), (170d, 183d)], (x, y) => { var c = Gradient((x + y) / 600d); return (c.R, c.G, c.B, (byte)255); });
Polygon([(254d, 259d), (303d, 250d), (384d, 379d), (331d, 379d)], (x, y) => { var c = Gradient((x + y) / 650d); return (c.R, c.G, c.B, (byte)255); });

Line(286, 154, 363, 108, 3, 139, 233, 253); Line(318, 211, 405, 211, 3, 139, 233, 253); Line(286, 266, 370, 326, 3, 139, 233, 253);
foreach (var (x, y) in new[] { (286,154), (363,108), (318,211), (405,211), (286,266), (370,326) }) { Circle(x, y, 13, 99, 102, 241); Circle(x, y, 8, 245, 251, 255); }

Directory.CreateDirectory("../../assets");
WritePng("../../assets/ralfai-icon.png", size, size, pixels);

static void WritePng(string path, int width, int height, byte[] rgba)
{
    using var file = File.Create(path);
    file.Write([137, 80, 78, 71, 13, 10, 26, 10]);
    using var header = new MemoryStream();
    WriteInt(header, width); WriteInt(header, height); header.Write([8, 6, 0, 0, 0]);
    Chunk(file, "IHDR", header.ToArray());
    using var raw = new MemoryStream();
    for (var y = 0; y < height; y++) { raw.WriteByte(0); raw.Write(rgba, y * width * 4, width * 4); }
    using var compressed = new MemoryStream();
    using (var z = new ZLibStream(compressed, CompressionLevel.SmallestSize, true)) raw.WriteTo(z);
    Chunk(file, "IDAT", compressed.ToArray()); Chunk(file, "IEND", []);
}

static void Chunk(Stream output, string type, byte[] data)
{
    WriteInt(output, data.Length); var name = Encoding.ASCII.GetBytes(type); output.Write(name); output.Write(data);
    var crcData = name.Concat(data).ToArray(); WriteInt(output, unchecked((int)Crc32(crcData)));
}
static void WriteInt(Stream s, int value) => s.Write([(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value]);
static uint Crc32(byte[] data)
{
    uint crc = 0xffffffff;
    foreach (var b in data) { crc ^= b; for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xedb88320 ^ (crc >> 1) : crc >> 1; }
    return crc ^ 0xffffffff;
}

result: OK — written src/LogoRasterizer/Program.cs (5001 chars)

---

### TOOL — DotNet.RunCommand — 01:14:26

args: workingDirectory=src/LogoRasterizer, arguments=run --framework net10.0
result: $ dotnet run --framework net10.0
Working dir: D:\Dev\2026\repos\RalfAI\src\LogoRasterizer
Exit code: 0


---

### TOOL — Image.GetInfo — 01:14:28

args: relativePath=assets/ralfai-icon.png
result: 512x512 px, format: PNG, 11693 bytes

---

### TOOL — FileSystem.WriteFile — 01:14:35

args: relativePath=src/LogoRasterizer/Program.cs, content=var path = Path.GetFullPath("../../README.md");
var markdown = File.ReadAllText(path);
const string heading = "# RalfAI\n";
const string branding = "\n<p align=\"center\">\n  <img src=\"assets/ralfai-logo.svg\" alt=\"RalfAI — autonomous coding agent\" width=\"680\">\n</p>\n\n";
if (!markdown.Contains("assets/ralfai-logo.svg", StringComparison.Ordinal))
    File.WriteAllText(path, markdown.Replace(heading, heading + branding, StringComparison.Ordinal));

result: OK — written src/LogoRasterizer/Program.cs (457 chars)

---

### TOOL — DotNet.RunCommand — 01:14:40

args: workingDirectory=src/LogoRasterizer, arguments=run --framework net10.0
result: $ dotnet run --framework net10.0
Working dir: D:\Dev\2026\repos\RalfAI\src\LogoRasterizer
Exit code: 0


---

### TOOL — FileSystem.DeleteFile — 01:14:42

args: relativePath=src/LogoRasterizer/Program.cs
result: OK — deleted src/LogoRasterizer/Program.cs

---

### TOOL — FileSystem.DeleteFile — 01:14:45

args: relativePath=src/LogoRasterizer/LogoRasterizer.csproj
result: OK — deleted src/LogoRasterizer/LogoRasterizer.csproj

---

### TOOL — DotNet.RunBuild — 01:14:52

args: projectPath=src/RalfAI.sln
result: $ dotnet build "D:\Dev\2026\repos\RalfAI\src\RalfAI.sln"
Working dir: D:\Dev\2026\repos\RalfAI\src
Exit code: 0
--- STDOUT ---
  Determining projects to restore...
  All projects are up-to-date for restore.
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Core\RalfAI.Core.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI\RalfAI.csproj]
C:\Program Files\dotnet\sdk\10.0.400-preview.0.26322.102\Sdks\Microsoft.NET.Sdk\targets\Microsoft.NET.RuntimeIdentifierInference.targets(383,5): message NETSDK1057: You are using a preview version of .NET. See: https://aka.ms/dotnet-support-policy [D:\Dev\2026\repos\RalfAI\src\RalfAI.Abstractions\RalfAI.Abstractions.csproj]
  RalfAI.Abstractions -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.dll
  RalfAI.Core -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.dll
  RalfAI.Contexts.DotNet -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
  RalfAI.Contexts.Markdown -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
  RalfAI.Contexts.HtmlApps -> D:\Dev\2026\repos\RalfAI\src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
  RalfAI -> D:\Dev\2026\repos\RalfAI\src\RalfAI\bin\Debug\net10.0\ralf.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:04.91


---

### TOOL — DotNet.RunTest — 01:14:56

args: projectPath=src/RalfAI.sln
result: $ dotnet test "D:\Dev\2026\repos\RalfAI\src\RalfAI.sln" --logger "console;verbosity=normal"
Working dir: D:\Dev\2026\repos\RalfAI\src
Exit code: 0
--- STDOUT ---
  Determining projects to restore...
  All projects are up-to-date for restore.


---

### TOOL — FileSystem.ReadFile — 01:14:58

args: relativePath=README.md
result: # RalfAI

<p align="center">
  <img src="assets/ralfai-logo.svg" alt="RalfAI — autonomous coding agent" width="680">
</p>


An autonomous coding agent CLI. Point it at a folder, pick a context (.NET, Markdown docs,
or a static HTML/CSS/JS app), and it builds/edits files, runs `dotnet build`/`dotnet test`,
fetches URLs, previews HTML apps over local HTTP, and optionally talks to you (speech in/out)
— all from the terminal. Built on [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/),
so it can talk to Azure AI Foundry, native OpenAI, or Anthropic Claude — each through that
provider's own official .NET SDK, picked by one config setting.

For how it's built internally (project layout, the provider/plugin abstractions, the turn
loop), see [ARCHITECTURE.md](ARCHITECTURE.md).

## Install

RalfAI is a [.NET global tool](https://learn.microsoft.com/dotnet/core/tools/global-tools),
installed as the `ralf` command. From the `src/` folder of this repo:

```
dotnet pack RalfAI/RalfAI.csproj -c Release -o ./nupkg
dotnet tool install --global --add-source ./nupkg RalfAI
```

After that, `ralf` is available from any directory, in any terminal.

To pick up code changes after editing the source, reinstall:

```
dotnet pack RalfAI/RalfAI.csproj -c Release -o ./nupkg
dotnet tool update --global --add-source ./nupkg RalfAI
```

To remove it: `dotnet tool uninstall --global RalfAI`.

## Configure

### Where the config file lives

`dotnet tool update` overwrites everything the package ships, **including a bundled
`appsettings.json`** — so keeping real secrets in the file next to the binary means losing
them on every update. RalfAI resolves which config file to load in this order:

1. `--config <path>` CLI flag — wins outright.
2. `RALFAI_CONFIG_PATH` environment variable — an absolute path to a file living *outside*
   the tool's install directory, so it survives reinstalls. Set it once, persistently:

   ```powershell
   [Environment]::SetEnvironmentVariable("RALFAI_CONFIG_PATH", "D:\Configurations\RalfAI\config.json", "User")
   ```

3. `appsettings.json` next to the installed binary — the zero-config fallback. Ships with
   placeholder values only (see [config.example.json](config.example.json) for the same
   template, meant to be copied to your `RALFAI_CONFIG_PATH` location and filled in).

Whichever file loads, individual settings can still be overridden with `RALFAI_`-prefixed
environment variables (e.g. `RALFAI_AzureAIFoundry__ApiKey`).

### Choosing a provider

```json
{
    "AIProvider": "foundry",
    "AzureAIFoundry": { "Endpoint": "...", "ApiKey": "...", "ModelId": "..." },
    "OpenAI":         { "ApiKey": "...", "ModelId": "gpt-5.1" },
    "Anthropic":      { "ApiKey": "...", "ModelId": "claude-sonnet-4-5" }
}
```

`AIProvider` picks which section is actually used — `"foundry"` (default), `"openai"`, or
`"anthropic"`. Only the section for the active provider needs real values; the others can be
left as placeholders. Override per-run with `RALFAI_AIProvider=openai` without touching the
file.

**Foundry / Azure OpenAI** — `Endpoint` accepts a plain resource host
(`https://<resource>.openai.azure.com`), a Foundry project URL
(`.../api/projects/<name>` — the path is stripped automatically, only the host is used), or
a full `.../chat/completions` URL.

**Multiple models per Foundry context** — instead of one flat `ModelId`, define named models
and map contexts to them:

```json
"AzureAIFoundry": {
    "Endpoint": "...", "ApiKey": "...",
    "Models": {
        "gpt":      { "ModelId": "gpt-5.6-sol" },
        "deepseek": { "ModelId": "deepseek-r1" }
    },
    "ContextModels": { "dotnet": "gpt", "markdown": "deepseek" },
    "DefaultModel": "gpt"
}
```

**Voice (optional)** — add an `AzureSpeech` section to enable dictation (`F2`) and
read-aloud (`F3`):

```json
"AzureSpeech": {
    "Region": "your-region",
    "ApiKey": "...",
    "RecognitionLanguage": "en-US",
    "VoiceName": "en-US-JennyNeural"
}
```

**AI image generation (optional)** — add `ImageModelId` under the active provider's section
(`AzureAIFoundry` or `OpenAI`) to enable the `Image.GenerateAI` tool (logos, icons,
illustrations from a text prompt):

```json
"AzureAIFoundry": { "Endpoint": "...", "ApiKey": "...", "ModelId": "...", "ImageModelId": "dall-e-3" },
"OpenAI":         { "ApiKey": "...", "ModelId": "gpt-5.1", "ImageModelId": "gpt-image-1" }
```

Leave `ImageModelId` empty/absent to leave the tool disabled. Anthropic has no
image-generation API, so `Image.GenerateAI` is unavailable when `AIProvider` is `anthropic`
regardless of config.

## Use

```console
ralf                          # opens a folder picker (remembers the last folder)
ralf <target-folder>          # work in a specific folder
ralf <target-folder> --context html
ralf --context markdown --debug
ralf --config D:\Configurations\RalfAI\config.json
ralf --list-contexts          # show available contexts and exit
```

If no folder is given and none is remembered yet, a full-screen file browser opens
(`↑↓` navigate, `Enter` open folder, `⌫` up, `F9` use folder, `F2` rename, `F7` new folder,
`F8`/`Del` delete, `F3` view file, `F4` open file in an external editor, `F10`/`Esc` cancel).
The same browser opens on `F7` during a session (see the key table below) to switch folders.

`F4` runs the command in the `Editor` config key (`RALFAI_Editor` env override), defaulting to
`code.exe` (VS Code) if unset — set it to whatever editor/IDE you use.

Once running, type a message (paste freely, multi-line) and:

| Key | Action |
| --- | --- |
| `Ctrl+Enter` | send the message |
| `Enter` | newline (multi-line compose) |
| `Esc` | clear the current draft |
| `F1` | full-screen command reference |
| `F2` | dictate via microphone *(needs AzureSpeech)* |
| `F3` | read the last answer aloud *(needs AzureSpeech, Esc interrupts)* |
| `F5` | clear the screen |
| `F6` | switch generation context (`↑↓`/`Enter`/`Esc`) |
| `F7` | switch target folder (file browser) |
| `F8` | toggle debug messages (tool calls, rate-limit notices) |
| `F10` / `Ctrl+Q` | quit |
| `Ctrl+C` | interrupt the current turn (press twice to force-quit) |
| `Esc` / `Ctrl+X` | while the agent is working: same interrupt as `Ctrl+C` |

The whole session runs in the terminal's alternate screen buffer — your normal scrollback
is untouched and restored on exit.

## Contexts

| Key | What it builds |
| --- | --- |
| `dotnet` | .NET 10 projects via the `dotnet` CLI — builds and runs tests until green |
| `markdown` | A set of Markdown documentation files |
| `html` | Static HTML/CSS/JS apps — Tailwind via CDN, vanilla JS, one ES class per file, previewed over local HTTP |

`dotnet` and `html` both use a `src/` (code) · `ops/` (config/deployment) top-level layout
inside the target folder. Documentation is markdown files at the target folder **root** —
never a `docs/` subfolder — using the same fixed set of names as this repo:
`README.md`, `USAGE.md`, `ARCHITECTURE.md`, `ROADMAP.md` (only the ones that fit the size of
what's being built), plus `PROMPTS.md` — a running, numbered log of every objective the agent
was given for that project, appended to before any other work on a turn. Unlike the other
four, `PROMPTS.md` isn't optional past the first objective: it's what makes a project's
history traceable to the requests that drove it. The `markdown` context follows the same
convention when the objective is documenting a software project (`PROMPTS.md` always, the
other four when applicable).

The `html` context also registers a `StaticServer` tool: the agent starts a local Kestrel
server rooted at `src/` and reports the preview URL, since browsers block
`<script type="module">` imports over `file://`.

Each target folder remembers its own context (and debug setting) in
`<folder>/.ralfai/config.yaml`, so re-running `ralf <folder>` — or letting it auto-resume
with no argument — picks the same context back up. Priority when resolving: `--context`
flag → the folder's `config.yaml` → `AgentContext` in the config file → built-in default
(`markdown`).

The conversation transcript for a folder lives at `<folder>/.ralfai/session.md` and is
replayed automatically the next time you point RalfAI at that folder.

Adding a new context, provider, or tool is covered in [ARCHITECTURE.md](ARCHITECTURE.md).


---

### TOOL — FileSystem.ListFiles — 01:14:58

args: subdirectory=assets
result: [FILE] assets\ralfai-icon.png
[FILE] assets\ralfai-icon.svg
[FILE] assets\ralfai-logo.svg

---

### TOOL — FileSystem.ListFiles — 01:14:58

args: subdirectory=src/LogoRasterizer
result: [DIR]  src\LogoRasterizer\bin
[DIR]  src\LogoRasterizer\bin\Debug
[DIR]  src\LogoRasterizer\bin\Debug\net10.0
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.deps.json
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.dll
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.exe
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.pdb
[FILE] src\LogoRasterizer\bin\Debug\net10.0\LogoRasterizer.runtimeconfig.json
[DIR]  src\LogoRasterizer\obj
[DIR]  src\LogoRasterizer\obj\Debug
[DIR]  src\LogoRasterizer\obj\Debug\net10.0
[FILE] src\LogoRasterizer\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\LogoRasterizer\obj\Debug\net10.0\apphost.exe
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.AssemblyInfo.cs
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.AssemblyInfoInputs.cache
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.assets.cache
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.csproj.CoreCompileInputs.cache
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.csproj.FileListAbsolute.txt
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.dll
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.genruntimeconfig.cache
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.GlobalUsings.g.cs
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.pdb
[FILE] src\LogoRasterizer\obj\Debug\net10.0\LogoRasterizer.sourcelink.json
[DIR]  src\LogoRasterizer\obj\Debug\net10.0\ref
[FILE] src\LogoRasterizer\obj\Debug\net10.0\ref\LogoRasterizer.dll
[DIR]  src\LogoRasterizer\obj\Debug\net10.0\refint
[FILE] src\LogoRasterizer\obj\Debug\net10.0\refint\LogoRasterizer.dll
[FILE] src\LogoRasterizer\obj\LogoRasterizer.csproj.nuget.dgspec.json
[FILE] src\LogoRasterizer\obj\LogoRasterizer.csproj.nuget.g.props
[FILE] src\LogoRasterizer\obj\LogoRasterizer.csproj.nuget.g.targets
[FILE] src\LogoRasterizer\obj\project.assets.json
[FILE] src\LogoRasterizer\obj\project.nuget.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.deps.json
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.deps.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.dll
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.dll

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.exe
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.exe

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.pdb
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.pdb

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.runtimeconfig.json
result: OK — deleted src/LogoRasterizer/bin/Debug/net10.0/LogoRasterizer.runtimeconfig.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.dgspec.json
result: OK — deleted src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.dgspec.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.g.props
result: OK — deleted src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.g.props

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.g.targets
result: OK — deleted src/LogoRasterizer/obj/LogoRasterizer.csproj.nuget.g.targets

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/project.assets.json
result: OK — deleted src/LogoRasterizer/obj/project.assets.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/project.nuget.cache
result: OK — deleted src/LogoRasterizer/obj/project.nuget.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/.NETCoreApp,Version=v10.0.AssemblyAttributes.cs

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/apphost.exe
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/apphost.exe

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.AssemblyInfo.cs
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.AssemblyInfo.cs

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.AssemblyInfoInputs.cache
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.AssemblyInfoInputs.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.assets.cache
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.assets.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.csproj.CoreCompileInputs.cache
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.csproj.CoreCompileInputs.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.csproj.FileListAbsolute.txt
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.csproj.FileListAbsolute.txt

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.dll
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.dll

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.GeneratedMSBuildEditorConfig.editorconfig
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.GeneratedMSBuildEditorConfig.editorconfig

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.genruntimeconfig.cache
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.genruntimeconfig.cache

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.GlobalUsings.g.cs
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.GlobalUsings.g.cs

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.pdb
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.pdb

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.sourcelink.json
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/LogoRasterizer.sourcelink.json

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/ref/LogoRasterizer.dll
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/ref/LogoRasterizer.dll

---

### TOOL — FileSystem.DeleteFile — 01:15:07

args: relativePath=src/LogoRasterizer/obj/Debug/net10.0/refint/LogoRasterizer.dll
result: OK — deleted src/LogoRasterizer/obj/Debug/net10.0/refint/LogoRasterizer.dll

---

### AGENT — 01:15:11

Creo un’identità visiva minimale che unisce la lettera **R**, un nodo neurale e i colori blu/viola associati a .NET e AI. Salverò il sorgente vettoriale nel repository, produrrò una versione PNG e la integrerò nel README senza alterare la struttura applicativa.
Logo di **RalfAI** creato e integrato.

### File aggiunti

- `assets/ralfai-logo.svg` — logo completo vettoriale
- `assets/ralfai-icon.svg` — icona vettoriale quadrata
- `assets/ralfai-icon.png` — icona PNG 512×512 con trasparenza

Il design combina:

- una **R** geometrica;
- connessioni neurali/circuitali;
- palette blu, indaco e viola;
- sfondo scuro adatto sia a temi chiari sia scuri.

Il logo completo è ora visualizzato all’inizio di `README.md`. La richiesta è stata registrata anche in `PROMPTS.md`.

### Verifiche

- `dotnet build src/RalfAI.sln`: completata, **0 errori e 0 warning**
- `dotnet test src/RalfAI.sln`: completato con exit code **0**
- PNG verificato: **512×512**, formato valido, 11.693 byte.

---

### USER — 17:16:55

Vorrei che l'editor da CLI permetta di editare la richiesta con maggior ricchezza....non posso cancellare ad esempio...e quando incollo da clipboard, perdo caratteri. usa i filesd claude per la storia dello sviluppo. E convertila in una tua storia

---

### TOOL — FileSystem.ReadFile — 17:16:59

args: relativePath=PROMPTS.md
result: # Prompts

Log cronologico e numerato delle richieste che hanno guidato la realizzazione di RalfAI — la
stessa convenzione (`PROMPTS.md` alla radice, aggiornato a ogni obiettivo, prima di qualunque
altra azione) che i context `dotnet`/`html`/`markdown` ora applicano a ogni progetto che
generano. Vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
[USAGE.md](USAGE.md#log-dei-prompt-promptsmd).

> **Nota sulla provenienza.** Questo file è stato scritto da un assistente di coding (Claude
> Code) che lavorava su questo repo via git — non da `ralf` in esecuzione su se stesso. Le voci
> 1–8 riguardano funzionalità arrivate in sessioni precedenti a questa: la git history parte da
> un "Initial Commit" già completo (vedi [ROADMAP.md](ROADMAP.md)), quindi il testo esatto di
> quelle richieste non è disponibile — sono **ricostruite** a partire dal changelog in
> `ROADMAP.md` e marcate `[ricostruito]`. Le voci dalla 9 in poi sono **verbatim**, prese da
> questa conversazione.

## 1. Realizzare un agente di coding autonomo per .NET `[ricostruito]`

> Costruisci un agente CLI autonomo che, data una cartella e un obiettivo, scaffoldi un
> progetto .NET con `dotnet new`, scriva i file sorgente, compili con `dotnet build` e testi
> con `dotnet test`, correggendo finché non vanno a buon fine.

Realizzato: `RalfAIApp` (Spectre.Console) + `DotNetContext` come unico context, motore basato
su Microsoft Agent Framework contro Azure AI Foundry.

## 2. Rendere il context non hardcoded `[ricostruito]`

> Il context .NET è cablato direttamente in `Program.cs` — rendilo selezionabile, così in
> futuro si possono aggiungere altri stack senza toccare l'app.

Realizzato: `AgentContextFactory`/`ContextRegistry`, flag `--context`, chiave `AgentContext` in
config/env, `dotnet` come default (poi diventato `markdown`).

## 3. Aggiungere Speech-to-Text / Text-to-Speech `[ricostruito]`

> Aggiungi la possibilità di dettare i messaggi a voce e farsi leggere le risposte ad alta
> voce, usando Azure AI Speech.

Realizzato: `SpeechService` (motore condiviso), comandi `/voice`/`/speak` in console,
opzionale via sezione `AzureSpeech` in config — nessun impatto sul resto dell'app se assente.

## 4. Estrarre un motore condiviso dalla console `[ricostruito]`

> La logica dell'agente è mischiata con il rendering Spectre.Console — separala in un motore
> riusabile da altri frontend futuri.

Realizzato: `AgentSession` (poi rinominato `AgentEngine`) in `RalfAI.Core`, nessuna dipendenza
UI, eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice`.

## 5. Distribuire come global tool e rendere la cartella opzionale `[ricostruito]`

> Pacchettizza RalfAI come .NET global tool (`ralf`) installabile da riga di comando, e
> permetti di avviarlo senza specificare la cartella, con un picker.

Realizzato: `PackAsTool`/`ToolCommandName=ralf`, `FileBrowser` + `LastFolderStore` per il
picker e la memoria dell'ultima cartella, nuovi context `markdown` (default) e `html`.

## 6. Sessione interattiva a schermo intero `[ricostruito]`

> Sostituisci il prompt a riga singola con un'interfaccia a schermo intero, composizione
> multi-riga e scorciatoie da tastiera invece dei comandi testuali.

Realizzato: screen buffer alternato del terminale, `Ctrl+Enter` per inviare, tasti `F1`-`F10`
(help, dettatura, lettura, pulizia schermo, cambio context/cartella, toggle debug, quit).

## 7. Supportare più provider AI `[ricostruito]`

> Non vincolare RalfAI ad Azure AI Foundry — fai in modo che possa parlare anche con OpenAI
> nativo e Anthropic Claude, ciascuno tramite il proprio SDK ufficiale.

Realizzato: `AgentEngine.BuildChatClient`, switch su `AIProvider` (`foundry`/`openai`/
`anthropic`), sezioni di config dedicate per provider.

## 8. Scrivere la documentazione del progetto `[ricostruito]`

> Scrivi README, architettura, guida all'uso e roadmap per RalfAI.

Realizzato: `README.md`, `ARCHITECTURE.md`, `USE.md` (poi rinominato `USAGE.md`, voce 11),
`ROADMAP.md`.

---

## 9. "Il file USE riporta la app desktop che non c'è"

`USE.md` e `ROADMAP.md` descrivevano `RalfAI.Desktop`/`RalfAI.ChatUI` (WinForms + WebView2 /
Blazor Hybrid) come già implementati, ma quei progetti non esistevano né nella solution né sul
filesystem. Confermato scavando nel codice (nessun riferimento reale, `RalfAI.sln` non li
include) e riscritto `USE.md` da zero contro il comportamento effettivo (global tool `ralf`,
cartella opzionale, default context `markdown`, sessione a schermo intero, `.ralfai/` invece di
`.chat/`).

## 10. "Anche la roadmap è disallineata"

Stesso problema in `ROADMAP.md`: la sezione "Interfaccia Desktop" dichiarava il frontend
grafico come fatto. Sostituita con voci che riflettono lo stato reale (global tool, file
browser, sessione a schermo intero, motore `AgentEngine`, provider multipli); l'interfaccia
grafica spostata in "📋 Pianificato" come idea aperta, non decisa.

## 11. "Puoi fare un aggiornamento ai contesti? I files md stanno sempre in radice, non in docs. In generale sempre: README USAGE ARCHITECTURE ROADMAP. Puoi aggiungere un supporto alla manipolazione di immagini jpg e png"

Due richieste in una:
- Rimosso `docs/` dal layout dei context `dotnet`/`html`: documentazione sempre alla radice
  della cartella target, con il set fisso `README.md`/`USAGE.md`/`ARCHITECTURE.md`/
  `ROADMAP.md` — rinominato anche `USE.md` → `USAGE.md` in questo repo, per coerenza.
- Nuovo `ImagePlugin` (`SixLabors.ImageSharp`): `Image.GetInfo`/`Resize`/`Crop`/`Rotate`/
  `Flip`/`Grayscale`/`ConvertFormat`, tool engine-level sempre disponibili in ogni context,
  nessuna configurazione richiesta.

## 12. "Per un progetto puoi anche generare un logo?"

Chiarito che si trattava di generazione AI (non disegno programmatico via forme/testo).
Aggiunti `ImageGenerationService` e `ImageGenerationPlugin` (`Image.GenerateAI`): genera
un'immagine da un prompt testuale via DALL-E/gpt-image su Azure AI Foundry o OpenAI, salvata
come JPG/PNG. Opzionale via `ImageModelId` sotto la sezione del provider attivo; non
disponibile con `AIProvider` `anthropic` (nessuna API di generazione immagini).

## 13. "Inoltre, quando sta generando, puoi dare un dettaglio di quello che sta pensando scrivendo generando? Come un log?"

Nessun meccanismo nuovo: la visibilità delle tool-call già esistente (descrizione nello
spinner mentre il tool gira, righe `⏺`/`⎿` con `F8`/`--debug` attivo) copre già questo caso —
la `[Description]` di `Image.GenerateAI` è stata scritta per essere informativa in quel
contesto.

## 14. "Hai aggiornato anche il comando?"

Chiarito (dopo un giro di domanda) che si riferiva ai comandi `dotnet pack`/`dotnet tool
install` in `README.md`. Verificato con un pack di prova che non serve alcuna modifica:
`SixLabors.ImageSharp` viene incluso automaticamente come dipendenza transitiva via
`RalfAI.Core`, nessuna DLL nativa coinvolta.

## 15. "Puoi aggiungere anche un file md con i PROMPTS? che in qualche modo rispecchia la realizzazione intera del progetto stesso"

Aggiunto `PROMPTS.md` come quinto file nel set canonico di documentazione — l'unico non
opzionale: ogni context (`dotnet`, `html`, `markdown`) ora apre ogni turno con un passo "Log"
che ci scrive prima di qualunque altra azione, così la storia di un progetto generato resta
tracciabile fino alle richieste che l'ha guidata. Questo stesso file applica la convenzione
a ritroso a RalfAI.

## 16. "Aggiungi la possibilità di aprire un file durante la selezione: default code.exe, altrimenti variabile nel file di config"

Nuovo tasto `F4` in `FileBrowser` (usato sia al primo avvio senza cartella sulla riga di
comando, sia con `F7` durante una sessione): apre il file selezionato in un editor esterno via
`Process.Start(UseShellExecute: true)`, fire-and-forget. Comando risolto dalla nuova chiave di
config `Editor` (`RALFAI_Editor` per l'override via env), default `code.exe` (VS Code) se non
impostata — `FileBrowser` resta senza dipendenza diretta da `IConfiguration`, il comando viene
passato dai chiamanti (`Program.cs`, `RalfAIApp.SwitchFolder`).

## 17. "Puoi aggiornare l'help con un po' tutti i comandi e verificare che tutta la documentazione sia a posto?"

Due parti:
- **Help in-app**: `F1` (interattivo) ora elenca anche `Ctrl+C`/`Esc`/`Ctrl+X` (interruzione
  turno), prima mancanti. `/help` in modalità pipe non condivide più la tabella dei tasti
  funzione (inapplicabile lì) — mostra la tabella dei comandi testuali realmente disponibili
  (`/exe`, `/cancel`, `/voice`, `/speak`, `/clear`, `/switch-context`, `/switch-folder`,
  `/toggle-debug`, `/help`, `exit`). Nel farlo, trovato e corretto un bug: `/help` in pipe
  chiamava `Console.ReadKey` su stdin rediretto, che lancia `InvalidOperationException` —
  verificato con un test end-to-end (`echo "/help\nexit" | ralf`) che prima sarebbe andato in
  crash e ora non lo fa più.
- **Audit documentazione**: verificati tutti i link tra `README.md`/`USAGE.md`/
  `ARCHITECTURE.md`/`ROADMAP.md`/`PROMPTS.md`. Corretti tre anchor rotti (due in italiano
  dentro un `README.md` inglese, uno verso una sezione mai esistita in `ARCHITECTURE.md`), un
  link relativo a un file sorgente che risaliva di una cartella di troppo, e una voce di
  ROADMAP che citava una classe (`AgentContextFactory`) ormai rinominata (`ContextRegistry`).

## 18. "Vuoi creare il logo del progetto? Una immagine?" — "procedi"

Creazione di un logo moderno e minimale per RalfAI, in formato SVG sorgente e PNG per README,
con integrazione nel repository e aggiornamento della documentazione.


---

### TOOL — FileSystem.WriteFile — 17:17:19

args: relativePath=PROMPTS.md, content=# Prompts

Log cronologico e numerato delle richieste che hanno guidato la realizzazione di RalfAI — la
stessa convenzione (`PROMPTS.md` alla radice, aggiornato a ogni obiettivo, prima di qualunque
altra azione) che i context `dotnet`/`html`/`markdown` ora applicano a ogni progetto che
generano. Vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
[USAGE.md](USAGE.md#log-dei-prompt-promptsmd).

> **Nota sulla provenienza.** Questo file è stato scritto da un assistente di coding (Claude
> Code) che lavorava su questo repo via git — non da `ralf` in esecuzione su se stesso. Le voci
> 1–8 riguardano funzionalità arrivate in sessioni precedenti a questa: la git history parte da
> un "Initial Commit" già completo (vedi [ROADMAP.md](ROADMAP.md)), quindi il testo esatto di
> quelle richieste non è disponibile — sono **ricostruite** a partire dal changelog in
> `ROADMAP.md` e marcate `[ricostruito]`. Le voci dalla 9 in poi sono **verbatim**, prese da
> questa conversazione.

## 1. Realizzare un agente di coding autonomo per .NET `[ricostruito]`

> Costruisci un agente CLI autonomo che, data una cartella e un obiettivo, scaffoldi un
> progetto .NET con `dotnet new`, scriva i file sorgente, compili con `dotnet build` e testi
> con `dotnet test`, correggendo finché non vanno a buon fine.

Realizzato: `RalfAIApp` (Spectre.Console) + `DotNetContext` come unico context, motore basato
su Microsoft Agent Framework contro Azure AI Foundry.

## 2. Rendere il context non hardcoded `[ricostruito]`

> Il context .NET è cablato direttamente in `Program.cs` — rendilo selezionabile, così in
> futuro si possono aggiungere altri stack senza toccare l'app.

Realizzato: `AgentContextFactory`/`ContextRegistry`, flag `--context`, chiave `AgentContext` in
config/env, `dotnet` come default (poi diventato `markdown`).

## 3. Aggiungere Speech-to-Text / Text-to-Speech `[ricostruito]`

> Aggiungi la possibilità di dettare i messaggi a voce e farsi leggere le risposte ad alta
> voce, usando Azure AI Speech.

Realizzato: `SpeechService` (motore condiviso), comandi `/voice`/`/speak` in console,
opzionale via sezione `AzureSpeech` in config — nessun impatto sul resto dell'app se assente.

## 4. Estrarre un motore condiviso dalla console `[ricostruito]`

> La logica dell'agente è mischiata con il rendering Spectre.Console — separala in un motore
> riusabile da altri frontend futuri.

Realizzato: `AgentSession` (poi rinominato `AgentEngine`) in `RalfAI.Core`, nessuna dipendenza
UI, eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice`.

## 5. Distribuire come global tool e rendere la cartella opzionale `[ricostruito]`

> Pacchettizza RalfAI come .NET global tool (`ralf`) installabile da riga di comando, e
> permetti di avviarlo senza specificare la cartella, con un picker.

Realizzato: `PackAsTool`/`ToolCommandName=ralf`, `FileBrowser` + `LastFolderStore` per il
picker e la memoria dell'ultima cartella, nuovi context `markdown` (default) e `html`.

## 6. Sessione interattiva a schermo intero `[ricostruito]`

> Sostituisci il prompt a riga singola con un'interfaccia a schermo intero, composizione
> multi-riga e scorciatoie da tastiera invece dei comandi testuali.

Realizzato: screen buffer alternato del terminale, `Ctrl+Enter` per inviare, tasti `F1`-`F10`
(help, dettatura, lettura, pulizia schermo, cambio context/cartella, toggle debug, quit).

## 7. Supportare più provider AI `[ricostruito]`

> Non vincolare RalfAI ad Azure AI Foundry — fai in modo che possa parlare anche con OpenAI
> nativo e Anthropic Claude, ciascuno tramite il proprio SDK ufficiale.

Realizzato: `AgentEngine.BuildChatClient`, switch su `AIProvider` (`foundry`/`openai`/
`anthropic`), sezioni di config dedicate per provider.

## 8. Scrivere la documentazione del progetto `[ricostruito]`

> Scrivi README, architettura, guida all'uso e roadmap per RalfAI.

Realizzato: `README.md`, `ARCHITECTURE.md`, `USE.md` (poi rinominato `USAGE.md`, voce 11),
`ROADMAP.md`.

---

## 9. "Il file USE riporta la app desktop che non c'è"

`USE.md` e `ROADMAP.md` descrivevano `RalfAI.Desktop`/`RalfAI.ChatUI` (WinForms + WebView2 /
Blazor Hybrid) come già implementati, ma quei progetti non esistevano né nella solution né sul
filesystem. Confermato scavando nel codice (nessun riferimento reale, `RalfAI.sln` non li
include) e riscritto `USE.md` da zero contro il comportamento effettivo (global tool `ralf`,
cartella opzionale, default context `markdown`, sessione a schermo intero, `.ralfai/` invece di
`.chat/`).

## 10. "Anche la roadmap è disallineata"

Stesso problema in `ROADMAP.md`: la sezione "Interfaccia Desktop" dichiarava il frontend
grafico come fatto. Sostituita con voci che riflettono lo stato reale (global tool, file
browser, sessione a schermo intero, motore `AgentEngine`, provider multipli); l'interfaccia
grafica spostata in "📋 Pianificato" come idea aperta, non decisa.

## 11. "Puoi fare un aggiornamento ai contesti? I files md stanno sempre in radice, non in docs. In generale sempre: README USAGE ARCHITECTURE ROADMAP. Puoi aggiungere un supporto alla manipolazione di immagini jpg e png"

Due richieste in una:
- Rimosso `docs/` dal layout dei context `dotnet`/`html`: documentazione sempre alla radice
  della cartella target, con il set fisso `README.md`/`USAGE.md`/`ARCHITECTURE.md`/
  `ROADMAP.md` — rinominato anche `USE.md` → `USAGE.md` in questo repo, per coerenza.
- Nuovo `ImagePlugin` (`SixLabors.ImageSharp`): `Image.GetInfo`/`Resize`/`Crop`/`Rotate`/
  `Flip`/`Grayscale`/`ConvertFormat`, tool engine-level sempre disponibili in ogni context,
  nessuna configurazione richiesta.

## 12. "Per un progetto puoi anche generare un logo?"

Chiarito che si trattava di generazione AI (non disegno programmatico via forme/testo).
Aggiunti `ImageGenerationService` e `ImageGenerationPlugin` (`Image.GenerateAI`): genera
un'immagine da un prompt testuale via DALL-E/gpt-image su Azure AI Foundry o OpenAI, salvata
come JPG/PNG. Opzionale via `ImageModelId` sotto la sezione del provider attivo; non
disponibile con `AIProvider` `anthropic` (nessuna API di generazione immagini).

## 13. "Inoltre, quando sta generando, puoi dare un dettaglio di quello che sta pensando scrivendo generando? Come un log?"

Nessun meccanismo nuovo: la visibilità delle tool-call già esistente (descrizione nello
spinner mentre il tool gira, righe `⏺`/`⎿` con `F8`/`--debug` attivo) copre già questo caso —
la `[Description]` di `Image.GenerateAI` è stata scritta per essere informativa in quel
contesto.

## 14. "Hai aggiornato anche il comando?"

Chiarito (dopo un giro di domanda) che si riferiva ai comandi `dotnet pack`/`dotnet tool
install` in `README.md`. Verificato con un pack di prova che non serve alcuna modifica:
`SixLabors.ImageSharp` viene incluso automaticamente come dipendenza transitiva via
`RalfAI.Core`, nessuna DLL nativa coinvolta.

## 15. "Puoi aggiungere anche un file md con i PROMPTS? che in qualche modo rispecchia la realizzazione intera del progetto stesso"

Aggiunto `PROMPTS.md` come quinto file nel set canonico di documentazione — l'unico non
opzionale: ogni context (`dotnet`, `html`, `markdown`) ora apre ogni turno con un passo "Log"
che ci scrive prima di qualunque altra azione, così la storia di un progetto generato resta
tracciabile fino alle richieste che l'ha guidata. Questo stesso file applica la convenzione
a ritroso a RalfAI.

## 16. "Aggiungi la possibilità di aprire un file durante la selezione: default code.exe, altrimenti variabile nel file di config"

Nuovo tasto `F4` in `FileBrowser` (usato sia al primo avvio senza cartella sulla riga di
comando, sia con `F7` durante una sessione): apre il file selezionato in un editor esterno via
`Process.Start(UseShellExecute: true)`, fire-and-forget. Comando risolto dalla nuova chiave di
config `Editor` (`RALFAI_Editor` per l'override via env), default `code.exe` (VS Code) se non
impostata — `FileBrowser` resta senza dipendenza diretta da `IConfiguration`, il comando viene
passato dai chiamanti (`Program.cs`, `RalfAIApp.SwitchFolder`).

## 17. "Puoi aggiornare l'help con un po' tutti i comandi e verificare che tutta la documentazione sia a posto?"

Due parti:
- **Help in-app**: `F1` (interattivo) ora elenca anche `Ctrl+C`/`Esc`/`Ctrl+X` (interruzione
  turno), prima mancanti. `/help` in modalità pipe non condivide più la tabella dei tasti
  funzione (inapplicabile lì) — mostra la tabella dei comandi testuali realmente disponibili
  (`/exe`, `/cancel`, `/voice`, `/speak`, `/clear`, `/switch-context`, `/switch-folder`,
  `/toggle-debug`, `/help`, `exit`). Nel farlo, trovato e corretto un bug: `/help` in pipe
  chiamava `Console.ReadKey` su stdin rediretto, che lancia `InvalidOperationException` —
  verificato con un test end-to-end (`echo "/help\nexit" | ralf`) che prima sarebbe andato in
  crash e ora non lo fa più.
- **Audit documentazione**: verificati tutti i link tra `README.md`/`USAGE.md`/
  `ARCHITECTURE.md`/`ROADMAP.md`/`PROMPTS.md`. Corretti tre anchor rotti (due in italiano
  dentro un `README.md` inglese, uno verso una sezione mai esistita in `ARCHITECTURE.md`), un
  link relativo a un file sorgente che risaliva di una cartella di troppo, e una voce di
  ROADMAP che citava una classe (`AgentContextFactory`) ormai rinominata (`ContextRegistry`).

## 18. "Vuoi creare il logo del progetto? Una immagine?" — "procedi"

Creazione di un logo moderno e minimale per RalfAI, in formato SVG sorgente e PNG per README,
con integrazione nel repository e aggiornamento della documentazione.

## 19. "Vorrei che l'editor da CLI permetta di editare la richiesta con maggior ricchezza....non posso cancellare ad esempio...e quando incollo da clipboard, perdo caratteri. usa i filesd claude per la storia dello sviluppo. E convertila in una tua storia"

Migliorerò l'editor interattivo della richiesta con cancellazione e incolla affidabile, quindi
userò i file di storia di Claude presenti nel repository come fonte per riscrivere la storia
dello sviluppo in una narrazione autonoma e coerente di RalfAI.

result: OK — written PROMPTS.md (10015 chars)

---

### TOOL — FileSystem.ListFiles — 17:17:21

args: subdirectory=
result: [DIR]  .git
[FILE] .git\COMMIT_EDITMSG
[FILE] .git\config
[FILE] .git\description
[FILE] .git\FETCH_HEAD
[FILE] .git\HEAD
[DIR]  .git\hooks
[FILE] .git\hooks\applypatch-msg.sample
[FILE] .git\hooks\commit-msg.sample
[FILE] .git\hooks\fsmonitor-watchman.sample
[FILE] .git\hooks\post-update.sample
[FILE] .git\hooks\pre-applypatch.sample
[FILE] .git\hooks\pre-commit.sample
[FILE] .git\hooks\pre-merge-commit.sample
[FILE] .git\hooks\pre-push.sample
[FILE] .git\hooks\pre-rebase.sample
[FILE] .git\hooks\pre-receive.sample
[FILE] .git\hooks\prepare-commit-msg.sample
[FILE] .git\hooks\push-to-checkout.sample
[FILE] .git\hooks\sendemail-validate.sample
[FILE] .git\hooks\update.sample
[FILE] .git\index
[DIR]  .git\info
[FILE] .git\info\exclude
[DIR]  .git\logs
[FILE] .git\logs\HEAD
[DIR]  .git\logs\refs
[DIR]  .git\logs\refs\heads
[FILE] .git\logs\refs\heads\main
[DIR]  .git\logs\refs\remotes
[DIR]  .git\logs\refs\remotes\origin
[FILE] .git\logs\refs\remotes\origin\HEAD
[FILE] .git\logs\refs\remotes\origin\main
[DIR]  .git\objects
[DIR]  .git\objects\00
[FILE] .git\objects\00\a7d8397992efbcf634e87d01dfc32e9cb640fa
[DIR]  .git\objects\01
[FILE] .git\objects\01\80566140c23d605344a2fcab628d4bf4d6aa6d
[FILE] .git\objects\01\942ffa38da10f0ac18c86d1a3d67c72c33d87f
[FILE] .git\objects\01\a5dd410815721833e32bcd277c899f5b49c930
[DIR]  .git\objects\02
[FILE] .git\objects\02\a585c4b871b74e9a312af3ff3d72d9658665b7
[FILE] .git\objects\02\bef14e3a66085de453961624b793edb0ffde19
[FILE] .git\objects\02\d068e02f2b3265c7fdde9feb60c97db8fcb95a
[DIR]  .git\objects\03
[FILE] .git\objects\03\2b5edf7848e97b05e55db6009220e87c2bd00b
[DIR]  .git\objects\04
[FILE] .git\objects\04\ebc45deda2180aeba4e1b4d24e666f79b5da84
[DIR]  .git\objects\05
[FILE] .git\objects\05\821e64afcaa1af395e4bdb1312fef04e942e59
[DIR]  .git\objects\06
[FILE] .git\objects\06\58406fa9786edfa226d9c327af2ae493e120d3
[FILE] .git\objects\06\790da1f900378190e8bcaeff3805b804ec9c2c
[DIR]  .git\objects\07
[FILE] .git\objects\07\08077a6404c84089e7c35fa1bb035bf25e5e4a
[FILE] .git\objects\07\dabdb1da7545c0da57fc5a31d662a3069a5e66
[DIR]  .git\objects\08
[FILE] .git\objects\08\34d8bbf0dde6093a90122d181bbb844b127f97
[FILE] .git\objects\08\392b119084760a0fad0b74ba5f3e1e026c4f0d
[FILE] .git\objects\08\9988d6485e02ab4a9a77e473f2fd585064aad7
[FILE] .git\objects\08\d2749b28e20939c707c4739f3892b4d3d041e0
[DIR]  .git\objects\09
[FILE] .git\objects\09\53a19a8510c768656dba65f7a9e7d656d5d83f
[FILE] .git\objects\09\b123e5460727bd6f9d8f7995144a52d14d26d1
[DIR]  .git\objects\0b
[FILE] .git\objects\0b\30ad4abd0066b4d8edae135043004f7e731995
[DIR]  .git\objects\0c
[FILE] .git\objects\0c\03cadeda6a20eb92b3a98d658d2117d0cda4e3
[FILE] .git\objects\0c\704be2f67452106e684ea698bb7e4172d5fe5c
[FILE] .git\objects\0c\d77bdf623d70ae8db67bce7b9bfecafb254c1c
[DIR]  .git\objects\0d
[FILE] .git\objects\0d\27cdfcc3cf83cb7046cc0cfdd621ad69b857c3
[FILE] .git\objects\0d\373fbe1452e774e2ea46eff8d049816b697074
[FILE] .git\objects\0d\4d30fc9d9dce7b3c94bf44f6d594abf497a0f6
[FILE] .git\objects\0d\986e7a4cd21e36e964149fdb723b3cbf6b7a26
[FILE] .git\objects\0d\ee5c57f8f04f4a1bef94ced84f40e938de65f2
[DIR]  .git\objects\0e
[FILE] .git\objects\0e\932f0e0215e1dfda5eba823770229ebada1e51
[FILE] .git\objects\0e\ea9296f1048410098c0df8e12f1ad4b5bd8cf8
[DIR]  .git\objects\0f
[FILE] .git\objects\0f\0d7cdd07c64a5730dadfed1ac0541cec60d635
[FILE] .git\objects\0f\5baa7894159c79273e84716c30f07930f79c58
[FILE] .git\objects\0f\5cdb3d0521f625979269ead851e30b8096ef92
[FILE] .git\objects\0f\b26103e2b0a0d95c9af40832f8e09009aae4c1
[DIR]  .git\objects\10
[FILE] .git\objects\10\238e81a702b0eff4f816a28eb076b73c8f4893
[FILE] .git\objects\10\b5885475c02543005bc748b4fa51203425ca1b
[DIR]  .git\objects\11
[FILE] .git\objects\11\599915337c158348a968b35474b08a4aa79a65
[FILE] .git\objects\11\a3845f76003de92412c9652b2283ab6f49ef1c
[FILE] .git\objects\11\c579ea7df3bc734a7824b30625a82ec1ae0e47
[DIR]  .git\objects\12
[FILE] .git\objects\12\115869f1db39f3cda988f9af7dba43555de966
[DIR]  .git\objects\13
[FILE] .git\objects\13\4dfb933b7d18a0716c5c90d3c9f7ec20aaf064
[FILE] .git\objects\13\6866bd8ac191321430fdcf9120b24b2db89f2a
[FILE] .git\objects\13\7c8609beddaa1cc3d29dac534a149e95a10118
[FILE] .git\objects\13\b1f9a7bd3ca3c486e7e50a1ccda1ca0d588333
[FILE] .git\objects\13\cb226edeb47921fbcdbb4c8fcbf656e48300e3
[DIR]  .git\objects\14
[FILE] .git\objects\14\67a1f85583da0083005f7c7ca83677930749b2
[FILE] .git\objects\14\6b1e72e88c68730d9a58ae2631d393aed7a4eb
[FILE] .git\objects\14\8edcf5bdaa2d5d62590e19063130a732586b49
[FILE] .git\objects\14\cfeb86204b3433362136bfe3236ec8135af0da
[DIR]  .git\objects\15
[FILE] .git\objects\15\6753e0d0349549d003e842e823f8c8094fdb31
[DIR]  .git\objects\16
[FILE] .git\objects\16\3022c6385aed3e7ca8bdd8f75733a0a0bdbce0
[FILE] .git\objects\16\5b5534d270dc42e95405b52b59b2da341b440e
[FILE] .git\objects\16\7f290fc8c6a79db637dd44d69fc54e20ae421b
[FILE] .git\objects\16\9274a4f2dd170b3a82b7a4d776f45786c86603
[FILE] .git\objects\16\b261e064bf20639fdc3607045cf0349aba4e3e
[FILE] .git\objects\16\f8e54b69cc70f708630f1d60b8f9e3e98a6ea7
[DIR]  .git\objects\17
[FILE] .git\objects\17\198025459795b1bc91b387141b30a5ed06877f
[FILE] .git\objects\17\5005054597f0cb131d3021565551f5f32ed2be
[DIR]  .git\objects\18
[FILE] .git\objects\18\2157b09a077b914b7dca76db46af674be45d1b
[FILE] .git\objects\18\6a6858991ac67b7d0da342fd563d35c7c556f3
[FILE] .git\objects\18\7d60c858d895a427f9f445056f01072f6454d3
[FILE] .git\objects\18\e70792bb0321e0c39f8a078b1211870cdabef0
[FILE] .git\objects\18\f95c9a9e235b64d19ea3bf9cef895cd926601a
[DIR]  .git\objects\1a
[FILE] .git\objects\1a\9d63bf85aeb75fc5d9dbe867fe2a15ef2e318f
[FILE] .git\objects\1a\f9380bbd3a1e66f2a244f34c98d5a1e4350f8e
[DIR]  .git\objects\1b
[FILE] .git\objects\1b\0de61675f59770f61db5b373ede861a732cc98
[FILE] .git\objects\1b\fb1b062aeffd3689332e939f47ae076aee9b07
[DIR]  .git\objects\1c
[FILE] .git\objects\1c\42be3c5b7b9a2da299ac5eef359ac3b9859242
[DIR]  .git\objects\1d
[FILE] .git\objects\1d\76485b2a81b434618c887004263b9a46fa157b
[FILE] .git\objects\1d\ae90b1e62f3e4bfd7b65fe9dce62b6684e7960
[DIR]  .git\objects\1e
[FILE] .git\objects\1e\3fb80b726dbac3436a9888d7db7eb33af39ce5
[FILE] .git\objects\1e\75105604345605533f0fc69931407c0845709e
[FILE] .git\objects\1e\b813dc19f03d213e6edd8b290f6ca5445050b2
[DIR]  .git\objects\1f
[FILE] .git\objects\1f\0c355efe596d4e42c9066baa0c663f02a81a02
[FILE] .git\objects\1f\1fc91ab9c629a1d214cf9b8e7902049f47994e
[FILE] .git\objects\1f\82976bdbb87a10e15b6a8edf8a36f79e83c2e2
[FILE] .git\objects\1f\9793de34fd3cb5b00500570f62f605e91e5292
[FILE] .git\objects\1f\b89abb746840452384cde74671ea59fece776f
[DIR]  .git\objects\20
[FILE] .git\objects\20\f2afeb1886a50a3d4e9b1379fb1403520f98c2
[DIR]  .git\objects\22
[FILE] .git\objects\22\2540b4bbbb488faa6db6b644ed014fea941ab0
[FILE] .git\objects\22\cb07afc08d1299f801472087ddd196aa897b28
[FILE] .git\objects\22\d2e0cbe64d23383be6c06efbc2e395cd693dc5
[FILE] .git\objects\22\e8a96c381182a885a33a94cd7f87f50794a6a4
[FILE] .git\objects\22\f86af44d08d1c3d46fcf2c5992437431d26ff0
[FILE] .git\objects\22\f91db60c89bb484f529d60a8cc9b7659b86741
[DIR]  .git\objects\23
[FILE] .git\objects\23\2bd09953b410a6b7f7bc1a52d1b8182304a7b0
[DIR]  .git\objects\24
[FILE] .git\objects\24\7ed197c5d1967149c76428cad33b3d65f74f04
[FILE] .git\objects\24\d98e0d6f4bb63adb2a77d9b3cfdf67354be250
[DIR]  .git\objects\25
[FILE] .git\objects\25\1e9d59a57ddd701fe2b8222442a09593f2f775
[FILE] .git\objects\25\3db945de6e308fc19f88cc8942c3b7f5d6015f
[DIR]  .git\objects\26
[FILE] .git\objects\26\bf58f6dc890d272f48cd7d80fadb7caa8ae624
[FILE] .git\objects\26\f4a101e39bcea6423878467125349122de49f5
[FILE] .git\objects\26\f8e3c999f4786ff0e9b036d283effd19bebcd5
[DIR]  .git\objects\27
[FILE] .git\objects\27\4251ae2c3a8777db0c448a4b1a288824fee425
[FILE] .git\objects\27\ba88bdc95ff1071530b1ede2dccd8a548c515b
[FILE] .git\objects\27\e4551a00c9787e269e724c4024954f9ac3694f
[DIR]  .git\objects\28
[FILE] .git\objects\28\3fe3cbcc311e83d16b495f20712a8e5686829f
[FILE] .git\objects\28\bae31747cfc6ce2912fe9144b1cc8550a82f20
[FILE] .git\objects\28\c2f0d9e9a8a3ddd95e15cd44f6ebfb414428c2
[FILE] .git\objects\28\e66a3eb40d6279273a19b84e4ae01774b0eee3
[DIR]  .git\objects\29
[FILE] .git\objects\29\af417cd291e85c2ab13f264976863d24cdfff3
[DIR]  .git\objects\2a
[FILE] .git\objects\2a\90aa5979c689cd3e179688fb3367c37d7fb031
[FILE] .git\objects\2a\bf9ec5aad296151f24c2c9ef16f1b6202ae080
[DIR]  .git\objects\2b
[FILE] .git\objects\2b\9fb215b4fb01e5cb24b8a3b4f590c9049f5125
[DIR]  .git\objects\2c
[FILE] .git\objects\2c\5d8d922b7ce21a9d4f5de0df2919d46c44b5ef
[FILE] .git\objects\2c\f9c59de9db56d4a25b089d8c62dfffca9f63b0
[DIR]  .git\objects\2d
[FILE] .git\objects\2d\8618fdeed507655a5d3f8519ade5aa06467e25
[DIR]  .git\objects\2e
[FILE] .git\objects\2e\675594aa5b92a642ad8e155ca6579264e493e0
[FILE] .git\objects\2e\e7fa0be115cbb44b30733d86dfef40941b8ac5
[DIR]  .git\objects\2f
[FILE] .git\objects\2f\67189ea5b622853a9cebfbff760f14a85cb3f3
[FILE] .git\objects\2f\ae0065177d04b41ae29a2dbc32c78177e5cc49
[FILE] .git\objects\2f\fdc45b237ddc01c9fc10fddc4e70adc22e3c7b
[DIR]  .git\objects\30
[FILE] .git\objects\30\43f180a535a4cf065642550f5f1025fa52999e
[FILE] .git\objects\30\971f3aa8abd5ccd70cf192e3b48e6fde8466ef
[FILE] .git\objects\30\a4fc8b14dcf252e193eddfc33a79caabd24e56
[FILE] .git\objects\30\cfb60691739379e8b7cf42ecac373db6d37f77
[FILE] .git\objects\30\e985f6d60235d09fc622eff9ad378f1932d2a8
[DIR]  .git\objects\31
[FILE] .git\objects\31\b505b021b2a071d4e8bb920633f52be518851e
[FILE] .git\objects\31\d922ecfa392cbe808d4beadb88100a8dda461b
[DIR]  .git\objects\32
[FILE] .git\objects\32\60a0471d27fee22d76c2689e371033e60e7cd0
[FILE] .git\objects\32\6c2077a8038f1004b283aee2c02e8e18f3b9a6
[FILE] .git\objects\32\fc42eb147fc1129bb394913c5211701c25bde8
[DIR]  .git\objects\33
[FILE] .git\objects\33\8c45be2120a2b3032012014bf6a5f6808343a8
[FILE] .git\objects\33\cd5128e75576c930dd0ef9e72685893f64a609
[FILE] .git\objects\33\f7ce667c90f5d38588114abb4d2aef30b66863
[DIR]  .git\objects\34
[FILE] .git\objects\34\01d6a6bdf3110055994a8c9f93aa693572b9bf
[FILE] .git\objects\34\0bb4c52cd171baa59caf6719cebe5b08c2ca39
[FILE] .git\objects\34\a58cdefd2d328a60b9fd87f2c301fe61b5e0e0
[FILE] .git\objects\34\c53b34062aeeb7a773c2e615ff383002b8e200
[DIR]  .git\objects\35
[FILE] .git\objects\35\5a1b4e12e77c34407d1889e5bb2c66b1667498
[DIR]  .git\objects\36
[FILE] .git\objects\36\05010644bc9326299e4da3637f8f9d0714fc98
[FILE] .git\objects\36\4f3cabe4d693e529976056eb004460f4ef3485
[FILE] .git\objects\36\72dcc8b5d2566709ff2b6a409f0f7b280101b5
[FILE] .git\objects\36\b2d26b0907f840ef6ac8572dc69965d7ad7fc9
[FILE] .git\objects\36\ffd2d3bf0d96126c8254931565ce97b0ed020a
[DIR]  .git\objects\37
[FILE] .git\objects\37\220554acc6a5a9bee42fc12fc1d0256cd2661d
[FILE] .git\objects\37\7fd5516fc91ec4d873573e58ab70b43a78ff18
[FILE] .git\objects\37\8dcc567acde97e523e79440b221d1a43d4e87c
[FILE] .git\objects\37\d51debeffa884d2a23dbcfbfb65b7001c7cc41
[DIR]  .git\objects\38
[FILE] .git\objects\38\bf348ad9325ca9c7d12f31896591a5b44b38f1
[DIR]  .git\objects\39
[FILE] .git\objects\39\3a38d280f4b1f0a623c3d8530273dc24da9413
[FILE] .git\objects\39\b6944d44c8d28f5ba0cecc906015fef28a5d15
[DIR]  .git\objects\3a
[FILE] .git\objects\3a\71cf76a49f452f599d6608df89d571892c5634
[FILE] .git\objects\3a\efd93eeb5755e32c71bec5a23ba3f7d8760803
[DIR]  .git\objects\3b
[FILE] .git\objects\3b\57d4514dea568a0ddb5c802b5a191f86bca1b0
[FILE] .git\objects\3b\87fe9db54a3f00c788eb185a46f828a0965693
[DIR]  .git\objects\3c
[FILE] .git\objects\3c\07e93df58fb3d5021662c6bd827a5539f180b9
[FILE] .git\objects\3c\40ffe699c4101a1d3585d28d62c34f21e5166c
[FILE] .git\objects\3c\69dc9b2cde1b40f6c00b2947fc37d38f03adea
[FILE] .git\objects\3c\ada970db36aec96cb21fad556bfaf104d6345e
[FILE] .git\objects\3c\e9bce60120746a9538106c31889d5459ae70e4
[DIR]  .git\objects\3d
[FILE] .git\objects\3d\08fb0dba2593dca8588551bd1dc48aa6c221ef
[FILE] .git\objects\3d\40c879cf3a82a731027f9acc7ff70800b59eeb
[FILE] .git\objects\3d\680f33317a5fe62b5824e9507ea2f3c9a714fb
[FILE] .git\objects\3d\e766d8d260eb2fd3adedcc8b0f322ea8b30251
[DIR]  .git\objects\3e
[FILE] .git\objects\3e\3c0a2796b6b5b84dde124ebfeabd59463f16fc
[DIR]  .git\objects\3f
[FILE] .git\objects\3f\676188793036df11273344c8e3fceca078932a
[FILE] .git\objects\3f\beec46a0cf9b09a9a4de1f62bb266529295a89
[FILE] .git\objects\3f\daba1a99e559f0d205a0d29a7e3884d20bcb1a
[DIR]  .git\objects\40
[FILE] .git\objects\40\23579cb4208ea9c5af78b353b7b9df6cf83052
[DIR]  .git\objects\41
[FILE] .git\objects\41\15de6fc0d301c8222db568d5528b6cad8c5480
[FILE] .git\objects\41\723efbaa1ab3dfeab1d1acb91c3e6e9c187ef7
[FILE] .git\objects\41\749091d74daab748a11dae42cf1b607fda49a2
[DIR]  .git\objects\42
[FILE] .git\objects\42\40b6c5f719dc9eda7e4c061a18543a35f6c36e
[FILE] .git\objects\42\894fd3c2ef18a66c7af6450952c53730031af6
[FILE] .git\objects\42\e634a3ddb3e5b3f91970ac56dc256309d1e418
[DIR]  .git\objects\43
[FILE] .git\objects\43\be6fa89388b41754541a1c1b11a4e9b4232c85
[DIR]  .git\objects\44
[FILE] .git\objects\44\234c573e9f43e9467d36364ffe3052ed36f01f
[FILE] .git\objects\44\79e146efceb629c425c336566e1bcff7d8b332
[FILE] .git\objects\44\abcc7f4ea9c77c4173e81c946a630fbf309033
[FILE] .git\objects\44\ba230c1740b2487b27d2b61bb7b806a6e69e71
[FILE] .git\objects\44\d5121ab4e4c76a3f3417201d091c6d358edb36
[DIR]  .git\objects\45
[FILE] .git\objects\45\95019f9cc3a874d83ff8428ab35c0defc8b103
[DIR]  .git\objects\46
[FILE] .git\objects\46\0a1d25545ed3b46aa0fd53e5bde16fb3586426
[FILE] .git\objects\46\36b2ed6fd632e9f8ddd349908849e785df3a15
[FILE] .git\objects\46\5debbeb7c787ed193b061abcbfc5a1bc916cd9
[DIR]  .git\objects\47
[FILE] .git\objects\47\0cdf185256451343b1c565cce5d9537e1fe872
[FILE] .git\objects\47\284a188136294398dd58989bddf3b6e41803df
[FILE] .git\objects\47\b7785194fb7bc9e31f79f6b11da6eb66c0bd74
[FILE] .git\objects\47\c2c9e14cd6272d8f1a354650f39951ca50c018
[DIR]  .git\objects\48
[FILE] .git\objects\48\7f984c2ee2e9fe1e9c54bfd72d4d9697e6be53
[FILE] .git\objects\48\aa9d56c6c89d397fcebf57d36e7a7bd0cb8fd5
[DIR]  .git\objects\4a
[FILE] .git\objects\4a\1421531720342d8213eb260ddcc5f754295a59
[FILE] .git\objects\4a\1be9976979df281dca648eca975c85cdfc2689
[FILE] .git\objects\4a\9a364ca2c000a670182ed29d7de2a79be31f01
[FILE] .git\objects\4a\9e60015fe73606ec758a82541b57dd59cc539f
[FILE] .git\objects\4a\ba1a20c06067e0b9b237c29492a46970915a27
[DIR]  .git\objects\4c
[FILE] .git\objects\4c\1a65a8e1d90fa0c0c22519fc2d63a7b0c87247
[FILE] .git\objects\4c\1c28fa040c06dc26ef4620a598382f29671551
[FILE] .git\objects\4c\5138fecf8de92b51e0328ee0fe04b78eac3610
[FILE] .git\objects\4c\58d21de9cb1f2a8fe3f8eee8dae577df5163ed
[FILE] .git\objects\4c\d323be60bfb4d9eafa01c73bd6867053ed7bc2
[FILE] .git\objects\4c\fa90824e16bdfa908489ec7b323e2aee83f7c8
[DIR]  .git\objects\4d
[FILE] .git\objects\4d\96c6b91a55ce80fbb89a2adb1f765f4c16f809
[FILE] .git\objects\4d\b9d78099110fbfa84d7a695aef4e51b8b84467
[FILE] .git\objects\4d\d9260bc15728ed7679874f270203b969c48b58
[DIR]  .git\objects\4e
[FILE] .git\objects\4e\4a7d72e264887d2c380390be0cc981d6eacbb2
[FILE] .git\objects\4e\4f72308a75221ade6871636909621f4c2ba98d
[FILE] .git\objects\4e\6fd679d8f35c6085fc190a86399aa6cf8faf04
[FILE] .git\objects\4e\9f2e1a1f76c246899b0bea4d26b6947087811e
[DIR]  .git\objects\4f
[FILE] .git\objects\4f\c8df5d83f5dfdd2dd46466b4a17f63e8b9229b
[FILE] .git\objects\4f\fc0d6463f210381c727c3d7705463817d3eb69
[DIR]  .git\objects\50
[FILE] .git\objects\50\1b69a13bbaa8794288a85d963c27f8f1319222
[FILE] .git\objects\50\293a40da9bb31faddeb496e35053753e9deec1
[FILE] .git\objects\50\cd65021ae9a2f4c80e691002ff7b25f2e88da9
[DIR]  .git\objects\51
[FILE] .git\objects\51\965b937607cea44cc6b2077ad6cb20a79d261d
[FILE] .git\objects\51\fb32890165f9012781ee0e116a0e6903a89e80
[DIR]  .git\objects\52
[FILE] .git\objects\52\24789f459539d98ad60b13d37d64fd9e6b30c5
[FILE] .git\objects\52\7a960ae25bf8b8321c343e533afab66ec37f34
[FILE] .git\objects\52\f15b3152e9b92fca3da2e2bc424a909f2f889b
[DIR]  .git\objects\53
[FILE] .git\objects\53\01756c7ccd6d435ee096d29aa2795a964d9d54
[DIR]  .git\objects\54
[FILE] .git\objects\54\8739516685a87597dd7c6bf957de33a23b4eec
[FILE] .git\objects\54\c94bb1cc64ec1c6c007063958747fa5971ac4f
[DIR]  .git\objects\56
[FILE] .git\objects\56\06a404d6c495144c8f7681c6112e79737b1b91
[FILE] .git\objects\56\acebb40e71eacdbbdb65468a275e473c586d11
[DIR]  .git\objects\57
[FILE] .git\objects\57\8526194f9f96ab178e3376cf35c2e6a1208b30
[FILE] .git\objects\57\bf7b0bfd6b5525cdcfd67532e97e261df8c322
[DIR]  .git\objects\58
[FILE] .git\objects\58\310c1cadc01362dadc7bc43e1928655a695a45
[DIR]  .git\objects\59
[FILE] .git\objects\59\0e5342ad0cc7876414b95894f653cd08de12c7
[FILE] .git\objects\59\9c5a1c85f950740435d626b6731c60946e9b2c
[DIR]  .git\objects\5a
[FILE] .git\objects\5a\b9ee3774804cbc0bdc39c798f532f06d2f7c0b
[FILE] .git\objects\5a\f9a522d77a209b5039ffb9769a10e9a61f4a4e
[DIR]  .git\objects\5c
[FILE] .git\objects\5c\7b8cc1cf317d676404bcbd5ccf8fa9f500fa11
[DIR]  .git\objects\5e
[FILE] .git\objects\5e\8653ae7ced7ea1fe2bf3a75c7df5476900628f
[FILE] .git\objects\5e\913a1e5c7eca7896c8dbb425545c683dd8054c
[DIR]  .git\objects\5f
[FILE] .git\objects\5f\2d1b4b6786e8a6e3c0718681ebb3febcc83fef
[FILE] .git\objects\5f\8591e4985d0fe4f6d8fd00221dc43b4a9fe123
[FILE] .git\objects\5f\a44499c63711ab257faca59b0f33406356e291
[FILE] .git\objects\5f\c61056552c70bcf08791771601bfffd0242e5e
[FILE] .git\objects\5f\cc98f9cdb67e4952543e36f356d2f41509d9a1
[FILE] .git\objects\5f\e64834b956e31388acd1e1316c48d59b5f6db1
[DIR]  .git\objects\60
[FILE] .git\objects\60\308aacb109772e56fce0633c4358c2a45c078e
[FILE] .git\objects\60\61d68c04a579b8b5ca18e71403e4d6345135c0
[FILE] .git\objects\60\9730b3764e25297aad646485aa25fe287b53c5
[DIR]  .git\objects\61
[FILE] .git\objects\61\2683b867376413fc487d68076e043396ab7bb5
[FILE] .git\objects\61\7a4b4fd5e2c201125b6b1bd57355bb48217d85
[DIR]  .git\objects\62
[FILE] .git\objects\62\a155b738ad6718365894ce55306855acc27ce2
[DIR]  .git\objects\63
[FILE] .git\objects\63\26ee581aece13b7552a154d7dd335631868937
[FILE] .git\objects\63\5ad456fe8a2496cbb0cadb907d4d0ab72d009b
[FILE] .git\objects\63\b336e381b874228fd9006e56eda8fdf976a0bc
[FILE] .git\objects\63\b96b7577b622588c5f9c950e0bd099dc2c954a
[FILE] .git\objects\63\ca056ec607e7bdd0a0ea8984cffbe26a57ee15
[DIR]  .git\objects\64
[FILE] .git\objects\64\be4fa393eaeb1b63c9c18510738c7fd0477268
[FILE] .git\objects\64\dabcf6f51f3cb7ec904767e364122bf0c5f41a
[DIR]  .git\objects\65
[FILE] .git\objects\65\2a12b36294e164521c07e7c2881db0d9a7cb1c
[FILE] .git\objects\65\f442c10e22ff59cfbce78050bb93fc49beee9c
[DIR]  .git\objects\66
[FILE] .git\objects\66\32aa68d51700f4f6806da8ccf852542e1edd6d
[FILE] .git\objects\66\ce53bdec854c732b25f82b1e00866965303e12
[FILE] .git\objects\66\d030b4f98d4f4248521db0878202711ac9cc9d
[DIR]  .git\objects\67
[FILE] .git\objects\67\50fe27304b63403138b3cdb9d4d241504b842e
[FILE] .git\objects\67\8c628537f5140e1cdd61862bf7ba693c54d252
[DIR]  .git\objects\68
[FILE] .git\objects\68\af9727ec2775eaa4c0f7545fa91d848cb58a43
[FILE] .git\objects\68\c33ebd01c8a46648283925960be5a81ae1cfc9
[FILE] .git\objects\68\c4a8866b1181779305404066e77c99e6622bd8
[FILE] .git\objects\68\ec026d03d72e1d359a56fd4407f3cbdc0ca131
[DIR]  .git\objects\69
[FILE] .git\objects\69\4a3ef119bcbf9303cb64b73fa3468707ec3063
[FILE] .git\objects\69\dda9b82e9892f5534cdfd21557cfd0850bf436
[FILE] .git\objects\69\e8c64e3f8a2d31762e687e696e870c3a732bda
[DIR]  .git\objects\6a
[FILE] .git\objects\6a\1d3a22d23bb4376c6659e5552743d06f0c6e6b
[FILE] .git\objects\6a\7bd07b1d1c08a93d381f5f9d26acc49f741f46
[FILE] .git\objects\6a\a58f7ef955f34d2f5a1c49accc5d1b2498b408
[FILE] .git\objects\6a\a8df7b6789c566d59a28f4e9e01e2c17c7a753
[DIR]  .git\objects\6b
[FILE] .git\objects\6b\2bdd95a80a763ffab873e1140b78269e48ca84
[FILE] .git\objects\6b\727c61d7b7d37d70a2a47815d247c8bef10862
[DIR]  .git\objects\6c
[FILE] .git\objects\6c\aeafcf2089ae756066fd25cdfbd24897ad983c
[FILE] .git\objects\6c\dd7e6652b776b29c18427ad31f8a4a990cf5c9
[DIR]  .git\objects\6e
[FILE] .git\objects\6e\ff9fa076f4caa838d758d9318fb0f9a7dbea55
[DIR]  .git\objects\6f
[FILE] .git\objects\6f\47d52ef719e0d3501c71b98b7f271734a1e48f
[FILE] .git\objects\6f\be286c79f7d77cb0ea5cd21b5e9b7c810bfa20
[FILE] .git\objects\6f\cbb8541ae5b7769c8f966a7a375c881714d7ec
[DIR]  .git\objects\70
[FILE] .git\objects\70\159e5037d4e4592396a040ae8685a842442ade
[FILE] .git\objects\70\163d9e8ed44b64cd056df05c8e472ee9aa61ad
[FILE] .git\objects\70\eb267927c66118c6a6a0f5b13475f4118c056a
[FILE] .git\objects\70\f376b87427ceaf18178f6835f9de57f88f6721
[DIR]  .git\objects\71
[FILE] .git\objects\71\6e6629e117cee74de14e2c292106b9a6f4a776
[FILE] .git\objects\71\868b044fc77b2af304aa9ae98a30282758ff7e
[FILE] .git\objects\71\f0f192f4ac5df028d44ade30a2fa3a506205db
[DIR]  .git\objects\72
[FILE] .git\objects\72\9d157de50d71f3302817f38e86d21374217918
[FILE] .git\objects\72\b013699d8bbc531148a88917a37a1f987a82a6
[DIR]  .git\objects\73
[FILE] .git\objects\73\2451d9e64ab857e2197b8188da9e62424fc7f0
[FILE] .git\objects\73\5bb7d2a07f834c3bb018286469e906704d1b1c
[FILE] .git\objects\73\72f53239321b428d4571f5c5e6b34572e24192
[FILE] .git\objects\73\b16c60f7af49f0cf23d2eab49a50a330556265
[DIR]  .git\objects\74
[FILE] .git\objects\74\2c7d414fc33e83057d3590b097fc38c3135829
[FILE] .git\objects\74\682835b8d424ff1d6a13dbb7720955408bddad
[FILE] .git\objects\74\bff31f38d347bfb05ba8c518aa492e8afafd50
[FILE] .git\objects\74\c358f4697c60a836de85ec5e50fc665d01ea48
[DIR]  .git\objects\75
[FILE] .git\objects\75\b690ed6d86aba78b62b6ed1a3a173cc12bf994
[FILE] .git\objects\75\da2e46e567ed1c2b0ab7c3a8383b82860b722a
[DIR]  .git\objects\76
[FILE] .git\objects\76\706f4c62ba48a519ddea76239a005a233f4959
[FILE] .git\objects\76\ab6850adb5afbc59c050e9c3da20aa17408d49
[FILE] .git\objects\76\ce93bf408d05b902eb7b52eb4e987b16fcc4e6
[DIR]  .git\objects\77
[FILE] .git\objects\77\01b4627ed6e6f3d6de48703d8e67f9a3e0b846
[FILE] .git\objects\77\54bebf893d1ffd6e9d9628475cb7c7920ff791
[FILE] .git\objects\77\883e8ead496a8913990f070f30de1985847014
[FILE] .git\objects\77\ac64c03f699d2a2ac1fc09dff07ea810a8a470
[FILE] .git\objects\77\e1d6ccb12f358378958c76f77807d16d3da8a7
[FILE] .git\objects\77\fcf78c56104ddf3ab85cf327ffe352b1f25bd9
[DIR]  .git\objects\78
[FILE] .git\objects\78\3afa12f10c2f1924e310c9f48859bf6e851351
[FILE] .git\objects\78\4f1cc0e33e857cdbbddfe68b7bdbc3eed83f93
[FILE] .git\objects\78\617ec7dc46671c2995d987c4c23ff8f0317ad8
[FILE] .git\objects\78\7f44c4345db3fef3cd0924276f3bcc08d6dad5
[DIR]  .git\objects\7a
[FILE] .git\objects\7a\0c1679b48b660ece93f3313a861f2efa0174d7
[FILE] .git\objects\7a\a30b439398e8243e8c4a43a0af1816e7a7dda1
[FILE] .git\objects\7a\b855016d504355a4c2bfa4ec0f6543e7134867
[DIR]  .git\objects\7b
[FILE] .git\objects\7b\5ceadcffef5f4503b7eb0adf7fa7a31ea1813b
[FILE] .git\objects\7b\eeff2edc2b5d1f7783cafcd7c2ab67f55a157b
[DIR]  .git\objects\7c
[FILE] .git\objects\7c\5d531054776431bf8203a0f36f4f48a3e10f80
[FILE] .git\objects\7c\5dce5df084dbf14cc4c04bcd5a8a6301c8eba4
[FILE] .git\objects\7c\c42edd77330efee12c0e44f027eeadd3f62ec5
[FILE] .git\objects\7c\c5d6837ce16b47ab6f80ecf019acbb9be6cff2
[FILE] .git\objects\7c\e84d982be18126189f3fcc16519fd15baf02c6
[DIR]  .git\objects\7d
[FILE] .git\objects\7d\33eecb4b8f902741f9ddff5c168601aae51fe5
[FILE] .git\objects\7d\7fca63f81e72fb023a0ec57b8dfff6f93f4d81
[FILE] .git\objects\7d\ae196fa08f920de0f08fa9c114d6b32f4006e5
[FILE] .git\objects\7d\ae3507a4333c11909514125008eb6ea970876b
[FILE] .git\objects\7d\c470492d10c4af03a198b8cc580f86babe01d5
[FILE] .git\objects\7d\e3aab9ae894c626e0982a587b522fd506e9ad1
[DIR]  .git\objects\7e
[FILE] .git\objects\7e\01ecad0b51a31256bafd078c1e5478c5f547d5
[DIR]  .git\objects\7f
[FILE] .git\objects\7f\4fdf6f648202593859f09729fe7e5ccd82b662
[FILE] .git\objects\7f\ca23b88c0fd8a67c49b0c0bc80a6b384020b0f
[DIR]  .git\objects\80
[FILE] .git\objects\80\31d7aff735d3a23b2e79ffd874e33f79d8e292
[FILE] .git\objects\80\78b7405e98195d1529c0401337eb0da41fe4f3
[FILE] .git\objects\80\865953c57cc605332c035e0ef273da82e61a18
[FILE] .git\objects\80\b6e6c344b1785f6811635bb2b2605da3cd6c5d
[DIR]  .git\objects\81
[FILE] .git\objects\81\ce48f43e88d523a7b736455536823a75403341
[FILE] .git\objects\81\eb4aae1db09d07c8a7bb5252e878bb2bb951a8
[DIR]  .git\objects\82
[FILE] .git\objects\82\0610a9aaf6eb807debf384b972cb875f1d84e7
[FILE] .git\objects\82\6d061181cdb6366c98e4f51f77e6a2fab5718b
[FILE] .git\objects\82\ce2e779f5887636245e19fb2b1a911191b9688
[FILE] .git\objects\82\fb70a205592b938e40528653b233bc83563db9
[DIR]  .git\objects\83
[FILE] .git\objects\83\39f27eeae261fd51d09b3f624c6c5a2e8f5338
[FILE] .git\objects\83\84c77702572d6d879912a5d76a8184937dc1eb
[FILE] .git\objects\83\e6db736fd2d181f3ebfda0bc7b760a90ad95b6
[DIR]  .git\objects\84
[FILE] .git\objects\84\46b97321ef29d4e4644cab549914b4c4507615
[DIR]  .git\objects\85
[FILE] .git\objects\85\55ad948ae3b7889648610b3e736a3f8467c033
[DIR]  .git\objects\86
[FILE] .git\objects\86\8b33e4b665f9c82ff61f1069659cd5849ba63a
[DIR]  .git\objects\87
[FILE] .git\objects\87\1c7cd4fc1d2d5f06ef0c36ea0b815583a6c467
[FILE] .git\objects\87\2260e0328000392323d41f4a2054e5033e5b0f
[FILE] .git\objects\87\4284807367974309762eb5ccbe076283d52683
[DIR]  .git\objects\89
[FILE] .git\objects\89\2d1a4b4b8ac09552b20b27c195c095d326b868
[FILE] .git\objects\89\5508dd0e35fc92660564ba06ac7d12e0ad2149
[FILE] .git\objects\89\a678b2c09696fd316c6d95103b2d7f763595b0
[FILE] .git\objects\89\aefc7d0486519f3d5a4a969dafef2e765f6234
[DIR]  .git\objects\8a
[FILE] .git\objects\8a\505ce81ffc211d4ae0b1d695b18dd783449db1
[FILE] .git\objects\8a\865a3180f4ee60706692d92cd3bad7fa16f5ea
[FILE] .git\objects\8a\9e0edeb475ebbe24433d5e27244f7626db42ad
[FILE] .git\objects\8a\c6728e3ce412af7218559c7b9bf00c650b2c78
[FILE] .git\objects\8a\fb289410e8f5bb6b5271b20771055c6246f146
[DIR]  .git\objects\8b
[FILE] .git\objects\8b\261107bd17c6d2ed584020bcf7fecc019c5a8e
[FILE] .git\objects\8b\e5dd26ccdd5fcd0fdf468698eb01e868f9fee4
[DIR]  .git\objects\8c
[FILE] .git\objects\8c\5765e54790df5a693dd8e3c8e89fa87a5170be
[FILE] .git\objects\8c\7d5fd8ca78229a7a0084261a97490ed0e580f0
[DIR]  .git\objects\8d
[FILE] .git\objects\8d\24fb8a70162607d5fd5da7e657c3e112d0cd47
[FILE] .git\objects\8d\43a327163ec2a4ee422c67c97b5af8428e1ecb
[FILE] .git\objects\8d\95ca3a40b11cb3d5333479b7125ccf863b94ed
[DIR]  .git\objects\8e
[FILE] .git\objects\8e\456f5dcf86d521c5979de3ca368fd58aa66840
[FILE] .git\objects\8e\940e3b83dec452266b938a7606380290649bdf
[DIR]  .git\objects\8f
[FILE] .git\objects\8f\5c082fadc372d74eec664c7d0ce9d00bae09e7
[FILE] .git\objects\8f\d47c6ff23141cc2430ffb6709d1a4363de6e33
[FILE] .git\objects\8f\f6bc2a79f5e2e194ae35921fc00b0a8d97923e
[DIR]  .git\objects\90
[FILE] .git\objects\90\819b2c1f747445464f25fc51b4932403ee039c
[DIR]  .git\objects\91
[FILE] .git\objects\91\02d6008844e95e32692145fb072028e8d9790f
[FILE] .git\objects\91\05c81afdaa0c489581443bf182e29dfc3916f3
[FILE] .git\objects\91\413a8695ae40cb0ee9e0be6a81df70499d54bf
[FILE] .git\objects\91\f47b576450b3e60626e6d2ab7e3f1e9216288b
[DIR]  .git\objects\92
[FILE] .git\objects\92\5368cdb6c1be2eb6026ca0fe2c9fc7b8366111
[FILE] .git\objects\92\5b1351bcda9f1244c4634843cb5f994a6b5008
[FILE] .git\objects\92\811937edda77d453960cff1bb00c5e539c455b
[FILE] .git\objects\92\8159782b41505a325843ccb05fffefa61e6042
[FILE] .git\objects\92\a9babe89671a2d044fd70d48ce9821a953270e
[FILE] .git\objects\92\adbb9154e02650e621c16eceed5d7262bbe9d2
[FILE] .git\objects\92\b9dc15f72dd0d68315bc3d67460d646dec4616
[FILE] .git\objects\92\f54154554df3219aa32cf9c1efc6bfef96ab5b
[DIR]  .git\objects\93
[FILE] .git\objects\93\1cb30c732b4031a55d5c3dc78d470e25a6b52e
[FILE] .git\objects\93\563503eeeec84ec92d7681abf39a6318803d6d
[FILE] .git\objects\93\9a5cc7d1882dd6b6aacad26d8b74c457e621c9
[FILE] .git\objects\93\a9962c3ef98ab33d09031e3633a07a53ef0df1
[FILE] .git\objects\93\b6d9ee41eea8efb3ae1c6994296ea2d4cc7e6b
[DIR]  .git\objects\94
[FILE] .git\objects\94\07b67bd1636434f4db9a3dd4a12018d4f011ec
[FILE] .git\objects\94\32143b6e7590dbeab3d106bc31cba7fafbebf5
[FILE] .git\objects\94\54b936701286d5c0b474fff5a83c4d3d8eb842
[FILE] .git\objects\94\9f753174ce1f7cf83fe90163f84b8781c52991
[DIR]  .git\objects\95
[FILE] .git\objects\95\3de6e9bc9ef830b4be1607b4bc670b82749e83
[FILE] .git\objects\95\ba3fb97f56e76e5fc6f6e528d541ac3edfe38c
[DIR]  .git\objects\96
[FILE] .git\objects\96\184bf8b9d0578a4bf30396d3eb5fc21cb63cc8
[FILE] .git\objects\96\5a145c867b5c247665edf6aa247181f6eca3b8
[FILE] .git\objects\96\f0bddfb973f1ea737beb9a877dfc9264030edd
[DIR]  .git\objects\97
[FILE] .git\objects\97\40ce7aaa1fabba613d00daa8620e317d5fa374
[FILE] .git\objects\97\5db2b944d33d2335d5bf62cf2f6692a2e295b9
[FILE] .git\objects\97\703f6745a09a6e442f00c9d20751e532304167
[DIR]  .git\objects\98
[FILE] .git\objects\98\e4a228867845ee27064225dbc60e38994c1e46
[DIR]  .git\objects\99
[FILE] .git\objects\99\25120fc92ad5bb20cff2d7c4572df2d74016b4
[FILE] .git\objects\99\321932f86bcd4ab01eb6a010cb77edbc035b18
[FILE] .git\objects\99\4b317d30ad635f33665188fa92cd9b012448cb
[DIR]  .git\objects\9a
[FILE] .git\objects\9a\437940d0fbb731f518d000a9efef362ea481f5
[FILE] .git\objects\9a\76d34f416f9541d974f736f97b87096423286a
[FILE] .git\objects\9a\f1ccebf554e8240b9a04bb0f50d89a2201d08a
[DIR]  .git\objects\9b
[FILE] .git\objects\9b\1ec403d2c49b8ae19ee4da8d69441bddfb01d9
[FILE] .git\objects\9b\baa51cf47a1ebe9f3bc27e9e0fea81eef0d59e
[FILE] .git\objects\9b\cb354367f75ed243366b2160a9a2c09fa36d40
[DIR]  .git\objects\9c
[FILE] .git\objects\9c\0ff8d0a5d204127691fd443af761bb9a55de84
[FILE] .git\objects\9c\131c56fb3aedd3109a5e14654aebe975405fe5
[FILE] .git\objects\9c\25b0ab013b403b904dafc1bc9fce9106be68b6
[FILE] .git\objects\9c\3082c7e397abf3f803d6d9b6c3839440a1fe01
[FILE] .git\objects\9c\c8198ddd5ed1ecc27fecdd2ad2ea6ce23af540
[DIR]  .git\objects\9d
[FILE] .git\objects\9d\26fd86a0722df73af611982d0f1736f1a9a61f
[FILE] .git\objects\9d\5313495c38e2d9fe6d19bd335f7ab738aee741
[FILE] .git\objects\9d\6343a065e592ed70953a043be9d2ed28712066
[FILE] .git\objects\9d\9e906f6f381be1a72a3ff2f61950a5ca82c95e
[FILE] .git\objects\9d\aac640701812c7dfa5243d8f476df62f7ffc3c
[FILE] .git\objects\9d\db861884112a387f5866eec842b92b35e39e66
[DIR]  .git\objects\9e
[FILE] .git\objects\9e\5faa9ca1f3f7590e41180bfcb98db8e3a7ac44
[DIR]  .git\objects\9f
[FILE] .git\objects\9f\1ce3cc2050430c10ebb41d8855a0c27a8b914c
[FILE] .git\objects\9f\506ac1799a050528c89529d65cf6d640bf643e
[FILE] .git\objects\9f\512268f9760ca15d1121ad289b53e9b8791d8c
[FILE] .git\objects\9f\f51e4a658415092afbc10d5bc6b88f66e334bf
[DIR]  .git\objects\a0
[FILE] .git\objects\a0\0bbdb21a548c971494751f9adcdc41cad281a7
[FILE] .git\objects\a0\593d6e1072e6c1accb5070785d9d0c1ba7829e
[FILE] .git\objects\a0\e9089c55ba26af2742ce9896f14b02d2f3e152
[DIR]  .git\objects\a1
[FILE] .git\objects\a1\99304faca6e5bc47064f49cee3e66e323c7b81
[DIR]  .git\objects\a2
[FILE] .git\objects\a2\5758a90f0f18182fb28501ce8e1a6206ac813c
[FILE] .git\objects\a2\f3aa160cf77931d6b87019c66aed4f2882c244
[DIR]  .git\objects\a3
[FILE] .git\objects\a3\0c00aa051595b6963219709f1fe9fe44652bdc
[FILE] .git\objects\a3\504ea129a9140c177c088dfb8d585cfc04e088
[FILE] .git\objects\a3\d600fc800e6d1cfe804b789a73e636a39ad1e0
[DIR]  .git\objects\a4
[FILE] .git\objects\a4\3c79936864a4ebdf1086a9253fa86090bf1ecf
[FILE] .git\objects\a4\710cb6c67e3abb8c6f9fc12dfa38e7fb34edc0
[DIR]  .git\objects\a5
[FILE] .git\objects\a5\0b24b5e925538e00461db3ff0d0b86d14e165e
[FILE] .git\objects\a5\0f429634b49e2a10f3596ecbcddf40e5a57191
[FILE] .git\objects\a5\587ec37a123e55a17b31e2a41c3c40fe661f6c
[FILE] .git\objects\a5\e45823a449ffedc9d71c99b1865a24336d9743
[DIR]  .git\objects\a6
[FILE] .git\objects\a6\56886deb1bec6ab86d71b1265f8e2149e75486
[FILE] .git\objects\a6\6156820f2598016e728f444821d9504bfe37d4
[FILE] .git\objects\a6\a11830d8d5251695e0057abcda380d1aaaedb5
[DIR]  .git\objects\a7
[FILE] .git\objects\a7\158c3c8ed62b30cdb83cc8bac6ad193ea7fd60
[FILE] .git\objects\a7\4ab9a589bf8f82a2491e400b4b3b827c91ca17
[FILE] .git\objects\a7\5e6fa7147298b1355d872a672d45ef243e624b
[DIR]  .git\objects\a8
[FILE] .git\objects\a8\02a294d58a3496723fcd1a37d85ff564747a8c
[FILE] .git\objects\a8\5cf853eb3cb3c26a3fe1a4e0381ae314dad708
[FILE] .git\objects\a8\a9c5a4d0adac30da22cf989393728ecd08fbf7
[FILE] .git\objects\a8\ccd59563871774064c80cda3a2d80c5785bb0a
[FILE] .git\objects\a8\fce39cb35653edb0d31811b92ed8d1c3b26068
[DIR]  .git\objects\a9
[FILE] .git\objects\a9\5fae61af0fe4d13c469bb4a23bbbab6fb7da26
[FILE] .git\objects\a9\c4de4a13fdf5253e93f19dd09183dc9b7d73cc
[FILE] .git\objects\a9\c73ff020b3388c87c9b30c11fb19b930414f86
[FILE] .git\objects\a9\da897d0fb3c5766bf93a0721f378d77c160190
[FILE] .git\objects\a9\e2098db6826f3da5035f656b6f8bedb5dfd39f
[DIR]  .git\objects\aa
[FILE] .git\objects\aa\6a55920f5abe441f74a71a85474c36884da3c2
[FILE] .git\objects\aa\c99dc26eb26d7d055daf06324d2f4e66ecfa14
[DIR]  .git\objects\ab
[FILE] .git\objects\ab\e86b1a848372bcec184f40beee88748d1acef1
[DIR]  .git\objects\ac
[FILE] .git\objects\ac\1c6067b19576b731c2c806a10471d40288e001
[FILE] .git\objects\ac\1e28c32f5a392c5cb49c472356efa472f44814
[FILE] .git\objects\ac\b7e82ede446249db13e529fca9407f62c6875b
[FILE] .git\objects\ac\c506f583448735c3f52cf647c2b238ed3d33f0
[FILE] .git\objects\ac\e3cd629f0917429874f586f360f33da137b948
[DIR]  .git\objects\ad
[FILE] .git\objects\ad\8307c90c19c5c51e91d34b403908d855c27816
[DIR]  .git\objects\ae
[FILE] .git\objects\ae\5b31da17362761350545c9cd4145d9d5aa5859
[FILE] .git\objects\ae\ae0e84043cda2445f66e64c36b6a8b67fff9ae
[FILE] .git\objects\ae\c9ab0598e2e834d4b90250b84ca7da6c742da3
[DIR]  .git\objects\af
[FILE] .git\objects\af\891ec3d401fa455ca93f93971ef5c25c1393f3
[FILE] .git\objects\af\cf2523e22e85c620b8c1325ce35c9f258e611c
[FILE] .git\objects\af\d46fc834eea1cfd97f4fa3b070b4156e27d485
[DIR]  .git\objects\b0
[FILE] .git\objects\b0\02df02c12193a4626a0710fead96b15e298284
[FILE] .git\objects\b0\3cd96a0fe7f6858a23a81e614d7f748faffbad
[FILE] .git\objects\b0\f7aaa918bfdc6ca82afdd35a5c2dbfb5521a86
[DIR]  .git\objects\b1
[FILE] .git\objects\b1\bf3b70a5bf325a021609cc43a405e583bf9dd6
[DIR]  .git\objects\b2
[FILE] .git\objects\b2\72be10d649c9e01a2b2bfa97ede299b7b681f0
[DIR]  .git\objects\b3
[FILE] .git\objects\b3\ed084e1fe79bc0d52ad618fcb6e3065807341b
[FILE] .git\objects\b3\f5290a300218655da799e042a2003bdb68eeb3
[DIR]  .git\objects\b4
[FILE] .git\objects\b4\3e801d1b1e60b7a21ad88e1d6741eba23760c2
[FILE] .git\objects\b4\62721d55ca7e541cec4345e40cd7070e784b44
[DIR]  .git\objects\b5
[FILE] .git\objects\b5\5c367371b0ed5eba300e8cb88cf98e05f2874a
[DIR]  .git\objects\b6
[FILE] .git\objects\b6\7183d3590f1be0ff8e95a65030841bd55c5234
[DIR]  .git\objects\b7
[FILE] .git\objects\b7\868a6e4df3d8e0f6099e8d01a9a2e736ad76ce
[FILE] .git\objects\b7\99e7d12d700a3817b8ecff79848c4bd76d2bce
[DIR]  .git\objects\b9
[FILE] .git\objects\b9\3bc3efb8a6958315edce31e3ab03f9878d140c
[DIR]  .git\objects\ba
[FILE] .git\objects\ba\8dcbbec2afacd7d695a156de94151236d5848a
[FILE] .git\objects\ba\f2fcea27a64cb2581d0935ad676df848e2b964
[DIR]  .git\objects\bb
[FILE] .git\objects\bb\0dfa38495689ceaa78816f22383df5e9a58515
[FILE] .git\objects\bb\10d52075d2bfba2fde6b5438fedc205f7c6863
[FILE] .git\objects\bb\2ecf9f0ab026c9aff9dd7563850f96bdc3e9c7
[FILE] .git\objects\bb\f44ac3dfd6d1871785e875bccc789bdcddba46
[FILE] .git\objects\bb\fcdba73d6b720a84a41ed5c7e1be4bc8765252
[DIR]  .git\objects\bc
[FILE] .git\objects\bc\191ceab3dd6c2d6e119fcb44a4d543d29026e1
[FILE] .git\objects\bc\1ff0da981e63e7604ea3a67c2b22c129d3133c
[FILE] .git\objects\bc\29c24091b41c5f35aacacf3df25e4d19e4aeca
[DIR]  .git\objects\bd
[FILE] .git\objects\bd\7174f3a7e58bb6905569b3e03a252873102aba
[FILE] .git\objects\bd\9d786fdb832df97f540387bb2b2bdb6669cc90
[DIR]  .git\objects\be
[FILE] .git\objects\be\4e13773d8458114ba7a4171dc7873a49fd1621
[FILE] .git\objects\be\d0fb8a994f12042984fd8124170fef4d054d80
[DIR]  .git\objects\bf
[FILE] .git\objects\bf\7025e2b533e417b5a420aeeedf4ea6166b6cc6
[DIR]  .git\objects\c0
[FILE] .git\objects\c0\1a1b7572b1a0192d051b6e4469c4739426fdf1
[FILE] .git\objects\c0\e5393447776d4d3735d82b2d55269877a740de
[DIR]  .git\objects\c1
[FILE] .git\objects\c1\22fbde2e02514633788b37ee1926152a4b1b17
[FILE] .git\objects\c1\856d52fbbaaee130c61987b5a36143d1f1395c
[DIR]  .git\objects\c2
[FILE] .git\objects\c2\a70fa46caa161126cf455366bd5682421feb62
[DIR]  .git\objects\c3
[FILE] .git\objects\c3\40547dd32033a077017b886c7b73d527311558
[FILE] .git\objects\c3\56fa483fce4660ef5e7f728a63284388beaa01
[DIR]  .git\objects\c4
[FILE] .git\objects\c4\17ae0d6ee9b75f818e9f7eefa674e96ad277bd
[FILE] .git\objects\c4\f57984c85b5ce6660e771b97056a5ef4ef0ef8
[DIR]  .git\objects\c5
[FILE] .git\objects\c5\40cbc1655f8ee019d3fc07f396dc1ea0bcc1f4
[FILE] .git\objects\c5\54909a021ad54379221d20c28d8e6b028e3a6c
[DIR]  .git\objects\c6
[FILE] .git\objects\c6\13821c515d166024e3f9b8f5bc6eb3bf592cd0
[FILE] .git\objects\c6\204d3006663da9bcec0d501b0e82c45723bdc3
[FILE] .git\objects\c6\29e207a2ed2668cb41b7d8798fbac580e1bb70
[FILE] .git\objects\c6\7063b4a75c080272643bec7552ff410a717484
[DIR]  .git\objects\c7
[FILE] .git\objects\c7\578c68ce155294991e58d9828242d300819de8
[DIR]  .git\objects\c8
[FILE] .git\objects\c8\1653bf408d02a330dd92fc4ce4d91d7839486e
[FILE] .git\objects\c8\32938dfedb4540c18ac80d60a433fad195dbaf
[FILE] .git\objects\c8\5dd7964106dd34bb76a8a54c5eb4b0e0857f7d
[DIR]  .git\objects\c9
[FILE] .git\objects\c9\2d98ec380d2d369bf1e4f1932deb5385f392cc
[FILE] .git\objects\c9\d91ac018046bb861d0d1d5472d2b468fb8e716
[DIR]  .git\objects\ca
[FILE] .git\objects\ca\478348576cbd05c71e9c9f36d7fb1b46291373
[FILE] .git\objects\ca\aaf4e0c421827ade81917b506b6a28784e09c9
[DIR]  .git\objects\cb
[FILE] .git\objects\cb\07515bc83dbae35673cd524f4d4b0d2068f082
[DIR]  .git\objects\cc
[FILE] .git\objects\cc\c6018f9068625ebd596379dc03639120a3be5d
[FILE] .git\objects\cc\db7e50dd79744cce31e98a1737466abab1f9d5
[FILE] .git\objects\cc\e99126792693f1c00a180c4fa3ec56ac70bb30
[DIR]  .git\objects\cd
[FILE] .git\objects\cd\054dddf2afcaa2abfc45d0ad6b28e3aea08072
[FILE] .git\objects\cd\451aa0b1284ffffc9e04c44b88b02915e1ee0b
[FILE] .git\objects\cd\89b9ef1b0c2f530d67f42967a2d067a5ccfd39
[FILE] .git\objects\cd\e79bc1014d02330dee1f42674bb16f83a17266
[DIR]  .git\objects\ce
[FILE] .git\objects\ce\0f2c4e4e5b83571d1a2785274d0d24a219e408
[FILE] .git\objects\ce\926dcf79996b0ca99f61d51cf7e6e4127eedaf
[FILE] .git\objects\ce\e9039761174aa7295905f38447d022e71c0db4
[DIR]  .git\objects\cf
[FILE] .git\objects\cf\06a1156a1fa2bd5114f5c42d54fe397919c014
[FILE] .git\objects\cf\2e0f53ea8a0a78afa408de5145b924a35dc56d
[FILE] .git\objects\cf\7d975dc1d57a70480b21c7634a669cc8ad1da1
[FILE] .git\objects\cf\9310ab611033a77e618b644c0226a1ce64e4b1
[FILE] .git\objects\cf\eddb40630e1b8e9024ce821e0391dca734dd28
[FILE] .git\objects\cf\f06b78ada5b6873f6d845b27473ed078a5470c
[DIR]  .git\objects\d0
[FILE] .git\objects\d0\22f9d58c6b8e4cebd998ab362c713d46326e0c
[FILE] .git\objects\d0\47d5e846ce7a93f5ca2ad9c3aaa2777e50d677
[DIR]  .git\objects\d1
[FILE] .git\objects\d1\2bcbc76ed30239132cc61c564f61f652e0ad1c
[FILE] .git\objects\d1\9eda0bb192bd94ea9fe815cdd11ac5c05f0fc3
[DIR]  .git\objects\d2
[FILE] .git\objects\d2\68f9edc17e9faef627e68df45a06acabe675b3
[FILE] .git\objects\d2\8fdf3fd36014e8eb530b2d9828851dba625cd7
[DIR]  .git\objects\d3
[FILE] .git\objects\d3\9e8afcbca950bda59df7415cd551c3a8f1fb3c
[FILE] .git\objects\d3\e55e972a93ac996a54f14bff37ace48e051125
[DIR]  .git\objects\d4
[FILE] .git\objects\d4\126e70e8694234261ba5a1286425f981312a74
[FILE] .git\objects\d4\279ae9e25f355ac948b2f39df428a84bae213a
[FILE] .git\objects\d4\514e12e38d3cbc0fa1925c556d9ed1b4367bbb
[FILE] .git\objects\d4\7479aa9fca641284da79ce9d997046f5e1e9b1
[FILE] .git\objects\d4\7cf6b1dd89c7680962eddc2f45a808968a966a
[FILE] .git\objects\d4\bb549418b5b4d4cc477b980a2fa32b03be8d67
[DIR]  .git\objects\d5
[FILE] .git\objects\d5\1946fe52e29acf632b3056ad872589f4fdaaa7
[FILE] .git\objects\d5\5d053e1bb929cb881e94d09fa859890aeaa17e
[FILE] .git\objects\d5\af93855ce15e41ce2c496c30f139e569e809e9
[DIR]  .git\objects\d6
[FILE] .git\objects\d6\0348b14e382864b13bcb71dee3b59143d8dad4
[DIR]  .git\objects\d7
[FILE] .git\objects\d7\8ca14cfba0e1e14fed97509c415b6849b85195
[DIR]  .git\objects\d8
[FILE] .git\objects\d8\7b80c7059385359d653a22f33408c5bd358a7e
[FILE] .git\objects\d8\860a8130adb4139c180bba2f7fff8dcbc2018b
[FILE] .git\objects\d8\b22a1aff23ad84ef668d26361da95779cdde2f
[FILE] .git\objects\d8\d3bcd3fc90f6d559368d18e4168d8404ef649d
[DIR]  .git\objects\d9
[FILE] .git\objects\d9\0e11030c5e5248acd37839873882c497001160
[FILE] .git\objects\d9\4aa4ce768e24558e5dd2cf8db4cde0802a453e
[FILE] .git\objects\d9\a7282b99d0ee8b134c6cc4969c28441328d23a
[DIR]  .git\objects\da
[FILE] .git\objects\da\549538361ea1e8ef2e67fdc8666e763106b858
[FILE] .git\objects\da\8baae5efc694d6c72b64d42c36cec9dbae15f0
[FILE] .git\objects\da\933d9ed5c2d391647a17779f5a93e1d9586b57
[DIR]  .git\objects\db
[FILE] .git\objects\db\6b5e0f2778af02c67ceb5c458df62008d17696
[FILE] .git\objects\db\be7b7c851a217f093bde07217786c482eec4e1
[FILE] .git\objects\db\df47df81e9d27fe160a314a37d0cce22651376
[DIR]  .git\objects\dc
[FILE] .git\objects\dc\08508dcc903931a98fa82e0fbe8fe9834a7de1
[FILE] .git\objects\dc\643f62fa7d925cca042ab5be3865202f664b6a
[FILE] .git\objects\dc\bab0d6de92e3e6579e765025e13f8ca4bddc86
[FILE] .git\objects\dc\c964aded12c3e5456b74cef0b9d8ef6928d7a3
[FILE] .git\objects\dc\d3b48a5690315ea95d78d93ed19733529afe9e
[FILE] .git\objects\dc\f648ae9310fa14525bf3d4e6bae95b9ef70874
[DIR]  .git\objects\dd
[FILE] .git\objects\dd\6878bea0badef222cf11d9124439badf3d755c
[DIR]  .git\objects\de
[FILE] .git\objects\de\790a80460238c3cde8fed9f30d0d1b71e52813
[FILE] .git\objects\de\f1770c2f6f32b3f413506b3136d0c4452de4bb
[DIR]  .git\objects\df
[FILE] .git\objects\df\1d509dc2708f66ef4912f4060ef456b1934749
[FILE] .git\objects\df\4b561a331a713885853ba7021a5fa1e420bbc0
[FILE] .git\objects\df\7d3fe1add16fcbf48efd6269127af7646beef9
[FILE] .git\objects\df\ee4c09b0690ea83ae3303849cb95f89cf45363
[DIR]  .git\objects\e0
[FILE] .git\objects\e0\cfea938761a6b3c3fc556ef43a2c1206c4623c
[DIR]  .git\objects\e1
[FILE] .git\objects\e1\1ea159ac4cfb0760a2ea37c8aaa2fb0a21e02b
[DIR]  .git\objects\e2
[FILE] .git\objects\e2\09c5acac4433b37599998e357f7c9ef6004461
[FILE] .git\objects\e2\de349966956e2516d639055a7a57d607b636de
[FILE] .git\objects\e2\f70cfaf7c92061cf0945ac533425a8eb55918b
[DIR]  .git\objects\e3
[FILE] .git\objects\e3\0c8af8f67ba8d5287d738ddb126bb67fd13a85
[FILE] .git\objects\e3\1b14afa125863a19e538a123b349ff0091e792
[FILE] .git\objects\e3\2f59ac412f1e2e517ce225716c5f0a9d0a07c6
[FILE] .git\objects\e3\4164cc3dcf0170e11ff59d9a391136c56fa35a
[FILE] .git\objects\e3\5ad5196ade62038676fd9cdc20d82c74c8eab8
[FILE] .git\objects\e3\ae48903fab590507f3e986ab0d1f1cb1d60636
[DIR]  .git\objects\e4
[FILE] .git\objects\e4\87b71f11dd7d22357bffbef2dbb9f24f78be4f
[DIR]  .git\objects\e5
[FILE] .git\objects\e5\43ccb046831c9d7493e46911c9b36ccfe63493
[FILE] .git\objects\e5\5520fef08ded8975fd4cf37d1b3415e37402e0
[FILE] .git\objects\e5\5a9fe2058c210e2a087ec7b2387fe168fa3ab2
[FILE] .git\objects\e5\612663d9ec066c8d8106eae25cf11c5816afeb
[FILE] .git\objects\e5\c02512bc37fb139ef20a02b2150d1a61ee2730
[DIR]  .git\objects\e6
[FILE] .git\objects\e6\10cb8c929237860170e55148763590b50874df
[FILE] .git\objects\e6\989ec2807c0f8df1096be67696adcc6895d927
[FILE] .git\objects\e6\9de29bb2d1d6434b8b29ae775ad8c2e48c5391
[FILE] .git\objects\e6\a78a5850b38e5cb1202e8aedf1ad69be8e6cd2
[FILE] .git\objects\e6\eb7667447f003ad44a896dd7871cafa240db46
[DIR]  .git\objects\e7
[FILE] .git\objects\e7\b00585949e978fb63227d872cd4739e0de0c7e
[DIR]  .git\objects\e8
[FILE] .git\objects\e8\5a2a1be83605ef6e79e725ba4521f8d3692d23
[DIR]  .git\objects\e9
[FILE] .git\objects\e9\48d859f0bae580c9fea9d6408a23052ae7bd50
[FILE] .git\objects\e9\82c4481b1949d7617527f146abb1f1e2327ef4
[FILE] .git\objects\e9\b5722073ea1e57816d9a3471842d3a80bdd3e9
[DIR]  .git\objects\ea
[FILE] .git\objects\ea\6d71ca15026a04c95a3c340b1ccfa477d65e0f
[FILE] .git\objects\ea\bb3ec77a55a505f85ce55f73469f61701714bd
[FILE] .git\objects\ea\c460f2c9845a7ab8d5fd6ef06ce94900881fab
[FILE] .git\objects\ea\c87ea4813cb4a0b61f7a2701430fc10e593a65
[DIR]  .git\objects\eb
[FILE] .git\objects\eb\7c2fdab722fbb133ca9ff8f25c6de5a867384e
[FILE] .git\objects\eb\b613751db1d4c87164f8daf1dca2fe257c3045
[FILE] .git\objects\eb\ea4a934f76dbbeec236f15d5ced2e3bad1f102
[FILE] .git\objects\eb\ed41826b648719f700c2758cedb870099c1815
[DIR]  .git\objects\ec
[FILE] .git\objects\ec\1c4d601cd11688dc8e138bafe6f091087f41dd
[FILE] .git\objects\ec\63becebf7c47c1fa2789b795a39baa64640e55
[FILE] .git\objects\ec\e4180263215c26c67bcd1ba9a131f2dafd135f
[FILE] .git\objects\ec\f025b891405f55c9f6b59763c856a8b19a023c
[DIR]  .git\objects\ed
[FILE] .git\objects\ed\0d738f8c9f6bc049af6c7d6b0cf6bc24a084cf
[FILE] .git\objects\ed\5b5b4a38d904ea357aeb24c89e1cc47c75ed79
[FILE] .git\objects\ed\837a7e63d0abb0301b552bc78a01c1f9babf08
[FILE] .git\objects\ed\a836eb37259f66166e55e345fc043c43ed3c3e
[DIR]  .git\objects\ee
[FILE] .git\objects\ee\08ae94554db8c1ce1267f953bfba40bfb3fcf0
[FILE] .git\objects\ee\0ce0f79ba796c479174af598f66f774476397f
[FILE] .git\objects\ee\d175898dff79553993178707dc19974b4595a8
[DIR]  .git\objects\ef
[FILE] .git\objects\ef\3c2ebc0609822202d9f76697dc46b1c2694253
[FILE] .git\objects\ef\b1b1cacb9bc33ecce0a5a7d299fcb1a9aa0874
[DIR]  .git\objects\f0
[FILE] .git\objects\f0\250dd5c767e0e811805b51877f6c245528dd85
[FILE] .git\objects\f0\d57bb8584c212ad77db3959d2b27f7d9b2369b
[FILE] .git\objects\f0\f521528f25561a27e00f5ac153101da92013a3
[DIR]  .git\objects\f1
[FILE] .git\objects\f1\86f74dba5fa79604f2f7cf8085e2dba34a1977
[DIR]  .git\objects\f2
[FILE] .git\objects\f2\a6fb5060abd85cd66588a0d2c99c8509412e91
[FILE] .git\objects\f2\b8541723aca5d7ff89849720d3b8ff06c95034
[DIR]  .git\objects\f3
[FILE] .git\objects\f3\c443158d8139a738f1adcc8c2607bfdef40cd4
[DIR]  .git\objects\f4
[FILE] .git\objects\f4\4d99ab5edd62ec9cbffc9924c36d1c2f6cc269
[FILE] .git\objects\f4\5e9b4cc3fe65b17deb82ae03221d1675f92cd8
[DIR]  .git\objects\f5
[FILE] .git\objects\f5\3e541c546437a71e89e7c9dbab5240d94ab95d
[FILE] .git\objects\f5\7da450cbcc8ffb5cb29dc28efc577fc3c9676e
[FILE] .git\objects\f5\fa53381f6604eb2ef2a2504a9894b80893a00c
[DIR]  .git\objects\f6
[FILE] .git\objects\f6\63b68523458dc640a0c7cbe1d371d0aa27c8ba
[FILE] .git\objects\f6\b41a5e577123a65ab1ec4da1014eb47a536ba1
[FILE] .git\objects\f6\b96dd25b7b79dd47563519e01847d3585082ba
[FILE] .git\objects\f6\ee2fcddcdaa2a82c80de31f3ad1cc6b3253c08
[FILE] .git\objects\f6\ef35a7b2566a924734fc2586cba2e042d08076
[DIR]  .git\objects\f8
[FILE] .git\objects\f8\2a600efdf6340f3e8ce3c2fb72974a5c5e2603
[FILE] .git\objects\f8\757c211a996d1892e44dffbbd90f3209a805f5
[DIR]  .git\objects\f9
[FILE] .git\objects\f9\d06b354dfb7d656abfe83d8baf2a1bdb9748db
[DIR]  .git\objects\fa
[FILE] .git\objects\fa\043db6dacb51e39408a1a8ff1e9d68c2971236
[DIR]  .git\objects\fb
[FILE] .git\objects\fb\ccd875b73df232d27a01b27e2e683c28e41961
[FILE] .git\objects\fb\f0d79adbc6ae9c659f714d5e1c1b265515f90a
[DIR]  .git\objects\fc
[FILE] .git\objects\fc\222ab1ee4ca7ea4c239d5bf866b1ef774fad7e
[FILE] .git\objects\fc\48166d1d11777bebe8b7c76642ca0e8937752a
[FILE] .git\objects\fc\65af395823c0fc4d3f38b76f618fa3eb196921
[DIR]  .git\objects\fd
[FILE] .git\objects\fd\617b9a229ccc6fe9fe8a68e733d6c8948a0736
[FILE] .git\objects\fd\789db2d945c4b64c64f6d10d2119eebaf01c1f
[FILE] .git\objects\fd\bf505446167b6dbc463e45a09b6ccefbb75cec
[FILE] .git\objects\fd\f3bf484d98e87f7927453204d5020663f72adf
[DIR]  .git\objects\fe
[FILE] .git\objects\fe\0c057721811f0d54a6ce5b2be55d3ed7e14fff
[FILE] .git\objects\fe\6ba4c549bcb22c25e2687f7ce7d3fcc5b0e289
[FILE] .git\objects\fe\b921e1bb44fe4cd60f0d6fd6c6841493c8ffb6
[DIR]  .git\objects\ff
[FILE] .git\objects\ff\bdcc8bd84b00e3b583cb645efd0fe64e9a3960
[FILE] .git\objects\ff\c08929035b3b8c0565d69a81d8d91aa0125cfc
[DIR]  .git\objects\info
[DIR]  .git\objects\pack
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.idx
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.pack
[FILE] .git\objects\pack\pack-4e41816efe8a1043e2af89116aabfdde7d4b0c68.rev
[FILE] .git\ORIG_HEAD
[FILE] .git\packed-refs
[DIR]  .git\refs
[DIR]  .git\refs\heads
[FILE] .git\refs\heads\main
[DIR]  .git\refs\remotes
[DIR]  .git\refs\remotes\origin
[FILE] .git\refs\remotes\origin\HEAD
[FILE] .git\refs\remotes\origin\main
[DIR]  .git\refs\tags
[DIR]  .ralfai
[FILE] .ralfai\config.yaml
[FILE] .ralfai\session.md
[FILE] ARCHITECTURE.md
[DIR]  assets
[FILE] assets\ralfai-icon.png
[FILE] assets\ralfai-icon.svg
[FILE] assets\ralfai-logo.svg
[FILE] PROMPTS.md
[FILE] README.md
[FILE] ROADMAP.md
[DIR]  src
[DIR]  src\LogoRasterizer
[DIR]  src\LogoRasterizer\bin
[DIR]  src\LogoRasterizer\bin\Debug
[DIR]  src\LogoRasterizer\bin\Debug\net10.0
[DIR]  src\LogoRasterizer\obj
[DIR]  src\LogoRasterizer\obj\Debug
[DIR]  src\LogoRasterizer\obj\Debug\net10.0
[DIR]  src\LogoRasterizer\obj\Debug\net10.0\ref
[DIR]  src\LogoRasterizer\obj\Debug\net10.0\refint
[DIR]  src\nupkg
[FILE] src\nupkg\RalfAI.1.1.0.nupkg
[DIR]  src\RalfAI
[DIR]  src\RalfAI.Abstractions
[DIR]  src\RalfAI.Abstractions\bin
[DIR]  src\RalfAI.Abstractions\bin\Debug
[DIR]  src\RalfAI.Abstractions\bin\Debug\net10.0
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.deps.json
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[DIR]  src\RalfAI.Abstractions\bin\Release
[DIR]  src\RalfAI.Abstractions\bin\Release\net10.0
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.deps.json
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\IAgentContext.cs
[DIR]  src\RalfAI.Abstractions\obj
[DIR]  src\RalfAI.Abstractions\obj\Debug
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.AssemblyInfo.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.assets.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.GlobalUsings.g.cs
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\RalfAI.Abstractions.sourcelink.json
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\ref\RalfAI.Abstractions.dll
[DIR]  src\RalfAI.Abstractions\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Abstractions\obj\Debug\net10.0\refint\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\project.assets.json
[FILE] src\RalfAI.Abstractions\obj\project.nuget.cache
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.g.props
[FILE] src\RalfAI.Abstractions\obj\RalfAI.Abstractions.csproj.nuget.g.targets
[DIR]  src\RalfAI.Abstractions\obj\Release
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.AssemblyInfo.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.assets.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.GlobalUsings.g.cs
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\RalfAI.Abstractions.sourcelink.json
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0\ref
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\ref\RalfAI.Abstractions.dll
[DIR]  src\RalfAI.Abstractions\obj\Release\net10.0\refint
[FILE] src\RalfAI.Abstractions\obj\Release\net10.0\refint\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Abstractions\RalfAI.Abstractions.csproj
[DIR]  src\RalfAI.Contexts.DotNet
[DIR]  src\RalfAI.Contexts.DotNet\bin
[DIR]  src\RalfAI.Contexts.DotNet\bin\Debug
[DIR]  src\RalfAI.Contexts.DotNet\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.deps.json
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.DotNet\bin\Release
[DIR]  src\RalfAI.Contexts.DotNet\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.deps.json
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.DotNet\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.DotNet\Contexts
[FILE] src\RalfAI.Contexts.DotNet\Contexts\DotNetContext.cs
[DIR]  src\RalfAI.Contexts.DotNet\obj
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.C.C91D3321.Up2Date
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.assets.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\RalfAI.Contexts.DotNet.sourcelink.json
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\ref\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.DotNet\obj\Debug\net10.0\refint\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\project.assets.json
[FILE] src\RalfAI.Contexts.DotNet\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.DotNet\obj\RalfAI.Contexts.DotNet.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.C.C91D3321.Up2Date
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.assets.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\RalfAI.Contexts.DotNet.sourcelink.json
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\ref\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.DotNet\obj\Release\net10.0\refint\RalfAI.Contexts.DotNet.dll
[DIR]  src\RalfAI.Contexts.DotNet\Plugins
[FILE] src\RalfAI.Contexts.DotNet\Plugins\DotNetPlugin.cs
[FILE] src\RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj
[DIR]  src\RalfAI.Contexts.HtmlApps
[DIR]  src\RalfAI.Contexts.HtmlApps\bin
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Debug
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.deps.json
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Release
[DIR]  src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.deps.json
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.HtmlApps\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.HtmlApps\Contexts
[FILE] src\RalfAI.Contexts.HtmlApps\Contexts\HtmlAppContext.cs
[DIR]  src\RalfAI.Contexts.HtmlApps\obj
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.C.6CC3700C.Up2Date
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.assets.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\RalfAI.Contexts.HtmlApps.sourcelink.json
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\ref\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Debug\net10.0\refint\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\project.assets.json
[FILE] src\RalfAI.Contexts.HtmlApps\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.HtmlApps\obj\RalfAI.Contexts.HtmlApps.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.C.6CC3700C.Up2Date
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.assets.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\RalfAI.Contexts.HtmlApps.sourcelink.json
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\ref\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.HtmlApps\obj\Release\net10.0\refint\RalfAI.Contexts.HtmlApps.dll
[DIR]  src\RalfAI.Contexts.HtmlApps\Plugins
[FILE] src\RalfAI.Contexts.HtmlApps\Plugins\StaticServerPlugin.cs
[FILE] src\RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj
[DIR]  src\RalfAI.Contexts.Markdown
[DIR]  src\RalfAI.Contexts.Markdown\bin
[DIR]  src\RalfAI.Contexts.Markdown\bin\Debug
[DIR]  src\RalfAI.Contexts.Markdown\bin\Debug\net10.0
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.deps.json
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.Markdown\bin\Release
[DIR]  src\RalfAI.Contexts.Markdown\bin\Release\net10.0
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.deps.json
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Contexts.Markdown\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Contexts.Markdown\Contexts
[FILE] src\RalfAI.Contexts.Markdown\Contexts\MarkdownContext.cs
[DIR]  src\RalfAI.Contexts.Markdown\obj
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.C.05DC118C.Up2Date
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.assets.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\RalfAI.Contexts.Markdown.sourcelink.json
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\ref\RalfAI.Contexts.Markdown.dll
[DIR]  src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Contexts.Markdown\obj\Debug\net10.0\refint\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\project.assets.json
[FILE] src\RalfAI.Contexts.Markdown\obj\project.nuget.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.g.props
[FILE] src\RalfAI.Contexts.Markdown\obj\RalfAI.Contexts.Markdown.csproj.nuget.g.targets
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.C.05DC118C.Up2Date
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.AssemblyInfo.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.assets.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.GlobalUsings.g.cs
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\RalfAI.Contexts.Markdown.sourcelink.json
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0\ref
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\ref\RalfAI.Contexts.Markdown.dll
[DIR]  src\RalfAI.Contexts.Markdown\obj\Release\net10.0\refint
[FILE] src\RalfAI.Contexts.Markdown\obj\Release\net10.0\refint\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj
[DIR]  src\RalfAI.Core
[FILE] src\RalfAI.Core\AgentEngine.cs
[FILE] src\RalfAI.Core\AgentSessionTypes.cs
[DIR]  src\RalfAI.Core\bin
[DIR]  src\RalfAI.Core\bin\Debug
[DIR]  src\RalfAI.Core\bin\Debug\net10.0
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.deps.json
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI.Core\bin\Release
[DIR]  src\RalfAI.Core\bin\Release\net10.0
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.deps.json
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\bin\Release\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\ConversationLogger.cs
[DIR]  src\RalfAI.Core\ImageGeneration
[FILE] src\RalfAI.Core\ImageGeneration\ImageGenerationService.cs
[FILE] src\RalfAI.Core\MarkdownText.cs
[DIR]  src\RalfAI.Core\obj
[DIR]  src\RalfAI.Core\obj\Debug
[DIR]  src\RalfAI.Core\obj\Debug\net10.0
[FILE] src\RalfAI.Core\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.C.DE964921.Up2Date
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.AssemblyInfo.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.assets.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.GlobalUsings.g.cs
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\obj\Debug\net10.0\RalfAI.Core.sourcelink.json
[DIR]  src\RalfAI.Core\obj\Debug\net10.0\ref
[FILE] src\RalfAI.Core\obj\Debug\net10.0\ref\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\obj\Debug\net10.0\refint
[FILE] src\RalfAI.Core\obj\Debug\net10.0\refint\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\project.assets.json
[FILE] src\RalfAI.Core\obj\project.nuget.cache
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.dgspec.json
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.g.props
[FILE] src\RalfAI.Core\obj\RalfAI.Core.csproj.nuget.g.targets
[DIR]  src\RalfAI.Core\obj\Release
[DIR]  src\RalfAI.Core\obj\Release\net10.0
[FILE] src\RalfAI.Core\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.C.DE964921.Up2Date
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.AssemblyInfo.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.AssemblyInfoInputs.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.assets.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.AssemblyReference.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.csproj.FileListAbsolute.txt
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.GlobalUsings.g.cs
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.pdb
[FILE] src\RalfAI.Core\obj\Release\net10.0\RalfAI.Core.sourcelink.json
[DIR]  src\RalfAI.Core\obj\Release\net10.0\ref
[FILE] src\RalfAI.Core\obj\Release\net10.0\ref\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\obj\Release\net10.0\refint
[FILE] src\RalfAI.Core\obj\Release\net10.0\refint\RalfAI.Core.dll
[DIR]  src\RalfAI.Core\Plugins
[FILE] src\RalfAI.Core\Plugins\AIPlugin.cs
[FILE] src\RalfAI.Core\Plugins\FileSystemPlugin.cs
[FILE] src\RalfAI.Core\Plugins\ImageGenerationPlugin.cs
[FILE] src\RalfAI.Core\Plugins\ImagePlugin.cs
[FILE] src\RalfAI.Core\Plugins\SpeechPlugin.cs
[FILE] src\RalfAI.Core\Plugins\WebPlugin.cs
[FILE] src\RalfAI.Core\RalfAI.Core.csproj
[FILE] src\RalfAI.Core\RalfAIConfig.cs
[FILE] src\RalfAI.Core\RateLimiter.cs
[DIR]  src\RalfAI.Core\Voice
[FILE] src\RalfAI.Core\Voice\SpeechService.cs
[FILE] src\RalfAI.sln
[FILE] src\RalfAI\appsettings.json
[DIR]  src\RalfAI\bin
[DIR]  src\RalfAI\bin\Debug
[DIR]  src\RalfAI\bin\Debug\net10.0
[FILE] src\RalfAI\bin\Debug\net10.0\Anthropic.dll
[FILE] src\RalfAI\bin\Debug\net10.0\appsettings.json
[FILE] src\RalfAI\bin\Debug\net10.0\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Azure.Core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Debug\net10.0\last-folder.txt
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Debug\net10.0\OpenAI.dll
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.deps.json
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.dll
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.exe
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-x64
[DIR]  src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Debug\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Debug\net10.0\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Debug\net10.0\Spectre.Console.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.ClientModel.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Debug\net10.0\System.Numerics.Tensors.dll
[DIR]  src\RalfAI\bin\Release
[DIR]  src\RalfAI\bin\Release\net10.0
[FILE] src\RalfAI\bin\Release\net10.0\Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\appsettings.json
[FILE] src\RalfAI\bin\Release\net10.0\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Azure.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Release\net10.0\OpenAI.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish
[FILE] src\RalfAI\bin\Release\net10.0\publish\Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\appsettings.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\Azure.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Azure.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Google.Protobuf.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.Anthropic.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Agents.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Bcl.AsyncInterfaces.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.CognitiveServices.Speech.csharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.Evaluation.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.AI.OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Caching.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Compliance.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.DependencyInjection.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Logging.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.Primitives.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.Extensions.VectorData.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Microsoft.ML.Tokenizers.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\OpenAI.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.deps.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.exe
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64
[DIR]  src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\Spectre.Console.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.ClientModel.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Release\net10.0\publish\System.Numerics.Tensors.dll
[FILE] src\RalfAI\bin\Release\net10.0\ralf.deps.json
[FILE] src\RalfAI\bin\Release\net10.0\ralf.dll
[FILE] src\RalfAI\bin\Release\net10.0\ralf.exe
[FILE] src\RalfAI\bin\Release\net10.0\ralf.pdb
[FILE] src\RalfAI\bin\Release\net10.0\ralf.runtimeconfig.json
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Abstractions.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Abstractions.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.DotNet.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.DotNet.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.HtmlApps.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.Markdown.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Contexts.Markdown.pdb
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Core.dll
[FILE] src\RalfAI\bin\Release\net10.0\RalfAI.Core.pdb
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\android-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\ios-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\iossimulator-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-arm64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.core.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.audio.sys.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.codec.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared_openssl3.so
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\linux-x64\native\libpal_azure_c_shared.so
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-arm64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\maccatalyst-x64\native\libMicrosoft.CognitiveServices.Speech.core.a
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-arm64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib\netstandard2.0
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\lib\netstandard2.0\Microsoft.CognitiveServices.Speech.csharp.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.core.dylib
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\osx-x64\native\libMicrosoft.CognitiveServices.Speech.extension.kws.ort.dylib
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-arm64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-arm64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-x64
[DIR]  src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.core.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.audio.sys.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.codec.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.dll
[FILE] src\RalfAI\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.CognitiveServices.Speech.extension.kws.ort.dll
[FILE] src\RalfAI\bin\Release\net10.0\SixLabors.ImageSharp.dll
[FILE] src\RalfAI\bin\Release\net10.0\Spectre.Console.Ansi.dll
[FILE] src\RalfAI\bin\Release\net10.0\Spectre.Console.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.ClientModel.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.Memory.Data.dll
[FILE] src\RalfAI\bin\Release\net10.0\System.Numerics.Tensors.dll
[FILE] src\RalfAI\config.example.json
[FILE] src\RalfAI\ContextRegistry.cs
[FILE] src\RalfAI\FileBrowser.cs
[FILE] src\RalfAI\FolderConfigStore.cs
[FILE] src\RalfAI\FullScreenConsole.cs
[FILE] src\RalfAI\InputCollector.cs
[FILE] src\RalfAI\LastFolderStore.cs
[DIR]  src\RalfAI\obj
[DIR]  src\RalfAI\obj\Debug
[DIR]  src\RalfAI\obj\Debug\net10.0
[FILE] src\RalfAI\obj\Debug\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI\obj\Debug\net10.0\apphost.exe
[FILE] src\RalfAI\obj\Debug\net10.0\ralf.dll
[FILE] src\RalfAI\obj\Debug\net10.0\ralf.pdb
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.AssemblyInfo.cs
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.AssemblyInfoInputs.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.assets.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.AssemblyReference.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.FileListAbsolute.txt
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.csproj.Up2Date
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.genruntimeconfig.cache
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.GlobalUsings.g.cs
[FILE] src\RalfAI\obj\Debug\net10.0\RalfAI.sourcelink.json
[DIR]  src\RalfAI\obj\Debug\net10.0\ref
[FILE] src\RalfAI\obj\Debug\net10.0\ref\ralf.dll
[DIR]  src\RalfAI\obj\Debug\net10.0\refint
[FILE] src\RalfAI\obj\Debug\net10.0\refint\ralf.dll
[FILE] src\RalfAI\obj\project.assets.json
[FILE] src\RalfAI\obj\project.nuget.cache
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.dgspec.json
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.g.props
[FILE] src\RalfAI\obj\RalfAI.csproj.nuget.g.targets
[DIR]  src\RalfAI\obj\Release
[DIR]  src\RalfAI\obj\Release\net10.0
[FILE] src\RalfAI\obj\Release\net10.0\.NETCoreApp,Version=v10.0.AssemblyAttributes.cs
[FILE] src\RalfAI\obj\Release\net10.0\apphost.exe
[FILE] src\RalfAI\obj\Release\net10.0\DotnetToolSettings.xml
[FILE] src\RalfAI\obj\Release\net10.0\PublishOutputs.64b92f381d.txt
[FILE] src\RalfAI\obj\Release\net10.0\PublishOutputs.e8d25c20c9.txt
[FILE] src\RalfAI\obj\Release\net10.0\ralf.dll
[FILE] src\RalfAI\obj\Release\net10.0\ralf.pdb
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.AssemblyInfo.cs
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.AssemblyInfoInputs.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.assets.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.AssemblyReference.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.CoreCompileInputs.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.FileListAbsolute.txt
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.csproj.Up2Date
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.GeneratedMSBuildEditorConfig.editorconfig
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.genruntimeconfig.cache
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.GlobalUsings.g.cs
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.sourcelink.json
[FILE] src\RalfAI\obj\Release\net10.0\RalfAI.toolssettingsinput.cache
[DIR]  src\RalfAI\obj\Release\net10.0\ref
[FILE] src\RalfAI\obj\Release\net10.0\ref\ralf.dll
[DIR]  src\RalfAI\obj\Release\net10.0\refint
[FILE] src\RalfAI\obj\Release\net10.0\refint\ralf.dll
[FILE] src\RalfAI\obj\Release\RalfAI.1.0.0.nuspec
[FILE] src\RalfAI\obj\Release\RalfAI.1.1.0.nuspec
[FILE] src\RalfAI\Program.cs
[DIR]  src\RalfAI\Properties
[FILE] src\RalfAI\Properties\launchSettings.json
[FILE] src\RalfAI\RalfAI.csproj
[FILE] src\RalfAI\RalfAIApp.cs
[FILE] USAGE.md

---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=src/RalfAI/InputCollector.cs
result: using System.Text;
using Spectre.Console;
using Spectre.Console.Rendering;
using RalfAI.Voice;

namespace RalfAI;

/// <summary>
/// Collects multi-line input into a plain (unboxed) prompt line and submits it on Ctrl+Enter.
///
/// This solves the problem of pasting text that contains blank lines:
/// every line (including empty ones) is accumulated, and only Ctrl+Enter triggers processing.
///
/// In an interactive console, input is collected live, key by key, with no persistent key-binding
/// list — press F1 for a full-screen overlay with the command reference instead. When stdin is
/// redirected (piped/scripted input), falls back to a plain line-by-line prompt with typed
/// /-commands, since key-by-key reading (and therefore function keys) requires a real console.
///
/// Interactive key bindings (live prompt only — see F1 for the full reference):
///   Ctrl+Enter        submit the current buffer
///   Enter              insert a newline (multi-line compose / paste)
///   Esc                clear the buffer and start over (while composing)
///   F1                 show command reference (full-screen overlay)
///   F2                 dictate a line via the microphone (requires AzureSpeech configuration)
///   F3                 read the agent's last response aloud (requires AzureSpeech configuration)
///   F5                 clear the screen
///   F6                 switch to a different generation context
///   F7                 switch to a different target folder
///   F8                 toggle debug messages (tool calls, rate-limit notices)
///   F10 / Ctrl+Q       quit RalfAI
///   Ctrl+C             interrupt the current turn while the agent is working (twice = force quit) — handled in RalfAIApp, not here
///   Esc / Ctrl+X       same interrupt as Ctrl+C, while the agent is working — handled in RalfAIApp, not here
///
/// Piped/scripted input (redirected stdin, see <see cref="CollectPlain"/>) has no function
/// keys — it uses typed "/"-commands instead, with their own reference shown by "/help"
/// (<see cref="ShowHelpOverlayPlain"/>), distinct from the interactive one above.
/// </summary>
public sealed class InputCollector(SpeechService? speech = null)
{
    private readonly List<string> _lines = [];
    private string? _hintOverride;
    private string? _status;

    /// <summary>
    /// Collects input until Ctrl+Enter and returns the assembled text.
    /// Returns <c>null</c> when the user quits (F10/Ctrl+Q, or EOF/'exit' in piped mode).
    /// Any other string (including the internal "/speak", "/clear" markers) is
    /// returned as-is for the caller to handle.
    /// </summary>
    /// <param name="status">
    /// Ready-to-render Spectre markup (e.g. current context/model/folder) shown above the
    /// prompt line — lets the caller keep the user oriented without a persistent command list.
    /// </param>
    public string? Collect(string? status = null)
    {
        _lines.Clear();
        _status = status;
        return Console.IsInputRedirected ? CollectPlain() : CollectLive();
    }

    // ── Interactive mode: live plain prompt, no border ──────────────────────

    private string? CollectLive()
    {
        var current = new StringBuilder();
        string? result = null;
        var exitLoop = false;

        // Use the terminal's own cursor instead of a fake glyph — avoids Unicode fallback
        // artifacts and looks orphaned less on an otherwise-empty continuation line.
        try { Console.CursorVisible = true; } catch (IOException) { }

        while (!exitLoop)
        {
            var showHelp = false;

            AnsiConsole.Live(RenderPrompt(current)).Start(ctx =>
            {
                var innerDone = false;

                while (!innerDone)
                {
                    ctx.UpdateTarget(RenderPrompt(current));
                    ctx.Refresh();

                    var key = Console.ReadKey(intercept: true);
                    var ctrl = (key.Modifiers & ConsoleModifiers.Control) != 0;

                    if (key.Key == ConsoleKey.Enter && ctrl)
                    {
                        if (current.Length > 0)
                        {
                            _lines.Add(current.ToString());
                            current.Clear();
                        }

                        if (_lines.Count > 0)
                        {
                            result = string.Join("\n", _lines).Trim();
                            exitLoop = true;
                            innerDone = true;
                        }
                        else
                        {
                            _hintOverride = "[yellow]Buffer is empty — type something first.[/]";
                        }
                        continue;
                    }

                    if (key.Key == ConsoleKey.F10 || (key.Key == ConsoleKey.Q && ctrl))
                    {
                        result = null;
                        exitLoop = true;
                        innerDone = true;
                        continue;
                    }

                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            _lines.Add(current.ToString());
                            current.Clear();
                            break;

                        case ConsoleKey.F1:
                            showHelp = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F2:
                            DictateInto(ctx);
                            break;

                        case ConsoleKey.F3:
                            result = "/speak";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F5:
                            result = "/clear";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F6:
                            result = "/switch-context";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F7:
                            result = "/switch-folder";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.F8:
                            result = "/toggle-debug";
                            exitLoop = true;
                            innerDone = true;
                            break;

                        case ConsoleKey.Backspace:
                            if (current.Length > 0)
                                current.Remove(current.Length - 1, 1);
                            else if (_lines.Count > 0)
                            {
                                current.Append(_lines[^1]);
                                _lines.RemoveAt(_lines.Count - 1);
                            }
                            break;

                        case ConsoleKey.Escape:
                            _lines.Clear();
                            current.Clear();
                            break;

                        default:
                            if (!char.IsControl(key.KeyChar))
                                current.Append(key.KeyChar);
                            break;
                    }
                }

                ctx.UpdateTarget(RenderPrompt(current));
                ctx.Refresh();
            });

            if (showHelp) ShowHelpOverlayInteractive();
        }

        try { Console.CursorVisible = false; } catch (IOException) { }

        return result;
    }

    // ── Voice dictation (F2) ─────────────────────────────────────────────────

    /// <summary>
    /// Listens on the microphone and appends the recognized text as a new line in the
    /// buffer, as if the user had typed it. No-op with a hint if AzureSpeech isn't configured.
    /// </summary>
    private void DictateInto(LiveDisplayContext ctx)
    {
        if (speech is null)
        {
            _hintOverride = "[red]Voice non configurata — imposta AzureSpeech in appsettings.json.[/]";
            return;
        }

        ctx.UpdateTarget(new Markup("[cyan]🎤 In ascolto… parla pure — si ferma da solo alla pausa.[/]"));
        ctx.Refresh();

        var (spoken, error) = speech.RecognizeOnceAsync().GetAwaiter().GetResult();

        if (spoken is not null)
            _lines.Add(spoken);

        _hintOverride = spoken is not null
            ? null
            : $"[red]{Markup.Escape(error ?? "Riconoscimento non riuscito.")}[/]";
    }

    private IRenderable RenderPrompt(StringBuilder current)
    {
        var displayLines = new List<string>(_lines) { current.ToString() };
        var hasContent = displayLines.Any(l => l.Length > 0);

        // No fake cursor glyph here — the real terminal cursor (shown during CollectLive) ends
        // up in the right place naturally, since it's just wherever this text stopped printing.
        IRenderable promptLine = hasContent
            ? new Markup(string.Join("\n", displayLines.Select((l, i) =>
                (i == 0 ? "[green]❯[/] " : "  ") + Markup.Escape(l))))
            : new Markup("[green]❯[/] [grey]Type your message… (F1 for help)[/]");

        // Only transient feedback (errors, "buffer empty", dictation status) gets a line below
        // the prompt — no persistent key-binding list. Shown for exactly one render, then cleared.
        var hint = _hintOverride;
        _hintOverride = null;

        var rows = new List<IRenderable>();
        if (_status is not null) rows.Add(new Markup(_status));
        rows.Add(promptLine);
        if (hint is not null) rows.Add(new Markup(hint));

        return rows.Count == 1 ? rows[0] : new Rows(rows);
    }

    // ── Fallback mode: plain line-by-line prompt (redirected/piped stdin) ───
    //
    // Function keys and Ctrl+Enter can't be read from redirected stdin, so this path keeps
    // the original typed /-commands for scripted/piped usage.

    private string? CollectPlain()
    {
        while (true)
        {
            // Prompt changes after the first line to show multi-line mode
            if (_lines.Count == 0)
                AnsiConsole.Markup("[bold green]you>[/] ");
            else
                AnsiConsole.Markup($"[dim]  ({_lines.Count} lines)[/] ");

            string? line;
            try { line = Console.ReadLine(); }
            catch { return null; }

            if (line is null) return null; // EOF / Ctrl+Z

            var trimmed = line.Trim();

            switch (trimmed.ToLowerInvariant())
            {
                case "exit":
                case "/exit":
                    return null;

                case "/cancel":
                    _lines.Clear();
                    AnsiConsole.MarkupLine("[dim]  ↩ Buffer cleared.[/]");
                    continue;

                case "/exe":
                    if (_lines.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[dim]  Buffer is empty — type something first.[/]");
                        continue;
                    }
                    return string.Join("\n", _lines).Trim();

                case "/help":
                    ShowHelpOverlayPlain();
                    continue;

                case "/speak":
                    return "/speak";

                case "/clear":
                    return "/clear";

                case "/switch-context":
                    return "/switch-context";

                case "/switch-folder":
                    return "/switch-folder";

                case "/toggle-debug":
                    return "/toggle-debug";

                case "/voice":
                    if (speech is null)
                    {
                        AnsiConsole.MarkupLine("[red]  Voice non configurata — imposta AzureSpeech in appsettings.json.[/]");
                        continue;
                    }

                    AnsiConsole.MarkupLine("[cyan]  🎤 In ascolto… parla pure.[/]");
                    var (spoken, error) = speech.RecognizeOnceAsync().GetAwaiter().GetResult();

                    if (spoken is null)
                    {
                        AnsiConsole.MarkupLine($"[red]  {Markup.Escape(error ?? "Riconoscimento non riuscito.")}[/]");
                        continue;
                    }

                    _lines.Add(spoken);
                    AnsiConsole.MarkupLine($"[dim]  ({_lines.Count} lines) \"{Markup.Escape(spoken)}\"[/]");
                    continue;

                default:
                    _lines.Add(line);
                    if (_lines.Count == 1)
                        AnsiConsole.MarkupLine("[dim]  (add more lines or type [bold]/exe[/] to send)[/]");
                    continue;
            }
        }
    }

    // ── F1 — full-screen overlay for the interactive (live) prompt ──────────

    private static void ShowHelpOverlayInteractive()
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Key[/]").Centered())
            .AddColumn("[bold]What it does[/]");

        table.AddRow("[green]Ctrl+Enter[/]", "Send the current buffer to the agent");
        table.AddRow("[yellow]Esc[/]",       "While composing: clear the buffer and start fresh");
        table.AddRow("[cyan]F2[/]",          "Dictate a line via microphone (needs AzureSpeech config)");
        table.AddRow("[cyan]F3[/]",          "Read the agent's last response aloud (needs AzureSpeech config)");
        table.AddRow("[cyan]F5[/]",          "Clear the screen");
        table.AddRow("[cyan]F6[/]",          "Switch to a different generation context");
        table.AddRow("[cyan]F7[/]",          "Switch to a different target folder (file browser)");
        table.AddRow("[cyan]F8[/]",          "Toggle debug messages (tool calls, rate-limit notices)");
        table.AddRow("[grey]F1[/]",          "Show this table");
        table.AddRow("[red]F10 / Ctrl+Q[/]", "Quit RalfAI");
        table.AddRow("[yellow]Ctrl+C[/]",    "While the agent is working: interrupt the current turn (twice = force quit)");
        table.AddRow("[yellow]Esc / Ctrl+X[/]", "While the agent is working: same interrupt as Ctrl+C");

        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(table));
        AnsiConsole.WriteLine();
        AnsiConsole.Write(Align.Center(new Markup("[dim]Premi un tasto per tornare…[/]")));

        // Safe here: this overlay only runs on the live (non-redirected) path.
        Console.ReadKey(intercept: true);
        AnsiConsole.Clear();
    }

    // ── /help — printed reference for piped/scripted input (no function keys there) ──

    private static void ShowHelpOverlayPlain()
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Command[/]").Centered())
            .AddColumn("[bold]What it does[/]");

        table.AddRow("[green]/exe[/]",            "Send the accumulated buffer to the agent");
        table.AddRow("[yellow]/cancel[/]",        "Clear the buffer without sending");
        table.AddRow("[cyan]/voice[/]",           "Dictate a line via microphone, appended to the buffer (needs AzureSpeech config)");
        table.AddRow("[cyan]/speak[/]",           "Read the agent's last response aloud (needs AzureSpeech config)");
        table.AddRow("[cyan]/clear[/]",           "Clear the screen");
        table.AddRow("[cyan]/switch-context[/]",  "Switch to a different generation context");
        table.AddRow("[cyan]/switch-folder[/]",   "Switch to a different target folder (file browser)");
        table.AddRow("[cyan]/toggle-debug[/]",    "Toggle debug messages (tool calls, rate-limit notices)");
        table.AddRow("[grey]/help[/]",            "Show this table");
        table.AddRow("[red]exit / /exit[/]",      "Quit RalfAI (EOF/Ctrl+Z also works)");

        // No Console.ReadKey here (unlike the interactive overlay): stdin is redirected on
        // this path, so reading a key would throw — just print and let the next ReadLine()
        // prompt continue the loop.
        AnsiConsole.WriteLine();
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
}


---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=src/RalfAI/FullScreenConsole.cs
result: namespace RalfAI;

/// <summary>
/// Switches the terminal to its alternate screen buffer for the lifetime of the returned
/// <see cref="IDisposable"/> — the same mechanism vim/less/htop use to take over the whole
/// screen without touching the user's normal scrollback, which is restored exactly as it was
/// on dispose. Spectre.Console has no built-in concept of this (it renders sequentially into
/// whatever buffer is current), but it writes plain ANSI under the hood, so entering/exiting
/// the alternate buffer around a whole session composes fine with it.
/// </summary>
public static class FullScreenConsole
{
    private const string EnterAltScreen = "\x1b[?1049h\x1b[H";
    private const string ExitAltScreen = "\x1b[?1049l";

    /// <summary>No-op (returns null) when output/input isn't a real interactive console.</summary>
    public static IDisposable? Enter()
    {
        if (Console.IsOutputRedirected || Console.IsInputRedirected) return null;

        Console.Out.Write(EnterAltScreen);
        try { Console.CursorVisible = false; } catch (IOException) { }

        return new Session();
    }

    private sealed class Session : IDisposable
    {
        public void Dispose()
        {
            try { Console.CursorVisible = true; } catch (IOException) { }
            Console.Out.Write(ExitAltScreen);
        }
    }
}


---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=src/RalfAI/RalfAIApp.cs
result: using Microsoft.Extensions.Configuration;
using Spectre.Console;
using RalfAI.Contexts;
using Spectre.Console.Rendering;

namespace RalfAI;

/// <summary>
/// Console frontend on top of <see cref="AgentEngine"/>: renders the chat loop with
/// Spectre.Console (spinner, tool-call lines, panels) and collects input via
/// <see cref="InputCollector"/>. All agent logic (Microsoft Agent Framework, rate limiting,
/// logging, voice) lives in <see cref="AgentEngine"/> so other frontends can reuse it unchanged.
/// </summary>
public sealed class RalfAIApp
{
    private readonly IConfiguration _config;
    private AgentEngine _session;
    private string _contextKey;
    private bool _debug;

    // Set just before each LLM call so tool-call event handlers can update the spinner.
    private StatusContext? _liveStatus;

    private RalfAIApp(IConfiguration config, string contextKey, bool debug, AgentEngine session)
    {
        _config = config;
        _contextKey = contextKey;
        _debug = debug;
        _session = session;
        AttachSessionEvents();
    }

    public static async Task<RalfAIApp> CreateAsync(
        IConfiguration config, string targetFolder, string contextKey, IAgentContext context, bool debug)
    {
        var session = await AgentEngine.CreateAsync(config, targetFolder, contextKey, context);
        return new RalfAIApp(config, contextKey, debug, session);
    }

    private void AttachSessionEvents()
    {
        _session.ToolCallStarted += OnToolCallStarted;
        _session.ToolCallCompleted += OnToolCallCompleted;
        _session.Notice += OnNotice;
    }

    private void DetachSessionEvents()
    {
        _session.ToolCallStarted -= OnToolCallStarted;
        _session.ToolCallCompleted -= OnToolCallCompleted;
        _session.Notice -= OnNotice;
    }

    // ── Main loop ────────────────────────────────────────────────────────────

    public async Task RunAsync(CancellationToken appToken = default)
    {
        // Ctrl+C cancels the current LLM call (first press); second press exits.
        using var globalCts = CancellationTokenSource.CreateLinkedTokenSource(appToken);

        Console.CancelKeyPress += (_, e) =>
        {
            if (!globalCts.IsCancellationRequested)
            {
                e.Cancel = true;
                globalCts.Cancel();
                AnsiConsole.MarkupLine(
                    "\n  [yellow]⚠ Ctrl+C received — interrupting. Press Ctrl+C again to force quit.[/]");
            }
        };

        var input = new InputCollector(_session.Speech);

        while (!appToken.IsCancellationRequested)
        {

            // Recreate per-turn CTS so Ctrl+C only cancels the current turn.
            using var turnCts = CancellationTokenSource.CreateLinkedTokenSource(appToken);

            // Background task: detect Escape or Ctrl+X during processing.
            // Runs only while we're waiting for the LLM.
            using var escSrc = new CancellationTokenSource();
            var escListener = ListenForEscapeAsync(turnCts, escSrc.Token);

            var text = input.Collect(BuildStatusLine());

            // Stop the escape listener — we're reading input now.
            await escSrc.CancelAsync();
            await escListener.ConfigureAwait(false);

            if (text is null) break;

            // ── Special commands handled by the app (not sent to the agent) ──
            if (text.Equals("/speak", StringComparison.OrdinalIgnoreCase))
            {
                // Listen for Esc/Ctrl+X while audio is playing so it can be interrupted.
                using var escSrcSpeak = new CancellationTokenSource();
                var escListenerSpeak  = ListenForEscapeAsync(turnCts, escSrcSpeak.Token);

                await SpeakLastResponseAsync(turnCts.Token);

                await escSrcSpeak.CancelAsync();
                await escListenerSpeak.ConfigureAwait(false);
                continue;
            }

            if (text.Equals("/clear", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.Clear();
                continue;
            }

            if (text.Equals("/switch-context", StringComparison.OrdinalIgnoreCase))
            {
                await SwitchContext();
                continue;
            }

            if (text.Equals("/switch-folder", StringComparison.OrdinalIgnoreCase))
            {
                await SwitchFolder();
                continue;
            }

            if (text.Equals("/toggle-debug", StringComparison.OrdinalIgnoreCase))
            {
                ToggleDebug();
                continue;
            }

            // ── LLM processing turn ──────────────────────────────────────────
            AgentTurnResult? turnResult = null;

            // Start the escape listener again while the LLM is working.
            using var escSrc2  = new CancellationTokenSource();
            var escListener2   = ListenForEscapeAsync(turnCts, escSrc2.Token);

            try
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots2)
                    .SpinnerStyle(Style.Parse("cyan"))
                    .StartAsync("[dim cyan]thinking…[/]", async ctx =>
                    {
                        _liveStatus = ctx;
                        try
                        {
                            turnResult = await _session.SendAsync(text, turnCts.Token);
                        }
                        finally
                        {
                            _liveStatus = null;
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("  [yellow]↩ Processing cancelled.[/]");
            }

            // Stop escape listener.
            await escSrc2.CancelAsync();
            await escListener2.ConfigureAwait(false);

            if (turnResult is null) continue;

            if (turnResult.Error is not null)
            {
                AnsiConsole.MarkupLine($"\n  [red]⛔ Error: {Markup.Escape(turnResult.Error)}[/]");
                continue;
            }

            ShowAgentResponse(turnResult.Thinking, turnResult.FinalText!);
        }

        AnsiConsole.Write(new Rule().RuleStyle("grey dim"));
        AnsiConsole.MarkupLine(
            $"  [dim]Session saved → {_session.LogPath}[/]");
    }

    /// <summary>Context · model · folder shown above the prompt, so switching (F6/F7) stays visible.</summary>
    private string BuildStatusLine()
    {
        var trimmed = _session.TargetFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var folderName = Path.GetFileName(trimmed);
        if (string.IsNullOrEmpty(folderName)) folderName = trimmed;

        return $"[dim]{Markup.Escape(_session.Context.Name)} · {Markup.Escape(_session.ModelId)} · {Markup.Escape(folderName)}[/]";
    }

    // ── AgentSession event handlers ──────────────────────────────────────────

    private void OnToolCallStarted(ToolCallStart call)
    {
        // Update the spinner status to the current tool — kept even with debug off, it's
        // transient progress feedback rather than debug noise. Shows the human-readable
        // [Description] instead of the raw Plugin.Function name.
        _liveStatus?.Status($"[yellow]{Markup.Escape(call.Description)}…[/]");

        if (!_debug) return;

        // Write a permanent line above the spinner.
        AnsiConsole.MarkupLine(
            $"  [bold yellow]⏺[/] [yellow]{Markup.Escape(call.Description)}[/] " +
            $"[dim]({Markup.Escape(call.Name)})[/][grey]({Markup.Escape(call.ArgsPreview)})[/]");
    }

    private void OnToolCallCompleted(ToolCallResult call)
    {
        if (!_debug) return;

        var preview = call.Result.Length > 120
            ? call.Result[..120].Replace("\n", " ") + "…"
            : call.Result.Replace("\n", " ");

        AnsiConsole.MarkupLine($"    [dim]⎿  {Markup.Escape(preview)}[/]");
    }

    private void OnNotice(AgentNotice notice)
    {
        // Errors always surface; info/warning pacing notices are debug noise.
        if (!_debug && notice.Level != NoticeLevel.Error) return;

        var color = notice.Level switch
        {
            NoticeLevel.Warning => "yellow",
            NoticeLevel.Error => "red",
            _ => "dim"
        };
        AnsiConsole.MarkupLine($"  [{color}]{Markup.Escape(notice.Message)}[/]");
    }

    // ── UI helpers ───────────────────────────────────────────────────────────

    private async Task SpeakLastResponseAsync(CancellationToken ct)
    {
        if (_session.Speech is null)
        {
            AnsiConsole.MarkupLine("  [red]Voice non configurata — imposta AzureSpeech in appsettings.json.[/]");
            return;
        }

        if (string.IsNullOrWhiteSpace(_session.LastFinalText))
        {
            AnsiConsole.MarkupLine("  [yellow]Nessuna risposta dell'agente da leggere ancora.[/]");
            return;
        }

        AnsiConsole.MarkupLine("  [cyan]🔊 Lettura in corso… [dim](Esc per interrompere)[/][/]");
        try
        {
            var error = await _session.Speech.SpeakAsync(_session.LastFinalText, ct);
            if (error is not null)
                AnsiConsole.MarkupLine($"  [red]{Markup.Escape(error)}[/]");
        }
        catch (OperationCanceledException)
        {
            AnsiConsole.MarkupLine("  [yellow]↩ Lettura interrotta.[/]");
        }
    }

    private static void ShowAgentResponse(string? thinking, string text)
    {
        AnsiConsole.WriteLine();

        if (!string.IsNullOrWhiteSpace(thinking))
        {
            AnsiConsole.Write(new Rule("[grey]✻ Thinking[/]").RuleStyle("grey dim").LeftJustified());
            AnsiConsole.MarkupLine($"[grey italic]{Markup.Escape(MarkdownText.StripFormatting(thinking))}[/]");
            AnsiConsole.WriteLine();
        }

        AnsiConsole.Write(new Rule("[cyan]agent[/]").RuleStyle("cyan dim"));

        // Console can't render Markdown, so strip it before escaping Spectre markup characters.
        AnsiConsole.MarkupLine(Markup.Escape(MarkdownText.StripFormatting(text)));

        AnsiConsole.Write(new Rule().RuleStyle("cyan dim"));
        AnsiConsole.WriteLine();
    }

    private async Task SwitchContext()
    {
        var options = ContextRegistry.All().ToList();
        var selected = Math.Max(0, options.FindIndex(o => o.Key == _contextKey));
        (string Key, IAgentContext Context)? choice = null;

        AnsiConsole.Live(RenderContextMenu(options, selected)).Start(ctx =>
        {
            var done = false;
            while (!done)
            {
                ctx.UpdateTarget(RenderContextMenu(options, selected));
                ctx.Refresh();

                var key = Console.ReadKey(intercept: true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        selected = Math.Max(0, selected - 1);
                        break;

                    case ConsoleKey.DownArrow:
                        selected = Math.Min(options.Count - 1, selected + 1);
                        break;

                    case ConsoleKey.Enter:
                        choice = options[selected];
                        done = true;
                        break;

                    case ConsoleKey.Escape:
                        done = true;
                        break;
                }
            }
        });

        AnsiConsole.Clear();

        if (choice is null)
        {
            AnsiConsole.MarkupLine("  [dim]Cambio contesto annullato.[/]");
            return;
        }

        if (choice.Value.Key == _contextKey)
        {
            AnsiConsole.MarkupLine($"  [dim]Già su {Markup.Escape(choice.Value.Context.Name)}.[/]");
            return;
        }

        DetachSessionEvents();
        _contextKey = choice.Value.Key;
        _session = await AgentEngine.CreateAsync(_config, _session.TargetFolder, _contextKey, choice.Value.Context);
        AttachSessionEvents();
        FolderConfigStore.SaveContext(_session.TargetFolder, _contextKey);

        AnsiConsole.MarkupLine(
            $"  [green]✓ Passato a {Markup.Escape(_session.Context.Name)}.[/] " +
            $"[dim]Modello: {Markup.Escape(_session.ModelId)} · " +
            $"({_session.ResumedMessageCount} messaggi ripresi dal log)[/]");
    }

    private static IRenderable RenderContextMenu(List<(string Key, IAgentContext Context)> options, int selected)
    {
        var lines = options.Select((o, i) =>
        {
            var label = Markup.Escape($"{o.Context.Name} — {o.Context.Description}");
            return i == selected ? $"[invert]{label}[/]" : label;
        });

        var panel = new Panel(string.Join("\n", lines))
        {
            Header = new PanelHeader(" Select generation context "),
            Border = BoxBorder.Rounded,
            BorderStyle = Style.Parse("cyan"),
            Padding = new Padding(1, 0, 1, 0)
        };

        return new Rows(panel, new Markup("[dim]↑↓ move · Enter select · Esc cancel[/]"));
    }

    private async Task SwitchFolder()
    {
        var editorCommand = _config["Editor"] ?? FileBrowser.DefaultEditor;
        var picked = FileBrowser.Pick(_session.TargetFolder, editorCommand);
        AnsiConsole.Clear();

        if (picked is null)
        {
            AnsiConsole.MarkupLine("  [dim]Cambio cartella annullato.[/]");
            return;
        }

        var newFolder = Path.GetFullPath(picked);
        if (string.Equals(newFolder, _session.TargetFolder, StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("  [dim]Stessa cartella già in uso.[/]");
            return;
        }

        Directory.CreateDirectory(newFolder);
        LastFolderStore.Save(newFolder);

        // The destination folder may remember its own context (.ralfai/config.yaml);
        // fall back to whatever context is currently active if it doesn't.
        var newContextKey = FolderConfigStore.LoadContext(newFolder) ?? _contextKey;
        var context = ContextRegistry.Create(newContextKey);
        FolderConfigStore.SaveContext(newFolder, newContextKey);

        DetachSessionEvents();
        _contextKey = newContextKey;
        _session = await AgentEngine.CreateAsync(_config, newFolder, _contextKey, context);
        AttachSessionEvents();

        AnsiConsole.MarkupLine(
            $"  [green]✓ Cartella target: {Markup.Escape(newFolder)}.[/] " +
            $"[dim]Contesto: {Markup.Escape(_session.Context.Name)} · Modello: {Markup.Escape(_session.ModelId)} · " +
            $"({_session.ResumedMessageCount} messaggi ripresi dal log)[/]");
    }

    private void ToggleDebug()
    {
        _debug = !_debug;
        FolderConfigStore.SaveDebug(_session.TargetFolder, _debug);

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine(_debug
            ? "  [green]✓ Debug messages: ON[/] [dim](tool calls, rate-limit notices)[/]"
            : "  [dim]✓ Debug messages: OFF[/]");
    }

    // ── Key listener (Esc / Ctrl+X interrupts the current turn) ─────────────

    private static async Task ListenForEscapeAsync(
        CancellationTokenSource turnCts,
        CancellationToken listenerToken)
    {
        // Console.KeyAvailable throws when stdin is redirected (piped/scripted input) —
        // there are no keystrokes to listen for in that case.
        if (Console.IsInputRedirected) return;

        try
        {
            while (!listenerToken.IsCancellationRequested && !turnCts.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    var isEsc    = key.Key == ConsoleKey.Escape;
                    var isCtrlX  = key.Key == ConsoleKey.X &&
                                   key.Modifiers == ConsoleModifiers.Control;
                    if (isEsc || isCtrlX)
                    {
                        AnsiConsole.MarkupLine(
                            "\n  [yellow]⚠ Escape/Ctrl+X — interrupting turn…[/]");
                        await turnCts.CancelAsync();
                        return;
                    }
                }
                await Task.Delay(50, listenerToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { /* expected */ }
    }
}


---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=src/RalfAI/RalfAI.csproj
result: <Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>ralf</AssemblyName>
    <RootNamespace>RalfAI</RootNamespace>
    <AllowUnsafeBlocks>false</AllowUnsafeBlocks>
    <!-- Suppress SK experimental-API warnings; the AzureAIInference connector is stable enough for use -->
    <NoWarn>$(NoWarn);SKEXP0070;SKEXP0001</NoWarn>

    <!-- dotnet tool packaging (dotnet pack, then dotnet tool install as a global tool) -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>ralf</ToolCommandName>
    <PackageId>RalfAI</PackageId>
    <Version>1.1.0</Version>
    <Description>RalfAI — an autonomous coding agent CLI for .NET, Markdown, and HTML/CSS/JS projects.</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Spectre.Console" Version="0.57.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\RalfAI.Abstractions\RalfAI.Abstractions.csproj" />
    <ProjectReference Include="..\RalfAI.Core\RalfAI.Core.csproj" />
    <ProjectReference Include="..\RalfAI.Contexts.DotNet\RalfAI.Contexts.DotNet.csproj" />
    <ProjectReference Include="..\RalfAI.Contexts.Markdown\RalfAI.Contexts.Markdown.csproj" />
    <ProjectReference Include="..\RalfAI.Contexts.HtmlApps\RalfAI.Contexts.HtmlApps.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- CopyToOutputDirectory alone is enough: PackAsTool packages the whole publish output
         (which already includes this), so AppContext.BaseDirectory-based config loading finds
         it correctly after a global install too. -->
    <None Update="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>


---

### TOOL — FileSystem.ReadFile — 17:17:26

args: relativePath=USAGE.md
result: # Guida all'uso

RalfAI è un agente di coding autonomo a riga di comando, distribuito come
[.NET global tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) (`ralf`). Per come è
fatto internamente vedi [ARCHITECTURE.md](ARCHITECTURE.md); per l'installazione e la configurazione
completa (provider AI, rate limiting, voce) vedi [README.md](README.md). Questa guida copre l'uso
quotidiano una volta installato.

## Avvio

```bash
ralf                          # apre un folder picker (ricorda l'ultima cartella usata)
ralf "C:\path\to\output-folder"
```

L'argomento posizionale con la cartella target è **opzionale**: se non lo passi e non c'è ancora
una cartella ricordata da un avvio precedente, si apre un file browser a schermo intero (vedi
[sotto](#file-browser)) per sceglierla. La cartella viene creata automaticamente se non esiste.

In fase di sviluppo, dalla cartella `src/` del repo, si può anche lanciare senza installare il tool:

```bash
cd src
dotnet run --project RalfAI -- "C:\path\to\output-folder"
```

### Scegliere il context

```bash
ralf "C:\path\to\output-folder" --context html
ralf --list-contexts
```

- `--context <nome>` ha priorità su tutto.
- In alternativa si può impostare `"AgentContext": "dotnet"` in `appsettings.json` (o la variabile
  d'ambiente `RALFAI_AgentContext`).
- Se non specificato, il default è **`markdown`** (vedi [ARCHITECTURE.md](ARCHITECTURE.md) e
  [ROADMAP.md](ROADMAP.md)) — non `dotnet`.
- `--list-contexts` elenca tutti i context registrati (`dotnet`, `markdown`, `html`) senza avviare
  una sessione.
- Un nome sconosciuto fa fallire l'avvio con un errore che elenca i context registrati.
- Ogni cartella target ricorda il proprio context (e il flag debug) in
  `<cartella>/.ralfai/config.yaml`: rilanciare `ralf <cartella>` — o lasciare che si riprenda da
  solo senza argomenti — riparte con lo stesso context.

Altri flag: `--debug` (mostra le tool-call in tempo reale fin dall'avvio) e
`--config <path>` (usa un file di configurazione alternativo — vedi
[README.md](README.md#where-the-config-file-lives)).

## Sessione interattiva

L'intera sessione gira nello screen buffer alternato del terminale (la stessa tecnica di
`vim`/`less`/`htop`): il tuo scrollback normale resta intatto e viene ripristinato all'uscita.
Scrivi il messaggio (anche su più righe, incolla pure liberamente) e usa questi tasti:

| Tasto | Azione |
| --- | --- |
| `Ctrl+Enter` | invia il messaggio |
| `Enter` | va a capo nel buffer (composizione multi-riga) |
| `Esc` | svuota il buffer corrente |
| `F1` | mostra la tabella comandi a schermo intero |
| `F2` | detta una riga via microfono (richiede `AzureSpeech`) |
| `F3` | legge ad alta voce l'ultima risposta dell'agente (richiede `AzureSpeech`, `Esc` interrompe) |
| `F5` | pulisce lo schermo |
| `F6` | cambia generation context (`↑↓`/`Enter`/`Esc` nel menu) |
| `F7` | cambia cartella target (apre il file browser) |
| `F8` | attiva/disattiva i messaggi di debug (tool call, notice di rate-limit) |
| `F10` / `Ctrl+Q` | esce da RalfAI |
| `Ctrl+C` | interrompe il turno corrente (due volte forza la chiusura del processo) |
| `Esc` / `Ctrl+X` durante l'elaborazione | interrompe il turno in corso (come il primo `Ctrl+C`) |

Non c'è più una lista di comandi persistente sullo schermo: sopra il prompt vedi solo lo stato
corrente (context · modello · cartella); premi `F1` quando ti serve il riepilogo dei tasti.

### File browser

Si apre all'avvio senza argomenti (nessuna cartella ricordata) o con `F7` durante una sessione:
un pannello a singolo riquadro per navigare il filesystem, con anteprima file e piccole operazioni
di gestione.

| Tasto | Azione |
| --- | --- |
| `↑` / `↓` | muovi la selezione |
| `PgUp` / `PgDn` | muovi la selezione di 10 |
| `Enter` | apri la cartella selezionata |
| `Backspace` | sali di un livello |
| `F9` | usa la cartella corrente come target RalfAI |
| `F3` | visualizza il file selezionato (sola lettura, max 200 KB, no binari) |
| `F4` | apre il file selezionato in un editor esterno (vedi sotto) |
| `F2` | rinomina l'elemento selezionato |
| `F7` | crea una nuova cartella |
| `F8` / `Del` | elimina (chiede conferma) |
| `F1` | mostra questa tabella |
| `F10` / `Esc` | annulla e torna indietro |

`F4` lancia il comando configurato sotto la chiave `Editor` in `appsettings.json` (override
via `RALFAI_Editor`), passandogli il percorso completo del file selezionato come argomento —
default **`code.exe`** (VS Code) se non configurato. Non aspetta che l'editor si chiuda: apre
e torna subito al file browser. Se il comando non è nel `PATH` o non esiste, viene mostrato un
messaggio d'errore con l'indicazione di correggere `Editor`.

### Input da pipe/script

Se lo stdin è rediretto (es. `echo "..." | ralf ...`), l'input passa in modalità riga-per-riga
(niente box interattivo, niente listener Esc, niente file browser a schermo intero): utile per
test automatici o script. In questa modalità i tasti funzione non esistono, quindi le stesse azioni
sono comandi testuali:

- **`/exe`** invia il buffer accumulato all'agente.
- **`/cancel`** svuota il buffer senza inviare.
- **`/help`** mostra la tabella comandi.
- **`/speak`** legge ad alta voce l'ultima risposta dell'agente (richiede `AzureSpeech`).
- **`/clear`** pulisce lo schermo.
- **`/switch-context`** cambia generation context.
- **`/switch-folder`** cambia cartella target.
- **`/toggle-debug`** attiva/disattiva i messaggi di debug.
- **`/voice`** registra dal microfono e aggiunge il testo riconosciuto come riga nel buffer
  (richiede `AzureSpeech` — vedi sotto): poi invii con `/exe` come di consueto. La registrazione si
  ferma da sola quando l'SDK rileva una pausa nel parlato.
- **`exit`** (o EOF/Ctrl+Z) chiude RalfAI.

## Cosa fa l'agente a ogni richiesta (context `dotnet`)

Ogni context (`dotnet`, `markdown`, `html`) ha il proprio set di tool e il proprio prompt di
sistema — vedi la tabella in [README.md](README.md#contexts). Il flusso del context `dotnet`,
preso come esempio rappresentativo:

1. **Logga** la richiesta in `PROMPTS.md` (voce numerata, creato alla prima richiesta) prima di
   fare qualunque altra cosa — vedi [Log dei prompt](#log-dei-prompt-promptsmd) sotto.
2. **Esplora** la cartella con `FileSystem.ListFiles` prima di scrivere, per non sovrascrivere
   lavoro esistente. Se nel messaggio c'è una URL, la scarica con `Web.FetchUrl` per leggere
   specifiche/documentazione.
3. **Pianifica** in 2-3 frasi cosa costruirà.
4. **Scaffolda** con `dotnet new` (`DotNet.RunDotnetNew`, `--framework net10.0`), dentro `src/`.
5. **Implementa** scrivendo i file sorgente con `FileSystem.WriteFile`.
6. **Compila** con `DotNet.RunBuild` puntato su `src/`; in caso di errori, corregge e ricompila
   (fino a 10 iterazioni).
7. **Testa** con `DotNet.RunTest`; corregge finché i test non passano (fino a 10 iterazioni).
8. **Riporta** un riepilogo di cosa ha costruito e come eseguirlo.

I progetti `dotnet` e `html` usano entrambi un layout `src/` (codice) · `ops/` (config/deployment)
dentro la cartella target. La documentazione (`README.md`, `USAGE.md`, `ARCHITECTURE.md`,
`ROADMAP.md`) vive sempre alla **radice** della cartella target, mai in una sottocartella `docs/`
— stessa convenzione di questo repo. Il context `markdown` segue la stessa convenzione quando
l'obiettivo è documentare un progetto software. `PROMPTS.md` è l'eccezione: viene sempre
mantenuto, in ogni context, a partire dalla prima richiesta (vedi sotto).

### Log dei prompt (`PROMPTS.md`)

Ogni context, in ogni progetto generato, mantiene `PROMPTS.md` alla radice della cartella
target: un log numerato e cronologico di ogni richiesta ricevuta dall'agente per quel progetto,
con una riga di riepilogo di cosa è stato fatto in risposta. Viene aggiornato come primo passo di
ogni turno, prima di qualunque altra azione — così riflette l'intento anche se il resto del turno
fallisce. A differenza degli altri quattro file (`README.md`/`USAGE.md`/`ARCHITECTURE.md`/
`ROADMAP.md`, creati solo se ha senso per le dimensioni del progetto), `PROMPTS.md` non è
opzionale: è quello che rende la storia di un progetto tracciabile fino alle richieste che
l'hanno guidata.

Ogni chiamata a strumento è visibile in tempo reale quando `F8`/`--debug` è attivo:

```text
⏺ DotNet.RunBuild(projectPath=)
    ⎿  $ dotnet build  Exit code: 0 ...
```

## Esempi di prompt

```text
Crea una console app che calcola i numeri di Fibonacci, con un metodo Fibonacci(int n)
testato con xUnit per i casi 0, 1, 10 e un input negativo che deve lanciare un'eccezione.
```

```text
Leggi la spec su https://example.com/api-spec.md e genera un client HTTP tipizzato
per quell'API, con test che usano un HttpMessageHandler finto.
```

```text
Aggiungi un endpoint minimal API GET /health che ritorna 200 OK, senza rompere
la build esistente.
```

## Immagini (JPG/PNG)

L'agente ha accesso a un set di tool `Image.*` in ogni context, senza bisogno di configurazione:

- **`Image.GetInfo`** — dimensioni, formato, dimensione file.
- **`Image.Resize`**, **`Image.Crop`**, **`Image.Rotate`**, **`Image.Flip`**,
  **`Image.Grayscale`**, **`Image.ConvertFormat`** — manipolazione di file `.jpg`/`.jpeg`/`.png`
  già presenti nella cartella target (es. ridimensionare uno screenshot, convertire un asset da
  PNG a JPG).

Non c'è bisogno di chiederli esplicitamente: l'agente li usa quando la richiesta lo richiede
(es. "ridimensiona logo.png a 256x256").

**`Image.GenerateAI`** (opzionale) genera una nuova immagine — logo, icona, illustrazione — da
una descrizione testuale, tramite un modello di generazione immagini (DALL-E/gpt-image) su Azure
AI Foundry o OpenAI. Richiede `ImageModelId` configurato sotto la sezione del provider attivo
(vedi [README.md](README.md#configure)); se assente, il tool semplicemente non è registrato —
nessun impatto sul resto dell'app. Non disponibile con `AIProvider` `anthropic` (nessuna API di
generazione immagini). La generazione può richiedere qualche secondo: mentre è in corso, la
descrizione del tool resta visibile nella riga di stato (spinner), e con `F8`/`--debug` attivo
compaiono anche le righe `⏺`/`⎿` con il prompt e l'esito — lo stesso meccanismo di visibilità
delle tool-call di [Cosa fa l'agente](#cosa-fa-lagente-a-ogni-richiesta-context-dotnet).

## Log della sessione

Ogni turno (utente, agente, tool call) viene scritto in `<target>/.ralfai/session.md`. Al riavvio
di RalfAI sulla stessa cartella, la sessione precedente viene ricaricata automaticamente (tool
call e blocchi "thinking" esclusi dalla history rimandata al modello). Nella stessa cartella
`.ralfai/` vive anche `config.yaml`, che ricorda il context e il flag debug di quella cartella.

## Voce (Speech-to-Text / Text-to-Speech)

Richiede una risorsa **Azure AI Speech** configurata in `appsettings.json`:

```json
"AzureSpeech": {
  "Region": "westeurope",
  "ApiKey": "<speech-api-key>",
  "RecognitionLanguage": "it-IT",
  "VoiceName": "it-IT-IsabellaNeural"
}
```

(anche qui vale l'override via `RALFAI_AzureSpeech__Region` /
`RALFAI_AzureSpeech__ApiKey`). Se `Region`/`ApiKey` restano vuoti, `F2`/`F3` (o `/voice`/`/speak`
in modalità pipe) rispondono con un messaggio d'errore invece di funzionare — il resto dell'app
non è impattato.

- **`F2`** (interattivo) / **`/voice`** (pipe) — attiva il microfono, ascolta finché non rileva
  una pausa, e aggiunge il testo riconosciuto come nuova riga nel buffer corrente. Puoi combinarlo
  con testo digitato a mano nello stesso messaggio, prima di inviare con `Ctrl+Enter`/`/exe`.
- **`F3`** (interattivo) / **`/speak`** (pipe) — legge ad alta voce l'ultima risposta finale
  dell'agente (quella già mostrata a schermo).
- `RecognitionLanguage` (default `it-IT`) e `VoiceName` (default `it-IT-IsabellaNeural`) sono
  configurabili per usare un'altra lingua/voce fra quelle supportate da Azure AI Speech.

## Configurazione rate limiting

In `appsettings.json`, sotto `AzureAIFoundry.RateLimits` (usata per il pacing indipendentemente
dal provider AI attivo — vedi [README.md](README.md#choosing-a-provider) per la scelta del
provider):

| Chiave | Significato | Default |
|---|---|---|
| `RequestsPerMinute` | limite RPM dell'endpoint | 20 |
| `TokensPerMinute` | limite TPM dell'endpoint | 20000 |
| `SafetyFactor` | margine di sicurezza sul pacing (1.25 = spazia le richieste come se il limite fosse il 25% più stretto) | 1.25 |

Su risposta 429 il client fa retry (fino a 3 tentativi) rispettando l'header `Retry-After` se
presente, altrimenti attende 60s.

## Troubleshooting

- **`AzureAIFoundry:Endpoint missing...`** — manca `Endpoint`/`ApiKey` in `appsettings.json` o
  nelle variabili d'ambiente `RALFAI_AzureAIFoundry__Endpoint` /
  `RALFAI_AzureAIFoundry__ApiKey` (solo se `AIProvider` è `foundry`, il default — vedi
  [README.md](README.md)).
- **`Unknown context '<nome>'`** — il nome passato a `--context` non è registrato; i nomi
  disponibili sono elencati nel messaggio d'errore (o con `ralf --list-contexts`).
- **Build/test che non convergono dopo 10 iterazioni** — l'agente si ferma e spiega il problema
  residuo nella risposta finale invece di continuare all'infinito.
- **`dotnet` non trovato** durante l'esecuzione (context `dotnet`) — verifica che il .NET SDK sia
  nel `PATH` del processo che esegue RalfAI.
- **`Voice non configurata...`** su `F2`/`F3` (o `/voice`/`/speak` in modalità pipe) — manca
  `Region`/`ApiKey` sotto `AzureSpeech` in `appsettings.json` (o le relative variabili
  d'ambiente).
- **`Nessun parlato riconosciuto.`** — il microfono non ha catturato audio comprensibile entro il
  timeout di silenzio dell'SDK; riprova parlando più vicino al microfono o subito dopo aver
  attivato la dettatura.
- **`Could not open '<comando>'...`** su `F4` nel file browser — il comando configurato sotto
  `Editor` (default `code.exe`) non è nel `PATH` o non esiste; correggi `Editor` in
  `appsettings.json` (o `RALFAI_Editor`) con il comando giusto per il tuo editor.


---



---

### TOOL — DotNet.RunCommand — 17:17:27

args: arguments=new list, workingDirectory=src
result: $ dotnet new list
Working dir: D:\Dev\2026\repos\RalfAI\src
Exit code: 0
--- STDOUT ---
These templates matched your input: 

Template Name                                 Short Name                    Language    Tags                              
--------------------------------------------  ----------------------------  ----------  ----------------------------------
AI Chat Web App                               aichatweb                     [C#]        Common/AI/Web/Blazor/.NET Aspire  
API Controller                                apicontroller                 [C#]        Web/ASP.NET                       
ASP.NET Core Empty                            web                           [C#],F#     Web/Empty                         
ASP.NET Core gRPC Service                     grpc                          [C#]        Web/gRPC/API/Service              
ASP.NET Core Web API                          webapi                        [C#],F#     Web/WebAPI/Web API/API/Service    
ASP.NET Core Web API (native AOT)             webapiaot                     [C#]        Web/Web API/API/Service           
ASP.NET Core Web App (Model-View-Controller)  mvc                           [C#],F#     Web/MVC                           
ASP.NET Core Web App (Razor Pages)            webapp,razor                  [C#]        Web/MVC/Razor Pages               
ASP.NET Core with Angular                     angular                       [C#]        Web/MVC/SPA                       
ASP.NET Core with React.js                    react                         [C#]        Web/MVC/SPA                       
ASP.NET Core with React.js and Redux          reactredux                    [C#]        Web/MVC/SPA                       
Blazor Server App                             blazorserver                  [C#]        Web/Blazor                        
Blazor Web App                                blazor                        [C#]        Web/Blazor/WebAssembly            
Blazor Web App                                mudblazor                     [C#]        Web/Blazor/WebAssembly/MudBlazor  
Blazor WebAssembly Standalone App             blazorwasm                    [C#]        Web/Blazor/WebAssembly/PWA        
Class Library                                 classlib                      [C#],F#,VB  Common/Library                    
Console App                                   console                       [C#],F#,VB  Common/Console                    
dotnet gitattributes file                     gitattributes,.gitattributes              Config                            
dotnet gitignore file                         gitignore,.gitignore                      Config                            
Dotnet local tool manifest file               tool-manifest                             Config                            
EditorConfig file                             editorconfig,.editorconfig                Config                            
global.json file                              globaljson,global.json                    Config                            
MSBuild Directory.Build.props file            buildprops                                MSBuild/props                     
MSBuild Directory.Build.targets file          buildtargets                              MSBuild/props                     
MSBuild Directory.Packages.props file         packagesprops                             MSBuild/packages/props/CPM        
MSTest Playwright Test Project                mstest-playwright             [C#]        Test/MSTest/Playwright/Desktop/Web
MSTest Test Class                             mstest-class                  [C#],F#,VB  Test/MSTest                       
MSTest Test Project                           mstest                        [C#],F#,VB  Test/MSTest/Desktop/Web           
MVC Controller                                mvccontroller                 [C#]        Web/ASP.NET                       
MVC ViewImports                               viewimports                   [C#]        Web/ASP.NET                       
MVC ViewStart                                 viewstart                     [C#]        Web/ASP.NET                       
NuGet Config                                  nugetconfig,nuget.config                  Config                            
NUnit Playwright Test Project                 nunit-playwright              [C#]        Test/NUnit/Playwright/Desktop/Web 
NUnit Test Item                               nunit-test                    [C#],F#,VB  Test/NUnit                        
NUnit Test Project                            nunit                         [C#],F#,VB  Test/NUnit/Desktop/Web            
Protocol Buffer File                          proto                                     Web/gRPC                          
Razor Class Library                           razorclasslib                 [C#]        Web/Razor/Library                 
Razor Component                               razorcomponent                [C#]        Web/ASP.NET                       
Razor Page                                    page                          [C#]        Web/ASP.NET                       
Razor View                                    view                          [C#]        Web/ASP.NET                       
Solution File                                 sln,solution                              Solution                          
Solution Filter File                          slnf,solutionfilter                       Solution                          
Web Config                                    webconfig                                 Config                            
Windows Forms App                             winforms                      [C#],VB     Common/WinForms                   
Windows Forms Class Library                   winformslib                   [C#],VB     Common/WinForms                   
Windows Forms Control Library                 winformscontrollib            [C#],VB     Common/WinForms                   
Worker Service                                worker                        [C#],F#     Common/Worker/Web                 
WPF Application                               wpf                           [C#],VB     Common/WPF                        
WPF Class Library                             wpflib                        [C#],VB     Common/WPF                        
WPF Custom Control Library                    wpfcustomcontrollib           [C#],VB     Common/WPF                        
WPF User Control Library                      wpfusercontrollib             [C#],VB     Common/WPF                        
xUnit Test Project                            xunit                         [C#],F#,VB  Test/xUnit/Desktop/Web


---

### TOOL — DotNet.RunCommand — 17:17:27

args: arguments=tool run dotnet-script -- --version, workingDirectory=src
result: $ dotnet tool run dotnet-script -- --version
Working dir: D:\Dev\2026\repos\RalfAI\src
Exit code: 1
--- STDERR ---
Cannot find a tool in the manifest file that has a command named 'dotnet-script'.


---

