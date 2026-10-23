using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace RalfAI.Plugins;

/// <summary>
/// Cargo/rustup access for the Rust context. An existing Rust install (on PATH, in $CARGO_HOME or
/// ~/.cargo) is used as it is. When there is none, the official rustup is downloaded, verified
/// against its published SHA-256 and installed into a folder owned by RalfAI
/// (<see cref="LocalHome"/>) - nothing is added to the user's profile or PATH.
/// </summary>
public sealed class RustPlugin(string rootFolder)
{
    private const int MaxOutputChars = 30_000;

    /// <summary>
    /// Where RalfAI keeps its own Rust install when none is found: <c>~/.ralfai/rust</c>, or the
    /// folder in the RALFAI_RUST_HOME environment variable.
    /// </summary>
    public static string LocalHome =>
        Environment.GetEnvironmentVariable("RALFAI_RUST_HOME") is { Length: > 0 } custom
            ? Path.GetFullPath(custom)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ralfai", "rust");

    private static string LocalCargoHome => Path.Combine(LocalHome, "cargo");
    private static string LocalRustupHome => Path.Combine(LocalHome, "rustup");

    public static string ResolveDirectory(string root, string? relative)
    {
        root = Path.GetFullPath(root);
        var full = Path.GetFullPath(Path.Combine(root, relative ?? ""));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!full.Equals(root, comparison) && !full.StartsWith(Path.TrimEndingDirectorySeparator(root) + Path.DirectorySeparatorChar, comparison))
            throw new UnauthorizedAccessException("Path outside workspace.");
        return full;
    }

    /// <summary>The rustup-init target triple for a platform, or null when rustup has no build for it.</summary>
    public static string? RustupTriple(OSPlatform platform, Architecture architecture)
    {
        if (platform == OSPlatform.Windows)
            return architecture switch { Architecture.X64 => "x86_64-pc-windows-msvc", Architecture.Arm64 => "aarch64-pc-windows-msvc", Architecture.X86 => "i686-pc-windows-msvc", _ => null };
        if (platform == OSPlatform.OSX)
            return architecture switch { Architecture.X64 => "x86_64-apple-darwin", Architecture.Arm64 => "aarch64-apple-darwin", _ => null };
        if (platform == OSPlatform.Linux)
            return architecture switch { Architecture.X64 => "x86_64-unknown-linux-gnu", Architecture.Arm64 => "aarch64-unknown-linux-gnu", _ => null };
        return null;
    }

    /// <summary>The hash from a <c>.sha256</c> file next to a rustup download ("&lt;hash&gt; *rustup-init.exe").</summary>
    public static string? ParseChecksum(string content)
    {
        var token = content.Split([' ', '\t', '\r', '\n', '*'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return token is { Length: 64 } && token.All(Uri.IsHexDigit) ? token.ToLowerInvariant() : null;
    }

    // ── Toolchain discovery ──────────────────────────────────────────────────

    private sealed record Toolchain(string BinDir, bool IsLocal, string Description);

    private static string ExecutableName(string name) => name + (OperatingSystem.IsWindows() ? ".exe" : "");

    private static string? FindOnPath(string name)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                var candidate = Path.Combine(dir, ExecutableName(name));
                if (File.Exists(candidate)) return Path.GetDirectoryName(candidate);
            }
            catch (ArgumentException) { /* malformed PATH entry */ }
        }
        return null;
    }

    private static Toolchain? FindToolchain()
    {
        var userCargo = Environment.GetEnvironmentVariable("CARGO_HOME") is { Length: > 0 } home
            ? home
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cargo");

        if (FindOnPath("cargo") is { } onPath) return new Toolchain(onPath, IsLocal: false, $"system install ({onPath})");

        var userBin = Path.Combine(userCargo, "bin");
        if (File.Exists(Path.Combine(userBin, ExecutableName("cargo")))) return new Toolchain(userBin, false, $"user install ({userBin})");

        var localBin = Path.Combine(LocalCargoHome, "bin");
        if (File.Exists(Path.Combine(localBin, ExecutableName("cargo")))) return new Toolchain(localBin, true, $"RalfAI-local install ({LocalHome})");

        return null;
    }

    // ── Tools ────────────────────────────────────────────────────────────────

    [Description("Check cargo and rustc. If Rust is missing, download the official rustup (checksum-verified) and install stable Rust " +
                 "into RalfAI's own folder (~/.ralfai/rust) - nothing is added to the user's profile or PATH. Requires Internet and ~1 GB of disk; " +
                 "Windows additionally needs the MSVC C++ Build Tools for linking. Safe to call repeatedly.")]
    public async Task<string> EnsureToolchain()
    {
        var found = FindToolchain();
        if (found is not null)
        {
            var check = await Verify(found);
            if (check.Ok) return $"Rust ready - {found.Description}\n{check.Text}{await LinkerHint()}";

            // cargo is there (rustup proxy) but no toolchain is selected: install stable.
            var fix = await Run(found, "rustup", ["toolchain", "install", "stable", "--profile", "default"], rootFolder);
            if (fix.Code == 0) await Run(found, "rustup", ["default", "stable"], rootFolder);
            check = await Verify(found);
            return check.Ok ? $"Rust ready - {found.Description}\n{check.Text}{await LinkerHint()}" : fix.Text + check.Text;
        }

        var installed = await InstallRustup();
        if (installed is not null) return installed;

        found = FindToolchain();
        if (found is null) return "rustup finished but cargo was not found afterwards.";
        var verification = await Verify(found);
        return (verification.Ok ? $"Rust installed - {found.Description}\n" : "Installation finished but verification failed:\n") + verification.Text + await LinkerHint();
    }

    [Description("Run cargo with an array of arguments (new, init, build, test, fmt, clippy, run, add, install...). Installs Rust first if it is missing. " +
                 "workingDirectory is relative to the workspace. Returns the exit code and (truncated) stdout/stderr.")]
    public async Task<string> RunCargo(string[] arguments, string? workingDirectory = null)
    {
        var dir = ResolveDirectory(rootFolder, workingDirectory);
        var toolchain = FindToolchain();
        if (toolchain is null || !(await Verify(toolchain)).Ok)
        {
            var report = await EnsureToolchain();
            toolchain = FindToolchain();
            if (toolchain is null || !(await Verify(toolchain)).Ok) return report;
        }

        Directory.CreateDirectory(dir);
        return (await Run(toolchain, "cargo", arguments, dir)).Text;
    }

    [Description("Run rustup with an array of arguments, to download extra Rust tools the project needs: " +
                 "['component','add','clippy'], ['target','add','wasm32-unknown-unknown'], ['toolchain','install','nightly'], ['update']. " +
                 "Installs Rust first if it is missing. Only add what the task requires and tell the user what was downloaded.")]
    public async Task<string> RunRustup(string[] arguments)
    {
        var toolchain = FindToolchain();
        if (toolchain is null)
        {
            var report = await EnsureToolchain();
            toolchain = FindToolchain();
            if (toolchain is null) return report;
        }
        return (await Run(toolchain, "rustup", arguments, rootFolder)).Text;
    }

    // ── Installation ─────────────────────────────────────────────────────────

    /// <summary>Downloads, verifies and runs rustup-init. Returns an error text, or null on success.</summary>
    private async Task<string?> InstallRustup()
    {
        var platform = OperatingSystem.IsWindows() ? OSPlatform.Windows : OperatingSystem.IsMacOS() ? OSPlatform.OSX : OSPlatform.Linux;
        var triple = RustupTriple(platform, RuntimeInformation.OSArchitecture);
        if (triple is null) return $"Rust installation is not supported on {RuntimeInformation.OSDescription} / {RuntimeInformation.OSArchitecture}.";

        var fileName = OperatingSystem.IsWindows() ? "rustup-init.exe" : "rustup-init";
        var url = $"https://static.rust-lang.org/rustup/dist/{triple}/{fileName}";
        var temp = Path.Combine(Path.GetTempPath(), "ralf-rust-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
            var bytes = await http.GetByteArrayAsync(url);
            var expected = ParseChecksum(await http.GetStringAsync(url + ".sha256"));
            var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (expected is null) return $"Could not read the published checksum for {url}; not running the installer.";
            if (expected != actual) return $"Checksum mismatch for {url} (expected {expected}, got {actual}); not running the installer.";

            var installer = Path.Combine(temp, fileName);
            await File.WriteAllBytesAsync(installer, bytes);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(installer, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            Directory.CreateDirectory(LocalCargoHome);
            Directory.CreateDirectory(LocalRustupHome);
            var local = new Toolchain(Path.Combine(LocalCargoHome, "bin"), IsLocal: true, "RalfAI-local install");
            var result = await RunFile(local, installer,
                ["-y", "--profile", "default", "--default-toolchain", "stable", "--no-modify-path"], rootFolder);
            return result.Code == 0 ? null : $"rustup-init failed.\n{result.Text}";
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or TaskCanceledException or UnauthorizedAccessException)
        {
            return $"Rust installation failed: {ex.Message}";
        }
        finally
        {
            try { Directory.Delete(temp, true); } catch (IOException) { }
        }
    }

    private async Task<(bool Ok, string Text)> Verify(Toolchain toolchain)
    {
        var cargo = await Run(toolchain, "cargo", ["--version"], rootFolder);
        var rustc = await Run(toolchain, "rustc", ["--version"], rootFolder);
        return (cargo.Code == 0 && rustc.Code == 0, cargo.Text + rustc.Text);
    }

    /// <summary>On Windows, Rust links with the MSVC linker; say so up front when it is not installed.</summary>
    private static async Task<string> LinkerHint()
    {
        if (!OperatingSystem.IsWindows()) return "";
        try
        {
            var vswhere = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? "", "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (File.Exists(vswhere))
            {
                var info = new ProcessStartInfo(vswhere) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                foreach (var arg in new[] { "-products", "*", "-requires", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64", "-latest", "-property", "installationPath" })
                    info.ArgumentList.Add(arg);
                using var process = Process.Start(info)!;
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (process.ExitCode == 0 && output.Trim().Length > 0) return "";
            }
        }
        catch (Exception ex) when (ex is Win32Exception or IOException) { }
        return "\nNote: the MSVC C++ build tools were not detected. Rust needs them to link on Windows: " +
               "install 'Visual Studio Build Tools' with the 'Desktop development with C++' workload (RalfAI does not install it).\n";
    }

    // ── Process helpers ──────────────────────────────────────────────────────

    private static Task<(int Code, string Text)> Run(Toolchain toolchain, string tool, string[] arguments, string directory)
        => RunFile(toolchain, Path.Combine(toolchain.BinDir, ExecutableName(tool)), arguments, directory);

    private static async Task<(int Code, string Text)> RunFile(Toolchain? toolchain, string executable, string[] arguments, string? rootFolder)
    {
        var info = new ProcessStartInfo(executable)
        {
            WorkingDirectory = rootFolder ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var arg in arguments) info.ArgumentList.Add(arg);

        if (toolchain is not null)
        {
            // Child processes (cargo -> rustc, clippy-driver, linkers) find the toolchain through PATH.
            info.Environment["PATH"] = toolchain.BinDir + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
            if (toolchain.IsLocal)
            {
                info.Environment["CARGO_HOME"] = LocalCargoHome;
                info.Environment["RUSTUP_HOME"] = LocalRustupHome;
            }
        }

        try
        {
            using var process = Process.Start(info)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(15));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(true); return (-1, "Command timed out after 15 minutes."); }
            var text = $"$ {Path.GetFileName(executable)} {string.Join(' ', arguments)}\nExit code: {process.ExitCode}\n{await stdout}\n{await stderr}\n";
            return (process.ExitCode, Truncate(text));
        }
        catch (Win32Exception ex) { return (-1, $"Cannot start {executable}: {ex.Message}\n"); }
    }

    /// <summary>Long build logs go back into the model's context on every later call: keep the head and the tail.</summary>
    public static string Truncate(string text)
    {
        if (text.Length <= MaxOutputChars) return text;
        var head = MaxOutputChars / 3;
        var tail = MaxOutputChars - head;
        return text[..head] + $"\n... [{text.Length - MaxOutputChars:N0} characters omitted] ...\n" + text[^tail..];
    }
}
