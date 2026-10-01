using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Shell;

namespace UnboundOS.Infrastructure.Startup;

public sealed class WindowsGuideMenu : IGuideMenu
{
    private readonly IVolumeKeys _volume;
    private readonly IOsSettingsHub _settings;
    private readonly ISessionPower _power;
    private readonly IDesktopMode _desktop;

    public WindowsGuideMenu(
        IVolumeKeys volume,
        IOsSettingsHub settings,
        ISessionPower power,
        IDesktopMode desktop)
    {
        _volume = volume;
        _settings = settings;
        _power = power;
        _desktop = desktop;
    }

    public IReadOnlyList<GuideAction> Actions => GuideMenuCatalog.All;

    public async Task<(bool Succeeded, string Message)> InvokeAsync(string id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var action = GuideMenuCatalog.Find(id);
        if (action is null)
        {
            return (false, "Unknown Guide action.");
        }

        return action.Id switch
        {
            "volume-up" => await _volume.VolumeUpAsync().ConfigureAwait(false),
            "volume-down" => await _volume.VolumeDownAsync().ConfigureAwait(false),
            "mute" => await _volume.MuteAsync().ConfigureAwait(false),
            "lock" => await _power.LockAsync(cancellationToken).ConfigureAwait(false),
            "sleep" => await _power.SleepAsync(cancellationToken).ConfigureAwait(false),
            "shutdown" => await _power.ShutdownAsync(cancellationToken).ConfigureAwait(false),
            "desktop" => await _desktop.StartDesktopAsync(cancellationToken).ConfigureAwait(false),
            "perf" => (true, OsProductCopy.GuideHonesty),
            _ when !string.IsNullOrWhiteSpace(action.Uri) =>
                await _settings.OpenUriAsync(action.Uri, cancellationToken).ConfigureAwait(false),
            _ => (false, action.Hint)
        };
    }
}
