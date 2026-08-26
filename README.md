# RalfAI

<p align="center"><img src="assets/ralfai-logo.svg" alt="RalfAI" width="680"></p>

RalfAI è un coding agent .NET 10 disponibile come CLI e come **RalfAI Studio**, applicazione
Blazor Hybrid Windows con file tree, editor Monaco e anteprima Markdown.

## Installazione dei tool

CLI:

```powershell
.\ops\install-tool.ps1
ralf --version
```

Studio:

```powershell
.\ops\install-studio-tool.ps1
ralf-studio --version
ralf-studio D:\progetto
```

Gli script creano i pacchetti Release e installano o aggiornano i global tool. Per un manifest
locale aggiungere `-Local` e usare `dotnet tool run ralf` oppure `dotnet tool run ralf-studio`.

## Avvio senza installazione

```bash
ralf [cartella]
dotnet run --project src/RalfAI.Studio/RalfAI.Studio -- [cartella]
```

La CLI mostra la versione sopra ogni prompt. L'editor supporta cursore visibile, frecce,
Home/End, Backspace/Delete e incolla atomico con `Ctrl+V`; `Ctrl+Enter` invia. Durante un turno
mostra fasi operative e tool call (persistenti con `F8`). Dopo la risposta mostra token e costo
stimato; ogni turno è registrato in `.ralfai/cost-history.jsonl`. `F9` (o `/files`) apre un file
manager a doppio pannello in stile Norton Commander, con viewer di file scorrevole.

Il costo dipende dalle tariffe configurate sotto `Pricing:<provider>:<model>` e non è una fattura:
le chat API normalmente restituiscono i token, non l'importo monetario.

Vedi [USAGE.md](USAGE.md), [ARCHITECTURE.md](ARCHITECTURE.md) e [PROMPTS.md](PROMPTS.md).
