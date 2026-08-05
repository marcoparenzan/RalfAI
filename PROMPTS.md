# Prompts

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
