using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using UnboundOS.App.Services;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Navigation;

namespace UnboundOS.App.Views;

public sealed partial class NavigationCubeView : UserControl
{
    private readonly DispatcherTimer _ticker = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Dictionary<CubeDestination, FaceVisual> _faces = [];
    private IUiMotionPolicy? _motion;
    private CubePose _pose = CubePose.Home;
    private float _visualYaw;
    private float _visualPitch;
    private float _targetYaw;
    private float _targetPitch;
    private float _yawVelocity;
    private float _pitchVelocity;
    private float _idle;
    private bool _dragging;
    private bool _moved;
    private Point _press;
    private Point _lastPoint;
    private DateTimeOffset _lastMoveAt;
    private DateTimeOffset _idleOrigin = DateTimeOffset.UtcNow;
    private CubeDestination _announced = CubeDestination.Session;
    private bool _wired;

    public NavigationCubeView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        GotFocus += (_, _) => FocusRing.Opacity = 1;
        LostFocus += (_, _) => FocusRing.Opacity = 0;
        SizeChanged += (_, _) => ApplyVisuals();
    }

    public CubePose Pose => _pose;

    public event EventHandler<CubeDestination>? FaceActivated;

    public event EventHandler<CubeDestination>? FrontChanged;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_wired)
        {
            return;
        }

        _wired = true;
        _faces[CubeDestination.Session] = new(FaceSession, SpecSession, TicksSession);
        _faces[CubeDestination.Tools] = new(FaceTools, SpecTools, TicksTools);
        _faces[CubeDestination.Network] = new(FaceNetwork, SpecNetwork, TicksNetwork);
        _faces[CubeDestination.Mods] = new(FaceMods, SpecMods, TicksMods);
        _faces[CubeDestination.Files] = new(FaceFiles, SpecFiles, TicksFiles);
        _faces[CubeDestination.Hardware] = new(FaceHardware, SpecHardware, TicksHardware);

        _motion = TryMotion();
        if (_motion is not null)
        {
            _motion.Changed += OnMotionChanged;
        }

        Announce(_pose.Front);
        _ticker.Tick += OnTick;
        _ticker.Start();
        ApplyVisuals();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _ticker.Stop();
        _ticker.Tick -= OnTick;
        if (_motion is not null)
        {
            _motion.Changed -= OnMotionChanged;
            _motion = null;
        }

        _wired = false;
    }

    private void OnMotionChanged(object? sender, EventArgs e)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            if (!AllowMotion)
            {
                SnapVisualToPose();
            }

            ApplyVisuals();
        });
    }

    private void OnTick(object? sender, object e)
    {
        var allow = AllowMotion;
        if (!allow)
        {
            SnapVisualToPose();
            ApplyVisuals();
            return;
        }

        var dt = 0.016f;
        var targetYaw = CubeInput.NearestEquivalentDegrees(_visualYaw, _targetYaw);
        _visualYaw = CubeInput.SpringStep(_visualYaw, targetYaw, ref _yawVelocity, dt);
        _visualPitch = CubeInput.SpringStep(_visualPitch, _targetPitch, ref _pitchVelocity, dt);
        _idle = _dragging ? 0 : CubeInput.IdleYawDegrees((DateTimeOffset.UtcNow - _idleOrigin).TotalSeconds);
        ApplyVisuals();
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var name = e.Key.ToString();
        if (CubeInput.IsActivateKey(name) || e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space)
        {
            ActivateFront();
            e.Handled = true;
            return;
        }

        var turn = CubeInput.FromKey(name);
        if (turn is null)
        {
            return;
        }

        Rotate(turn.Value);
        e.Handled = true;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pt = e.GetCurrentPoint(PerspectiveHost);
        _dragging = true;
        _moved = false;
        _press = pt.Position;
        _lastPoint = _press;
        _lastMoveAt = DateTimeOffset.UtcNow;
        PerspectiveHost.CapturePointer(e.Pointer);
        Focus(FocusState.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var pt = e.GetCurrentPoint(PerspectiveHost);
        var dx = pt.Position.X - _press.X;
        var dy = pt.Position.Y - _press.Y;
        if (Math.Abs(dx) + Math.Abs(dy) > 8)
        {
            _moved = true;
        }

        if (_moved)
        {
            var preview = CubeInput.PreviewDrag(_pose, (float)dx, (float)dy);
            _targetYaw = (float)preview.Yaw;
            _targetPitch = (float)preview.Pitch;
            if (!AllowMotion)
            {
                _visualYaw = _targetYaw;
                _visualPitch = _targetPitch;
            }
        }

        _lastPoint = pt.Position;
        _lastMoveAt = DateTimeOffset.UtcNow;
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var pt = e.GetCurrentPoint(PerspectiveHost);
        PerspectiveHost.ReleasePointerCapture(e.Pointer);
        _dragging = false;

        if (_moved)
        {
            var dt = Math.Max(0.001, (DateTimeOffset.UtcNow - _lastMoveAt).TotalSeconds);
            var vx = (pt.Position.X - _lastPoint.X) / dt;
            var vy = (pt.Position.Y - _lastPoint.Y) / dt;
            var snapped = CubeInput.SnapFromDegrees(_targetYaw, _targetPitch);
            var flick = CubeInput.FlickTurn((float)vx, (float)vy);
            SetPose(flick is null ? snapped : snapped.Turn(flick.Value));
        }
        else
        {
            HandleClick(pt.Position);
        }

        e.Handled = true;
    }

    private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        SetPose(CubeInput.SnapFromDegrees(_visualYaw, _visualPitch));
    }

    private void OnWheel(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(PerspectiveHost).Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        Rotate(delta > 0 ? CubeTurn.Left : CubeTurn.Right);
        e.Handled = true;
    }

    private void HandleClick(Point position)
    {
        var nx = (float)((position.X / Math.Max(PerspectiveHost.ActualWidth, 1)) * 2 - 1);
        var ny = (float)((position.Y / Math.Max(PerspectiveHost.ActualHeight, 1)) * 2 - 1);
        var hit = CubeInput.HitFromNormalizedPoint(nx, ny);
        if (hit.Activates)
        {
            ActivateFront();
            return;
        }

        if (hit.Turn is { } turn)
        {
            Rotate(turn);
        }
    }

    public void Rotate(CubeTurn turn) => SetPose(_pose.Turn(turn));

    public void ActivateFront() => FaceActivated?.Invoke(this, _pose.Front);

    private void SetPose(CubePose pose)
    {
        _pose = pose;
        _targetYaw = CubeInput.NearestEquivalentDegrees(_visualYaw, pose.YawDegrees);
        _targetPitch = pose.PitchDegrees;
        if (!AllowMotion)
        {
            SnapVisualToPose();
        }

        Announce(pose.Front);
        ApplyVisuals();
    }

    private void SnapVisualToPose()
    {
        _visualYaw = _pose.YawDegrees;
        _visualPitch = _pose.PitchDegrees;
        _targetYaw = _visualYaw;
        _targetPitch = _visualPitch;
        _yawVelocity = 0;
        _pitchVelocity = 0;
        _idle = 0;
    }

    private void Announce(CubeDestination front)
    {
        var info = CubeCatalog.Info(front);
        FrontCaption.Text = info.Title;
        FrontHint.Text = info.Hint;
        AutomationProperties.SetName(this, CubeCatalog.Announce(front));
        AutomationProperties.SetName(FrontCaption, $"Front face {info.Title}");
        if (_announced != front)
        {
            _announced = front;
            FrontChanged?.Invoke(this, front);
        }
    }

    private void ApplyVisuals()
    {
        if (_faces.Count == 0)
        {
            return;
        }

        try
        {
            var size = (float)CubeRoot.ActualWidth;
            if (size <= 1)
            {
                size = 248;
            }

            var half = size * 0.5f;
            var idle = AllowMotion ? _idle : 0;
            var cube = CubeLayout.CubeRotation(_visualYaw, _visualPitch, idle);
            var hostVisual = ElementCompositionPreview.GetElementVisual(PerspectiveHost);
            hostVisual.TransformMatrix = CubeLayout.Perspective();

            var rootVisual = ElementCompositionPreview.GetElementVisual(CubeRoot);
            Reset(rootVisual);
            rootVisual.TransformMatrix = CubeLayout.Centered(cube, size, size);

            var cyan = (Brush)Application.Current.Resources["CyanPulseBrush"];
            var subtle = (Brush)Application.Current.Resources["LineSubtleBrush"];

            foreach (var (destination, face) in _faces)
            {
                var visual = ElementCompositionPreview.GetElementVisual(face.Border);
                Reset(visual);
                visual.TransformMatrix = CubeLayout.Centered(CubeLayout.FaceLocal(destination, half), size, size);
                var facing = CubeLayout.FacingCamera(destination, cube);
                face.Border.Opacity = CubeLayout.FaceOpacity(facing);
                Canvas.SetZIndex(face.Border, CubeLayout.DepthIndex(destination, cube, half));
                var front = destination == _pose.Front;
                face.Border.BorderBrush = front ? cyan : subtle;
                face.Border.BorderThickness = new Thickness(front ? 2 : 1);
                face.Ticks.Opacity = front ? 1 : 0;
                face.Specular.Opacity = CubeLayout.SpecularOpacity(facing, idle);
            }
        }
        catch (Exception)
        {
            // Designer / missing compositor — caption and keyboard still work.
        }
    }

    private static void Reset(Microsoft.UI.Composition.Visual visual)
    {
        visual.Offset = Vector3.Zero;
        visual.Scale = Vector3.One;
        visual.RotationAngleInDegrees = 0;
        visual.CenterPoint = Vector3.Zero;
    }

    private bool AllowMotion => _motion?.AllowMotion ?? true;

    private static IUiMotionPolicy? TryMotion()
    {
        try
        {
            return AppServices.Get<IUiMotionPolicy>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed record FaceVisual(Border Border, UIElement Specular, UIElement Ticks);
}
