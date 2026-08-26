# Architettura

- `src/RalfAI`: frontend CLI Spectre.Console e pacchetto `dotnet tool`.
- `src/RalfAI.Core`: Agent Framework, provider, strumenti, metriche e storico costi.
- `src/RalfAI.Studio`: host WinForms + BlazorWebView (Blazor Hybrid), file tree, Monaco e preview.
- `src/RalfAI.Contexts.*`: contesti DotNet, Markdown e HTML.
- `src/RalfAI.Tests`: test xUnit di buffer, metriche e persistenza costi.
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
cartella target) e `NortonCommander` (`F9`/`/files`, file manager a doppio pannello con
copia/sposta/rinomina/mkdir/delete/apertura esterna) — nessuna dipendenza da `IConfiguration`:
il comando dell'editor esterno (chiave `Editor`) viene risolto dai chiamanti e passato come
stringa.
