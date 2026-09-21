namespace UnboundOS.Core.Abstractions;

/// <summary>Persisted Home HUD strip: clock, temps, and the strip itself.</summary>
public interface IHomeHudSettings
{
    bool HudEnabled { get; }

    bool ClockEnabled { get; }

    bool TempsEnabled { get; }

    event EventHandler? Changed;

    Task InitializeAsync(CancellationToken ct = default);

    Task SetHudEnabledAsync(bool enabled, CancellationToken ct = default);

    Task SetClockEnabledAsync(bool enabled, CancellationToken ct = default);

    Task SetTempsEnabledAsync(bool enabled, CancellationToken ct = default);
}
