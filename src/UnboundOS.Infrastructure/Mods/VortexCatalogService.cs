using System.Text.Json;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Mods;

/// <summary>
/// Read-only Vortex discovery. Scans user data and known staging roots.
/// Never opens <c>state.v2</c> (LevelDB) and never writes Vortex files.
/// </summary>
public sealed class VortexCatalogService(VortexDiscoverySettings? settings = null) : IModCatalogService
{
    internal const string StagingMarkerName = "__vortex_staging_folder";
    internal const string AdapterId = "vortex-readonly";

    private static readonly HashSet<string> ReservedFolderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "state", "state.v1", "state.v2", "downloads", "download", "temp", "tmp",
        "plugins", "extensions", "crashinfo", "crashdumps", "logs", "cache",
        "locales", "resources", "node_modules", "application", "store", "settings",
        "metadb", "dump", ".git"
    };

    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["baldursgate3"] = "Baldur's Gate 3",
        ["crimsondesert"] = "Crimson Desert",
        ["cyberpunk2077"] = "Cyberpunk 2077",
        ["deathstranding2onthebeach"] = "Death Stranding 2",
        ["fallout4"] = "Fallout 4",
        ["fallout76"] = "Fallout 76",
        ["helldivers2"] = "Helldivers 2",
        ["mountandblade2bannerlord"] = "Mount & Blade II: Bannerlord"
    };

    private readonly VortexDiscoverySettings _settings = settings ?? new VortexDiscoverySettings();

    public Task<IReadOnlyList<ModGame>> DiscoverAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() => Discover(cancellationToken), cancellationToken);

    internal IReadOnlyList<ModGame> Discover(CancellationToken cancellationToken)
    {
        var games = new Dictionary<string, ModGame>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in EnumerateUserDataRoots())
        {
            cancellationToken.ThrowIfCancellationRequested();
            DiscoverUserDataRoot(root, games);
        }

        foreach (var root in EnumerateStagingRoots())
        {
            cancellationToken.ThrowIfCancellationRequested();
            DiscoverStagingRoot(root, games);
        }

        return games.Values
            .OrderBy(game => game.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private IEnumerable<string> EnumerateUserDataRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddExisting(roots, _settings.UserDataRoots);

        if (_settings.UseDefaultWindowsLocations)
        {
            AddExisting(roots, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Vortex"));
            AddExisting(roots, Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Vortex"));
        }

        return roots;
    }

    private IEnumerable<string> EnumerateStagingRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddExisting(roots, _settings.ExtraStagingRoots);

        if (_settings.UseDefaultWindowsLocations)
        {
            foreach (var path in DefaultExtraStagingRoots())
            {
                AddExisting(roots, path);
            }

            foreach (var recorded in ReadRecordedInstallPaths())
            {
                AddExisting(roots, recorded);
            }
        }

        return roots;
    }

    internal static IEnumerable<string> DefaultExtraStagingRoots()
    {
        yield return @"F:\Vortex Mods";
        yield return @"F:\vmods";

        foreach (var drive in EnumerateReadyDriveLetters())
        {
            yield return $@"{drive}:\Vortex Mods";
            yield return $@"{drive}:\vmods";
        }
    }

    private void DiscoverUserDataRoot(string root, IDictionary<string, ModGame> games)
    {
        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateDirectories(root);
        }
        catch
        {
            return;
        }

        foreach (var directory in children)
        {
            var gameId = Path.GetFileName(directory);
            if (ReservedFolderNames.Contains(gameId) || !LooksLikeGameId(gameId))
            {
                continue;
            }

            var staging = ResolveStagingPath(directory);
            var profiles = ReadProfiles(directory);
            if (staging is null && profiles.Count == 0)
            {
                continue;
            }

            AddOrMerge(games, gameId, staging ?? directory, profiles);
        }
    }

    private void DiscoverStagingRoot(string root, IDictionary<string, ModGame> games)
    {
        if (HasStagingMarker(root))
        {
            var gameId = Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (LooksLikeGameId(gameId) && !ReservedFolderNames.Contains(gameId))
            {
                AddOrMerge(games, gameId, root, []);
            }

            return;
        }

        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateDirectories(root);
        }
        catch
        {
            return;
        }

        foreach (var directory in children)
        {
            var gameId = Path.GetFileName(directory);
            if (ReservedFolderNames.Contains(gameId) || !LooksLikeGameId(gameId))
            {
                continue;
            }

            var staging = ResolveStagingPath(directory) ?? directory;
            AddOrMerge(games, gameId, staging, ReadProfiles(directory));
        }
    }

    private static void AddOrMerge(
        IDictionary<string, ModGame> games,
        string gameId,
        string stagingPath,
        IReadOnlyList<VortexProfileHint> profiles)
    {
        var discovered = CreateGame(gameId, stagingPath, profiles);
        if (!games.TryGetValue(gameId, out var existing))
        {
            games[gameId] = discovered;
            return;
        }

        var richerMods = discovered.Mods.Count >= existing.Mods.Count ? discovered : existing;
        var profileId = discovered.VortexProfileId ?? existing.VortexProfileId;
        var display = richerMods.DisplayName;
        if (profileId is not null &&
            !display.Contains('·') &&
            discovered.DisplayName.Contains('·'))
        {
            display = discovered.DisplayName;
        }

        games[gameId] = richerMods with
        {
            DisplayName = display,
            VortexProfileId = profileId,
            VortexGameId = gameId
        };
    }

    internal static ModGame CreateGame(
        string gameId,
        string stagingPath,
        IReadOnlyList<VortexProfileHint> profiles)
    {
        var mods = DiscoverStagedMods(gameId, stagingPath);
        var profile = profiles
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefault();
        var display = DisplayNameFor(gameId);
        if (!string.IsNullOrWhiteSpace(profile?.Name))
        {
            display = $"{display} · {profile.Name}";
        }

        return new ModGame(
            $"vortex:{gameId}",
            display,
            stagingPath,
            ModProvider.NexusMods,
            ModCapabilities.VortexDiscoveryOnly,
            mods,
            AdapterId,
            VortexGameId: gameId,
            VortexProfileId: profile?.Id);
    }

    internal static IReadOnlyList<InstalledMod> DiscoverStagedMods(string gameId, string stagingPath)
    {
        if (!Directory.Exists(stagingPath))
        {
            return [];
        }

        var mods = new List<InstalledMod>();
        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateDirectories(stagingPath);
        }
        catch
        {
            return [];
        }

        foreach (var directory in children)
        {
            var name = Path.GetFileName(directory);
            if (string.Equals(name, StagingMarkerName, StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith('.') ||
                ReservedFolderNames.Contains(name) ||
                string.Equals(name, "profiles", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "mods", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            DateTimeOffset updated;
            try
            {
                updated = Directory.GetLastWriteTimeUtc(directory);
            }
            catch
            {
                updated = DateTimeOffset.UtcNow;
            }

            mods.Add(new InstalledMod(
                name,
                $"vortex:{gameId}",
                HumanizeModFolder(name),
                ModProvider.NexusMods,
                directory,
                true,
                mods.Count,
                [],
                [],
                ModUpdateState.Unknown,
                UpdatedAt: updated,
                SizeBytes: 0));
        }

        return mods;
    }

    internal static IReadOnlyList<VortexProfileHint> ReadProfiles(string gameDirectory)
    {
        var profilesRoot = Path.Combine(gameDirectory, "profiles");
        if (!Directory.Exists(profilesRoot))
        {
            return [];
        }

        var hints = new List<VortexProfileHint>();
        IEnumerable<string> children;
        try
        {
            children = Directory.EnumerateDirectories(profilesRoot);
        }
        catch
        {
            return [];
        }

        foreach (var directory in children)
        {
            var id = Path.GetFileName(directory);
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            DateTimeOffset updated;
            try
            {
                updated = Directory.GetLastWriteTimeUtc(directory);
            }
            catch
            {
                updated = DateTimeOffset.MinValue;
            }

            hints.Add(new VortexProfileHint(id, TryReadProfileName(directory) ?? id, updated));
        }

        return hints;
    }

    internal static string? ResolveStagingPath(string gameDirectory)
    {
        var mods = Path.Combine(gameDirectory, "mods");
        if (Directory.Exists(mods))
        {
            return mods;
        }

        if (HasStagingMarker(gameDirectory))
        {
            return gameDirectory;
        }

        return null;
    }

    internal static bool HasStagingMarker(string directory)
    {
        try
        {
            return File.Exists(Path.Combine(directory, StagingMarkerName));
        }
        catch
        {
            return false;
        }
    }

    internal static bool LooksLikeGameId(string name) =>
        name.Length >= 3 &&
        char.IsLetter(name[0]) &&
        name.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-');

    internal static string DisplayNameFor(string gameId) =>
        DisplayNames.TryGetValue(gameId, out var name)
            ? name
            : HumanizeModFolder(gameId);

    private IEnumerable<string> ReadRecordedInstallPaths()
    {
        var executable = _settings.FindExecutable();
        if (string.IsNullOrWhiteSpace(executable) || _settings.IsVortexRunning())
        {
            yield break;
        }

        var dump = _settings.TryReadState(executable, "settings.mods.installPath", TimeSpan.FromSeconds(4));
        if (string.IsNullOrWhiteSpace(dump))
        {
            yield break;
        }

        foreach (var path in ExtractPaths(dump))
        {
            yield return path;
        }
    }

    internal static IEnumerable<string> ExtractPaths(string dump)
    {
        try
        {
            using var document = JsonDocument.Parse(dump);
            return WalkStrings(document.RootElement)
                .Where(LooksLikeFilesystemPath)
                .ToArray();
        }
        catch (JsonException)
        {
            return dump.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim().Trim('"'))
                .Where(LooksLikeFilesystemPath)
                .ToArray();
        }
    }

    private static IEnumerable<string> WalkStrings(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var value = element.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    yield return value;
                }

                break;
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var child in WalkStrings(property.Value))
                    {
                        yield return child;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var child in WalkStrings(item))
                    {
                        yield return child;
                    }
                }

                break;
        }
    }

    private static bool LooksLikeFilesystemPath(string value) =>
        value.Length >= 3 &&
        (value.Contains('\\') || value.Contains('/')) &&
        !value.Contains("state.v2", StringComparison.OrdinalIgnoreCase);

    private static string? TryReadProfileName(string profileDirectory)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(profileDirectory, "*.json");
        }
        catch
        {
            return null;
        }

        foreach (var file in files)
        {
            try
            {
                var info = new FileInfo(file);
                if (info.Length is <= 0 or > 8192)
                {
                    continue;
                }

                using var document = JsonDocument.Parse(File.ReadAllText(file));
                if (document.RootElement.ValueKind == JsonValueKind.Object &&
                    document.RootElement.TryGetProperty("name", out var name) &&
                    name.ValueKind == JsonValueKind.String)
                {
                    return name.GetString();
                }
            }
            catch
            {
                // Ignore unreadable or non-JSON sidecar files.
            }
        }

        return null;
    }

    private static string HumanizeModFolder(string folder)
    {
        var trimmed = folder.Replace('_', ' ').Replace('-', ' ').Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? folder : trimmed;
    }

    private static void AddExisting(ISet<string> roots, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            AddExisting(roots, path);
        }
    }

    private static void AddExisting(ISet<string> roots, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            var full = Path.GetFullPath(path.Trim());
            if (Directory.Exists(full))
            {
                roots.Add(full);
            }
        }
        catch
        {
            // Ignore malformed extra roots.
        }
    }

    private static IEnumerable<char> EnumerateReadyDriveLetters()
    {
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch
        {
            yield break;
        }

        foreach (var drive in drives)
        {
            char? letter = null;
            try
            {
                if (drive.DriveType is DriveType.Fixed or DriveType.Removable &&
                    drive.IsReady &&
                    drive.Name.Length >= 2 &&
                    char.IsLetter(drive.Name[0]))
                {
                    letter = char.ToUpperInvariant(drive.Name[0]);
                }
            }
            catch
            {
                // Skip drives that throw while we probe readiness.
            }

            if (letter is { } ready)
            {
                yield return ready;
            }
        }
    }
}

public sealed record VortexProfileHint(string Id, string Name, DateTimeOffset UpdatedAt);
