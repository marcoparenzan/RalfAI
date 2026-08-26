using Microsoft.Extensions.DependencyInjection;

namespace RalfAI.Studio;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "unknown";
        if (args.Any(arg => arg is "--version" or "-v"))
        {
            Console.WriteLine($"RalfAI Studio {version}");
            return;
        }

        if (args.Any(arg => arg is "--help" or "-h"))
        {
            Console.WriteLine($"RalfAI Studio {version}");
            Console.WriteLine("Usage: ralf-studio [workspace]");
            return;
        }

        ApplicationConfiguration.Initialize();
        var root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        var services = new ServiceCollection();
        services.AddWindowsFormsBlazorWebView();
        services.AddSingleton(new WorkspaceService(root));
        using var provider = services.BuildServiceProvider();
        Application.Run(new MainForm(provider));
    }
}
