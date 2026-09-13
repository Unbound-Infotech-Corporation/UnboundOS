using Microsoft.Extensions.DependencyInjection;
using UnboundOS.Core;
using UnboundOS.Core.Overlay;
using UnboundOS.Infrastructure;
using UnboundOS.Infrastructure.Overlay;

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
