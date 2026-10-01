using UnboundOS.Core;
using UnboundOS.Core.Models;
using UnboundOS.Core.Shell;
using UnboundOS.Infrastructure.Diagnostics;
using UnboundOS.Infrastructure.Library;
using UnboundOS.Infrastructure.Profiles;
using UnboundOS.Infrastructure.Startup;

namespace UnboundOS.Tests;

public sealed class XboxModeAndConsoleTests
{
    [Fact]
    public void XboxModePolicy_RegistrationTokens_MatchCommunityHomeApps()
    {
        Assert.Equal("UnboundInfotech.UnboundOS.FseHome", XboxModePolicy.PackageIdentity);
        Assert.Equal("windows.gamingApp", XboxModePolicy.GamingAppExtension);
        Assert.Equal("Microsoft.appCategory.gamingHome_8wekyb3d8bbwe", XboxModePolicy.GamingHomeCapability);
        Assert.Contains("ms-settings:gaming-fullscreen", XboxModePolicy.SettingsUris);
        Assert.True(XboxModePolicy.IsFullscreenRequested(["UnboundOS.App.exe", "--fullscreen"]));
        Assert.True(XboxModePolicy.IsFullscreenRequested(["--xbox-home"]));
        Assert.False(XboxModePolicy.IsFullscreenRequested(["--health"]));
    }

    [Fact]
    public void XboxModePolicy_Resolve_FallsBackWithoutPackage()
    {
        Assert.Equal(
            XboxModeHomeState.FallbackRun,
            XboxModePolicy.Resolve(wanted: true, packagePresent: false, userConfirmedSelected: false, runAutostartOn: true));
        Assert.Equal(
            XboxModeHomeState.PackageMissing,
            XboxModePolicy.Resolve(wanted: true, packagePresent: false, userConfirmedSelected: false, runAutostartOn: false));
        Assert.Equal(
            XboxModeHomeState.RegisteredNotSelected,
            XboxModePolicy.Resolve(wanted: true, packagePresent: true, userConfirmedSelected: false, runAutostartOn: true));
        Assert.Equal(
            XboxModeHomeState.Selected,
            XboxModePolicy.Resolve(wanted: true, packagePresent: true, userConfirmedSelected: true, runAutostartOn: true));
        Assert.Equal(
            XboxModeHomeState.Unavailable,
            XboxModePolicy.Resolve(wanted: false, packagePresent: false, userConfirmedSelected: false, runAutostartOn: false));
    }

    [Fact]
    public async Task XboxModeHome_EnableWithoutPackage_IsGracefulFallback()
    {
        using var temp = new TempDir();
        var pref = Path.Combine(temp.Path, "xbox-mode-home.json");
        var opened = new List<string>();
        var xbox = new WindowsXboxModeHome(
            pref,
            new WindowsShellAutostart(Path.Combine(temp.Path, "missing.exe")),
            uri =>
            {
                opened.Add(uri);
                return true;
            },
            () => false);

        var result = await xbox.SetWantedAsync(true);
        Assert.True(result.Succeeded);
        Assert.Contains("fullscreen", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("falls back", OsProductCopy.XboxModeHonesty, StringComparison.OrdinalIgnoreCase);
        var snap = xbox.Probe();
        Assert.True(snap.Wanted);
        Assert.Equal(XboxModeHomeState.PackageMissing, snap.State);
        Assert.Contains(XboxModePolicy.SettingsUris[0], opened);
        Assert.Contains("does not document", OsProductCopy.XboxModeHonesty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No IoT LTSC", OsProductCopy.XboxModeHonesty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task XboxModeHome_Disable_IsReversible()
    {
        using var temp = new TempDir();
        var xbox = new WindowsXboxModeHome(
            Path.Combine(temp.Path, "xbox-mode-home.json"),
            new WindowsShellAutostart(Path.Combine(temp.Path, "missing.exe")),
            _ => false,
            () => true);
        await xbox.SetWantedAsync(true);
        var off = await xbox.SetWantedAsync(false);
        Assert.True(off.Succeeded);
        Assert.False(xbox.IsWanted);
        Assert.False(xbox.Probe().UserConfirmedSelected);
    }

    [Fact]
    public void GuideCatalog_HasRequiredQuickActions()
    {
        foreach (var id in new[] { "volume-up", "network", "bluetooth", "hdr", "perf", "sleep", "desktop" })
        {
            Assert.NotNull(GuideMenuCatalog.Find(id));
        }

        Assert.Contains("not a D3D", GuideMenuCatalog.Find("perf")!.Hint, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("D3D", OsProductCopy.GuideHonesty, StringComparison.Ordinal);
    }

    [Fact]
    public void OnScreenKeyboard_TypesAndDeletes()
    {
        var typed = OnScreenKeyboardLayout.Apply("", OnScreenKeyboardLayout.Qwerty.First(key => key.Id == "a"));
        typed = OnScreenKeyboardLayout.Apply(typed, OnScreenKeyboardLayout.Qwerty.First(key => key.Id == "b"));
        Assert.Equal("ab", typed);
        var del = OnScreenKeyboardLayout.Qwerty.First(key => key.Id == "back");
        Assert.Equal("a", OnScreenKeyboardLayout.Apply(typed, del));
        Assert.True(OnScreenKeyboardLayout.Qwerty.Count >= 36);
    }

    [Fact]
    public void GamepadFilePicker_FiltersAndWraps()
    {
        var entries = new[]
        {
            new FileBrowseEntry("Alpha", "/a", true, null, DateTimeOffset.UnixEpoch),
            new FileBrowseEntry("Beta", "/b", false, 10, DateTimeOffset.UnixEpoch),
            new FileBrowseEntry("Gamma", "/g", false, 20, DateTimeOffset.UnixEpoch)
        };
        var picker = new GamepadFilePicker(entries, 0, "");
        Assert.Equal("Beta", picker.Move(1).Selected?.Name);
        Assert.Equal("Gamma", picker.Move(-1).Selected?.Name);
        var filtered = picker.WithFilter("ga");
        Assert.Single(filtered.Visible);
        Assert.Equal("Gamma", filtered.Selected?.Name);
    }

    [Fact]
    public void LastSession_ResumeWindow()
    {
        var now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
        var fresh = new LastSessionRecord("1", "Dota", "steam://rungameid/570", null, now.AddHours(-2), true);
        Assert.True(LastSessionPolicy.ShouldOfferResume(fresh, now));
        var stale = fresh with { LastPlayedUtc = now.AddDays(-2) };
        Assert.False(LastSessionPolicy.ShouldOfferResume(stale, now));
        Assert.False(LastSessionPolicy.ShouldOfferResume(fresh with { ResumeAfterSleep = false }, now));
    }

    [Fact]
    public async Task LastSessionStore_RoundTrip()
    {
        using var temp = new TempDir();
        var store = new JsonLastSessionStore(Path.Combine(temp.Path, "last-session.json"));
        var record = new LastSessionRecord("570", "Dota 2", "steam://rungameid/570", null, DateTimeOffset.UtcNow, true);
        await store.SaveAsync(record);
        var loaded = await store.LoadAsync();
        Assert.Equal("570", loaded?.GameId);
        await store.ClearAsync();
        Assert.Null(await store.LoadAsync());
    }

    [Fact]
    public void OneGameSwitcher_DedupesFullscreen()
    {
        var apps = new[]
        {
            new RunningApp("1", "Notepad", "notepad", 1, false),
            new RunningApp("2", "Game A", "game", 2, true),
            new RunningApp("3", "Game A extra", "game", 3, true)
        };
        var focus = OneGameSwitcher.FocusList(apps);
        Assert.Single(focus);
        Assert.Equal("game", focus[0].ProcessName);
    }

    [Fact]
    public void ControllerPointer_DeadZoneAndHonesty()
    {
        var still = ControllerPointerPolicy.FromStick(0.05, 0.02, false, false);
        Assert.Equal(0, still.Dx);
        var move = ControllerPointerPolicy.FromStick(1, 0, true, false);
        Assert.True(move.Dx > 0);
        Assert.True(move.PrimaryClick);
        Assert.Contains("No game inject", OsProductCopy.ControllerHonesty, StringComparison.Ordinal);
    }

    [Fact]
    public void ShaderPrecache_NeverHooks()
    {
        var plan = ShaderPrecachePlan.Describe(new GameProfile("570", false, 0, true, ""));
        Assert.Contains("does not hook D3D", plan, StringComparison.Ordinal);
        Assert.Contains("does not inject", plan, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("off", ShaderPrecachePlan.Describe(null), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnifiedLibrary_ReadsEpicGogAndCustom()
    {
        using var temp = new TempDir();
        var epic = temp.Dir("epic");
        File.WriteAllText(Path.Combine(epic, "game.item"), """
            {"DisplayName":"Fort Sample","AppName":"FortSample","InstallLocation":"/opt/fort","LaunchExecutable":"Fort.exe"}
            """);
        var gog = temp.Dir("gog");
        File.WriteAllText(Path.Combine(gog, "goggame-12.info"), """
            {"name":"GOG Sample","gameId":"12"}
            """);
        var customPath = Path.Combine(temp.Path, "custom-library.json");
        var custom = new JsonCustomLibraryStore(customPath);
        await custom.AddAsync(new CustomLibraryEntry("custom-1", "My Game", "/tmp/my.exe", null));

        var catalog = new UnifiedGameLibraryCatalog(
            new SteamGameLibraryCatalog(new SteamLibraryDiscoverySettings { UseDefaultWindowsLocations = false }),
            custom,
            new UnifiedLibrarySettings
            {
                EpicManifestFolders = [epic],
                GogGameFolders = [gog]
            });
        var games = await catalog.DiscoverAsync();
        Assert.Contains(games, game => game.Store == GameStore.Epic && game.DisplayName == "Fort Sample");
        Assert.Contains(games, game => game.Store == GameStore.Gog && game.Id == "gog:12");
        Assert.Contains(games, game => game.Store == GameStore.Xbox);
        Assert.Contains(games, game => game.Store == GameStore.Custom && game.Id == "custom-1");
        Assert.True(LibraryLaunchService.IsSafeLaunchUri("goggalaxy://openGameView/12"));
        Assert.True(LibraryLaunchService.IsSafeLaunchUri("xbox:"));
    }

    [Fact]
    public async Task GameProfileAndDiagnostics_WriteWithoutSecrets()
    {
        using var temp = new TempDir();
        var profiles = new JsonGameProfileStore(Path.Combine(temp.Path, "game-profiles.json"));
        await profiles.SaveAsync(new GameProfile("570", true, 144, true, "cap"));
        var loaded = await profiles.LoadAsync("570");
        Assert.True(loaded.HdrPreferred);
        Assert.Equal(144, loaded.FrameCap);

        File.WriteAllText(Path.Combine(temp.Path, "health-latest.txt"), "ok");
        var export = new DiagnosticsExportService(temp.Path);
        var result = await export.ExportAsync();
        Assert.True(result.Succeeded);
        Assert.True(Directory.Exists(result.Path));
        Assert.True(File.Exists(Path.Combine(result.Path, "honesty.txt")));
        Assert.Contains("unverified", File.ReadAllText(Path.Combine(result.Path, "honesty.txt")), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductCopy_XboxLead_IsHonest()
    {
        Assert.Contains("windows.gamingApp", OsProductCopy.XboxModeHonesty, StringComparison.Ordinal);
        Assert.Contains("HKCU Run + fullscreen", OsProductCopy.XboxModeHonesty, StringComparison.Ordinal);
        Assert.Contains("Javelin", OsProductCopy.AnticheatHonesty, StringComparison.Ordinal);
        Assert.Contains("FACEIT", OsProductCopy.AnticheatHonesty, StringComparison.Ordinal);
        Assert.Contains("Xbox mode home", OsProductCopy.HealthHonesty, StringComparison.Ordinal);
        Assert.Contains("fullscreen", OsProductCopy.AutostartHonesty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No debloat ISO", OsProductCopy.XboxModeHonesty, StringComparison.Ordinal);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "unboundos-xbox-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public string Dir(string name)
        {
            var full = System.IO.Path.Combine(Path, name);
            Directory.CreateDirectory(full);
            return full;
        }

        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { /* best-effort */ }
        }
    }
}
