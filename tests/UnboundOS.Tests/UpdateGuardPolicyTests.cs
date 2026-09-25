using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Infrastructure.Session;
using UnboundOS.Infrastructure.Updates;

namespace UnboundOS.Tests;

public sealed class UpdateGuardPolicyTests
{
    [Fact]
    public async Task Apply_AlwaysMentionsMicrosoftSource_EvenWhenDenied()
    {
        var policy = new WindowsUpdateGuardPolicy();
        var apply = await policy.TryApplyUpdateGuardAsync();
        AssertLegal(apply.Message);
        Assert.DoesNotContain("CVE-only", apply.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("0FA1201D", apply.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Clear_AlwaysMentionsMicrosoftSource_EvenWhenDenied()
    {
        var policy = new WindowsUpdateGuardPolicy();
        var clear = await policy.TryClearUpdateGuardAsync();
        AssertLegal(clear.Message);
        Assert.Contains("Defender", clear.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenWindowsUpdate_StaysOnMicrosoft()
    {
        var policy = new WindowsUpdateGuardPolicy();
        var open = await policy.OpenWindowsUpdateSettingsAsync();
        AssertLegal(open.Message);
    }

    [Fact]
    public async Task Probe_ReportsShellVersion_WithoutInstalling()
    {
        var policy = new WindowsUpdateGuardPolicy();
        var status = await policy.ProbeQualityStatusAsync();
        Assert.False(string.IsNullOrWhiteSpace(status.ShellVersion));
        Assert.Contains(status.QualityLabel, ["unknown", "current", "pending", "needs restart"]);
        Assert.Contains("0.1.0", status.ShellVersion, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SessionEnter_DoesNotApplyUpdateGuard()
    {
        var skinny = new WindowsGamingSkinnyPolicy();
        var profile = new UnboundOS.Core.Models.SessionProfile
        {
            Id = "competitive",
            Name = "Competitive Edge",
            ApplyGameMode = true
        };
        var snap = await skinny.ApplyAsync(profile);
        Assert.DoesNotContain("Update Guard", string.Join(' ', snap.Actions), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TargetReleaseVersion", string.Join(' ', snap.Actions), StringComparison.OrdinalIgnoreCase);
        Assert.Contains(snap.Actions, line => line.Contains("Defender", StringComparison.OrdinalIgnoreCase));

        var ctor = typeof(UnboundOS.Infrastructure.Session.SessionEngine).GetConstructors()[0];
        Assert.DoesNotContain(ctor.GetParameters(), p => p.ParameterType == typeof(IUpdateGuardPolicy));
    }

    [Fact]
    public void Honesty_RefusesCveOnlyClaim()
    {
        Assert.Contains("Microsoft", OsProductCopy.UpdateGuardHonesty, StringComparison.Ordinal);
        Assert.Contains("does not redistribute", OsProductCopy.UpdateGuardHonesty, StringComparison.Ordinal);
        Assert.Contains("not CVE-only", OsProductCopy.UpdateGuardHonesty, StringComparison.Ordinal);
    }

    [Fact]
    public void BulletinSample_IsMetadataOnly()
    {
        var root = FindRepoRoot();
        var json = File.ReadAllText(Path.Combine(root, "docs", "update-guard-bulletin.sample.json"));
        Assert.Contains("does not host", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("recommendedLcuKb", json, StringComparison.Ordinal);
        Assert.Contains("Metadata only", json, StringComparison.Ordinal);
    }

    private static void AssertLegal(string message)
    {
        Assert.Contains("Microsoft", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not redistribute", message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Elevation may be required", message, StringComparison.OrdinalIgnoreCase);
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
