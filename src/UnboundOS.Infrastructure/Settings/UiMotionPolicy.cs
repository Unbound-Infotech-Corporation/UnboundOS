using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Settings;

public sealed class UiMotionPolicy : IUiMotionPolicy
{
    private readonly IShellSettingsStore _store;
    private readonly ISystemAnimationPreference _system;
    private readonly ISessionEngine _session;
    private ShellSettings _settings = ShellSettings.CreateDefault();

    public UiMotionPolicy(
        IShellSettingsStore store,
        ISystemAnimationPreference system,
        ISessionEngine session)
    {
        _store = store;
        _system = system;
        _session = session;
        _session.StateChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool UserWantsMotion => _settings.InterfaceMotionEnabled;

    public bool IsSessionLive =>
        _session.State is SessionState.Active or SessionState.Entering or SessionState.Exiting;

    public bool AllowMotion => Suppression == MotionSuppression.None;

    public MotionSuppression Suppression
    {
        get
        {
            if (!UserWantsMotion)
            {
                return MotionSuppression.UserDisabled;
            }

            if (!_system.AnimationsEnabled)
            {
                return MotionSuppression.SystemDisabled;
            }

            if (IsSessionLive)
            {
                return MotionSuppression.SessionLive;
            }

            return MotionSuppression.None;
        }
    }

    public string StatusText => Suppression switch
    {
        MotionSuppression.UserDisabled => "Interface motion is off. Lists, buttons, and Home labels use instant states.",
        MotionSuppression.SystemDisabled => "Interface motion is paused because Windows animations are off.",
        MotionSuppression.SessionLive => "Interface motion is paused while a session is live. The toggle stays available. Home labels and lists will not ease.",
        _ => "Interface motion is on. A few-percent scale, a 1px cyan hairline on the focused row, and a quiet enlarge-push on Home."
    };

    public event EventHandler? Changed;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _settings = await _store.LoadAsync(ct).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task SetUserWantsMotionAsync(bool enabled, CancellationToken ct = default)
    {
        var latest = await _store.LoadAsync(ct).ConfigureAwait(false);
        _settings = latest with { InterfaceMotionEnabled = enabled };
        await _store.SaveAsync(_settings, ct).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
