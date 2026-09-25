using UnboundOS.Core.Home;

namespace UnboundOS.Core.Abstractions;

/// <summary>Persisted Home extras: leftover plaques, clock/temps, widget look and positions. Off by default.</summary>
public interface IHomeHudSettings
{
    bool HudEnabled { get; }

    bool ClockEnabled { get; }

    bool TempsEnabled { get; }

    HomeWidgetAppearance Appearance { get; }

    IReadOnlyList<HomeWidgetPlacement> Placements { get; }

    event EventHandler? Changed;

    Task InitializeAsync(CancellationToken ct = default);

    Task SetHudEnabledAsync(bool enabled, CancellationToken ct = default);

    Task SetClockEnabledAsync(bool enabled, CancellationToken ct = default);

    Task SetTempsEnabledAsync(bool enabled, CancellationToken ct = default);

    Task SetAppearanceAsync(HomeWidgetAppearance appearance, CancellationToken ct = default);

    Task SetPlacementAsync(string id, double x, double y, CancellationToken ct = default);

    Task ResetPlacementsAsync(CancellationToken ct = default);
}
