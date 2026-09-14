using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Mods;

/// <summary>
/// Vortex remains the source of truth. This adapter never mutates staging folders or hardlinks.
/// </summary>
public sealed class VortexReadOnlyAdapter : IGameModAdapter
{
    public string Id => VortexCatalogService.AdapterId;

    public bool CanHandle(ModGame game) =>
        game.Provider == ModProvider.NexusMods ||
        string.Equals(game.AdapterId, Id, StringComparison.OrdinalIgnoreCase);

    public ModCapabilities GetCapabilities(ModGame game) =>
        ModCapabilities.VortexDiscoveryOnly;

    public Task<ModOperationResult> ApplyProfileAsync(
        ModGame game,
        ModProfile profile,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ModOperationResult.Unsupported(
            "Vortex owns Nexus install, enable, and deploy. UnboundOS will not write staging folders or hardlinks."));

    public Task<ModOperationResult> RestoreBackupAsync(
        ModConfigurationBackup backup,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ModOperationResult.Unsupported(
            "Vortex discovery is read-only, so it has no UnboundOS configuration backup to restore."));
}
