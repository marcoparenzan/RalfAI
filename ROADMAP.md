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

### Global tool (`ralf`) e cartella target opzionale
`RalfAI` si installa come [.NET global tool](https://learn.microsoft.com/dotnet/core/tools/global-tools)
(`ralf`). L'argomento posizionale con la cartella è opzionale: senza, si apre un file browser a
schermo intero (`FileBrowser`) e l'ultima cartella usata viene ricordata (`LastFolderStore`) per i
riavvii successivi. Nuovi context oltre a `dotnet`: `markdown` (default) e `html` (con tool
`StaticServer` per servire l'app su HTTP locale). Dettagli in [USE.md](USE.md) e
[README.md](README.md).

### Sessione interattiva a schermo intero
La console gira nello screen buffer alternato del terminale, con composizione multi-riga
(`Ctrl+Enter` per inviare) e tasti funzione (`F1`-`F10`) al posto della vecchia lista di comandi
`/exe`, `/cancel`, `/history`: cambio context (`F6`), cambio cartella (`F7`), toggle debug (`F8`),
dettatura/lettura vocale (`F2`/`F3`). I comandi testuali (`/exe`, `/switch-context`, …) restano
disponibili solo quando lo stdin è rediretto (input da pipe/script). Dettagli in
[USE.md](USE.md#sessione-interattiva).

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
[README.md](README.md#scegliere-un-provider) e [ARCHITECTURE.md](ARCHITECTURE.md).

## 📋 Pianificato

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
