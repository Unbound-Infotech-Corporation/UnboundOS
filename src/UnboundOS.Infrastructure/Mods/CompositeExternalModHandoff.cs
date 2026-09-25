using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Mods;

public sealed class CompositeExternalModHandoff(
    SteamExternalModHandoff steam,
    VortexLauncher vortex) : IExternalModHandoff
{
    public Task OpenWorkshopAsync(string steamAppId, CancellationToken cancellationToken = default) =>
        steam.OpenWorkshopAsync(steamAppId, cancellationToken);

    public Task OpenWorkshopItemAsync(string workshopItemId, CancellationToken cancellationToken = default) =>
        steam.OpenWorkshopItemAsync(workshopItemId, cancellationToken);

    public Task<ModOperationResult> OpenVortexAsync(
        string? vortexGameId,
        string? vortexProfileId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(vortex.Open(vortexGameId, vortexProfileId));
    }
}
