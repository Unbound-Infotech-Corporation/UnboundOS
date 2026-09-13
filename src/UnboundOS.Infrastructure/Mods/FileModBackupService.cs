using System.Text.Json;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Mods;

public sealed class FileModBackupService : IModBackupService
{
    private readonly string _root;

    public FileModBackupService()
    {
        _root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unbound Infotech Corporation",
            "UnboundOS",
            "ModBackups");
        Directory.CreateDirectory(_root);
    }

    public async Task<ModConfigurationBackup> CreateAsync(
        string gameId,
        string adapterId,
        IReadOnlyList<string> configurationPaths,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var id = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}";
        var backupRoot = Path.Combine(_root, Sanitize(gameId), id);
        Directory.CreateDirectory(backupRoot);

        var files = new List<BackupEntry>();
        foreach (var sourcePath in configurationPaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullSource = Path.GetFullPath(sourcePath);
            if (!File.Exists(fullSource))
            {
                continue;
            }

            var destinationName = $"{files.Count:D3}-{Path.GetFileName(fullSource)}";
            var destinationPath = Path.Combine(backupRoot, destinationName);
            await using var source = File.Open(fullSource, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await using var destination = File.Create(destinationPath);
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            files.Add(new BackupEntry(fullSource, destinationName));
        }

        var backup = new ModConfigurationBackup(
            id,
            gameId,
            adapterId,
            backupRoot,
            DateTimeOffset.UtcNow,
            reason);
        var manifest = new BackupManifest(backup, files);
        await File.WriteAllTextAsync(
            Path.Combine(backupRoot, "manifest.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken).ConfigureAwait(false);
        return backup;
    }

    public async Task<ModConfigurationBackup?> GetLatestAsync(
        string gameId,
        string adapterId,
        CancellationToken cancellationToken = default)
    {
        var gameRoot = Path.Combine(_root, Sanitize(gameId));
        if (!Directory.Exists(gameRoot))
        {
            return null;
        }

        foreach (var manifestPath in Directory.EnumerateFiles(gameRoot, "manifest.json", SearchOption.AllDirectories)
                     .OrderByDescending(File.GetLastWriteTimeUtc))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
                var manifest = JsonSerializer.Deserialize<BackupManifest>(json);
                if (manifest?.Backup.AdapterId == adapterId)
                {
                    return manifest.Backup;
                }
            }
            catch (JsonException)
            {
                // Ignore an incomplete/corrupt backup and try the next one.
            }
        }

        return null;
    }

    public async Task RestoreAsync(
        ModConfigurationBackup backup,
        CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(backup.BackupPath, "manifest.json");
        var json = await File.ReadAllTextAsync(manifestPath, cancellationToken).ConfigureAwait(false);
        var manifest = JsonSerializer.Deserialize<BackupManifest>(json)
            ?? throw new InvalidDataException("The mod backup manifest is invalid.");

        foreach (var entry in manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = Path.Combine(backup.BackupPath, entry.BackupFileName);
            var destination = Path.GetFullPath(entry.OriginalPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(source, destination, overwrite: true);
        }
    }

    private static string Sanitize(string value) =>
        string.Concat(value.Select(character =>
            Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    private sealed record BackupEntry(string OriginalPath, string BackupFileName);
    private sealed record BackupManifest(ModConfigurationBackup Backup, IReadOnlyList<BackupEntry> Files);
}
