using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IModCatalogService
{
    Task<IReadOnlyList<ModGame>> DiscoverAsync(CancellationToken cancellationToken = default);
}

public interface IGameModAdapter
{
    string Id { get; }
    bool CanHandle(ModGame game);
    ModCapabilities GetCapabilities(ModGame game);
    Task<ModOperationResult> ApplyProfileAsync(
        ModGame game,
        ModProfile profile,
        CancellationToken cancellationToken = default);
    Task<ModOperationResult> RestoreBackupAsync(
        ModConfigurationBackup backup,
        CancellationToken cancellationToken = default);
}

public interface IModProfileStore
{
    Task<IReadOnlyList<ModProfile>> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IReadOnlyList<ModProfile> profiles, CancellationToken cancellationToken = default);
}

public interface IModProfileManager
{
    Task<ModOperationResult> ApplyAsync(
        ModGame game,
        ModProfile profile,
        CancellationToken cancellationToken = default);
    Task<ModOperationResult> RestoreLatestAsync(
        ModGame game,
        CancellationToken cancellationToken = default);
}

public interface IModBackupService
{
    Task<ModConfigurationBackup> CreateAsync(
        string gameId,
        string adapterId,
        IReadOnlyList<string> configurationPaths,
        string reason,
        CancellationToken cancellationToken = default);
    Task<ModConfigurationBackup?> GetLatestAsync(
        string gameId,
        string adapterId,
        CancellationToken cancellationToken = default);
    Task RestoreAsync(
        ModConfigurationBackup backup,
        CancellationToken cancellationToken = default);
}

public interface IExternalModHandoff
{
    Task OpenWorkshopAsync(string steamAppId, CancellationToken cancellationToken = default);
    Task OpenWorkshopItemAsync(string workshopItemId, CancellationToken cancellationToken = default);
}
