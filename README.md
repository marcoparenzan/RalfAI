# RalfAI

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
