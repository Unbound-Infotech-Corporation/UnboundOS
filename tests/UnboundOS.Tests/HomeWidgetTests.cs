using Microsoft.Extensions.DependencyInjection;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Home;
using UnboundOS.Core.Models;
using UnboundOS.Infrastructure;
using UnboundOS.Infrastructure.Home;
using UnboundOS.Infrastructure.Settings;

namespace UnboundOS.Tests;

public sealed class HomeWidgetTests
{
    [Fact]
    public void BuiltIn_AreClockHonestTempsAndLoad()
    {
        Assert.Equal(5, HomeWidgets.BuiltIn.Count);
        Assert.Contains(HomeWidgets.BuiltIn, item => item.Id == HomeWidgets.Clock && item.Kind == "clock");
        Assert.Contains(HomeWidgets.BuiltIn, item => item.Id == HomeWidgets.Cpu && item.Kind == "temp");
        Assert.Contains(HomeWidgets.BuiltIn, item => item.Id == HomeWidgets.Gpu && item.Kind == "temp");
        Assert.Contains(HomeWidgets.BuiltIn, item => item.Id == HomeWidgets.Package && item.Kind == "temp");
        Assert.Contains(HomeWidgets.BuiltIn, item => item.Id == HomeWidgets.Load && item.Kind == "load");
        Assert.DoesNotContain(HomeWidgets.BuiltIn, item => item.Kind == "fake");
    }

    [Fact]
    public void Defaults_SitRightOfListKeepout()
    {
        Assert.All(HomeWidgets.Defaults, item =>
        {
            Assert.True(item.X >= HomeWidgets.ListKeepoutX);
            Assert.True(HomeWidgets.ClearsList(item));
        });
        var clock = HomeWidgets.DefaultOf(HomeWidgets.Clock);
        Assert.True(clock.Y < 0.2);
        Assert.False(HomeWidgets.ClearsList(new HomeWidgetPlacement { Id = HomeWidgets.Cpu, X = 0.03, Y = 0.04 }));
    }

    [Fact]
    public void Clamp_KeepsNormalizedRange()
    {
        var raw = new HomeWidgetPlacement { Id = " cpu ", X = 1.4, Y = -0.2 };
        var clamped = HomeWidgets.Clamp(raw);
        Assert.Equal("cpu", clamped.Id);
        Assert.Equal(1, clamped.X);
        Assert.Equal(0, clamped.Y);
    }

    [Fact]
    public void Merge_OverlaysStoredOnDefaults()
    {
        var merged = HomeWidgets.Merge(
        [
            new() { Id = HomeWidgets.Clock, X = 0.5, Y = 0.6, Visible = false }
        ]);
        var clock = HomeWidgets.Place(merged, HomeWidgets.Clock);
        Assert.Equal(0.5, clock.X);
        Assert.Equal(0.6, clock.Y);
        Assert.False(clock.IsVisible);
        Assert.True(HomeWidgets.Place(merged, HomeWidgets.Cpu).IsVisible);
        Assert.Equal(5, merged.Count);
    }

    [Fact]
    public void Appearance_ParsesKnownLooks_AndStaysRestrained()
    {
        Assert.Equal(HomeWidgetAppearance.Glass, HomeWidgets.ParseAppearance(null));
        Assert.Equal(HomeWidgetAppearance.Glass, HomeWidgets.ParseAppearance("rainbow"));
        Assert.Equal(HomeWidgetAppearance.Dim, HomeWidgets.ParseAppearance("DIM"));
        Assert.Equal(HomeWidgetAppearance.Compact, HomeWidgets.ParseAppearance("compact"));
        Assert.Equal("glass", HomeWidgets.AppearanceToken(HomeWidgetAppearance.Glass));
        Assert.StartsWith("#", HomeWidgets.FillHex(HomeWidgetAppearance.Glass));
        Assert.Equal("#1C2228", HomeWidgets.InkHex);
    }

    [Fact]
    public void Catalog_AppendsAddonSource()
    {
        var catalog = new HomeWidgetCatalog([new ExtraSource()]);
        Assert.Equal(6, catalog.Widgets.Count);
        Assert.Contains(catalog.Widgets, item => item.Id == "custom.ping");
        Assert.Equal(HomeWidgets.Clock, catalog.Widgets[0].Id);
    }

    [Fact]
    public void AddUnboundOs_RegistersWidgetCatalog()
    {
        var services = new ServiceCollection();
        services.AddUnboundOs();
        using var provider = services.BuildServiceProvider();
        var catalog = provider.GetRequiredService<IHomeWidgetCatalog>();
        Assert.IsType<HomeWidgetCatalog>(catalog);
        Assert.Equal(HomeWidgets.BuiltIn.Count, catalog.Widgets.Count);
    }

    [Fact]
    public async Task SettingsStore_PersistsAppearanceAndPlacement_WithoutClobberingMotion()
    {
        using var temp = new TempFolder();
        var store = new JsonShellSettingsStore(temp.Path);
        var hud = new HomeHudSettings(store);
        await hud.InitializeAsync();
        await hud.SetAppearanceAsync(HomeWidgetAppearance.Dim);
        await hud.SetPlacementAsync(HomeWidgets.Clock, 0.4, 0.55);

        var motion = new UiMotionPolicy(
            store,
            new DelegateSystemAnimationPreference(() => true),
            new IdleSession());
        await motion.InitializeAsync();
        await motion.SetUserWantsMotionAsync(false);

        var loaded = await store.LoadAsync();
        Assert.False(loaded.InterfaceMotionEnabled);
        Assert.Equal(HomeWidgetAppearance.Dim, loaded.WidgetLook);
        var clock = HomeWidgets.Place(HomeWidgets.Merge(loaded.HomeWidgetPlacements), HomeWidgets.Clock);
        Assert.Equal(0.4, clock.X, 3);
        Assert.Equal(0.55, clock.Y, 3);
        Assert.False(loaded.ShowHomeHud);
        var json = await File.ReadAllTextAsync(store.FilePath);
        Assert.Contains("homeWidgetAppearance", json, StringComparison.Ordinal);
        Assert.Contains("homeWidgetPlacements", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResetPlacements_RestoresDefaults()
    {
        using var temp = new TempFolder();
        var hud = new HomeHudSettings(new JsonShellSettingsStore(temp.Path));
        await hud.InitializeAsync();
        await hud.SetPlacementAsync(HomeWidgets.Cpu, 0.7, 0.7);
        await hud.ResetPlacementsAsync();
        var cpu = HomeWidgets.Place(hud.Placements, HomeWidgets.Cpu);
        Assert.Equal(HomeWidgets.DefaultOf(HomeWidgets.Cpu).X, cpu.X);
        Assert.Equal(HomeWidgets.DefaultOf(HomeWidgets.Cpu).Y, cpu.Y);
    }

    private sealed class ExtraSource : IHomeWidgetSource
    {
        public IReadOnlyList<HomeWidgetDefinition> Widgets { get; } =
        [
            new("custom.ping", "Ping", "NET", "custom")
        ];
    }

    private sealed class IdleSession : ISessionEngine
    {
        public SessionState State => SessionState.Idle;

        public SessionProfile? ActiveProfile => null;

        public SessionSnapshot? LastSnapshot => null;

        public event EventHandler<SessionState>? StateChanged
        {
            add { }
            remove { }
        }

        public Task<SessionMutationResult> EnterAsync(SessionProfile profile, CancellationToken ct = default) =>
            Task.FromResult(new SessionMutationResult { Succeeded = false, Message = "stub" });

        public Task<SessionMutationResult> ExitAsync(CancellationToken ct = default) =>
            Task.FromResult(new SessionMutationResult { Succeeded = false, Message = "stub" });
    }

    private sealed class TempFolder : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "UnboundOS-widgets-" + Guid.NewGuid().ToString("N"));

        public TempFolder() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
