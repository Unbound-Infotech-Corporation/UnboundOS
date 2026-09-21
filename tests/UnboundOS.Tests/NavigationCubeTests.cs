using System.Numerics;
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
        Assert.Equal("S", CubeCatalog.Info(CubeDestination.Session).Monogram);
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

        Assert.Equal("#00F0FF", CubeAtmosphere.Palette(CubeDestination.Session).AccentHex);
        Assert.False(CubeAtmosphere.IsBrandFamilyHex("#FF4FAD"));
        Assert.False(CubeAtmosphere.IsBrandFamilyHex("#FF7A00"));
        Assert.False(CubeAtmosphere.IsBrandFamilyHex("#B4FF00"));
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
        Assert.Contains("\"burst\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"accent\":\"#00F0FF\"", json, StringComparison.Ordinal);
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

        var open = CubeBridge.ToJson(CubeBridge.Open(CubeDestination.Tools, true));
        Assert.Contains("\"type\":\"open\"", open, StringComparison.Ordinal);
        Assert.Contains("\"front\":\"Tools\"", open, StringComparison.Ordinal);
        Assert.Contains("\"motion\":true", open, StringComparison.Ordinal);
        var reset = CubeBridge.ToJson(CubeBridge.Reset(CubeDestination.Session, false));
        Assert.Contains("\"type\":\"reset\"", reset, StringComparison.Ordinal);
        Assert.Contains("\"motion\":false", reset, StringComparison.Ordinal);
        Assert.InRange(CubeAtmosphere.OpenDurationMs, 800, 2000);
        Assert.True(CubeBridge.TryRead("""{"type":"opened","face":"Session"}""", out var opened));
        Assert.Equal("opened", opened.Type);
        Assert.Equal(CubeDestination.Session, CubeBridge.ParseFace(opened.Face));
    }
}
