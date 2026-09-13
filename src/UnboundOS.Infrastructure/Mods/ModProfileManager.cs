using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Mods;

public sealed class ModProfileManager(
    IEnumerable<IGameModAdapter> adapters,
    IModBackupService backups) : IModProfileManager
{
    private readonly IReadOnlyList<IGameModAdapter> _adapters = adapters.ToArray();

    public async Task<ModOperationResult> ApplyAsync(
        ModGame game,
        ModProfile profile,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(game.GameId, profile.GameId, StringComparison.OrdinalIgnoreCase))
        {
            return new ModOperationResult(false, "This profile belongs to a different game.", []);
        }

        var adapter = Resolve(game);
        if (adapter is null)
        {
            return ModOperationResult.Unsupported(
                "No safe game-specific adapter is installed. UnboundOS will not mutate this game's files generically.");
        }

        var capabilities = adapter.GetCapabilities(game);
        if (!capabilities.CanToggle && !capabilities.CanReorder)
        {
            return ModOperationResult.Unsupported(capabilities.Explanation);
        }

        return await adapter.ApplyProfileAsync(game, profile, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ModOperationResult> RestoreLatestAsync(
        ModGame game,
        CancellationToken cancellationToken = default)
    {
        var adapter = Resolve(game);
        if (adapter is null)
        {
            return ModOperationResult.Unsupported("No game-specific adapter is available for restore.");
        }

        var backup = await backups.GetLatestAsync(game.GameId, adapter.Id, cancellationToken)
            .ConfigureAwait(false);
        if (backup is null)
        {
            return new ModOperationResult(false, "No configuration backup exists for this game.", []);
        }

        return await adapter.RestoreBackupAsync(backup, cancellationToken).ConfigureAwait(false);
    }

    private IGameModAdapter? Resolve(ModGame game) =>
        _adapters.FirstOrDefault(adapter => adapter.CanHandle(game));
}

public sealed class SteamWorkshopReadOnlyAdapter : IGameModAdapter
{
    public string Id => "steam-workshop-readonly";

    public bool CanHandle(ModGame game) =>
        game.Provider == ModProvider.SteamWorkshop;

    public ModCapabilities GetCapabilities(ModGame game) =>
        ModCapabilities.SteamDiscoveryOnly;

    public Task<ModOperationResult> ApplyProfileAsync(
        ModGame game,
        ModProfile profile,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ModOperationResult.Unsupported(
            "Steam owns Workshop subscription and deployment for this title. The profile is saved in UnboundOS, but applying it requires a game-specific adapter."));

    public Task<ModOperationResult> RestoreBackupAsync(
        ModConfigurationBackup backup,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ModOperationResult.Unsupported(
            "Steam Workshop discovery is read-only, so it has no UnboundOS configuration backup to restore."));
}

/// <summary>
/// Base class for future game adapters. It enforces backup-before-write and rollback-on-failure.
/// Adapters only implement the game's documented configuration format.
/// </summary>
public abstract class TransactionalFileModAdapter(IModBackupService backups) : IGameModAdapter
{
    public abstract string Id { get; }
    public abstract bool CanHandle(ModGame game);
    public abstract ModCapabilities GetCapabilities(ModGame game);
    protected abstract IReadOnlyList<string> GetConfigurationPaths(ModGame game);
    protected abstract Task ApplyCoreAsync(
        ModGame game,
        ModProfile profile,
        CancellationToken cancellationToken);

    public async Task<ModOperationResult> ApplyProfileAsync(
        ModGame game,
        ModProfile profile,
        CancellationToken cancellationToken = default)
    {
        var paths = GetConfigurationPaths(game);
        var backup = await backups.CreateAsync(
            game.GameId,
            Id,
            paths,
            $"Before applying mod profile '{profile.Name}'",
            cancellationToken).ConfigureAwait(false);

        try
        {
            await ApplyCoreAsync(game, profile, cancellationToken).ConfigureAwait(false);
            return new ModOperationResult(
                true,
                $"Applied '{profile.Name}' safely.",
                ["Created configuration backup.", "Applied game-specific mod configuration."],
                backup.BackupPath);
        }
        catch (Exception applyError)
        {
            try
            {
                await backups.RestoreAsync(backup, cancellationToken).ConfigureAwait(false);
                return new ModOperationResult(
                    false,
                    $"Apply failed and was rolled back: {applyError.Message}",
                    ["Created configuration backup.", "Apply failed.", "Restored original configuration."],
                    backup.BackupPath);
            }
            catch (Exception restoreError)
            {
                return new ModOperationResult(
                    false,
                    $"Apply failed; automatic restore also failed. Backup retained at {backup.BackupPath}. {restoreError.Message}",
                    ["Created configuration backup.", "Apply failed.", "Automatic restore failed."],
                    backup.BackupPath);
            }
        }
    }

    public async Task<ModOperationResult> RestoreBackupAsync(
        ModConfigurationBackup backup,
        CancellationToken cancellationToken = default)
    {
        await backups.RestoreAsync(backup, cancellationToken).ConfigureAwait(false);
        return new ModOperationResult(
            true,
            "Restored the latest mod configuration backup.",
            ["Restored game-specific configuration files."],
            backup.BackupPath);
    }
}
