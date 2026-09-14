using Microsoft.Win32;

namespace UnboundOS.Infrastructure.Mods;

/// <summary>
/// Locates Vortex.exe and optionally reads state via <c>--get</c> when Vortex is not running.
/// Never uses --set / --del / --restore / --merge, and never opens state.v2 directly.
/// </summary>
public static class VortexLocator
{
    public static bool IsRunning()
    {
        try
        {
            return System.Diagnostics.Process.GetProcessesByName("Vortex").Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public static string? FindExecutable()
    {
        foreach (var candidate in EnumerateCandidateExecutables())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    public static IEnumerable<string> EnumerateCandidateExecutables()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        var candidates = new List<string>();
        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var normalized = path.Trim().Trim('"');
            if (!normalized.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                normalized = Path.Combine(normalized, "Vortex.exe");
            }

            if (seen.Add(normalized))
            {
                candidates.Add(normalized);
            }
        }

        Add(Path.Combine(local, "Programs", "Black Tree Gaming Ltd", "Vortex", "Vortex.exe"));
        Add(Path.Combine(local, "Programs", "Vortex", "Vortex.exe"));
        Add(Path.Combine(local, "Vortex", "Vortex.exe"));
        Add(Path.Combine(programFiles, "Black Tree Gaming Ltd", "Vortex", "Vortex.exe"));
        Add(Path.Combine(programFiles, "Vortex", "Vortex.exe"));
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            Add(Path.Combine(programFilesX86, "Black Tree Gaming Ltd", "Vortex", "Vortex.exe"));
            Add(Path.Combine(programFilesX86, "Vortex", "Vortex.exe"));
        }

        foreach (var fromRegistry in ReadUninstallLocations())
        {
            Add(fromRegistry);
        }

        foreach (var shortcut in FindStartMenuShortcuts())
        {
            Add(shortcut);
        }

        return candidates;
    }

    public static string? TryGetState(string executable, string statePath, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(executable) || string.IsNullOrWhiteSpace(statePath) || IsRunning())
        {
            return null;
        }

        if (LooksLikeWriteSwitch(statePath))
        {
            return null;
        }

        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = executable,
                Arguments = $"--get {Quote(statePath)}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return null;
            }

            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
        }
        catch
        {
            return null;
        }
    }

    public static string BuildLaunchArguments(string? gameId, string? profileId)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(gameId))
        {
            parts.Add($"--game {Quote(gameId)}");
        }

        if (!string.IsNullOrWhiteSpace(profileId))
        {
            parts.Add($"--profile {Quote(profileId)}");
        }

        return string.Join(" ", parts);
    }

    public static bool LooksLikeWriteSwitch(string argument) =>
        argument.Contains("--set", StringComparison.OrdinalIgnoreCase) ||
        argument.Contains("--del", StringComparison.OrdinalIgnoreCase) ||
        argument.Contains("--restore", StringComparison.OrdinalIgnoreCase) ||
        argument.Contains("--merge", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> ReadUninstallLocations()
    {
        string[] keys =
        [
            @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        ];

        foreach (var hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            foreach (var keyPath in keys)
            {
                RegistryKey? root = null;
                try
                {
                    root = hive.OpenSubKey(keyPath);
                }
                catch
                {
                    continue;
                }

                if (root is null)
                {
                    continue;
                }

                using (root)
                {
                    foreach (var name in root.GetSubKeyNames())
                    {
                        using var sub = root.OpenSubKey(name);
                        if (sub is null)
                        {
                            continue;
                        }

                        var display = sub.GetValue("DisplayName") as string;
                        var publisher = sub.GetValue("Publisher") as string;
                        if (!IsVortexUninstall(display, publisher))
                        {
                            continue;
                        }

                        if (sub.GetValue("DisplayIcon") is string icon)
                        {
                            yield return icon.Split(',')[0];
                        }

                        if (sub.GetValue("InstallLocation") is string install)
                        {
                            yield return install;
                        }
                    }
                }
            }
        }
    }

    private static bool IsVortexUninstall(string? display, string? publisher)
    {
        if (string.IsNullOrWhiteSpace(display) ||
            !display.Contains("Vortex", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(publisher) ||
               publisher.Contains("Black Tree", StringComparison.OrdinalIgnoreCase) ||
               publisher.Contains("Nexus", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> FindStartMenuShortcuts()
    {
        var roots = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                "Programs"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                "Programs")
        };

        foreach (var root in roots.Where(Directory.Exists))
        {
            IEnumerable<string> links;
            try
            {
                links = Directory.EnumerateFiles(root, "Vortex.lnk", SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            foreach (var link in links)
            {
                var sibling = Path.Combine(Path.GetDirectoryName(link) ?? string.Empty, "Vortex.exe");
                if (File.Exists(sibling))
                {
                    yield return sibling;
                }
            }
        }
    }

    private static string Quote(string value) =>
        value.Any(char.IsWhiteSpace) ? $"\"{value}\"" : value;
}
