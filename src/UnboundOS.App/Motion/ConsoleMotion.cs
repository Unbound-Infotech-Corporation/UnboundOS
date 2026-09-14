using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using UnboundOS.App.Services;
using UnboundOS.Core.Abstractions;

namespace UnboundOS.App.Motion;

public enum ConsoleMotionRole
{
    None = 0,
    Signal,
    Ghost,
    Nav,
    Tile,
    HeroTile
}

/// <summary>
/// GPU compositor motion for tiles and buttons. One-shot sheen, scale, glow.
/// Instant when the motion policy says off (toggle, Windows animations, or live session).
/// </summary>
public static class ConsoleMotion
{
    public static readonly DependencyProperty RoleProperty = DependencyProperty.RegisterAttached(
        "Role",
        typeof(ConsoleMotionRole),
        typeof(ConsoleMotion),
        new PropertyMetadata(ConsoleMotionRole.None, OnRoleChanged));

    private static readonly DependencyProperty HandleProperty = DependencyProperty.RegisterAttached(
        "Handle",
        typeof(MotionHandle),
        typeof(ConsoleMotion),
        new PropertyMetadata(null));

    public static ConsoleMotionRole GetRole(DependencyObject obj) =>
        (ConsoleMotionRole)obj.GetValue(RoleProperty);

    public static void SetRole(DependencyObject obj, ConsoleMotionRole value) =>
        obj.SetValue(RoleProperty, value);

    private static void OnRoleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        Detach(element);
        if (e.NewValue is ConsoleMotionRole role && role != ConsoleMotionRole.None)
        {
            Attach(element, role);
        }
    }

    private static void Attach(FrameworkElement element, ConsoleMotionRole role)
    {
        var handle = new MotionHandle(element, role);
        element.SetValue(HandleProperty, handle);
        element.Loaded += handle.OnLoaded;
        element.Unloaded += handle.OnUnloaded;
        element.SizeChanged += handle.OnSizeChanged;
        if (element.IsLoaded)
        {
            handle.OnLoaded(element, new RoutedEventArgs());
        }
    }

    private static void Detach(FrameworkElement element)
    {
        if (element.GetValue(HandleProperty) is not MotionHandle handle)
        {
            return;
        }

        element.Loaded -= handle.OnLoaded;
        element.Unloaded -= handle.OnUnloaded;
        element.SizeChanged -= handle.OnSizeChanged;
        handle.Dispose();
        element.ClearValue(HandleProperty);
    }

    private sealed class MotionHandle : IDisposable
    {
        private readonly FrameworkElement _element;
        private readonly ConsoleMotionRole _role;
        private IUiMotionPolicy? _policy;
        private FrameworkElement? _glow;
        private FrameworkElement? _sheen;
        private bool _pointerOver;
        private bool _pressed;
        private bool _focused;
        private bool _selected;
        private bool _wired;
        private bool _disposed;
        private long _selectedToken = -1;

        public MotionHandle(FrameworkElement element, ConsoleMotionRole role)
        {
            _element = element;
            _role = role;
        }

        public void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_disposed || _wired)
            {
                return;
            }

            _wired = true;
            _glow = FindNamed<FrameworkElement>(_element, "Glow");
            _sheen = FindNamed<FrameworkElement>(_element, "Sheen");
            _policy = TryPolicy();
            if (_policy is not null)
            {
                _policy.Changed += OnPolicyChanged;
            }

            _element.PointerEntered += OnPointerEntered;
            _element.PointerExited += OnPointerExited;
            _element.PointerPressed += OnPointerPressed;
            _element.PointerReleased += OnPointerReleased;
            _element.PointerCanceled += OnPointerCanceled;
            _element.PointerCaptureLost += OnPointerCanceled;
            _element.GotFocus += OnGotFocus;
            _element.LostFocus += OnLostFocus;

            if (_element is ListViewItem item)
            {
                _selected = item.IsSelected;
                _selectedToken = item.RegisterPropertyChangedCallback(
                    ListViewItem.IsSelectedProperty,
                    OnIsSelectedChanged);
            }

            Apply(playSheen: false);
        }

        public void OnUnloaded(object sender, RoutedEventArgs e) => Sleep();

        private void OnIsSelectedChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is ListViewItem item)
            {
                _selected = item.IsSelected;
                Apply(playSheen: false);
            }
        }

        public void OnSizeChanged(object sender, SizeChangedEventArgs e) => CenterVisuals();

        private void OnPolicyChanged(object? sender, EventArgs e)
        {
            var queue = _element.DispatcherQueue;
            if (queue is not null && !queue.HasThreadAccess)
            {
                _ = queue.TryEnqueue(() => Apply(playSheen: false));
                return;
            }

            Apply(playSheen: false);
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _pointerOver = true;
            Apply(playSheen: true);
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _pointerOver = false;
            _pressed = false;
            Apply(playSheen: false);
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _pressed = true;
            Apply(playSheen: false);
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _pressed = false;
            Apply(playSheen: false);
        }

        private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            _pressed = false;
            Apply(playSheen: false);
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            _focused = true;
            if (_element is ListViewItem item)
            {
                _selected = item.IsSelected;
            }

            Apply(playSheen: true);
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            _focused = false;
            Apply(playSheen: false);
        }

        private void Apply(bool playSheen)
        {
            if (_disposed)
            {
                return;
            }

            if (_element is ListViewItem item)
            {
                _selected = item.IsSelected;
            }

            var allow = _policy?.AllowMotion ?? true;
            var (scale, glow, offsetX, offsetY) = ResolvePose();
            Animate(_element, scale, offsetX, offsetY, allow);
            if (_glow is not null)
            {
                AnimateOpacity(_glow, glow, allow);
            }

            if (playSheen && allow && ( _pointerOver || _focused) && _sheen is not null)
            {
                PlaySheen(_sheen);
            }
            else if (!allow && _sheen is not null)
            {
                AnimateOpacity(_sheen, 0, allowMotion: false);
                ResetSheenOffset(_sheen);
            }
        }

        private (float Scale, float Glow, float OffsetX, float OffsetY) ResolvePose()
        {
            var rest = _role switch
            {
                ConsoleMotionRole.HeroTile => (1f, 0.15f, 0f, 0f),
                ConsoleMotionRole.Signal => (1f, 0f, 0f, 0f),
                _ => (1f, 0f, 0f, 0f)
            };

            if (_pressed)
            {
                return _role switch
                {
                    ConsoleMotionRole.HeroTile => (0.96f, 0.45f, 0f, 2f),
                    ConsoleMotionRole.Tile => (0.96f, 0.4f, 0f, 2f),
                    ConsoleMotionRole.Signal => (0.96f, 0.2f, 0f, 1f),
                    _ => (0.97f, 0.12f, 0f, 1f)
                };
            }

            if (_selected && _role is ConsoleMotionRole.Tile or ConsoleMotionRole.HeroTile)
            {
                if (_pointerOver || _focused)
                {
                    return (1.1f, 0.95f, -5f, -4f);
                }

                return (1.1f, 0.85f, -3f, -2f);
            }

            if (_pointerOver || _focused)
            {
                return _role switch
                {
                    ConsoleMotionRole.HeroTile => (1.08f, 0.7f, -6f, -5f),
                    ConsoleMotionRole.Tile => (1.06f, 0.6f, -5f, -4f),
                    ConsoleMotionRole.Signal => (1.04f, 0.35f, 0f, -2f),
                    ConsoleMotionRole.Nav => (1.03f, 0.28f, 0f, -1f),
                    _ => (1.03f, 0.22f, 0f, -1f)
                };
            }

            return rest;
        }

        private void Animate(FrameworkElement target, float scale, float offsetX, float offsetY, bool allowMotion)
        {
            try
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                Center(visual, target);
                var compositor = visual.Compositor;
                if (allowMotion)
                {
                    var duration = _pressed
                        ? TimeSpan.FromMilliseconds(90)
                        : TimeSpan.FromMilliseconds(200);
                    StartVector3(compositor, visual, "Scale", new Vector3(scale, scale, 1), duration);
                    StartVector3(compositor, visual, "Offset", new Vector3(offsetX, offsetY, 0), duration);
                }
                else
                {
                    visual.StopAnimation("Scale");
                    visual.StopAnimation("Offset");
                    visual.Scale = new Vector3(scale, scale, 1);
                    visual.Offset = new Vector3(offsetX, offsetY, 0);
                }
            }
            catch (Exception)
            {
                // Designer / missing compositor — leave the static visual state.
            }
        }

        private static void AnimateOpacity(FrameworkElement target, float opacity, bool allowMotion)
        {
            try
            {
                var visual = ElementCompositionPreview.GetElementVisual(target);
                if (allowMotion)
                {
                    StartScalar(visual.Compositor, visual, "Opacity", opacity, TimeSpan.FromMilliseconds(180));
                }
                else
                {
                    visual.StopAnimation("Opacity");
                    visual.Opacity = opacity;
                }
            }
            catch (Exception)
            {
                target.Opacity = opacity;
            }
        }

        private static void PlaySheen(FrameworkElement sheen)
        {
            try
            {
                var visual = ElementCompositionPreview.GetElementVisual(sheen);
                var compositor = visual.Compositor;
                visual.Opacity = 0.55f;
                visual.Offset = new Vector3(-220, 0, 0);

                var slide = compositor.CreateVector3KeyFrameAnimation();
                slide.InsertKeyFrame(0, new Vector3(-220, 0, 0));
                slide.InsertKeyFrame(1, new Vector3(240, 0, 0), Ease(compositor));
                slide.Duration = TimeSpan.FromMilliseconds(420);
                slide.StopBehavior = AnimationStopBehavior.SetToFinalValue;

                var fade = compositor.CreateScalarKeyFrameAnimation();
                fade.InsertKeyFrame(0, 0.55f);
                fade.InsertKeyFrame(0.7f, 0.4f);
                fade.InsertKeyFrame(1, 0);
                fade.Duration = TimeSpan.FromMilliseconds(420);

                visual.StartAnimation("Offset", slide);
                visual.StartAnimation("Opacity", fade);
            }
            catch (Exception)
            {
                sheen.Opacity = 0;
            }
        }

        private static void ResetSheenOffset(FrameworkElement sheen)
        {
            try
            {
                var visual = ElementCompositionPreview.GetElementVisual(sheen);
                visual.StopAnimation("Offset");
                visual.StopAnimation("Opacity");
                visual.Offset = new Vector3(-220, 0, 0);
                visual.Opacity = 0;
            }
            catch (Exception)
            {
                sheen.Opacity = 0;
            }
        }

        private void CenterVisuals()
        {
            try
            {
                Center(ElementCompositionPreview.GetElementVisual(_element), _element);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static void Center(Visual visual, FrameworkElement element)
        {
            var w = (float)element.ActualWidth;
            var h = (float)element.ActualHeight;
            if (w > 0 && h > 0)
            {
                visual.CenterPoint = new Vector3(w / 2f, h / 2f, 0);
            }
        }

        private static void StartVector3(
            Compositor compositor,
            Visual visual,
            string property,
            Vector3 value,
            TimeSpan duration)
        {
            var animation = compositor.CreateVector3KeyFrameAnimation();
            animation.InsertKeyFrame(1, value, Ease(compositor));
            animation.Duration = duration;
            animation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
            visual.StartAnimation(property, animation);
        }

        private static void StartScalar(
            Compositor compositor,
            Visual visual,
            string property,
            float value,
            TimeSpan duration)
        {
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertKeyFrame(1, value, Ease(compositor));
            animation.Duration = duration;
            animation.StopBehavior = AnimationStopBehavior.SetToFinalValue;
            visual.StartAnimation(property, animation);
        }

        private static CubicBezierEasingFunction Ease(Compositor compositor) =>
            compositor.CreateCubicBezierEasingFunction(
                new Vector2(0.16f, 1f),
                new Vector2(0.3f, 1f));

        private static IUiMotionPolicy? TryPolicy()
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

        private static T? FindNamed<T>(DependencyObject root, string name) where T : FrameworkElement
        {
            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match && match.Name == name)
                {
                    return match;
                }

                var nested = FindNamed<T>(child, name);
                if (nested is not null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void Sleep()
        {
            if (!_wired)
            {
                return;
            }

            if (_policy is not null)
            {
                _policy.Changed -= OnPolicyChanged;
                _policy = null;
            }

            if (_element is ListViewItem item && _selectedToken >= 0)
            {
                item.UnregisterPropertyChangedCallback(ListViewItem.IsSelectedProperty, _selectedToken);
                _selectedToken = -1;
            }

            _element.PointerEntered -= OnPointerEntered;
            _element.PointerExited -= OnPointerExited;
            _element.PointerPressed -= OnPointerPressed;
            _element.PointerReleased -= OnPointerReleased;
            _element.PointerCanceled -= OnPointerCanceled;
            _element.PointerCaptureLost -= OnPointerCanceled;
            _element.GotFocus -= OnGotFocus;
            _element.LostFocus -= OnLostFocus;
            _wired = false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Sleep();
        }
    }
}
