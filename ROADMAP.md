# Roadmap

Stato delle iniziative macro per RalfAI. Aggiornata al 2026-07-27.

## ✅ Fatto

### Documentazione
README, architettura, guida all'uso e questa roadmap (questo set di documenti).

### Context non più hardcoded
Prima `Program.cs` istanziava `new DotNetContext()` direttamente. Ora `AgentContextFactory`
risolve il context per nome (`--context <nome>` sulla riga di comando, oppure chiave
`AgentContext` in config/env), con `dotnet` come default. `DotNetContext` resta l'unica
implementazione — è quella che serve oggi — ma nessuna parte dell'app oltre `Program.cs` dipende
da .NET specificamente: aggiungere uno stack nuovo è un'implementazione di `IAgentContext` più una
riga nella factory, senza toccare `RalfAIApp`. Dettagli in
[ARCHITECTURE.md](ARCHITECTURE.md#contexts--astrazione-multi-stack).

### Speech-to-Text / Text-to-Speech con Azure AI Speech
Implementato con `Microsoft.CognitiveServices.Speech` (Azure Speech SDK), in
[Voice/SpeechService.cs](../src/RalfAI.Core/Voice/SpeechService.cs) (motore condiviso):

- **`/voice`** (dentro `InputCollector`, console) e **bottone 🎤** (`ChatView`, Desktop) —
  ascoltano dal microfono di default, si fermano da soli alla pausa (`RecognizeOnceAsync`).
- **`/speak`** (console) e **bottone 🔊** (`ChatView`, Desktop) — leggono ad alta voce l'ultima
  risposta finale dell'agente (`AgentSession.LastFinalText`).
- Configurazione opzionale sotto `AzureSpeech` in `appsettings.json` (region, key, lingua di
  riconoscimento, voce); se assente, `SpeechService.FromConfig` ritorna `null` e le funzioni
  rispondono con un messaggio d'errore invece di far fallire l'avvio — nessun impatto sul resto
  dell'app. Dettagli in [USE.md](USE.md#voce-speech-to-text--text-to-speech) e
  [ARCHITECTURE.md](ARCHITECTURE.md).

### Motore condiviso (`RalfAI.Core`) ed estrazione di `AgentSession`
Prerequisito per avere più frontend senza duplicare la logica dell'agente. `RalfAIApp`
mischiava Semantic Kernel, rate limiting, logging e voce con il rendering Spectre.Console; è stato
estratto tutto in `AgentSession` (progetto `RalfAI.Core`, nessuna dipendenza UI), che espone
`SendAsync` più gli eventi `ToolCallStarted`/`ToolCallCompleted`/`Notice` per farsi renderizzare da
qualsiasi frontend. La console (`RalfAI`) ora è un consumer sottile di `AgentSession`, non è
cambiato nulla nel suo comportamento visibile. Dettagli in [ARCHITECTURE.md](ARCHITECTURE.md).

### Interfaccia Desktop — WinForms + WebView2 (Blazor Hybrid)
App separata (`RalfAI.Desktop`), non sostituisce la console. Scelta deliberata: invece di
HTML/JS scritto a mano dentro un `WebView2` nudo, è **Blazor Hybrid** — WinForms che ospita un
`BlazorWebView` (che internamente usa comunque WebView2), con la chat scritta come componenti
Razor in `RalfAI.ChatUI` (Razor Class Library, dipende solo da `RalfAI.Core`). Il
motivo: quegli stessi componenti si potranno riusare tali e quali in una futura Blazor Server,
invece di riscrivere la UI due volte.

- `Program.cs` chiede la cartella di destinazione con un `FolderBrowserDialog` nativo all'avvio
  (non c'è ancora un selettore dentro la UI web — vedi sotto), costruisce `AgentSession` e la
  registra in un `ServiceCollection` insieme a `AddWindowsFormsBlazorWebView()`.
- `ChatView.razor` mostra messaggi, pannello "Activity" per le tool-call, input con invio/annulla,
  e i bottoni voce se `Session.Speech` non è `null` — qui il microfono/speaker sono quelli della
  macchina locale (il processo WinForms gira sul PC dell'utente), quindi la voce funziona come in
  console. Questo *non* varrà automaticamente per Blazor Server, vedi sotto.
- Configurazione propria (`RalfAI.Desktop/appsettings.json`, stesso schema della console).

Rifiniture rimandate (non richieste esplicitamente, ma annotate perché emerse durante
l'implementazione):
- Scegliere/cambiare la cartella di destinazione **dentro** la UI invece del dialog nativo iniziale.
- Persistenza/ripristino della finestra, icona, packaging (oggi è un semplice `dotnet run`).

## 📋 Pianificato

### Interfaccia Blazor Server
Riuserà `RalfAI.ChatUI` (gli stessi componenti Razor dell'app Desktop) dietro un host Blazor
Server invece che WinForms — non richiede di riscrivere la UI, ma introduce problemi nuovi:

- **Voce**: `SpeechService` è riusabile lato server, ma in Blazor Server il processo gira sul
  server, non sul browser dell'utente — il microfono/speaker di `RecognizeOnceAsync`/`SpeakAsync`
  sarebbero quelli del server, non quelli dell'utente. Serve catturare l'audio nel browser (JS
  interop / MediaRecorder) e inviarlo al backend, oppure usare lo Speech SDK client-side; per il
  TTS, riprodurre l'audio sintetizzato nel browser. I bottoni 🎤/🔊 di `ChatView` andranno adattati
  o resi condizionali all'host.
- Single-session vs. multi-utente/multi-sessione nella UI web (oggi `AgentSession` è pensata per
  un solo utente alla volta, come la console e il Desktop).
- Se il log `.chat/session.md` resta il meccanismo di persistenza o va sostituito.

### Irrobustire la gestione della API key
`appsettings.json` contiene oggi la API key Azure in chiaro — sia in `RalfAI/appsettings.json`
sia, ora, nella copia in `RalfAI.Desktop/appsettings.json` (stesso schema, stessa chiave, per
comodità di sviluppo locale). Rischioso se il repo viene condiviso o versionato. Il codice supporta
già l'override via variabile d'ambiente `RALFAI_AzureAIFoundry__ApiKey` in entrambe le app;
manca solo il passo di:
- rimuovere la key da entrambi gli `appsettings.json` (o sostituirla con un placeholder),
- documentare `dotnet user-secrets` come alternativa locale,
- aggiungere un `.gitignore`/verifica quando il repo verrà messo sotto controllo di versione.

Rimandato su richiesta esplicita — non ancora prioritario.

## Non pianificato (idee aperte, non richieste esplicitamente)
- Altri context (`IAgentContext`) per stack diversi da .NET (Python, Node, script di shell…): la
  factory è pronta ad accoglierli, ma nessuno è stato richiesto finora.
- Autenticazione/multi-utente per la futura UI Blazor.
