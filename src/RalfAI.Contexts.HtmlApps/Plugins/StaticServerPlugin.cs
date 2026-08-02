using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace RalfAI.Plugins;

/// <summary>
/// Serves the target folder's src/ directory over plain HTTP, so the agent (and the user) can
/// preview the static HTML/CSS/JS app in a real browser. Opening index.html directly via
/// file:// breaks ES module imports (`&lt;script type="module"&gt;`), which is exactly what
/// this context's convention uses — a real HTTP origin is required.
///
/// One server per plugin instance (one per session): calling Start again while already running
/// just returns the existing URL. The server is not explicitly stopped when the session ends —
/// it goes away with the process. Switching context/folder mid-session (F6/F7) can leave a
/// previous instance's server running until the app exits; call Stop first if that matters.
/// </summary>
public sealed class StaticServerPlugin(string targetFolder) : IAsyncDisposable
{
    private WebApplication? _app;
    private string? _url;

    [Description(
        "Start a local static file server for the app's src/ folder and return its URL " +
        "(e.g. http://127.0.0.1:5173/). Use this to preview the app in a real browser — opening " +
        "index.html directly via file:// breaks <script type=\"module\"> imports. Calling this " +
        "again while already running just returns the existing URL.")]
    public async Task<string> Start(
        [Description("Preferred TCP port; 0 (default) picks any free port")] int port = 0)
    {
        if (_app is not null && _url is not null)
            return _url;

        var root = Path.Combine(targetFolder, "src");
        if (!Directory.Exists(root))
            return $"Error: {root} does not exist yet — write src/index.html first.";

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        var fileProvider = new PhysicalFileProvider(root);
        _app = builder.Build();
        // UseDefaultFiles needs the same FileProvider explicitly — without it, it looks for
        // index.html in the default web root instead of src/, silently does nothing, and
        // UseStaticFiles then 404s on "/" since it doesn't serve directory listings.
        _app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        _app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });

        await _app.StartAsync();
        _url = _app.Urls.First();
        return _url;
    }

    [Description("Stop the local preview server started with StaticServer.Start, if one is running.")]
    public async Task<string> Stop()
    {
        if (_app is null) return "Not running.";

        await _app.StopAsync();
        await _app.DisposeAsync();
        _app = null;
        _url = null;
        return "Stopped.";
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
