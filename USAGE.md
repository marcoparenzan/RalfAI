# Guida all'uso

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
[README.md](README.md#dove-vive-il-file-di-configurazione)).

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
| `F2` | rinomina l'elemento selezionato |
| `F7` | crea una nuova cartella |
| `F8` / `Del` | elimina (chiede conferma) |
| `F1` | mostra questa tabella |
| `F10` / `Esc` | annulla e torna indietro |

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

1. **Esplora** la cartella con `FileSystem.ListFiles` prima di scrivere, per non sovrascrivere
   lavoro esistente. Se nel messaggio c'è una URL, la scarica con `Web.FetchUrl` per leggere
   specifiche/documentazione.
2. **Pianifica** in 2-3 frasi cosa costruirà.
3. **Scaffolda** con `dotnet new` (`DotNet.RunDotnetNew`, `--framework net10.0`), dentro `src/`.
4. **Implementa** scrivendo i file sorgente con `FileSystem.WriteFile`.
5. **Compila** con `DotNet.RunBuild` puntato su `src/`; in caso di errori, corregge e ricompila
   (fino a 10 iterazioni).
6. **Testa** con `DotNet.RunTest`; corregge finché i test non passano (fino a 10 iterazioni).
7. **Riporta** un riepilogo di cosa ha costruito e come eseguirlo.

I progetti `dotnet` e `html` usano entrambi un layout `src/` (codice) · `docs/` (documentazione) ·
`ops/` (config/deployment) dentro la cartella target.

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
dal provider AI attivo — vedi [README.md](README.md#scegliere-un-provider) per la scelta del
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
