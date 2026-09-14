using Microsoft.Win32;

namespace UnboundOS.Infrastructure.Tools;

/// <summary>
/// Read-only install probes: known paths, uninstall registry, Start Menu.
/// Never scrapes account data or credentials.
/// </summary>
public static class DesktopAppLocator
{
    public static string? FindFirstExisting(IEnumerable<string?> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            try
            {
                var path = candidate.Trim().Trim('"');
                if (File.Exists(path))
                {
                    return Path.GetFullPath(path);
                }
            }
            catch
            {
                // Ignore malformed candidates.
            }
        }

        return null;
    }

    public static IEnumerable<string> ProgramRoots()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var x86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(x86))
        {
            yield return x86;
        }

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return local;
        yield return Path.Combine(local, "Programs");
    }

    public static IEnumerable<string> Combine(IEnumerable<string> roots, params string[] relative)
    {
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            foreach (var path in relative)
            {
                yield return Path.Combine(root, path);
            }
        }
    }

    public static IEnumerable<string> UninstallExecutables(params string[] displayContains)
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
                        if (string.IsNullOrWhiteSpace(display) ||
                            !displayContains.Any(token =>
                                display.Contains(token, StringComparison.OrdinalIgnoreCase)))
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

    public static IEnumerable<string> StartMenuExecutables(params string[] exeFileNames)
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
            foreach (var exeName in exeFileNames)
            {
                IEnumerable<string> files;
                try
                {
                    files = Directory.EnumerateFiles(root, exeName, SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }

                IEnumerable<string> links;
                try
                {
                    var linkName = Path.ChangeExtension(exeName, ".lnk");
                    links = Directory.EnumerateFiles(root, linkName, SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

                foreach (var link in links)
                {
                    yield return Path.Combine(Path.GetDirectoryName(link) ?? string.Empty, exeName);
                }
            }
        }
    }

    public static IEnumerable<string> NewestAppFolderExecutables(string parent, string exeName)
    {
        if (!Directory.Exists(parent))
        {
            yield break;
        }

        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateDirectories(parent, "app-*")
                .OrderByDescending(Directory.GetLastWriteTimeUtc);
        }
        catch
        {
            yield break;
        }

        foreach (var directory in children)
        {
            yield return Path.Combine(directory, exeName);
        }
    }

    public static IEnumerable<string> ExpandInstallLocation(string? location, params string[] exeNames)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            yield break;
        }

        var trimmed = location.Trim().Trim('"');
        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            yield return trimmed;
            yield break;
        }

        foreach (var exe in exeNames)
        {
            yield return Path.Combine(trimmed, exe);
        }
    }
}
