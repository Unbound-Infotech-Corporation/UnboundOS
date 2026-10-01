using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Home;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Settings;

public sealed class HomeHudSettings : IHomeHudSettings
{
    private readonly IShellSettingsStore _store;
    private ShellSettings _settings = ShellSettings.CreateDefault();

    public HomeHudSettings(IShellSettingsStore store)
    {
        _store = store;
    }

    public bool HudEnabled => _settings.ShowHomeHud;

    public bool ClockEnabled => _settings.ShowHomeClock;

    public bool TempsEnabled => _settings.ShowHomeTemps;

    public HomeWidgetAppearance Appearance => _settings.WidgetLook;

    public IReadOnlyList<HomeWidgetPlacement> Placements =>
        HomeWidgets.Merge(_settings.HomeWidgetPlacements);

    public event EventHandler? Changed;

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        _settings = await _store.LoadAsync(ct).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task SetHudEnabledAsync(bool enabled, CancellationToken ct = default) =>
        MutateAsync(current => current with { HomeHudEnabled = enabled }, ct);

    public Task SetClockEnabledAsync(bool enabled, CancellationToken ct = default) =>
        MutateAsync(current => current with { HomeClockEnabled = enabled }, ct);

    public Task SetTempsEnabledAsync(bool enabled, CancellationToken ct = default) =>
        MutateAsync(current => current with { HomeTempsEnabled = enabled }, ct);

    public Task SetAppearanceAsync(HomeWidgetAppearance appearance, CancellationToken ct = default) =>
        MutateAsync(current => current with { HomeWidgetAppearance = HomeWidgets.AppearanceToken(appearance) }, ct);

    public Task SetPlacementAsync(string id, double x, double y, CancellationToken ct = default) =>
        MutateAsync(
            current => current with
            {
                HomeWidgetPlacements = HomeWidgets.WithPosition(
                    HomeWidgets.Merge(current.HomeWidgetPlacements),
                    id,
                    x,
                    y).ToList()
            },
            ct);

    public Task ResetPlacementsAsync(CancellationToken ct = default) =>
        MutateAsync(current => current with { HomeWidgetPlacements = HomeWidgets.Defaults.ToList() }, ct);

    private async Task MutateAsync(Func<ShellSettings, ShellSettings> mutate, CancellationToken ct)
    {
        var latest = await _store.LoadAsync(ct).ConfigureAwait(false);
        _settings = mutate(latest);
        await _store.SaveAsync(_settings, ct).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
