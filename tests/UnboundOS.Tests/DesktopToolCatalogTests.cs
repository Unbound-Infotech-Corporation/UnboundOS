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
        Assert.Equal(7, tools.Count);
        Assert.Equal(5, tools.Count(tool => tool.Group == DesktopToolGroup.Kit));
        Assert.Equal(2, tools.Count(tool => tool.Group == DesktopToolGroup.Utility));

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
            Assert.Equal("Get", tool.AvailabilityLabel);
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
        Assert.True(DesktopToolLauncher.IsSafeGetUri("ms-windows-store://pdp/?ProductId=9nblggh4v2k6"));
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
