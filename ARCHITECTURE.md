# Architettura

- `src/RalfAI`: frontend CLI Spectre.Console e pacchetto `dotnet tool`.
- `src/RalfAI.Core`: Agent Framework, provider, strumenti, metriche e storico costi.
- `src/RalfAI.Studio`: host WinForms + BlazorWebView (Blazor Hybrid), file tree, Monaco e preview.
- `src/RalfAI.Contexts.*`: strumenti specifici per tecnologia (`DotNet`, `Rust`, `StaticServer`, immagini).
- Contesti: file YAML (`ContextStore`, in `src/RalfAI/Profiles`) nella cartella dei contesti e in `<target>/.ralfai/contexts`;
  quelli forniti stanno in `config/contexts` (con `index.yaml`), inclusi nel tool e offerti anche online; `ContextSync`
  li porta nella cartella senza toccare i file modificati dall'utente.
  `ContextRegistry` li elenca a ogni richiesta; l'unico contesto nel codice è `ContextDesignerContext` (`contexts`), che
  crea e modifica gli altri con `ContextsPlugin`. `ToolCatalog` collega i nomi in `tools` agli strumenti.
- `src/RalfAI.Tests`: test xUnit di editor, metriche, strumenti sui file, `/undo`, limiti e del ciclo del turno (vedi sotto).
- `ops/install-tool.ps1`: packaging e install/update globale o locale.

`AgentEngine` espone eventi `Progress`, `ToolCallStarted`, `ToolCallCompleted` e `Notice`: i
frontend mostrano attività osservabile senza ricostruire ragionamenti privati. `TurnMetrics`
legge l'usage propagato dagli SDK e calcola una stima dalle tariffe configurate;
`CostHistoryStore` accoda ogni turno a `.ralfai/cost-history.jsonl`.

La CLI usa `PromptBuffer`, separato e testabile, per modifica multi-riga e navigazione. Studio
usa Blazor dentro un `BlazorWebView`; `WorkspaceService` confina letture/scritture alla root e
Monaco comunica con Razor via JS interop. Markdig genera la preview Markdown.

`DirectoryListing` (elenco/formattazione directory) e `FileViewer` (viewer di file scorrevole,
paginato, con numeri di riga) sono condivisi tra `FileBrowser` (picker a singolo pannello per la
cartella target; il file manager a doppio pannello `NortonCommander` è stato rimosso in favore
delle menzioni `@` nel prompt) — nessuna dipendenza da `IConfiguration`:
il comando dell'editor esterno (chiave `Editor`) viene risolto dai chiamanti e passato come
stringa.

## Test del motore

`AgentEngine.CreateAsync(config, cartella, chiave, context, chatClient)` accetta un `IChatClient` al posto del provider
configurato. I test (`AgentEngineTests`) usano `ScriptedChatClient`, un client finto che risponde da un copione: per ogni
chiamata si decide quali aggiornamenti tornano (testo, ragionamento, chiamate a tool, uso dei token) e si può ispezionare
cosa il motore ha inviato. Così streaming, tetti per turno, `/undo`, `PROMPTS.md`, allegati, annullamento e ripresa della
sessione si verificano in pochi secondi, senza rete né costi. `EngineHarness` crea l'engine su una cartella temporanea
senza pausa tra i turni (`RequestsPerMinute: 0`).
