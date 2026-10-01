using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Mods;
using UnboundOS.Infrastructure.Process;
using UnboundOS.Infrastructure.Profiles;

namespace UnboundOS.Tests;

public sealed class VortexIntegrationTests
{
    [Fact]
    public void Discover_ReadsUserDataGameFoldersAndSkipsStateDb()
    {
        using var temp = new TempTree();
        var userData = temp.Dir("Vortex");
        Directory.CreateDirectory(Path.Combine(userData, "state.v2", "LOCK"));
        Directory.CreateDirectory(Path.Combine(userData, "downloads", "baldursgate3"));
        var mods = temp.Dir("Vortex", "baldursgate3", "mods", "NativeCameras-42");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(mods)!, VortexCatalogService.StagingMarkerName), "ok");
        var profileDir = temp.Dir("Vortex", "baldursgate3", "profiles", "abc123");
        File.WriteAllText(Path.Combine(profileDir, "meta.json"), """{"name":"Co-op"}""");

        var catalog = new VortexCatalogService(Isolated(userData));
        var games = catalog.Discover(CancellationToken.None);

        var game = Assert.Single(games);
        Assert.Equal(ModProvider.NexusMods, game.Provider);
        Assert.Equal("baldursgate3", game.VortexGameId);
        Assert.Equal("abc123", game.VortexProfileId);
        Assert.Contains("Baldur's Gate 3", game.DisplayName, StringComparison.Ordinal);
        Assert.Contains("Co-op", game.DisplayName, StringComparison.Ordinal);
        Assert.Equal(ModManagementLevel.ExternalHandoff, game.Capabilities.ManagementLevel);
        Assert.False(game.Capabilities.CanToggle);
        Assert.False(game.Capabilities.CanInstall);
        Assert.Contains("NativeCameras", Assert.Single(game.Mods).DisplayName, StringComparison.Ordinal);
        Assert.DoesNotContain(games, item => item.VortexGameId == "state.v2");
        Assert.DoesNotContain(games, item => item.VortexGameId == "downloads");
    }

    [Fact]
    public void Discover_ReadsExtraStagingRoot()
    {
        using var temp = new TempTree();
        var staging = temp.Dir("vmods", "cyberpunk2077", "ArchiveXL-1");
        File.WriteAllText(Path.Combine(temp.Path, "vmods", "cyberpunk2077", VortexCatalogService.StagingMarkerName), "ok");

        var catalog = new VortexCatalogService(new VortexDiscoverySettings
        {
            UseDefaultWindowsLocations = false,
            ExtraStagingRoots = [Path.Combine(temp.Path, "vmods")],
            FindExecutable = () => null,
            IsVortexRunning = () => false,
            TryReadState = (_, _, _) => null
        });

        var game = Assert.Single(catalog.Discover(CancellationToken.None));
        Assert.Equal("cyberpunk2077", game.VortexGameId);
        Assert.Equal("Cyberpunk 2077", game.DisplayName);
        Assert.Contains("ArchiveXL", Assert.Single(game.Mods).DisplayName, StringComparison.Ordinal);
        Assert.StartsWith(staging[..^"ArchiveXL-1".Length].TrimEnd(Path.DirectorySeparatorChar), game.InstallPath);
    }

    [Fact]
    public void Discover_EmptyRoots_ReturnsNothingNotADemo()
    {
        using var temp = new TempTree();
        var catalog = new VortexCatalogService(Isolated(temp.Dir("empty")));
        Assert.Empty(catalog.Discover(CancellationToken.None));
    }

    [Fact]
    public void UnifiedMerge_KeepsWorkshopAndVortexSideBySide()
    {
        var workshop = new ModGame(
            "1086940",
            "Baldur's Gate 3",
            null,
            ModProvider.SteamWorkshop,
            ModCapabilities.SteamDiscoveryOnly,
            [],
            "steam-workshop-readonly",
            "1086940");
        var vortex = VortexCatalogService.CreateGame("baldursgate3", @"C:\staging\bg3", []);

        var merged = UnifiedModCatalogService.Merge([workshop], [vortex]);

        Assert.Equal(2, merged.Count);
        Assert.Contains(merged, game => game.Provider == ModProvider.SteamWorkshop);
        Assert.Contains(merged, game => game.Provider == ModProvider.NexusMods);
        Assert.DoesNotContain(merged, game => game.Provider == ModProvider.BuiltIn);
    }

    [Fact]
    public void UnifiedMerge_BothEmpty_UsesExistingPreviewNotVortexDemo()
    {
        var merged = UnifiedModCatalogService.Merge([], []);
        Assert.All(merged, game => Assert.Equal(ModProvider.BuiltIn, game.Provider));
    }

    [Fact]
    public void LaunchArguments_AreReadOnlyGameAndProfile()
    {
        var args = VortexLocator.BuildLaunchArguments("baldursgate3", "abc123");
        Assert.Equal("--game baldursgate3 --profile abc123", args);
        Assert.False(VortexLocator.LooksLikeWriteSwitch(args));
        Assert.True(VortexLocator.LooksLikeWriteSwitch("--set settings.foo=1"));
        Assert.True(VortexLocator.LooksLikeWriteSwitch("--del persistent.profiles"));
        Assert.True(VortexLocator.LooksLikeWriteSwitch("--restore backup"));
        Assert.True(VortexLocator.LooksLikeWriteSwitch("--merge backup"));
    }

    [Fact]
    public async Task CompositeHandoff_OpenVortex_UsesLauncher()
    {
        using var temp = new TempTree();
        var exe = Path.Combine(temp.Path, "Vortex.exe");
        File.WriteAllText(exe, "stub");
        var launcher = new VortexLauncher(
            Isolated(temp.Path) with { FindExecutable = () => exe },
            (_, _) => true);
        var handoff = new CompositeExternalModHandoff(new SteamExternalModHandoff(), launcher);

        var result = await handoff.OpenVortexAsync("fallout76", null);

        Assert.True(result.Succeeded);
        Assert.Contains("Fallout 76", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Open_ReportsVortexNotFound()
    {
        var launcher = new VortexLauncher(Isolated(Path.GetTempPath()), (_, _) => true);
        var result = launcher.Open("baldursgate3", null);
        Assert.False(result.Succeeded);
        Assert.Contains("Vortex not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Open_StartsVortexWithGameSwitch()
    {
        using var temp = new TempTree();
        var exe = Path.Combine(temp.Path, "Vortex.exe");
        File.WriteAllText(exe, "stub");
        string? startedArgs = null;
        var settings = Isolated(temp.Path) with { FindExecutable = () => exe };
        var launcher = new VortexLauncher(settings, (_, args) =>
        {
            startedArgs = args;
            return true;
        });

        var result = launcher.Open("helldivers2", "p1");

        Assert.True(result.Succeeded);
        Assert.Equal("--game helldivers2 --profile p1", startedArgs);
        Assert.Contains("Helldivers 2", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VortexAdapter_RefusesToMutateStaging()
    {
        var adapter = new VortexReadOnlyAdapter();
        var game = VortexCatalogService.CreateGame("fallout4", @"C:\staging\fo4", []);
        Assert.True(adapter.CanHandle(game));
        Assert.False(adapter.GetCapabilities(game).CanToggle);

        var apply = await adapter.ApplyProfileAsync(
            game,
            new ModProfile("p", game.GameId, "Draft", [], DateTimeOffset.UtcNow));
        Assert.False(apply.Succeeded);
        Assert.Contains("will not write staging", apply.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultProfiles_ProtectVortexAndNeverDenyIt()
    {
        Assert.True(ProcessGuardian.IsAlwaysProtected("Vortex"));
        Assert.True(ProcessGuardian.IsAlwaysProtected("Vortex.exe"));

        foreach (var profile in JsonProfileStore.CreateDefaults())
        {
            Assert.Contains("Vortex", profile.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain("Vortex", profile.TerminateProcessNames, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ExtractPaths_ReadsInstallPathDumpWithoutTouchingStateDb()
    {
        const string json = """{"baldursgate3":"F:\\Vortex Mods\\baldursgate3","downloads":"C:\\Users\\x\\AppData\\Roaming\\Vortex\\state.v2"}""";
        var paths = VortexCatalogService.ExtractPaths(json).ToArray();
        Assert.Contains(@"F:\Vortex Mods\baldursgate3", paths);
        Assert.DoesNotContain(paths, path => path.Contains("state.v2", StringComparison.OrdinalIgnoreCase));
    }

    private static VortexDiscoverySettings Isolated(string userData) => new()
    {
        UseDefaultWindowsLocations = false,
        UserDataRoots = [userData],
        ExtraStagingRoots = [],
        FindExecutable = () => null,
        IsVortexRunning = () => false,
        TryReadState = (_, _, _) => null
    };

    private sealed class TempTree : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "unboundos-vortex-" + Guid.NewGuid().ToString("N"));

        public TempTree() => Directory.CreateDirectory(Path);

        public string Dir(params string[] parts)
        {
            var full = System.IO.Path.Combine(new[] { Path }.Concat(parts).ToArray());
            Directory.CreateDirectory(full);
            return full;
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { /* best-effort */ }
        }
    }
}
