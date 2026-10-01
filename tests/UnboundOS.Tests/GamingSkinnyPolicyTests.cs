using UnboundOS.Core.Brand;
using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Profiles;
using UnboundOS.Infrastructure.Session;

namespace UnboundOS.Tests;

public sealed class GamingSkinnyPolicyTests
{
    [Fact]
    public void Tokens_StayOnThePackagedPalette()
    {
        Assert.Equal("#05070A", UnboundTokens.Obsidian);
        Assert.Equal("#00F0FF", UnboundTokens.Cyan);
        Assert.Equal("#F7FAFC", UnboundTokens.Paper);
    }

    [Fact]
    public async Task Apply_RecordsCompetitivePlan_WithoutTouchingDefender()
    {
        var policy = new WindowsGamingSkinnyPolicy();
        var competitive = JsonProfileStore.CreateDefaults().First(p => p.Id == "competitive");

        var snap = await policy.ApplyAsync(competitive);

        Assert.Contains(snap.Actions, line => line.Contains("Game DVR", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snap.Actions, line => line.Contains("Game Mode", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snap.Actions, line => line.Contains("Performance", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snap.Actions, line => line.Contains("Defender", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(snap.Actions, line => line.Contains("BCDEdit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(snap.Actions, line => line.Contains("HPET", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Apply_StreamerKeepsCapture()
    {
        var policy = new WindowsGamingSkinnyPolicy();
        var streamer = JsonProfileStore.CreateDefaults().First(p => p.Id == "streamer");

        var snap = await policy.ApplyAsync(streamer);

        Assert.DoesNotContain(snap.Actions, line => line.Contains("Game DVR / background capture off", StringComparison.Ordinal));
        Assert.Contains(snap.Actions, line => line.Contains("Defender", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Restore_NullSnapshot_Succeeds()
    {
        var policy = new WindowsGamingSkinnyPolicy();
        var result = await policy.RestoreAsync(null);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void NicPack_IsFetchNotGitBinaries()
    {
        var root = FindRepoRoot();
        var script = File.ReadAllText(Path.Combine(root, "scripts", "fetch-offline-nic-pack.ps1"));
        var manifest = File.ReadAllText(Path.Combine(root, "installer", "offline-nic-pack", "manifest.json"));
        Assert.Contains("5368709120", script);
        Assert.Contains("intel", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("realtek", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("image-build fetch", manifest, StringComparison.Ordinal);
        Assert.DoesNotContain("ghost spectre", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Hags_IsAnInformedToggle_NotAForcedOn()
    {
        var policy = new WindowsGamingSkinnyPolicy();
        var on = await policy.TrySetHagsAsync(true);
        var off = await policy.TrySetHagsAsync(false);
        Assert.Contains("frametimes", on.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not forced", on.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("frametimes", off.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not forced", off.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "UnboundOS.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("UnboundOS.sln not found from test base directory.");
    }
}
