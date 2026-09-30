using UnboundOS.Core;
using UnboundOS.Core.Diagnostics;
using UnboundOS.Infrastructure.Diagnostics;
using UnboundOS.Infrastructure.Startup;

namespace UnboundOS.Tests;

public sealed class HealthAndAutostartTests
{
    [Fact]
    public void Autostart_UsesHkcuRun_NeverShellEquals()
    {
        Assert.Equal("UnboundOS", WindowsShellAutostart.ValueName);
        Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Run", WindowsShellAutostart.RunKeyPath);
        Assert.DoesNotContain("Winlogon", WindowsShellAutostart.RunKeyPath, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Shell", WindowsShellAutostart.RunKeyPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not replace Explorer", OsProductCopy.AutostartHonesty, StringComparison.Ordinal);
        Assert.Contains("set Shell=", OsProductCopy.AutostartHonesty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Autostart_EnableMissingExe_DoesNotTouchWinlogon()
    {
        var autostart = new WindowsShellAutostart(
            Path.Combine(Path.GetTempPath(), "missing-unboundos-" + Guid.NewGuid().ToString("N") + ".exe"));
        Assert.False(autostart.IsEnabled);
        var result = await autostart.SetEnabledAsync(true);
        Assert.False(result.Succeeded);
        Assert.DoesNotContain("Winlogon", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
    }

    [Fact]
    public void PerfLog_IsOffByDefault_AndMeasureIsCheap()
    {
        Assert.False(PerfLog.IsEnabled);
        using var scope = PerfLog.Measure("test.noop");
        Assert.NotNull(scope);
        PerfLog.Event("test.event", 1);
    }

    [Fact]
    public async Task HealthCheck_WritesLog_WithRequiredProbes()
    {
        var report = await new OsHealthCheckService().RunAsync();
        Assert.Equal(5, report.Checks.Count);
        Assert.NotNull(report.Find("network"));
        Assert.NotNull(report.Find("gpu"));
        Assert.NotNull(report.Find("autostart"));
        Assert.NotNull(report.Find("updates"));
        Assert.NotNull(report.Find("disk"));
        Assert.True(report.Find("autostart")!.Ok);
        Assert.True(File.Exists(report.LogPath));
        Assert.True(File.Exists(Path.Combine(UnboundPaths.Root, "health-latest.txt")));
        var text = report.ToLogText();
        Assert.Contains("does not replace Explorer", text, StringComparison.Ordinal);
        Assert.Contains("does not change Windows Update, Defender, or Explorer", text, StringComparison.Ordinal);
        Assert.Contains("does not redistribute Windows", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Night City", text, StringComparison.OrdinalIgnoreCase);
    }
}
