using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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
    HeroTile,
    Page
}

/// <summary>
/// Shared console motion. One ease and one set of durations for focus,
/// page enter, and press. Instant when the motion policy says off.
/// </summary>
public static class ConsoleMotion
{
    public static readonly TimeSpan FocusDuration = TimeSpan.FromMilliseconds(320);
    public static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(380);
    public static readonly TimeSpan ExitDuration = TimeSpan.FromMilliseconds(260);
    public static readonly TimeSpan PressDuration = TimeSpan.FromMilliseconds(90);

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

    public static void PlayEnter(FrameworkElement element, bool allowMotion)
    {
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            Center(visual, element);
            if (!allowMotion)
            {
                visual.StopAnimation("Opacity");
                visual.StopAnimation("Offset");
                visual.Opacity = 1;
                visual.Offset = Vector3.Zero;
                return;
            }

            visual.Opacity = 0;
            visual.Offset = new Vector3(0, 16, 0);
            var compositor = visual.Compositor;
            StartScalar(compositor, visual, "Opacity", 1, EnterDuration);
            StartVector3(compositor, visual, "Offset", Vector3.Zero, EnterDuration);
        }
        catch (Exception)
        {
            // Designer / missing compositor.
        }
    }

    public static void SetVisible(FrameworkElement element, bool visible, bool allowMotion)
    {
        BumpHideGeneration(element);

        if (!allowMotion)
        {
            SnapRest(element);
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            element.Opacity = 1;
            return;
        }

        if (visible)
        {
            var wasHidden = element.Visibility != Visibility.Visible;
            element.Visibility = Visibility.Visible;
            if (wasHidden)
            {
                PlayEnter(element, allowMotion: true);
            }
            else
            {
                SnapRest(element);
            }

            return;
        }

        if (element.Visibility != Visibility.Visible)
        {
            return;
        }

        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            StartScalar(visual.Compositor, visual, "Opacity", 0, ExitDuration);
            StartVector3(visual.Compositor, visual, "Offset", new Vector3(0, 8, 0), ExitDuration);
        }
        catch (Exception)
        {
            element.Visibility = Visibility.Collapsed;
            return;
        }

        var generation = GetHideGeneration(element);
        _ = HideAfterAsync(element, generation);
    }

    private static readonly DependencyProperty HideGenerationProperty = DependencyProperty.RegisterAttached(
        "HideGeneration",
        typeof(int),
        typeof(ConsoleMotion),
        new PropertyMetadata(0));

    private static int GetHideGeneration(FrameworkElement element) =>
        (int)element.GetValue(HideGenerationProperty);

    private static void BumpHideGeneration(FrameworkElement element) =>
        element.SetValue(HideGenerationProperty, GetHideGeneration(element) + 1);

    private static void SnapRest(FrameworkElement element)
    {
        try
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.StopAnimation("Opacity");
            visual.StopAnimation("Offset");
            visual.Opacity = 1;
            visual.Offset = Vector3.Zero;
        }
        catch (Exception)
        {
            // ignored
        }
    }

    private static async System.Threading.Tasks.Task HideAfterAsync(FrameworkElement element, int generation)
    {
        await System.Threading.Tasks.Task.Delay(ExitDuration).ConfigureAwait(false);
        var queue = element.DispatcherQueue;
        if (queue is null)
        {
            return;
        }

        _ = queue.TryEnqueue(() =>
        {
            if (GetHideGeneration(element) != generation)
            {
                return;
            }

            element.Visibility = Visibility.Collapsed;
            element.Opacity = 1;
            SnapRest(element);
        });
    }

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
        private bool _entered;
        private bool _disposed;
        private bool _transitionsTuned;
        private bool _transitionsAllowed;
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

            if (_role != ConsoleMotionRole.Page)
            {
                _element.PointerEntered += OnPointerEntered;
                _element.PointerExited += OnPointerExited;
                _element.PointerPressed += OnPointerPressed;
                _element.PointerReleased += OnPointerReleased;
                _element.PointerCanceled += OnPointerCanceled;
                _element.PointerCaptureLost += OnPointerCanceled;
                _element.GotFocus += OnGotFocus;
                _element.LostFocus += OnLostFocus;
            }

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
            if (!_transitionsTuned || _transitionsAllowed != allow)
            {
                _transitionsTuned = true;
                _transitionsAllowed = allow;
                TuneVisualTransitions(allow);
            }

            if (_role == ConsoleMotionRole.Page)
            {
                if (!_entered)
                {
                    _entered = true;
                    PlayEnter(_element, allow);
                }

                return;
            }

            var (scale, opacity) = ResolvePose();
            Animate(scale, opacity, allow);
        }

        private (float Scale, float Opacity) ResolvePose()
        {
            var opacity = _pressed ? 0.94f : 1f;
            if (!IsLifted())
            {
                return (1f, opacity);
            }

            return _role switch
            {
                ConsoleMotionRole.HeroTile => (1.045f, opacity),
                ConsoleMotionRole.Tile => (1.035f, opacity),
                _ => (1.02f, opacity)
            };
        }

        private bool IsLifted()
        {
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
                var duration = _pressed ? PressDuration : FocusDuration;

                if (allowMotion)
                {
                    StartVector3(compositor, visual, "Scale", new Vector3(scale, scale, 1), duration);
                    StartScalar(compositor, visual, "Opacity", opacity, PressDuration);
                }
                else
                {
                    visual.StopAnimation("Scale");
                    visual.StopAnimation("Opacity");
                    visual.StopAnimation("Offset");
                    visual.Scale = new Vector3(scale, scale, 1);
                    visual.Opacity = opacity;
                    visual.Offset = Vector3.Zero;
                }
            }
            catch (Exception)
            {
                // Designer / missing compositor — leave the static visual state.
            }
        }

        private void TuneVisualTransitions(bool allow)
        {
            try
            {
                ApplyTransitions(_element, allow);
                if (VisualTreeHelper.GetChildrenCount(_element) > 0 &&
                    VisualTreeHelper.GetChild(_element, 0) is FrameworkElement child)
                {
                    ApplyTransitions(child, allow);
                }
            }
            catch (Exception)
            {
                // Template not ready.
            }
        }

        private static void ApplyTransitions(FrameworkElement root, bool allow)
        {
            var groups = VisualStateManager.GetVisualStateGroups(root);
            if (groups is null || groups.Count == 0)
            {
                return;
            }

            foreach (VisualStateGroup group in groups)
            {
                group.Transitions.Clear();
                if (allow)
                {
                    group.Transitions.Add(new VisualTransition
                    {
                        GeneratedDuration = FocusDuration,
                        GeneratedEasingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut }
                    });
                }
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

            if (_role != ConsoleMotionRole.Page)
            {
                _element.PointerEntered -= OnPointerEntered;
                _element.PointerExited -= OnPointerExited;
                _element.PointerPressed -= OnPointerPressed;
                _element.PointerReleased -= OnPointerReleased;
                _element.PointerCanceled -= OnPointerCanceled;
                _element.PointerCaptureLost -= OnPointerCanceled;
                _element.GotFocus -= OnGotFocus;
                _element.LostFocus -= OnLostFocus;
            }

            _transitionsTuned = false;
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
}
