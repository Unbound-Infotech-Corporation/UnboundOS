using UnboundOS.Core.Models;
using UnboundOS.Infrastructure.Mods;
using UnboundOS.Infrastructure.Profiles;
using UnboundOS.Infrastructure.Tools;

namespace UnboundOS.Tests;

public sealed class DesktopToolCatalogTests
{
    [Fact]
    public void Discover_FakePaths_MarksInstalledAndGetHonestly()
    {
        using var temp = new TempTree();
        var obs = temp.File("obs64.exe");
        var steam = temp.File("steam.exe");

        var catalog = new DesktopToolCatalog(new DesktopToolDiscoverySettings
        {
            UseDefaultWindowsLocations = false,
            ForcedExecutables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [DesktopToolIds.Obs] = obs,
                [DesktopToolIds.Steam] = steam,
                [DesktopToolIds.Vortex] = null,
                [DesktopToolIds.Discord] = null,
                [DesktopToolIds.Playnite] = null
            }
        });

        var tools = catalog.Discover();
        Assert.Equal(15, tools.Count);
        Assert.Equal(12, tools.Count(tool => tool.Group == DesktopToolGroup.Kit));
        Assert.Equal(3, tools.Count(tool => tool.Group == DesktopToolGroup.Utility));

        var obsTool = Assert.Single(tools, tool => tool.Id == DesktopToolIds.Obs);
        Assert.True(obsTool.IsInstalled);
        Assert.Equal("Installed", obsTool.AvailabilityLabel);
        Assert.Equal("OPEN", obsTool.StatusLabel);
        Assert.Equal("O", obsTool.Monogram);
        Assert.True(obsTool.HasObsRecipe);
        Assert.Equal(obs, obsTool.ExecutablePath);
        Assert.Equal(DesktopToolCatalog.ObsGetPath.Uri, obsTool.GetPath.Uri);

        var vortex = Assert.Single(tools, tool => tool.Id == DesktopToolIds.Vortex);
        Assert.False(vortex.IsInstalled);
        Assert.Equal("Get", vortex.AvailabilityLabel);
        Assert.Equal("GET", vortex.StatusLabel);
        Assert.Equal("V", vortex.Monogram);
        Assert.Null(vortex.ExecutablePath);

        Assert.False(Assert.Single(tools, tool => tool.Id == DesktopToolIds.Discord).IsInstalled);
        Assert.True(Assert.Single(tools, tool => tool.Id == DesktopToolIds.Steam).IsInstalled);
        Assert.Equal(DesktopToolGroup.Utility, Assert.Single(tools, tool => tool.Id == DesktopToolIds.NotepadPlusPlus).Group);
        Assert.Equal(DesktopToolGroup.Utility, Assert.Single(tools, tool => tool.Id == DesktopToolIds.SevenZip).Group);
        Assert.Equal(DesktopToolGroup.Utility, Assert.Single(tools, tool => tool.Id == DesktopToolIds.HwInfo).Group);
        Assert.False(Assert.Single(tools, tool => tool.Id == DesktopToolIds.HwInfo).IsInstalled);

        var rainmeter = Assert.Single(tools, tool => tool.Id == DesktopToolIds.Rainmeter);
        Assert.False(rainmeter.IsInstalled);
        Assert.Equal(DesktopToolCatalog.RainmeterGetPath.Uri, rainmeter.GetPath.Uri);
        Assert.Equal("https://www.rainmeter.net/", rainmeter.GetPath.Uri);

        var phenix = Assert.Single(tools, tool => tool.Id == DesktopToolIds.Phenix);
        Assert.False(phenix.IsInstalled);
        Assert.True(phenix.OpensViaUri);
        Assert.True(phenix.CanOpen);
        Assert.Equal("OPEN", phenix.StatusLabel);
        Assert.Equal(DesktopToolCatalog.PhenixGetPath.Uri, phenix.GetPath.Uri);
        Assert.Equal("https://visualskins.com/skin/phenix", phenix.OpenPath.Uri);
        Assert.Contains("Phenix", phenix.Job, StringComparison.Ordinal);
        Assert.DoesNotContain("Minimalistic", phenix.Job, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".rmskin", phenix.GetPath.Uri, StringComparison.OrdinalIgnoreCase);

        var clock = Assert.Single(tools, tool => tool.Id == DesktopToolIds.MinimalisticClock);
        Assert.False(clock.IsInstalled);
        Assert.Equal(DesktopToolCatalog.MinimalisticClockGetPath.Uri, clock.GetPath.Uri);
        Assert.Equal("https://visualskins.com/skin/minimalistic-clock", clock.GetPath.Uri);
        Assert.Contains("clock", clock.Job, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".rmskin", clock.GetPath.Uri, StringComparison.OrdinalIgnoreCase);

        var musicBee = Assert.Single(tools, tool => tool.Id == DesktopToolIds.MusicBee);
        Assert.False(musicBee.IsInstalled);
        Assert.Equal(DesktopToolCatalog.MusicBeeGetPath.Uri, musicBee.GetPath.Uri);
        Assert.Equal("https://getmusicbee.com/downloads/", musicBee.GetPath.Uri);

        var visualizers = Assert.Single(tools, tool => tool.Id == DesktopToolIds.Visualizers);
        Assert.False(visualizers.IsInstalled);
        Assert.False(visualizers.CanOpen);
        Assert.Equal(DesktopToolCatalog.VisualizersGetPath.Uri, visualizers.GetPath.Uri);
        Assert.Contains("github.com/MarcoPixel/Monstercat-Visualizer", visualizers.GetPath.Uri, StringComparison.Ordinal);

        var store = Assert.Single(tools, tool => tool.Id == DesktopToolIds.Store);
        Assert.False(store.IsInstalled);
        Assert.True(store.OpensViaUri);
        Assert.True(store.CanOpen);
        Assert.Equal("OPEN", store.StatusLabel);
        Assert.Equal("ms-windows-store://home", store.OpenPath.Uri);

        var xbox = Assert.Single(tools, tool => tool.Id == DesktopToolIds.Xbox);
        Assert.False(xbox.IsInstalled);
        Assert.True(xbox.OpensViaUri);
        Assert.True(xbox.CanOpen);
        Assert.Equal("xbox:", xbox.OpenPath.Uri);
        Assert.Equal("ms-windows-store://pdp/?ProductId=9MV0B5HZVK9Z", xbox.GetPath.Uri);
    }

    [Fact]
    public void Discover_NotepadPlusPlus_UsesForcedPathAndOfficialGet()
    {
        using var temp = new TempTree();
        var exe = temp.File("notepad++.exe");
        var catalog = new DesktopToolCatalog(new DesktopToolDiscoverySettings
        {
            UseDefaultWindowsLocations = false,
            ForcedExecutables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                [DesktopToolIds.NotepadPlusPlus] = exe
            }
        });

        var notepad = Assert.Single(catalog.Discover(), tool => tool.Id == DesktopToolIds.NotepadPlusPlus);
        Assert.True(notepad.IsInstalled);
        Assert.Equal(exe, notepad.ExecutablePath);
        Assert.Equal(DesktopToolGroup.Utility, notepad.Group);
        Assert.Equal(DesktopToolCatalog.NotepadPlusPlusGetPath.Uri, notepad.GetPath.Uri);
        Assert.False(notepad.HasObsRecipe);
    }

    [Fact]
    public void Discover_NoDefaults_DoesNotInventInstalls()
    {
        var catalog = new DesktopToolCatalog(new DesktopToolDiscoverySettings
        {
            UseDefaultWindowsLocations = false
        });

        Assert.All(catalog.Discover(), tool =>
        {
            Assert.False(tool.IsInstalled);
            if (tool.OpensViaUri)
            {
                Assert.True(tool.CanOpen);
                Assert.Equal("OPEN", tool.StatusLabel);
            }
            else
            {
                Assert.Equal("Get", tool.AvailabilityLabel);
                Assert.False(tool.CanOpen);
            }
        });
    }

    [Fact]
    public void GetPaths_AreOfficialHttps()
    {
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.ObsGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.VortexGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.DiscordGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.PlayniteGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.SteamGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.NotepadPlusPlusGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.SevenZipGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.HwInfoGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.RainmeterGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.PhenixGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.MinimalisticClockGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.MusicBeeGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.VisualizersGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.StoreGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.XboxGetPath.Uri));
        Assert.True(DesktopToolLauncher.IsSafeGetUri(DesktopToolCatalog.XboxLaunchPath.Uri));
        Assert.Equal("https://visualskins.com/skin/phenix", DesktopToolCatalog.PhenixGetPath.Uri);
        Assert.True(DesktopToolLauncher.IsSafeGetUri("ms-windows-store://pdp/?ProductId=9nblggh4v2k6"));
        Assert.True(DesktopToolLauncher.IsSafeGetUri("xbox:"));
        Assert.False(DesktopToolLauncher.IsSafeGetUri("http://obsproject.com/download"));
        Assert.False(DesktopToolLauncher.IsSafeGetUri(@"C:\setup.exe"));
        Assert.False(DesktopToolLauncher.IsSafeGetUri("file:///tmp/obs.exe"));
    }

    [Fact]
    public async Task Launch_MissingTool_AsksToGet()
    {
        var launcher = new DesktopToolLauncher(new VortexLauncher(IsolatedVortex()), (_, _) => true, _ => true);
        var tool = new DesktopTool(
            DesktopToolIds.Obs,
            "OBS Studio",
            "Capture",
            false,
            null,
            ["obs64"],
            DesktopToolCatalog.ObsGetPath,
            HasObsRecipe: true);

        var result = await launcher.LaunchAsync(tool);
        Assert.False(result.Succeeded);
        Assert.Contains("Get", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Launch_ObsStub_StartsExecutable()
    {
        using var temp = new TempTree();
        var exe = temp.File("obs64.exe");
        string? started = null;
        var launcher = new DesktopToolLauncher(
            new VortexLauncher(IsolatedVortex()),
            (path, _) =>
            {
                started = path;
                return true;
            },
            _ => true);

        var result = await launcher.LaunchAsync(new DesktopTool(
            DesktopToolIds.Obs,
            "OBS Studio",
            "Capture",
            true,
            exe,
            ["obs64"],
            DesktopToolCatalog.ObsGetPath,
            HasObsRecipe: true));

        Assert.True(result.Succeeded);
        Assert.Equal(exe, started);
    }

    [Fact]
    public async Task Launch_NotepadPlusPlus_StartsExecutable()
    {
        using var temp = new TempTree();
        var exe = temp.File("notepad++.exe");
        string? started = null;
        var launcher = new DesktopToolLauncher(
            new VortexLauncher(IsolatedVortex()),
            (path, _) =>
            {
                started = path;
                return true;
            },
            _ => true);

        var result = await launcher.LaunchAsync(new DesktopTool(
            DesktopToolIds.NotepadPlusPlus,
            "Notepad++",
            "Edit text",
            true,
            exe,
            ["notepad++"],
            DesktopToolCatalog.NotepadPlusPlusGetPath,
            DesktopToolGroup.Utility));

        Assert.True(result.Succeeded);
        Assert.Equal(exe, started);
    }

    [Fact]
    public async Task Launch_Vortex_UsesExistingHandoff()
    {
        using var temp = new TempTree();
        var exe = temp.File("Vortex.exe");
        var vortex = new VortexLauncher(
            IsolatedVortex() with { FindExecutable = () => exe },
            (_, _) => true);
        var launcher = new DesktopToolLauncher(vortex, (_, _) => throw new InvalidOperationException("should use VortexLauncher"), _ => true);

        var result = await launcher.LaunchAsync(new DesktopTool(
            DesktopToolIds.Vortex,
            "Vortex",
            "Mods",
            true,
            exe,
            ["Vortex"],
            DesktopToolCatalog.VortexGetPath));

        Assert.True(result.Succeeded);
        Assert.Contains("Vortex", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Launch_Rainmeter_UsesRainmeterHandoff()
    {
        using var temp = new TempTree();
        var exe = temp.File("Rainmeter.exe");
        string? started = null;
        var rainmeter = new RainmeterLauncher(
            new DesktopToolDiscoverySettings
            {
                UseDefaultWindowsLocations = false,
                ForcedExecutables = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    [DesktopToolIds.Rainmeter] = exe
                }
            },
            (path, args) =>
            {
                started = path;
                Assert.Equal(string.Empty, args);
                return true;
            });
        var launcher = new DesktopToolLauncher(
            new VortexLauncher(IsolatedVortex()),
            rainmeter,
            (_, _) => throw new InvalidOperationException("should use RainmeterLauncher"),
            _ => true);

        var result = await launcher.LaunchAsync(new DesktopTool(
            DesktopToolIds.Rainmeter,
            "Rainmeter",
            "Skins",
            true,
            exe,
            ["Rainmeter"],
            DesktopToolCatalog.RainmeterGetPath));

        Assert.True(result.Succeeded);
        Assert.Equal(exe, started);
        Assert.Contains("does not rewrite", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Launch_StoreUri_OpensOfficialScheme()
    {
        string? opened = null;
        var launcher = new DesktopToolLauncher(new VortexLauncher(IsolatedVortex()), (_, _) => true, uri =>
        {
            opened = uri;
            return true;
        });

        var result = await launcher.LaunchAsync(new DesktopTool(
            DesktopToolIds.Store,
            "Microsoft Store",
            "Store",
            false,
            null,
            [],
            DesktopToolCatalog.StoreGetPath,
            OpensViaUri: true,
            LaunchPath: DesktopToolCatalog.StoreGetPath));

        Assert.True(result.Succeeded);
        Assert.Equal("ms-windows-store://home", opened);
    }

    [Fact]
    public async Task Launch_XboxUri_OpensXboxScheme()
    {
        string? opened = null;
        var launcher = new DesktopToolLauncher(new VortexLauncher(IsolatedVortex()), (_, _) => true, uri =>
        {
            opened = uri;
            return true;
        });

        var result = await launcher.LaunchAsync(new DesktopTool(
            DesktopToolIds.Xbox,
            "Xbox",
            "Xbox",
            false,
            null,
            [],
            DesktopToolCatalog.XboxGetPath,
            OpensViaUri: true,
            LaunchPath: DesktopToolCatalog.XboxLaunchPath));

        Assert.True(result.Succeeded);
        Assert.Equal("xbox:", opened);
    }

    [Fact]
    public async Task Launch_Phenix_OpensOfficialPage()
    {
        string? opened = null;
        var launcher = new DesktopToolLauncher(new VortexLauncher(IsolatedVortex()), (_, _) => true, uri =>
        {
            opened = uri;
            return true;
        });

        var result = await launcher.LaunchAsync(new DesktopTool(
            DesktopToolIds.Phenix,
            "Phenix",
            "Starter",
            false,
            null,
            [],
            DesktopToolCatalog.PhenixGetPath,
            OpensViaUri: true,
            LaunchPath: DesktopToolCatalog.PhenixGetPath));

        Assert.True(result.Succeeded);
        Assert.Equal("https://visualskins.com/skin/phenix", opened);
    }

    [Fact]
    public async Task Get_OpensOfficialUri()
    {
        string? opened = null;
        var launcher = new DesktopToolLauncher(new VortexLauncher(IsolatedVortex()), (_, _) => true, uri =>
        {
            opened = uri;
            return true;
        });

        var result = await launcher.OpenGetPathAsync(new DesktopTool(
            DesktopToolIds.Obs,
            "OBS Studio",
            "Capture",
            false,
            null,
            ["obs64"],
            DesktopToolCatalog.ObsGetPath,
            HasObsRecipe: true));

        Assert.True(result.Succeeded);
        Assert.Equal(DesktopToolCatalog.ObsGetPath.Uri, opened);
        Assert.Contains("does not download", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DefaultProfiles_KeepCompetitiveAndStreamerKitSeparate()
    {
        var defaults = JsonProfileStore.CreateDefaults();
        var competitive = Assert.Single(defaults, profile => profile.Id == "competitive");
        var streamer = Assert.Single(defaults, profile => profile.Id == "streamer");
        var living = Assert.Single(defaults, profile => profile.Id == "living-room");

        Assert.Contains("Discord", competitive.TerminateProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Discord", competitive.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Vortex", competitive.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);

        Assert.Contains("obs64", streamer.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("discord", streamer.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Vortex", streamer.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Steam", streamer.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Discord", streamer.TerminateProcessNames, StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Playnite.DesktopApp", living.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Steam", living.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Rainmeter", living.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("MusicBee", living.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Rainmeter", competitive.TerminateProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Rainmeter", competitive.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Rainmeter", streamer.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("MusicBee", streamer.ProtectProcessNames, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Rainmeter", streamer.TerminateProcessNames, StringComparer.OrdinalIgnoreCase);

        Assert.True(competitive.DisableGameDvr);
        Assert.True(competitive.VisualEffectsPerformance);
        Assert.True(competitive.ApplyGameMode);
        Assert.False(streamer.DisableGameDvr);
        Assert.True(streamer.VisualEffectsPerformance);
        Assert.False(living.DisableGameDvr);
        Assert.False(living.VisualEffectsPerformance);
        Assert.Contains("Game DVR off", competitive.SkinnySummary, StringComparison.Ordinal);
        Assert.Contains("Defender", competitive.SkinnySummary, StringComparison.Ordinal);
    }

    private static VortexDiscoverySettings IsolatedVortex() => new()
    {
        UseDefaultWindowsLocations = false,
        FindExecutable = () => null,
        IsVortexRunning = () => false,
        TryReadState = (_, _, _) => null
    };

    private sealed class TempTree : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "unboundos-tools-" + Guid.NewGuid().ToString("N"));

        public TempTree() => Directory.CreateDirectory(Path);

        public string File(string name)
        {
            var full = System.IO.Path.Combine(Path, name);
            System.IO.File.WriteAllText(full, "stub");
            return full;
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { /* best-effort */ }
        }
    }
}
