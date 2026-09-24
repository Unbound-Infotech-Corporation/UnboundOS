using System.Numerics;
using UnboundOS.Core.Models;
using UnboundOS.Core.Navigation;

namespace UnboundOS.Tests;

public sealed class NavigationCubeTests
{
    [Fact]
    public void Home_FrontIsSession_AndChromeDestinationsAreNotFaces()
    {
        Assert.Equal(CubeDestination.Session, CubePose.Home.Front);
        Assert.Equal(6, CubeCatalog.Faces.Count);
        Assert.DoesNotContain(CubeCatalog.Faces, face => CubeCatalog.NavTag(face) is "Settings" or "Profiles" or "Overlay");
        Assert.Equal("Session", CubeCatalog.NavTag(CubeDestination.Session));
        Assert.Equal("Games", CubeCatalog.Info(CubeDestination.Session).Title);
        Assert.Equal("games", CubeCatalog.Info(CubeDestination.Session).Glyph);
        Assert.Equal(CubeOpenKind.Carousel, CubeCatalog.OpenKind(CubeDestination.Session));
        Assert.Equal(CubeOpenKind.List, CubeCatalog.OpenKind(CubeDestination.Network));
        Assert.True(CubeCatalog.StaysInCube(CubeDestination.Session));
        Assert.False(CubeCatalog.StaysInCube(CubeDestination.Files));
        Assert.Equal("G", CubeCatalog.Info(CubeDestination.Session).Monogram);
        Assert.NotEqual("I", CubeCatalog.Info(CubeDestination.Session).Monogram);
    }

    [Fact]
    public void Turns_MatchTheSixFaceMap()
    {
        Assert.Equal(CubeDestination.Tools, CubePose.Home.Turn(CubeTurn.Right).Front);
        Assert.Equal(CubeDestination.Network, CubePose.Home.Turn(CubeTurn.Left).Front);
        Assert.Equal(CubeDestination.Mods, CubePose.Home.Turn(CubeTurn.Up).Front);
        Assert.Equal(CubeDestination.Files, CubePose.Home.Turn(CubeTurn.Down).Front);
        Assert.Equal(CubeDestination.Hardware, CubePose.Home.Turn(CubeTurn.Right).Turn(CubeTurn.Right).Front);
        Assert.Equal(CubeDestination.Session, CubePose.Home.Turn(CubeTurn.Right).Turn(CubeTurn.Right).Turn(CubeTurn.Right).Turn(CubeTurn.Right).Front);
        Assert.Equal(CubeDestination.Session, CubePose.Home.Turn(CubeTurn.Up).Turn(CubeTurn.Down).Front);
        Assert.Equal(CubeDestination.Mods, CubePose.Home.Turn(CubeTurn.Down).Turn(CubeTurn.Up).Turn(CubeTurn.Up).Front);
    }

    [Fact]
    public void Pitch_ClampsSoTheCubeDoesNotGimbalThrough()
    {
        var top = CubePose.Home.Turn(CubeTurn.Up).Turn(CubeTurn.Up).Turn(CubeTurn.Up);
        Assert.Equal(-1, top.PitchSteps);
        Assert.Equal(CubeDestination.Mods, top.Front);
        var bottom = CubePose.Home.Turn(CubeTurn.Down).Turn(CubeTurn.Down);
        Assert.Equal(1, bottom.PitchSteps);
        Assert.Equal(CubeDestination.Files, bottom.Front);
    }

    [Fact]
    public void KeysAndGamepadStub_MapToTurns()
    {
        Assert.Equal(CubeTurn.Left, CubeInput.FromKey("Left"));
        Assert.Equal(CubeTurn.Right, CubeInput.FromKey("GamepadDPadRight"));
        Assert.Equal(CubeTurn.Up, CubeInput.FromKey("GamepadLeftThumbstickUp"));
        Assert.Equal(CubeTurn.Down, CubeInput.FromKey("Down"));
        Assert.Null(CubeInput.FromKey("Tab"));
        Assert.True(CubeInput.IsActivateKey("Enter"));
        Assert.True(CubeInput.IsActivateKey("GamepadA"));
        Assert.True(CubeInput.IsBackKey("Escape"));
        Assert.True(CubeInput.IsBackKey("GamepadB"));
        Assert.False(CubeInput.IsActivateKey("Escape"));
    }

    [Fact]
    public void HitTest_CenterOpens_EdgesRotate()
    {
        Assert.True(CubeInput.HitFromNormalizedPoint(0, 0).Activates);
        Assert.Equal(CubeTurn.Left, CubeInput.HitFromNormalizedPoint(-0.9f, 0.1f).Turn);
        Assert.Equal(CubeTurn.Right, CubeInput.HitFromNormalizedPoint(0.9f, 0).Turn);
        Assert.Equal(CubeTurn.Up, CubeInput.HitFromNormalizedPoint(0, -0.9f).Turn);
        Assert.Equal(CubeTurn.Down, CubeInput.HitFromNormalizedPoint(0.1f, 0.95f).Turn);
        Assert.False(CubeInput.HitFromNormalizedPoint(-0.9f, 0).Activates);
    }

    [Fact]
    public void DragAndFlick_FollowGrabDirection()
    {
        var preview = CubeInput.PreviewDrag(CubePose.Home, deltaX: -96f, deltaY: 0);
        Assert.Equal(90f, preview.Yaw, 3);
        var snapped = CubeInput.SnapFromDegrees(preview.Yaw, preview.Pitch);
        Assert.Equal(CubeDestination.Tools, snapped.Front);

        var down = CubeInput.PreviewDrag(CubePose.Home, 0, 96f);
        Assert.Equal(CubeDestination.Mods, CubeInput.SnapFromDegrees(down.Yaw, down.Pitch).Front);

        Assert.Equal(CubeTurn.Right, CubeInput.FlickTurn(-800, 10));
        Assert.Equal(CubeTurn.Left, CubeInput.FlickTurn(800, 10));
        Assert.Equal(CubeTurn.Up, CubeInput.FlickTurn(10, 800));
        Assert.Null(CubeInput.FlickTurn(20, -20));
    }

    [Fact]
    public void Snap_NormalizesFullTurns()
    {
        Assert.Equal(CubeDestination.Session, CubeInput.SnapFromDegrees(360, 0).Front);
        Assert.Equal(CubeDestination.Session, CubeInput.SnapFromDegrees(-360, 0).Front);
        Assert.Equal(CubeDestination.Network, CubeInput.SnapFromDegrees(-90, 0).Front);
    }

    [Fact]
    public void UnwindAndSpring_TakeTheShortPathAndSettle()
    {
        Assert.Equal(360f, CubeInput.NearestEquivalentDegrees(270, 0));
        Assert.Equal(-90f, CubeInput.NearestEquivalentDegrees(0, 270));

        var velocity = 0f;
        var value = 0f;
        for (var i = 0; i < 40; i++)
        {
            value = CubeInput.SpringStep(value, 90, ref velocity, 0.016f);
        }

        Assert.InRange(value, 80, 95);
    }

    [Fact]
    public void IdleYaw_StaysAHint()
    {
        var a = CubeInput.IdleYawDegrees(0);
        var b = CubeInput.IdleYawDegrees((Math.PI / 2d) / 0.55d);
        Assert.InRange(a, -0.01f, 0.01f);
        Assert.InRange(Math.Abs(b), 2.0f, 2.3f);
    }

    [Fact]
    public void Layout_PutsSessionTowardTheViewerAtHome()
    {
        var rot = CubeLayout.CubeRotation(0, 0);
        Assert.True(CubeLayout.FacingCamera(CubeDestination.Session, rot) > 0.9f);
        Assert.True(CubeLayout.FacingCamera(CubeDestination.Hardware, rot) < 0f);
        Assert.True(CubeLayout.DepthIndex(CubeDestination.Session, rot, 120) >
                    CubeLayout.DepthIndex(CubeDestination.Hardware, rot, 120));

        var tools = CubeLayout.CubeRotation(90, 0);
        Assert.True(CubeLayout.FacingCamera(CubeDestination.Tools, tools) > 0.9f);

        var mods = CubeLayout.CubeRotation(0, -90);
        Assert.True(CubeLayout.FacingCamera(CubeDestination.Mods, mods) > 0.9f);
    }

    [Fact]
    public void Copy_StaysHonest_NoCostume()
    {
        var files = CubeCatalog.Info(CubeDestination.Files);
        Assert.Contains("Explorer stays", files.Hint, StringComparison.Ordinal);
        Assert.DoesNotContain("Night City", files.Hint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PlayStation", CubeCatalog.Announce(CubeDestination.Session), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Home", CubeCatalog.Announce(CubeDestination.Session), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("label list", CubeCatalog.Announce(CubeDestination.Files), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Enter opens", CubeCatalog.Announce(CubeDestination.Files), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("corner glyphs", CubeCatalog.Announce(CubeDestination.Tools), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("top bar", CubeCatalog.Announce(CubeDestination.Tools), StringComparison.OrdinalIgnoreCase);
        Assert.InRange(CubeLayout.FaceOpacity(1), 0.95f, 1.01f);
        Assert.True(CubeLayout.FaceOpacity(-1) < 0.4f);
        Assert.True(CubeLayout.Perspective().M34 < 0);
        Assert.NotEqual(Matrix4x4.Identity, CubeLayout.FaceLocal(CubeDestination.Tools, 120));
    }

    [Fact]
    public void Atmosphere_RestPoseShowsThreeFaces()
    {
        Assert.NotEqual(0f, CubeAtmosphere.RestYawDegrees);
        Assert.NotEqual(0f, CubeAtmosphere.RestPitchDegrees);
        var rot = CubeAtmosphere.ViewRotation(0, 0);
        Assert.True(CubeLayout.FacingCamera(CubeDestination.Session, rot) > 0.7f);
        Assert.True(CubeLayout.FacingCamera(CubeDestination.Tools, rot) > 0.2f);
        Assert.True(CubeLayout.FacingCamera(CubeDestination.Mods, rot) > 0.15f);
        Assert.True(
            CubeLayout.FacingCamera(CubeDestination.Session, rot) >
            CubeLayout.FacingCamera(CubeDestination.Tools, rot));
        Assert.True(CubeLayout.FacingCamera(CubeDestination.Hardware, rot) < 0.2f);

        var mods = CubeAtmosphere.ViewRotation(0, -90);
        Assert.True(
            CubeLayout.FacingCamera(CubeDestination.Mods, mods) >
            CubeLayout.FacingCamera(CubeDestination.Session, mods));
    }

    [Fact]
    public void Atmosphere_PalettesStayInTheBrandFamily()
    {
        foreach (var face in CubeCatalog.Faces)
        {
            var palette = CubeAtmosphere.Palette(face);
            Assert.True(CubeAtmosphere.IsBrandFamilyHex(palette.AccentHex), palette.AccentHex);
            Assert.True(CubeAtmosphere.IsBrandFamilyHex(palette.SeamHex), palette.SeamHex);
            Assert.True(CubeAtmosphere.IsBrandFamilyHex(palette.CoreHex), palette.CoreHex);
            Assert.DoesNotContain("ff00ff", palette.AccentHex, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Night City", palette.AccentHex, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Equal("#6FA896", CubeAtmosphere.Palette(CubeDestination.Session).AccentHex);
        Assert.False(CubeAtmosphere.IsBrandFamilyHex("#FF4FAD"));
        Assert.False(CubeAtmosphere.IsBrandFamilyHex("#FF7A00"));
        Assert.False(CubeAtmosphere.IsBrandFamilyHex("#B4FF00"));
        Assert.False(CubeAtmosphere.IsBrandFamilyHex("#00F0FF"));
        Assert.True(CubeAtmosphere.IsBrandFamilyHex("#9A8A72"));
        Assert.True(CubeAtmosphere.IsBrandFamilyHex("#6A5A78"));
        Assert.True(CubeAtmosphere.IsBrandFamilyHex("#7A4A42"));
    }

    [Fact]
    public void AimedAt_KeepsYawWhenPitching_AndSnapsEquatorFaces()
    {
        var fromNetwork = CubeAtmosphere.AimedAt(CubeDestination.Mods, new CubePose(3, 0));
        Assert.Equal(3, fromNetwork.YawSteps);
        Assert.Equal(CubeDestination.Mods, fromNetwork.Front);

        var toTools = CubeAtmosphere.AimedAt(CubeDestination.Tools, fromNetwork);
        Assert.Equal(CubeDestination.Tools, toTools.Front);
        Assert.Equal(0, toTools.PitchSteps);

        Assert.Equal(CubeDestination.Session, CubeAtmosphere.AimedAt(CubeDestination.Session, CubePose.Home).Front);
        Assert.Equal(CubeDestination.Files, CubeAtmosphere.AimedAt(CubeDestination.Files, CubePose.Home).Front);
    }

    [Fact]
    public void Bridge_SerializesStateAndReadsHostMessages()
    {
        var json = CubeBridge.ToJson(CubeBridge.State(CubePose.Home, 0, 0, motion: true, burst: true));
        Assert.Contains("\"restYaw\":28", json, StringComparison.Ordinal);
        Assert.Contains("\"front\":\"Session\"", json, StringComparison.Ordinal);
        Assert.Contains("\"node\":0", json, StringComparison.Ordinal);
        Assert.Contains("\"overlay\":\"none\"", json, StringComparison.Ordinal);
        Assert.Contains("\"burst\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"hud\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"accent\":\"#6FA896\"", json, StringComparison.Ordinal);
        Assert.Contains("\"id\":\"Tools\"", json, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayStation", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Unreal", json, StringComparison.OrdinalIgnoreCase);

        Assert.True(CubeBridge.TryRead("""{"v":1,"type":"turn","turn":"Right"}""", out var turn));
        Assert.Equal(CubeTurn.Right, CubeBridge.ParseTurn(turn.Turn));
        Assert.True(CubeBridge.TryRead("""{"type":"pick","face":"Files"}""", out var pick));
        Assert.Equal(CubeDestination.Files, CubeBridge.ParseFace(pick.Face));
        Assert.True(CubeBridge.TryRead("""{"type":"dragEnd","dx":40,"dy":-12,"vx":-800,"vy":10}""", out var drag));
        Assert.Equal("dragEnd", drag.Type);
        Assert.Equal(40, drag.Dx);
        Assert.False(CubeBridge.TryRead("not-json", out _));
        Assert.Equal(CubeBridge.IndexUrl, "https://unboundos.cube/Cube/index.html");

        var gamesOpen = CubeBridge.ToJson(CubeBridge.Open(
            CubeDestination.Session,
            true,
            [new CubeBrowseItem("570", "Dota 2", "STEAM", "game", "D")]));
        Assert.Contains("\"mode\":\"carousel\"", gamesOpen, StringComparison.Ordinal);
        Assert.Contains("\"stay\":true", gamesOpen, StringComparison.Ordinal);
        Assert.Contains("\"node\":0", gamesOpen, StringComparison.Ordinal);
        Assert.Contains("\"origin\":\"top\"", gamesOpen, StringComparison.Ordinal);
        var fromBottom = CubeBridge.ToJson(CubeBridge.Open(
            CubeDestination.Tools,
            true,
            [new CubeBrowseItem("obs", "OBS", "KIT", "tool", "O")],
            0,
            "bottom"));
        Assert.Contains("\"origin\":\"bottom\"", fromBottom, StringComparison.Ordinal);
        Assert.Contains("\"stay\":true", fromBottom, StringComparison.Ordinal);
        Assert.Contains("\"title\":\"Dota 2\"", gamesOpen, StringComparison.Ordinal);
        Assert.Contains("\"glyph\":\"games\"", json, StringComparison.Ordinal);
        Assert.Contains("\"openKind\":\"carousel\"", json, StringComparison.Ordinal);

        var listOpen = CubeBridge.ToJson(CubeBridge.Open(CubeDestination.Files, true));
        Assert.Contains("\"mode\":\"list\"", listOpen, StringComparison.Ordinal);
        Assert.Contains("\"stay\":false", listOpen, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"open\"", listOpen, StringComparison.Ordinal);
        var toolsOpen = CubeBridge.ToJson(CubeBridge.Open(CubeDestination.Tools, true));
        Assert.Contains("\"front\":\"Tools\"", toolsOpen, StringComparison.Ordinal);
        Assert.Contains("\"motion\":true", toolsOpen, StringComparison.Ordinal);
        Assert.Contains("\"mode\":\"mosaic\"", toolsOpen, StringComparison.Ordinal);
        var optionsOpen = CubeBridge.ToJson(CubeBridge.OpenOptions(true, CubeBrowse.Options()));
        Assert.Contains("\"front\":\"Settings\"", optionsOpen, StringComparison.Ordinal);
        Assert.Contains("\"node\":-1", optionsOpen, StringComparison.Ordinal);
        Assert.Contains("\"stay\":true", optionsOpen, StringComparison.Ordinal);
        Assert.Contains("Interface motion", optionsOpen, StringComparison.Ordinal);
        var close = CubeBridge.ToJson(CubeBridge.Close(true));
        Assert.Contains("\"type\":\"close\"", close, StringComparison.Ordinal);
        var reset = CubeBridge.ToJson(CubeBridge.Reset(CubeDestination.Session, false));
        Assert.Contains("\"type\":\"reset\"", reset, StringComparison.Ordinal);
        Assert.Contains("\"motion\":false", reset, StringComparison.Ordinal);
        Assert.InRange(CubeAtmosphere.OpenDurationMs, 800, 2000);
        Assert.True(CubeBridge.TryRead("""{"type":"opened","face":"Session"}""", out var opened));
        Assert.Equal("opened", opened.Type);
        Assert.Equal(CubeDestination.Session, CubeBridge.ParseFace(opened.Face));
        Assert.Contains("\"node\":0", reset, StringComparison.Ordinal);
    }
}

public sealed class HomeGalaxyTests
{
    [Fact]
    public void Nodes_SitAlongTheRibbon_InLeftToRightOrder()
    {
        Assert.Equal(
            [
                CubeDestination.Session,
                CubeDestination.Tools,
                CubeDestination.Mods,
                CubeDestination.Network,
                CubeDestination.Files,
                CubeDestination.Hardware
            ],
            HomeGalaxy.Nodes);
        Assert.Equal(6, HomeGalaxy.Count);
        Assert.Equal(0, HomeGalaxy.IndexOf(CubeDestination.Session));
        Assert.Equal(2, HomeGalaxy.IndexOf(CubeDestination.Mods));
        Assert.Equal(5, HomeGalaxy.IndexOf(CubeDestination.Hardware));
        Assert.Equal(0f, HomeGalaxy.NodeX(0), 3);
        Assert.True(HomeGalaxy.NodeX(1) > 0f);
        Assert.True(HomeGalaxy.NodeX(3) < 0f);
    }

    [Fact]
    public void IdleKeys_MapToPanAndCategoryLists()
    {
        Assert.Equal(HomeIdleAction.PanLeft, HomeGalaxy.FromTurn(CubeTurn.Left));
        Assert.Equal(HomeIdleAction.PanRight, HomeGalaxy.FromTurn(CubeTurn.Right));
        Assert.Equal(HomeIdleAction.PanLeft, HomeGalaxy.FromTurn(CubeTurn.Up));
        Assert.Equal(HomeIdleAction.PanRight, HomeGalaxy.FromTurn(CubeTurn.Down));
        Assert.Equal(CubeDestination.Network, HomeGalaxy.Neighbor(CubeDestination.Session, -1));
        Assert.Equal(CubeDestination.Tools, HomeGalaxy.Neighbor(CubeDestination.Session, 1));
        Assert.Equal(CubeDestination.Mods, HomeGalaxy.Neighbor(CubeDestination.Hardware, -1));
        Assert.Equal(CubeDestination.Hardware, HomeGalaxy.Neighbor(CubeDestination.Mods, 1));
        Assert.Equal(CubeDestination.Mods, HomeGalaxy.DestinationAt(8));
        Assert.Equal(2, HomeGalaxy.ListStartIndex(3, fromBottom: true));
        Assert.Equal(0, HomeGalaxy.ListStartIndex(3, fromBottom: false));
    }

    [Fact]
    public void Announce_NamesTheHomeAndOverlay()
    {
        Assert.Contains("Games", HomeGalaxy.Announce(CubeDestination.Session), StringComparison.Ordinal);
        Assert.Contains("label list", HomeGalaxy.Announce(CubeDestination.Session), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Enter opens", HomeGalaxy.Announce(CubeDestination.Tools), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("corner glyphs", HomeGalaxy.Announce(CubeDestination.Tools), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Options", HomeGalaxy.AnnounceOptions(), StringComparison.Ordinal);
        var overlay = HomeGalaxy.AnnounceOverlay(new CubeBrowseItem("570", "Dota 2", "STEAM", "game", "D"), 0, 3);
        Assert.Contains("Dota 2", overlay, StringComparison.Ordinal);
        Assert.Contains("1 of 3", overlay, StringComparison.Ordinal);
        Assert.Contains("returns to Home", overlay, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PlayStation", overlay, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tabs_IncludeOptionsAfterTools()
    {
        Assert.Equal(
            ["Session", "Tools", "Settings", "Mods", "Network", "Files", "Hardware"],
            HomeGalaxy.TabIds);
        Assert.Equal(7, HomeGalaxy.TabIds.Count);
        Assert.Equal("Settings", HomeGalaxy.TabIds[HomeGalaxy.OptionsTabIndex]);
        Assert.Equal(0, HomeGalaxy.TabIndexOf(CubeDestination.Session));
        Assert.Equal(2, HomeGalaxy.TabIndexOf(CubeDestination.Session, optionsTab: true));
        Assert.Equal("Tools", HomeGalaxy.ShiftTab(CubeDestination.Session, false, 1).Id);
        Assert.Equal(CubeDestination.Tools, HomeGalaxy.ShiftTab(CubeDestination.Session, false, 1).Destination);
        Assert.Equal(CubeDestination.Mods, HomeGalaxy.ShiftTab(CubeDestination.Session, true, 1).Destination);
        Assert.Equal(CubeDestination.Tools, HomeGalaxy.ShiftTab(CubeDestination.Session, true, -1).Destination);
        Assert.Equal(CubeDestination.Files, HomeGalaxy.ShiftTab(CubeDestination.Hardware, false, -1).Destination);
    }

    [Fact]
    public void CategoryLists_CoverEveryNode()
    {
        var games = Array.Empty<LibraryGame>();
        var tools = Array.Empty<DesktopTool>();
        var mods = Array.Empty<ModGame>();
        foreach (var node in HomeGalaxy.Nodes)
        {
            var items = CubeBrowse.ForNode(node, games, tools, mods);
            Assert.NotEmpty(items);
        }

        var network = CubeBrowse.ForNode(CubeDestination.Network, games, tools, mods);
        Assert.Contains(network, item => item.Kind == "page");
        Assert.Equal(2, HomeGalaxy.ListStartIndex(network.Count, true));

        var options = CubeBrowse.Options();
        Assert.Equal(8, options.Count);
        Assert.All(options, item => Assert.Equal("settings", item.Kind));
        Assert.Contains(options, item => item.Id == "motion" && item.Title == "Interface motion");
        Assert.Contains(options, item => item.Id == "hud" && item.Title == "Home extras");
        Assert.Contains(options, item => item.Id == "skinny" && item.Title == "Session skinny");
        Assert.Contains(options, item => item.Id == "updates" && item.Title == "Updates");
        Assert.Contains(options, item => item.Id == "cleanup" && item.Title == "Finish setup");
    }
}
