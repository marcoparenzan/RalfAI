# La storia di RalfAI

Questa è la mia ricostruzione della storia del progetto. Non è una copia dei resoconti di Claude:
li usa come fonte insieme a `PROMPTS.md`, `.ralfai/session.md`, alla documentazione e al codice
presente. Dove le richieste originali non sono sopravvissute, la sequenza è dichiaratamente
ricostruita dai risultati verificabili.

## Dall'automazione .NET a un agente

RalfAI nasce da un obiettivo pratico: consegnare a un agente una cartella e una richiesta, e
lasciargli compiere l'intero ciclo di sviluppo .NET. Non soltanto suggerire codice, ma creare i
progetti, scrivere i file, eseguire build e test e correggersi finché il risultato è verde. Il
primo nucleo univa una CLI Spectre.Console, un contesto .NET e Microsoft Agent Framework con
Azure AI Foundry.

Il limite di quel primo nucleo era evidente: il contesto .NET era incorporato nell'applicazione.
La prima evoluzione architetturale ha quindi introdotto un registro di contesti. Da quel momento
il motore non ha più coinciso con un singolo stack: sono arrivati i contesti Markdown e HTML,
selezionabili da configurazione o riga di comando.

## Un motore separato dal terminale

La seconda trasformazione importante è stata separare il comportamento dell'agente dalla sua
interfaccia. `AgentEngine` è diventato il punto di coordinamento indipendente dalla UI, mentre la
CLI si è concentrata su rendering, input e feedback. Gli eventi per tool call e notice hanno
permesso al terminale di raccontare ciò che accade senza contaminare il motore con Spectre.Console.
Questa scelta ha reso possibile cambiare frontend senza riscrivere provider, strumenti, sessioni
o rate limiting.

Nello stesso periodo RalfAI è diventato un global tool, invocabile come `ralf`. La cartella target
è diventata opzionale grazie a un file browser che ricorda l'ultima destinazione. La sessione si è
spostata nel buffer alternato del terminale, con composizione multi-riga, tasti funzione e
interruzione dei turni in corso.

## Provider, voce e strumenti

Il progetto ha poi smesso di dipendere da un solo fornitore. Il motore può costruire client per
Azure AI Foundry, OpenAI e Anthropic usando le rispettive integrazioni .NET. In parallelo sono
arrivate dettatura e lettura tramite Azure Speech, mantenute opzionali perché una funzione
accessoria non impedisca l'uso del resto dell'applicazione.

La stessa filosofia ha guidato gli strumenti. Il contesto .NET gestisce scaffold, build e test;
il contesto HTML può avviare un'anteprima HTTP; gli strumenti comuni leggono e scrivono file,
scaricano URL e manipolano immagini. La generazione AI di immagini è registrata soltanto quando
il provider e il modello configurato la supportano.

## Quando la documentazione è diventata memoria

Una verifica successiva ha scoperto che la documentazione raccontava anche un'app desktop che
nel repository non esisteva. README, guida e roadmap sono stati riallineati al codice reale. Da
questa correzione è nata una regola più forte: la documentazione dei progetti generati vive alla
radice con nomi canonici, mentre `PROMPTS.md` conserva in ordine gli obiettivi che hanno guidato
il lavoro.

I resoconti di Claude hanno fornito la prima ricostruzione delle fasi iniziali e il dettaglio delle
richieste successive. Questa storia li converte in una narrazione autonoma: non attribuisce il
progetto a un singolo assistente, ma distingue le richieste dell'utente, le decisioni emerse e gli
artefatti che il repository permette di verificare.

## Un'interazione più umana

Dopo file browser, voce, immagini e identità visiva, il limite più concreto era rimasto nel punto
più usato: l'editor del prompt. Il primo input multi-riga accumulava caratteri in fondo alle righe;
non aveva un vero cursore, cancellazione in avanti o navigazione, e l'incolla veloce poteva perdere
caratteri mentre il display live si aggiornava.

L'editor è quindi diventato un buffer testabile con cursore. Ora supporta inserimento nel mezzo,
Backspace e Delete, frecce, Home/End, movimento per parola e incolla esplicito da clipboard come
un'unica operazione. È un passaggio coerente con l'intera evoluzione di RalfAI: partire da una
funzione che lavora e trasformarla, attraverso l'uso reale, in uno strumento che si lascia usare.

## Fonti

- `PROMPTS.md`: cronologia degli obiettivi, con le prime voci marcate come ricostruite.
- `.ralfai/session.md`: trascrizione operativa disponibile nel repository.
- `README.md`, `USAGE.md`, `ARCHITECTURE.md`, `ROADMAP.md`: stato e decisioni documentate.
- `src/`: implementazione verificabile delle tappe descritte.
- cronologia Git: riscontro temporale delle modifiche versionate.
