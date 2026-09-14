using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Files;

public sealed record FileBrowserSettings
{
    public IReadOnlyList<FilePlace> Places { get; init; } = [];

    public bool IncludeDrives { get; init; } = true;

    public Func<IReadOnlyList<DriveInfo>> ListDrives { get; init; } = static () => DriveInfo.GetDrives();
}

/// <summary>
/// Browse user places and drives. Does not replace Explorer.
/// Spec: docs/os-spec.md §3.
/// </summary>
public sealed class LocalFileBrowser : IFileBrowser
{
    private readonly FileBrowserSettings _settings;

    public LocalFileBrowser(FileBrowserSettings? settings = null)
    {
        _settings = settings ?? DefaultSettings();
    }

    public FileBrowsePage OpenPlaces()
    {
        var home = _settings.Places.FirstOrDefault()?.Path
                   ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return OpenPath(string.IsNullOrWhiteSpace(home) ? Directory.GetCurrentDirectory() : home);
    }

    public FileBrowsePage OpenPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var full = Path.GetFullPath(path);
        if (!Directory.Exists(full))
        {
            throw new DirectoryNotFoundException($"Folder not found: {full}");
        }

        var parent = Directory.GetParent(full)?.FullName ?? full;
        var entries = new List<FileBrowseEntry>();
        try
        {
            entries.AddRange(Directory.EnumerateDirectories(full).Select(MapDirectory));
            entries.AddRange(Directory.EnumerateFiles(full).Select(MapFile));
        }
        catch (Exception error)
        {
            throw new InvalidOperationException($"Could not read {full}: {error.Message}", error);
        }

        var ordered = entries
            .OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new FileBrowsePage(full, parent, BuildPlaces(), ordered);
    }

    private IReadOnlyList<FilePlace> BuildPlaces()
    {
        var places = _settings.Places.ToList();
        if (!_settings.IncludeDrives)
        {
            return places;
        }

        foreach (var drive in _settings.ListDrives())
        {
            if (!drive.IsReady)
            {
                continue;
            }

            var id = "drive-" + drive.Name.TrimEnd('\\', '/').Replace(":", string.Empty, StringComparison.Ordinal);
            places.Add(new FilePlace(id, $"Drive {drive.Name.TrimEnd('\\')}", drive.RootDirectory.FullName));
        }

        return places;
    }

    public static FileBrowserSettings DefaultSettings()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var downloads = Path.Combine(home, "Downloads");
        var places = new List<FilePlace>
        {
            new("home", "Home", string.IsNullOrWhiteSpace(home) ? Directory.GetCurrentDirectory() : home)
        };

        if (!string.IsNullOrWhiteSpace(desktop) && Directory.Exists(desktop))
        {
            places.Add(new FilePlace("desktop", "Desktop", desktop));
        }

        if (Directory.Exists(downloads))
        {
            places.Add(new FilePlace("downloads", "Downloads", downloads));
        }

        return new FileBrowserSettings { Places = places, IncludeDrives = true };
    }

    private static FileBrowseEntry MapDirectory(string path)
    {
        var info = new DirectoryInfo(path);
        return new FileBrowseEntry(info.Name, info.FullName, true, null, info.LastWriteTimeUtc);
    }

    private static FileBrowseEntry MapFile(string path)
    {
        var info = new FileInfo(path);
        return new FileBrowseEntry(info.Name, info.FullName, false, info.Length, info.LastWriteTimeUtc);
    }
}
