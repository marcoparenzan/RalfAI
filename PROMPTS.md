# Prompts

Log cronologico e numerato delle richieste che hanno guidato la realizzazione di RalfAI — la
stessa convenzione (`PROMPTS.md` alla radice, aggiornato a ogni obiettivo, prima di qualunque
altra azione) che i context `dotnet`/`html`/`markdown` ora applicano a ogni progetto che
generano. Vedi [ARCHITECTURE.md](ARCHITECTURE.md) e [USAGE.md](USAGE.md).

> Le voci 1–8 sono ricostruite dai risultati verificabili e dalla precedente storia di Claude;
> le successive derivano dalle richieste conservate nelle sessioni del progetto.

## 1. Realizzare un agente di coding autonomo per .NET `[ricostruito]`

Costruire un agente CLI che scaffoldi, implementi, compili e testi progetti .NET correggendosi
fino al successo.

## 2. Rendere il context non hardcoded `[ricostruito]`

Introdurre contesti selezionabili e configurabili senza modificare l'applicazione principale.

## 3. Aggiungere Speech-to-Text / Text-to-Speech `[ricostruito]`

Aggiungere dettatura e lettura delle risposte tramite Azure AI Speech.

## 4. Estrarre un motore condiviso dalla console `[ricostruito]`

Separare `AgentEngine` dal rendering Spectre.Console per consentire frontend riutilizzabili.

## 5. Distribuire come global tool e rendere la cartella opzionale `[ricostruito]`

Distribuire il comando `ralf`, aggiungere il file browser e ricordare l'ultima cartella.

## 6. Sessione interattiva a schermo intero `[ricostruito]`

Aggiungere buffer alternato, input multi-riga, tasti funzione e interruzione dei turni.

## 7. Supportare più provider AI `[ricostruito]`

Supportare Azure AI Foundry, OpenAI e Anthropic attraverso le integrazioni .NET ufficiali.

## 8. Scrivere la documentazione del progetto `[ricostruito]`

Creare README, architettura, guida d'uso e roadmap.

## 9. "Il file USE riporta la app desktop che non c'è"

Riallineata la guida all'applicazione CLI realmente presente.

## 10. "Anche la roadmap è disallineata"

Rimossa l'app desktop dalle funzionalità completate e spostata tra le ipotesi future.

## 11. "Puoi fare un aggiornamento ai contesti? I files md stanno sempre in radice, non in docs. In generale sempre: README USAGE ARCHITECTURE ROADMAP. Puoi aggiungere un supporto alla manipolazione di immagini jpg e png"

Standardizzata la documentazione alla radice e aggiunti strumenti ImageSharp per JPG/PNG.

## 12. "Per un progetto puoi anche generare un logo?"

Aggiunta generazione immagini AI opzionale per Foundry/OpenAI.

## 13. "Inoltre, quando sta generando, puoi dare un dettaglio di quello che sta pensando scrivendo generando? Come un log?"

Resa visibile la generazione tramite stato e tool-call di debug.

## 14. "Hai aggiornato anche il comando?"

Verificato il packaging del global tool con le nuove dipendenze.

## 15. "Puoi aggiungere anche un file md con i PROMPTS? che in qualche modo rispecchia la realizzazione intera del progetto stesso"

Aggiunto `PROMPTS.md` come log obbligatorio degli obiettivi.

## 16. "Aggiungi la possibilità di aprire un file durante la selezione: default code.exe, altrimenti variabile nel file di config"

Aggiunta apertura nell'editor esterno configurabile dal file browser.

## 17. "Puoi aggiornare l'help con un po' tutti i comandi e verificare che tutta la documentazione sia a posto?"

Aggiornato l'help e corretti collegamenti e riferimenti documentali.

## 18. "Vuoi creare il logo del progetto? Una immagine?" — "procedi"

Creati logo e icona SVG/PNG e integrato il marchio nel README.

## 19. "Vorrei che l'editor da CLI permetta di editare la richiesta con maggior ricchezza....non posso cancellare ad esempio...e quando incollo da clipboard, perdo caratteri. usa i filesd claude per la storia dello sviluppo. E convertila in una tua storia"

Aggiunto un editor con cursore, cancellazione, navigazione e incolla atomico; creata una storia
autonoma in `DEVELOPMENT-STORY.md` usando i materiali precedenti come fonti.

## 20. "puoi registrare il tool aggiornato come tool di dotnet?"

Compilato, testato, pacchettizzato e registrato RalfAI 1.1.1 come tool locale .NET; preparato
l'aggiornamento globale da completare fuori dal processo in uso.

## 21. "Nel codice di Ralf, pouoi dare un feedback più dettagliato di qurllo che sta facendo e epnsando? Puoi dare una visibilità del costo della richiesta? La API lo supporta? Nella CLI, riesci a mettere la versione dell'applicativo?"

Aggiungerò feedback operativo dettagliato e tempi, esporrò token e costo stimato quando i
provider restituiscono usage, documentando i limiti dell'API; mostrerò inoltre la versione nella
CLI e tramite opzione dedicata, con test per i calcoli non banali.

## 22. "Allora. non so se tutti gli aggiornamenti che ti avevo chiesto li hai pubblicati. 1. l'editor non mi da feedback con un cursone e non posso muovermi con il cursore per correggenre quanto ho già scritto 2. vorrei vedere un log progresso mentre stai pensando ...vedere di più quello che sta succedendo 3. avere un feedback sui costi dopo ogni richiesta e magari un log della storia dei costi 4. vorrei una hybrid app blazor che incorpori una visualizzazione del file system e un editor (monaco) per modificare editare visualizzare i files 5. una preview per i markdown 6. vedere la versione di Ralf quando apro il prompt 7. avere la registrazione sempre come dotnet tool"

Verificherò l'implementazione effettiva senza assumere che i cambi precedenti esistano, completerò editor, telemetria e storico costi, realizzerò una Blazor Hybrid app con file tree, Monaco e preview Markdown, mostrerò la versione all'avvio e automatizzerò packaging/installazione del tool .NET.

## 23. "come faccio a far partire studio?"

Verificherò progetto e documentazione per fornire il comando di avvio effettivo e gli eventuali prerequisiti.

## 24. "Puoi deployarlo come dotnet tool?"

Renderò RalfAI Studio installabile come tool .NET separato, con comando dedicato, packaging e installazione verificati.

## 25. "lancia studio"

Avvierò il tool globale RalfAI Studio sulla cartella corrente del progetto e verificherò che il processo venga lanciato correttamente.

## 26. "ralf-studio ... The system cannot find the file specified"

Riprodurrò e correggerò il packaging del launcher: verificherò il contenuto del pacchetto e l'estrazione del payload, aggiungerò diagnosi robuste e rigenererò/installlerò il tool prima di provarne realmente l'avvio.

## 27. "il problema è che se scrivo nella ui di studio, quando clicco non fa niente non riesco a selezionare niente, l'albero non si contrae o espande, non posso cliccare alcun file e vederne il contenuto, men che meno avere la preview markdown"

Diagnosticherò il circuito interattivo Blazor Hybrid e l'interoperabilità Monaco, correggerò eventi di albero, apertura file, editing e preview Markdown, quindi ripacchettizzerò e proverò il tool aggiornato.
