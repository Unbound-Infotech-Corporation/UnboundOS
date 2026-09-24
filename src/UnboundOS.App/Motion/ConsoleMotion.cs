using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
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
/// Quiet compositor motion: a few-percent focus scale on the active tile,
/// and a short opacity dip on press. Instant when the motion policy says off.
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

            Apply();
        }

        public void OnUnloaded(object sender, RoutedEventArgs e) => Sleep();

        private void OnIsSelectedChanged(DependencyObject sender, DependencyProperty dp)
        {
            if (sender is ListViewItem item)
            {
                _selected = item.IsSelected;
                Apply();
            }
        }

        public void OnSizeChanged(object sender, SizeChangedEventArgs e) => CenterVisuals();

        private void OnPolicyChanged(object? sender, EventArgs e)
        {
            var queue = _element.DispatcherQueue;
            if (queue is not null && !queue.HasThreadAccess)
            {
                _ = queue.TryEnqueue(Apply);
                return;
            }

            Apply();
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _pointerOver = true;
            Apply();
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            _pointerOver = false;
            _pressed = false;
            Apply();
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            _pressed = true;
            Apply();
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _pressed = false;
            Apply();
        }

        private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            _pressed = false;
            Apply();
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            _focused = true;
            if (_element is ListViewItem item)
            {
                _selected = item.IsSelected;
            }

            Apply();
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            _focused = false;
            Apply();
        }

        private void Apply()
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
            var (scale, opacity) = ResolvePose();
            Animate(scale, opacity, allow);
        }

        private (float Scale, float Opacity) ResolvePose()
        {
            var opacity = _pressed ? 0.96f : 1f;
            if (!IsFocusedTile())
            {
                return (1f, opacity);
            }

            var scale = _role == ConsoleMotionRole.HeroTile ? 1.06f : 1.08f;
            return (scale, opacity);
        }

        private bool IsFocusedTile()
        {
            if (_role is not (ConsoleMotionRole.Tile or ConsoleMotionRole.HeroTile))
            {
                return false;
            }

            if (_element is ListViewItem)
            {
                return _selected || _focused;
            }

            return _focused || _pointerOver;
        }

        private void Animate(float scale, float opacity, bool allowMotion)
        {
            try
            {
                var visual = ElementCompositionPreview.GetElementVisual(_element);
                Center(visual, _element);
                var compositor = visual.Compositor;
                var duration = _pressed
                    ? TimeSpan.FromMilliseconds(100)
                    : TimeSpan.FromMilliseconds(160);

                if (allowMotion)
                {
                    StartVector3(compositor, visual, "Scale", new Vector3(scale, scale, 1), duration);
                    StartScalar(compositor, visual, "Opacity", opacity, TimeSpan.FromMilliseconds(100));
                }
                else
                {
                    visual.StopAnimation("Scale");
                    visual.StopAnimation("Opacity");
                    visual.Scale = new Vector3(scale, scale, 1);
                    visual.Opacity = opacity;
                }
            }
            catch (Exception)
            {
                // Designer / missing compositor — leave the static visual state.
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
                new Vector2(0.33f, 1f),
                new Vector2(0.68f, 1f));

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
