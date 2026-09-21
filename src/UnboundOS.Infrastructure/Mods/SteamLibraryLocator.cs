using Microsoft.Win32;

namespace UnboundOS.Infrastructure.Mods;

/// <summary>
/// Shared Steam library-root discovery. Read-only; never touches credentials.
/// </summary>
internal static class SteamLibraryLocator
{
    public static IReadOnlyList<string> Find(
        IEnumerable<string>? extraFolders = null,
        bool useRegistry = true)
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (useRegistry)
        {
            AddIfPresent(roots, ReadRegistry(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath"));
            AddIfPresent(roots, ReadRegistry(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"));
            AddIfPresent(roots, ReadRegistry(Registry.LocalMachine, @"SOFTWARE\Valve\Steam", "InstallPath"));
        }

        if (extraFolders is not null)
        {
            foreach (var folder in extraFolders)
            {
                AddIfPresent(roots, folder);
            }
        }

        var primaryRoots = roots.ToArray();
        foreach (var root in primaryRoots)
        {
            var librariesPath = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(librariesPath))
            {
                continue;
            }

            ValveNode? libraries = null;
            try
            {
                libraries = ValveKeyValuesParser.Parse(File.ReadAllText(librariesPath)).Child("libraryfolders");
            }
            catch (IOException)
            {
                // Keep the primary Steam root.
            }

            if (libraries is null)
            {
                continue;
            }

            foreach (var child in libraries.Children)
            {
                AddIfPresent(roots, child.Value("path"));
            }
        }

        return roots.ToArray();
    }

    private static string? ReadRegistry(RegistryKey hive, string subKey, string valueName)
    {
        try
        {
            return hive.OpenSubKey(subKey)?.GetValue(valueName) as string;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
        {
            return null;
        }
    }

    private static void AddIfPresent(ISet<string> paths, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var normalized = path.Trim();
            if (OperatingSystem.IsWindows())
            {
                normalized = normalized.Replace('/', '\\');
                while (normalized.Contains(@"\\", StringComparison.Ordinal)
                       && !normalized.StartsWith(@"\\", StringComparison.Ordinal))
                {
                    normalized = normalized.Replace(@"\\", @"\", StringComparison.Ordinal);
                }

                if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
                {
                    paths.Add(normalized.TrimEnd('\\'));
                    return;
                }
            }
            else
            {
                normalized = normalized.Replace('\\', '/');
            }

            paths.Add(Path.GetFullPath(normalized));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // Ignore malformed library paths from a partial Steam install.
        }
    }
}
