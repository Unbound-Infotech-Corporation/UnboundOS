namespace UnboundOS.Infrastructure.Mods;

public sealed class SteamExternalModHandoff
{
    public Task OpenWorkshopAsync(string steamAppId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(steamAppId);
        return OpenAsync($"steam://url/SteamWorkshopPage/{Uri.EscapeDataString(steamAppId)}");
    }

    public Task OpenWorkshopItemAsync(string workshopItemId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(workshopItemId);
        return OpenAsync($"steam://url/CommunityFilePage/{Uri.EscapeDataString(workshopItemId)}");
    }

    private static Task OpenAsync(string uri)
    {
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true });
        return Task.CompletedTask;
    }
}
