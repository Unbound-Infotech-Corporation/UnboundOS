using Microsoft.Win32;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Shell;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// After sleep, try to focus the last game if it is still running.
/// Does not inject. If the process is gone, Guide can relaunch it.
/// </summary>
public sealed class WindowsSleepResumeBinder
{
    private readonly ILastSessionResume _resume;
    private readonly IRunningAppSwitcher _switcher;
    private bool _started;

    public WindowsSleepResumeBinder(ILastSessionResume resume, IRunningAppSwitcher switcher)
    {
        _resume = resume;
        _switcher = switcher;
    }

    public void Start()
    {
        if (_started || !OperatingSystem.IsWindows())
        {
            return;
        }

        _started = true;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    public void Stop()
    {
        if (!_started)
        {
            return;
        }

        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _started = false;
    }

    private async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        try
        {
            var last = await _resume.LoadAsync().ConfigureAwait(false);
            if (!LastSessionPolicy.ShouldOfferResume(last, DateTimeOffset.UtcNow) || last is null)
            {
                return;
            }

            var running = await _switcher.ListAsync().ConfigureAwait(false);
            var match = OneGameSwitcher.FocusList(running).FirstOrDefault(app =>
                (!string.IsNullOrWhiteSpace(last.ExecutablePath) &&
                 app.ProcessName.Equals(
                     Path.GetFileNameWithoutExtension(last.ExecutablePath),
                     StringComparison.OrdinalIgnoreCase)) ||
                app.Title.Contains(last.DisplayName, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                await _switcher.ActivateAsync(match).ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
            // Wake must not crash the shell.
        }
    }
}
