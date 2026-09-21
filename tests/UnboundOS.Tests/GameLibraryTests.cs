using UnboundOS.Core.Models;
using UnboundOS.Core.Navigation;
using UnboundOS.Infrastructure.Library;
using UnboundOS.Infrastructure.Profiles;

namespace UnboundOS.Tests;

public sealed class GameLibraryTests
{
    [Fact]
    public async Task SteamCatalog_ReadsInstalledGames_AndSkipsRuntimes()
    {
        var root = Path.Combine(Path.GetTempPath(), "unboundos-steam-" + Guid.NewGuid().ToString("N"));
        var apps = Path.Combine(root, "steamapps");
        Directory.CreateDirectory(apps);
        File.WriteAllText(Path.Combine(apps, "appmanifest_570.acf"), """
            "AppState"
            {
                "appid"		"570"
                "name"		"Dota 2"
                "installdir"		"dota 2 beta"
                "StateFlags"		"4"
            }
            """);
        File.WriteAllText(Path.Combine(apps, "appmanifest_228980.acf"), """
            "AppState"
            {
                "appid"		"228980"
                "name"		"Steamworks Common Redistributables"
                "installdir"		"Steamworks Shared"
                "StateFlags"		"4"
            }
            """);
        File.WriteAllText(Path.Combine(apps, "appmanifest_7.acf"), """
            "AppState"
            {
                "appid"		"7"
                "name"		"Not Installed"
                "StateFlags"		"1"
            }
            """);

        try
        {
            var catalog = new SteamGameLibraryCatalog(new SteamLibraryDiscoverySettings
            {
                UseDefaultWindowsLocations = false,
                ExtraLibraryFolders = [root]
            });
            var games = await catalog.DiscoverAsync();
            Assert.Single(games);
            Assert.Equal("570", games[0].Id);
            Assert.Equal("Dota 2", games[0].DisplayName);
            Assert.Equal(GameStore.Steam, games[0].Store);
            Assert.Equal("steam://rungameid/570", games[0].LaunchUri);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Browse_PadsEmptyLibrary_WithSessionAndKnownKit()
    {
        var tools = new[]
        {
            new DesktopTool(
                DesktopToolIds.Steam, "Steam", "Launch", true, "steam.exe", ["Steam"],
                new ToolGetPath("Official site", "https://store.steampowered.com/about/"))
        };
        var items = CubeBrowse.Games([], tools);
        Assert.Contains(items, item => item.Kind == "page" && item.Id == "session");
        Assert.Contains(items, item => item.Kind == "tool" && item.Id == DesktopToolIds.Steam);
        Assert.Equal(2, CubeBrowse.Wrap(-1, 3));
        Assert.Equal(1, CubeBrowse.Wrap(4, 3));
    }

    [Fact]
    public void Browse_Rearrange_StaggersNeighbors_AndPushesNearBlocksMore()
    {
        Assert.Equal(0, CubeBrowse.RearrangeDelaySeconds(1.2, isFocus: true));
        Assert.InRange(CubeBrowse.RearrangeDelaySeconds(0, isFocus: false), 0.020, 0.026);
        Assert.True(CubeBrowse.RearrangeDelaySeconds(1.8, false) > CubeBrowse.RearrangeDelaySeconds(0.2, false));
        Assert.InRange(CubeBrowse.RearrangeDelaySeconds(12, false), 0.10, 0.12);

        Assert.True(CubeBrowse.RearrangePush(0, carousel: true) > CubeBrowse.RearrangePush(1.4, carousel: true));
        Assert.True(CubeBrowse.RearrangePush(0.4, carousel: true) > CubeBrowse.RearrangePush(0.4, carousel: false));
        Assert.InRange(CubeBrowse.RearrangePush(0, carousel: true), 0.22, 0.28);
    }

    [Fact]
    public async Task Launch_OpensSafeSteamUri()
    {
        var opened = "";
        var service = new LibraryLaunchService(uri => { opened = uri; return true; }, (_, _) => false);
        var game = new LibraryGame(
            "570", "Dota 2", GameStore.Steam, null, "steam://rungameid/570", null, null, null, []);
        var result = await service.LaunchAsync(game, JsonProfileStore.CreateDefaults()[0]);
        Assert.True(result.Succeeded);
        Assert.Equal("steam://rungameid/570", opened);
        Assert.True(LibraryLaunchService.IsSafeLaunchUri("steam://rungameid/570"));
        Assert.False(LibraryLaunchService.IsSafeLaunchUri("javascript:alert(1)"));
        Assert.False(LibraryLaunchService.IsSafeLaunchUri("file:///tmp/game"));
    }
}
