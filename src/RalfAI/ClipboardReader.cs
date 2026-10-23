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
                StandardOutputEncoding = System.Text.Encoding.UTF8,
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
            return ("powershell.exe", "-NoProfile -NonInteractive -Command \"[Console]::OutputEncoding=[Text.Encoding]::UTF8; Get-Clipboard -Raw\"");
        if (OperatingSystem.IsMacOS()) return ("pbpaste", "");
        return ("sh", "-c \"command -v wl-paste >/dev/null && wl-paste --no-newline || xclip -selection clipboard -o\"");
    }

    /// <summary>
    /// Image on the clipboard, if any: a bitmap (screenshot, copied image) is saved as a PNG under
    /// <c>&lt;targetFolder&gt;/.ralfai/attachments</c>; copied image files (Explorer) are returned as they are.
    /// Best effort: Windows (always) and Linux with wl-paste/xclip; null/empty elsewhere.
    /// </summary>
    public static List<string> TryReadImages(string targetFolder)
    {
        try
        {
            var folder = Path.Combine(targetFolder, ".ralfai", "attachments");
            var file = Path.Combine(folder, $"clip-{DateTime.Now:yyyyMMdd-HHmmss}.png");
            if (OperatingSystem.IsWindows()) return ReadWindowsImages(folder, file);
            if (OperatingSystem.IsLinux()) return ReadLinuxImage(folder, file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
        }
        return [];
    }

    private static List<string> ReadWindowsImages(string folder, string file)
    {
        Directory.CreateDirectory(folder);
        var quoted = file.Replace("'", "''");
        var script = $$"""
            Add-Type -AssemblyName System.Windows.Forms, System.Drawing
            if ([System.Windows.Forms.Clipboard]::ContainsImage()) {
                $image = [System.Windows.Forms.Clipboard]::GetImage()
                $image.Save('{{quoted}}', [System.Drawing.Imaging.ImageFormat]::Png)
                '{{quoted}}'
            } elseif ([System.Windows.Forms.Clipboard]::ContainsFileDropList()) {
                [System.Windows.Forms.Clipboard]::GetFileDropList() | ForEach-Object { $_ }
            }
            """;

        var info = new ProcessStartInfo("powershell.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-STA", "-Command", script }) info.ArgumentList.Add(arg);

        using var process = Process.Start(info);
        if (process is null) return [];
        var output = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(8000)) { try { process.Kill(); } catch (InvalidOperationException) { } return []; }

        return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(path => ImageAttachments.IsImagePath(path) && File.Exists(path))
            .ToList();
    }

    private static List<string> ReadLinuxImage(string folder, string file)
    {
        Directory.CreateDirectory(folder);
        var info = new ProcessStartInfo("sh")
        {
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.ArgumentList.Add("-c");
        info.ArgumentList.Add("(command -v wl-paste >/dev/null && wl-paste --type image/png || xclip -selection clipboard -t image/png -o) > \"$1\" 2>/dev/null");
        info.ArgumentList.Add("sh");
        info.ArgumentList.Add(file);

        using var process = Process.Start(info);
        if (process is null || !process.WaitForExit(5000)) return [];
        var written = new FileInfo(file);
        if (process.ExitCode == 0 && written.Exists && written.Length > 0) return [file];
        if (written.Exists) written.Delete();
        return [];
    }
}
