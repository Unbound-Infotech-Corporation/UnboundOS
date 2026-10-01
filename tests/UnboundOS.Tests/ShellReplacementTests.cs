using UnboundOS.Core;
using UnboundOS.Core.Shell;
using UnboundOS.Infrastructure.Files;
using UnboundOS.Infrastructure.Startup;

namespace UnboundOS.Tests;

public sealed class ShellReplacementTests
{
    [Fact]
    public void WatchdogPolicy_ThreeCrashesInTwoMinutes_FallsBack()
    {
        var now = DateTimeOffset.Parse("2026-10-01T00:02:00Z");
        var crashes = new[]
        {
            now.AddSeconds(-90),
            now.AddSeconds(-40),
            now.AddSeconds(-5)
        };
        Assert.True(WatchdogPolicy.ShouldFallback(crashes, now));
        Assert.Contains("explorer.exe", WatchdogPolicy.FallbackReason(crashes, now), StringComparison.Ordinal);
    }

    [Fact]
    public void WatchdogPolicy_TwoCrashes_DoesNotFallback()
    {
        var now = DateTimeOffset.Parse("2026-10-01T00:02:00Z");
        var crashes = WatchdogPolicy.Record([now.AddSeconds(-30)], now.AddSeconds(-10));
        Assert.Equal(2, crashes.Count);
        Assert.False(WatchdogPolicy.ShouldFallback(crashes, now));
    }

    [Fact]
    public void WatchdogPolicy_OldCrashes_DropOutOfWindow()
    {
        var now = DateTimeOffset.Parse("2026-10-01T00:05:00Z");
        var recorded = WatchdogPolicy.Record(
            [now.AddMinutes(-5), now.AddMinutes(-4), now.AddMinutes(-3)],
            now);
        Assert.Single(recorded);
        Assert.False(WatchdogPolicy.ShouldFallback(recorded, now));
    }

    [Fact]
    public void WatchdogPolicy_FastExit_IsCrash()
    {
        Assert.True(WatchdogPolicy.IsCrashExit(0, TimeSpan.FromSeconds(2)));
        Assert.True(WatchdogPolicy.IsCrashExit(1, TimeSpan.FromMinutes(5)));
        Assert.False(WatchdogPolicy.IsCrashExit(0, TimeSpan.FromSeconds(20)));
    }

    [Fact]
    public void WatchdogCommand_RecognizesFlags()
    {
        Assert.True(WatchdogCommand.IsWatchdog(["UnboundOS.App.exe", "--watchdog"]));
        Assert.True(WatchdogCommand.IsWatchdog(["/watchdog"]));
        Assert.True(WatchdogCommand.IsRestore(["--restore-explorer"]));
        Assert.False(WatchdogCommand.IsWatchdog(["--health"]));
        Assert.False(WatchdogCommand.IsRestore(null));
    }

    [Fact]
    public void ShellReplacement_IsHkcuOnly()
    {
        Assert.Equal(@"Software\Microsoft\Windows NT\CurrentVersion\Winlogon", WindowsShellReplacement.WinlogonKey);
        Assert.DoesNotContain("SOFTWARE\\", WindowsShellReplacement.WinlogonKey, StringComparison.Ordinal);
        Assert.Equal("Shell", WindowsShellReplacement.ShellValueName);
        Assert.True(WindowsShellReplacement.LooksLikeUnbound(@"C:\app\UnboundOS.App.exe --watchdog"));
        Assert.False(WindowsShellReplacement.LooksLikeUnbound("explorer.exe"));
    }

    [Fact]
    public async Task ShellReplacement_MissingExe_DoesNotClaimSuccess()
    {
        var replacement = new WindowsShellReplacement(
            Path.Combine(Path.GetTempPath(), "missing-unboundos-" + Guid.NewGuid().ToString("N") + ".exe"));
        var result = await replacement.SetEnabledAsync(true);
        Assert.False(result.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));
        Assert.Contains("HKCU", OsProductCopy.ShellReplacementHonesty, StringComparison.Ordinal);
    }

    [Fact]
    public void OsSettingsCatalog_EveryEntryHasMsSettingsFallback()
    {
        Assert.Equal(14, OsSettingsCatalog.All.Count);
        Assert.All(OsSettingsCatalog.All, entry =>
        {
            Assert.StartsWith("ms-settings:", entry.Uri, StringComparison.Ordinal);
            Assert.False(string.IsNullOrWhiteSpace(entry.Title));
        });
        Assert.NotNull(OsSettingsCatalog.Find("updates"));
        Assert.NotNull(OsSettingsCatalog.Find("accounts"));
        Assert.NotNull(OsSettingsCatalog.Find("xboxmode"));
        Assert.NotNull(OsSettingsCatalog.Find("signin"));
    }

    [Fact]
    public void ProductCopy_ShellReplacementIsHonestAboutAnticheat()
    {
        Assert.Contains("HKCU", OsProductCopy.ShellReplacementHonesty, StringComparison.Ordinal);
        Assert.Contains("never HKLM", OsProductCopy.ShellReplacementHonesty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unverified", OsProductCopy.AnticheatHonesty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not rename explorer.exe", OsProductCopy.AnticheatHonesty, StringComparison.Ordinal);
        Assert.Contains("HKCU Run", OsProductCopy.AutostartHonesty, StringComparison.Ordinal);
        Assert.Contains("opt-in", OsProductCopy.FilesHonesty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FileBrowser_CopyMoveDelete_OnTempTree()
    {
        var root = Path.Combine(Path.GetTempPath(), "unbound-files-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var src = Path.Combine(root, "a.txt");
            File.WriteAllText(src, "hello");
            var destDir = Path.Combine(root, "out");
            Directory.CreateDirectory(destDir);
            var browser = new LocalFileBrowser();
            var copy = browser.Copy(src, destDir);
            Assert.True(copy.Ok);
            Assert.True(File.Exists(Path.Combine(destDir, "a.txt")));
            var movedDir = Path.Combine(root, "moved");
            Directory.CreateDirectory(movedDir);
            var move = browser.Move(Path.Combine(destDir, "a.txt"), movedDir);
            Assert.True(move.Ok);
            Assert.True(File.Exists(Path.Combine(movedDir, "a.txt")));
            var delete = browser.Delete(Path.Combine(movedDir, "a.txt"));
            Assert.True(delete.Ok);
            Assert.False(File.Exists(Path.Combine(movedDir, "a.txt")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
