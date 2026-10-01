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
/// First-party file manager. Explorer.exe stays on disk for Desktop mode.
/// Spec: docs/os-replacement-plan.md Stage 2.
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

    public FileOpResult Copy(string source, string destDir)
    {
        try
        {
            var dest = PrepareDest(source, destDir);
            if (Directory.Exists(source))
            {
                CopyDirectory(source, dest);
            }
            else
            {
                File.Copy(source, dest, overwrite: false);
            }

            return new FileOpResult(true, $"Copied to {dest}");
        }
        catch (Exception ex)
        {
            return new FileOpResult(false, ex.Message);
        }
    }

    public FileOpResult Move(string source, string destDir)
    {
        try
        {
            var dest = PrepareDest(source, destDir);
            if (Directory.Exists(source))
            {
                Directory.Move(source, dest);
            }
            else
            {
                File.Move(source, dest);
            }

            return new FileOpResult(true, $"Moved to {dest}");
        }
        catch (Exception ex)
        {
            return new FileOpResult(false, ex.Message);
        }
    }

    public FileOpResult Delete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else
            {
                File.Delete(path);
            }

            return new FileOpResult(true, $"Deleted {path}");
        }
        catch (Exception ex)
        {
            return new FileOpResult(false, ex.Message);
        }
    }

    public FileOpResult Eject(string root)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new FileOpResult(false, "Eject runs on Windows (removable volume).");
        }

        try
        {
            var letter = Path.GetPathRoot(root)?.TrimEnd('\\', '/');
            if (string.IsNullOrWhiteSpace(letter))
            {
                return new FileOpResult(false, "No drive letter.");
            }

            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -Command \"(New-Object -ComObject Shell.Application).Namespace(17).ParseName('{letter}').InvokeVerb('Eject')\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = System.Diagnostics.Process.Start(start);
            process?.WaitForExit(8000);
            return new FileOpResult(true, $"Eject requested for {letter}.");
        }
        catch (Exception ex)
        {
            return new FileOpResult(false, ex.Message);
        }
    }

    public FileOpResult OpenItem(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                OpenPath(path);
                return new FileOpResult(true, path);
            }

            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };
            using var process = System.Diagnostics.Process.Start(start);
            return new FileOpResult(process is not null, process is null ? "Open failed." : $"Opened {path}");
        }
        catch (Exception ex)
        {
            return new FileOpResult(false, ex.Message);
        }
    }

    public FileOpResult OpenWith(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            return OpenItem(path);
        }

        try
        {
            var start = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"shell32.dll,OpenAs_RunDLL {path}",
                UseShellExecute = true
            };
            using var process = System.Diagnostics.Process.Start(start);
            return new FileOpResult(process is not null, "Open with…");
        }
        catch (Exception ex)
        {
            return new FileOpResult(false, ex.Message);
        }
    }

    private static string PrepareDest(string source, string destDir)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destDir);
        Directory.CreateDirectory(destDir);
        var name = Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return Path.Combine(destDir, name);
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: false);
        }

        foreach (var child in Directory.EnumerateDirectories(source))
        {
            CopyDirectory(child, Path.Combine(dest, Path.GetFileName(child)));
        }
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
