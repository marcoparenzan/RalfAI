using System.Diagnostics;

namespace RalfAI;

public static class ClipboardReader
{
    public static string? ReadText()
    {
        var (fileName, arguments) = GetCommand();
        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (process is null) return null;
            var text = process.StandardOutput.ReadToEnd();
            process.WaitForExit(3000);
            return process.ExitCode == 0 ? text : null;
        }
        catch
        {
            return null;
        }
    }

    private static (string FileName, string Arguments) GetCommand()
    {
        if (OperatingSystem.IsWindows())
            return ("powershell.exe", "-NoProfile -NonInteractive -Command Get-Clipboard -Raw");
        if (OperatingSystem.IsMacOS()) return ("pbpaste", "");
        return ("sh", "-c \"command -v wl-paste >/dev/null && wl-paste --no-newline || xclip -selection clipboard -o\"");
    }
}
