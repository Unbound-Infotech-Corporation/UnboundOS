using Microsoft.Extensions.DependencyInjection;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Overlay;
using UnboundOS.Infrastructure;
using UnboundOS.Infrastructure.Overlay;
using UnboundOS.Infrastructure.Settings;

namespace UnboundOS.Tests;

public sealed class BrandingAndOverlayTests
{
    [Fact]
    public void Branding_MatchesUnboundInfotechSite()
    {
        Assert.Equal("UnboundOS", Branding.ProductName);
        Assert.Equal("Unbound Infotech Corporation", Branding.CompanyName);
        Assert.Equal("https://unboundinfotech.com", Branding.WebsiteUrl);
        Assert.Equal("#05070A", Branding.Palette.Obsidian);
        Assert.Equal("#00F0FF", Branding.Palette.CyanPulse);
        Assert.Equal("#1E40AF", Branding.Palette.Cobalt);
        Assert.DoesNotContain("lime", Branding.Tagline, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PlayStation", Branding.ProductName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PlayStation", Branding.CompanyName, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PlayStation", Branding.Tagline, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TileLabels_StayArtworkForwardWithoutStoreBranding()
    {
        var tool = new DesktopTool(
            DesktopToolIds.Obs,
            "OBS Studio",
            "Capture",
            true,
            @"C:\obs64.exe",
            ["obs64"],
            new ToolGetPath("OBS", "https://obsproject.com/download"));
        Assert.Equal("O", tool.Monogram);
        Assert.Equal("OPEN", tool.StatusLabel);

        var game = new ModGame(
            "skyrim",
            "Skyrim",
            null,
            ModProvider.NexusMods,
            ModCapabilities.VortexDiscoveryOnly,
            []);
        Assert.Equal("S", game.Monogram);
        Assert.Equal("VORTEX", game.ProviderLabel);
        Assert.Equal("0 ITEMS", game.ItemCountLabel);

        var profile = new SessionProfile
        {
            Id = "living",
            Name = "Living Room Shell",
            Kind = ProfileKind.LivingRoom
        };
        Assert.Equal("L", profile.Monogram);
        Assert.Equal("LIVINGROOM", profile.KindLabel);
    }

    [Fact]
    public void ApplyTo_MergesStoreProtectionsAndDropsConflictsFromDenylist()
    {
        var profile = new SessionProfile
        {
            Id = "test",
            Name = "Test",
            TerminateProcessNames = ["Steam", "Discord"],
            ProtectProcessNames = ["game"]
        };

        var updated = PlatformLaunchProtections.ApplyTo(profile, GameStore.Steam);

        Assert.Contains("Steam", updated.ProtectProcessNames);
        Assert.DoesNotContain("Steam", updated.TerminateProcessNames);
        Assert.Contains("Discord", updated.TerminateProcessNames);
        Assert.Contains("game", updated.ProtectProcessNames);
    }

    [Fact]
    public void DisabledOverlayHost_IsOffAndInert()
    {
        var host = new DisabledDesktopOverlayHost();

        Assert.False(host.IsEnabled);
        Assert.Empty(host.Widgets);
        Assert.Equal(Task.CompletedTask, host.StartAsync());
        Assert.Equal(Task.CompletedTask, host.StopAsync());
    }

    [Fact]
    public void AddUnboundOs_RegistersDisabledOverlayByDefault()
    {
        var services = new ServiceCollection();
        services.AddUnboundOs();
        using var provider = services.BuildServiceProvider();

        var host = provider.GetRequiredService<IDesktopOverlayHost>();
        Assert.IsType<DisabledDesktopOverlayHost>(host);
        Assert.False(host.IsEnabled);

        var motion = provider.GetRequiredService<IUiMotionPolicy>();
        Assert.IsType<UiMotionPolicy>(motion);
        Assert.True(motion.UserWantsMotion);
    }

    [Fact]
    public void AddUnboundOs_KeepsAddonOverlayRegistration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDesktopOverlayHost, EnabledProbeOverlayHost>();
        services.AddUnboundOs();
        using var provider = services.BuildServiceProvider();

        var host = provider.GetRequiredService<IDesktopOverlayHost>();
        Assert.IsType<EnabledProbeOverlayHost>(host);
        Assert.True(host.IsEnabled);
    }

    private sealed class EnabledProbeOverlayHost : IDesktopOverlayHost
    {
        public bool IsEnabled => true;
        public string DisplayName => "Probe";
        public IReadOnlyList<OverlayWidgetDescriptor> Widgets { get; } = [];
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
