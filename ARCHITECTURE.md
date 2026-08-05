# Architecture

## Project layout

```
RalfAI.Abstractions       IAgentContext — the only thing every other project depends on
RalfAI.Core                the engine: AgentEngine, RateLimiter, ConversationLogger,
                            generic tools (FileSystem, Web, AI, Image, Speech), voice
RalfAI.Contexts.DotNet     IAgentContext for .NET projects + the DotNet tool
RalfAI.Contexts.Markdown   IAgentContext for Markdown doc sets
RalfAI.Contexts.HtmlApps   IAgentContext for static HTML/CSS/JS apps + the StaticServer tool
RalfAI                     console frontend (Spectre.Console) and composition root
```

Dependency direction is strictly one-way: `RalfAI.Abstractions` ← `RalfAI.Core` ←
`RalfAI.Contexts.*` ← `RalfAI` (the exe). Nothing downstream is referenced by anything
upstream — a context project doesn't know the exe exists, and Core doesn't know any
particular context exists. `ContextRegistry` (in the exe) is the one place that knows about
every context, precisely because it's the composition root.

## The provider abstraction

RalfAI is built on [Microsoft Agent Framework](https://learn.microsoft.com/agent-framework/),
whose central type is `AIAgent` — a provider-agnostic wrapper around
`Microsoft.Extensions.AI.IChatClient` that adds instructions, tools, and automatic
tool-calling. Each AI provider is reached through *that provider's own official .NET SDK*,
not a shared connector:

| Provider | SDK | Entry point |
| --- | --- | --- |
| Azure AI Foundry / Azure OpenAI | `Azure.AI.OpenAI` | `AzureOpenAIClient(endpoint, key).GetChatClient(model).AsIChatClient()` |
| OpenAI | `OpenAI` (official SDK) | `OpenAIClient(key).GetChatClient(model).AsIChatClient()` |
| Anthropic | `Anthropic` (official SDK, via `Microsoft.Agents.AI.Anthropic`) | `AnthropicClient { ApiKey }.AsIChatClient(model)` |

`AgentEngine.BuildChatClient` (in `RalfAI.Core/AgentEngine.cs`) is the single switch point —
it reads `AIProvider` from config and returns an `IChatClient` plus the resolved model id.
Everything downstream (`AsAIAgent`, tools, middleware, sessions) is 100% provider-agnostic;
adding a fourth provider means adding one case to that switch and nothing else.

### Adding a provider

1. Add that provider's NuGet package to `RalfAI.Core.csproj`.
2. Add a `BuildXxx(IConfiguration)` method next to `BuildFoundry`/`BuildOpenAI`/`BuildAnthropic`
   in `AgentEngine.cs`, returning `(IChatClient, string modelId)`.
3. Add a case for it in `BuildChatClient`'s `switch`.
4. Document the new `AIProvider` value and its config section in `README.md`.

Image generation (`ImageGenerationService`, `RalfAI.Core/ImageGeneration/`) is a separate
concern with its own provider switch keyed off the same `AIProvider` value — adding a chat
provider doesn't automatically add image-generation support for it; that needs its own
`FromXxx(IConfiguration)` case in `ImageGenerationService.FromConfig`, built on that
provider's `ImageClient`-equivalent API (if it has one).

## The plugin (tool) system

There's no "plugin" type — a tool is just a C# method with a `[Description]` attribute (from
`System.ComponentModel`), turned into an `AIFunction` with
`AIFunctionFactory.Create(instance.Method, name: "...")`:

```csharp
[Description("Read the full content of a file in the target project folder.")]
public Task<string> ReadFile(
    [Description("Relative path from project root")] string relativePath)
{ ... }
```

```csharp
AIFunctionFactory.Create(fs.ReadFile, name: "FileSystem_ReadFile")
```

**Naming**: tool names can't contain dots (OpenAI/Anthropic reject them), so each tool is
registered as `Plugin_Method` (e.g. `FileSystem_ReadFile`) and converted back to the familiar
`Plugin.Method` display form (`AgentEngine.FriendlyToolName`) wherever it's shown to a human —
the debug tool-call line, the session log, the system prompt's tool list.

**Two kinds of tools**:

- **Context tools** — specific to one target stack, returned from
  `IAgentContext.GetTools(targetFolder)`. E.g. `DotNetContext` returns `FileSystem_*` +
  `DotNet_*` + `Web_FetchUrl`; `HtmlAppContext` adds `StaticServer_Start`/`Stop` on top.
- **Engine tools** — available in *every* context regardless of stack, registered directly
  in `AgentEngine.CreateAsync`: `AI_Ask` (delegate an isolated sub-question to the same
  chat client); `Image_GetInfo`/`Resize`/`Crop`/`Rotate`/`Flip`/`Grayscale`/`ConvertFormat`
  (JPG/PNG manipulation via `ImagePlugin`, `SixLabors.ImageSharp` — always registered, no
  config needed); `Image_GenerateAI` (text-to-image via `ImageGenerationPlugin` +
  `ImageGenerationService`, only when `ImageModelId` is configured under the active
  provider — Anthropic has no image-generation API); and, when `AzureSpeech` is configured,
  `Speech_Speak`/`Speech_Listen`.

Both kinds end up in the same `List<AITool>` passed to `chatClient.AsAIAgent(tools: ...)` —
the model can't tell them apart, and doesn't need to.

### Adding a tool

1. Add a method with `[Description]` on itself and its parameters to an existing plugin
   class (`RalfAI.Core/Plugins/*.cs` for a generic one, or a context project's `Plugins/`
   folder for a stack-specific one) — or a new plugin class if it doesn't fit an existing one.
2. Wrap it with `AIFunctionFactory.Create(instance.Method, name: "Plugin_Method")` in the
   owning context's `GetTools` (or in `AgentEngine.CreateAsync` if it should be universal).
3. Mention it in that context's `BuildSystemPrompt` "Available Tools" list, so the model
   knows it exists and roughly when to reach for it.

### Tool-call visibility

A single piece of Agent Framework *function-calling middleware*, attached via
`agent.AsBuilder().Use(...)` in `AgentEngine.CreateAsync`, wraps every tool invocation:
before calling `next()` it fires `ToolCallStarted` (name, description, argument preview) and
after it fires `ToolCallCompleted` (result) and appends a line to the session log. The
console frontend (`RalfAIApp`) subscribes to both to drive the "thinking…" spinner label and,
when `F8` debug mode is on, the permanent `⏺ …` / `⎿ …` lines.

## Turn processing and conversation state

Agent Framework's own `AgentSession` (the framework type — not to be confused with this
project's `AgentEngine`) tracks conversation state on the client side once created via
`agent.CreateSessionAsync()`; each `RunAsync` call appends to it automatically. `AgentEngine`
doesn't duplicate that state — it only keeps its own lightweight `List<AgentMessage>` for UI
display (message counts, etc.) and the markdown transcript on disk.

**Resuming a session**: `ConversationLogger.LoadExistingHistory()` parses
`<folder>/.ralfai/session.md` back into `(role, content)` pairs. On the *first* `SendAsync`
call after startup, if that list isn't empty, `AgentEngine` sends it as a
`List<ChatMessage>` (prior turns + the new user message) through the one `RunAsync` overload
that accepts a full message list — seeding the framework's session with the prior transcript
in a single call. Every call after that just sends the new user text; the session already
has full context.

**Rate limiting / 429s**: `RateLimiter` (unchanged, provider-agnostic — pure token-bucket
pacing based on a rough char-count token estimate) throttles before every call. Detecting a
429 to trigger its backoff has to be provider-aware, since each SDK throws its own exception
type: Azure OpenAI and OpenAI both surface `System.ClientModel.ClientResultException` with a
`.Status`, Anthropic throws `Anthropic.Exceptions.AnthropicRateLimitException`. `AgentEngine.Is429`
checks both, plus a message-text fallback ("429", "rate limit", "rate_limit") for anything
that slips through either path.

## The `.ralfai/` folder

Written inside the *target* folder (not the tool's install directory):

- `session.md` — human-readable transcript (USER/AGENT/TOOL blocks), replayed on resume.
- `config.yaml` — remembers that folder's `context:` and `debug:` settings (see
  `FolderConfigStore`), so `ralf <folder>` with no other flags picks up where you left off.

Not to be confused with `PROMPTS.md` (written at the target folder **root**, not inside
`.ralfai/`): `.ralfai/session.md` is the engine's own full-fidelity log (every turn, every
tool call), not part of the deliverable; `PROMPTS.md` is a curated, project-facing history
that each context's system prompt instructs the model to maintain as one of its outputs — see
[USAGE.md](USAGE.md#log-dei-prompt-promptsmd).

## Console frontend

`RalfAIApp` + `InputCollector` + `FileBrowser` are Spectre.Console-based and know nothing
about AI providers or tools — they only see `AgentEngine`'s public surface (`SendAsync`,
events, `Messages`). The whole interactive session runs inside the terminal's alternate
screen buffer (`FullScreenConsole`), the same mechanism `vim`/`less`/`htop` use, so nothing
leaks into the user's normal scrollback.

`FileBrowser` (F9-to-pick UI, used both at startup with no CLI folder and via F7 mid-session)
has one app-level side effect independent of `AgentEngine`: F4 shell-launches the selected
file in an external editor via `Process.Start(UseShellExecute: true)`, fire-and-forget. The
command comes from the `Editor` config key (`RALFAI_Editor` env override), resolved by the
caller (`Program.cs`/`RalfAIApp.SwitchFolder`) and passed into `FileBrowser.Pick` — `FileBrowser`
itself has no `IConfiguration` dependency, it just takes a plain string, defaulting to
`FileBrowser.DefaultEditor` (`code.exe`) if the key is unset.
