using CommunityToolkit.Mvvm.ComponentModel;
using UnboundOS.Core.Abstractions;

namespace UnboundOS.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IUiMotionPolicy _motion;
    private bool _suppressToggle;

    public SettingsViewModel(IUiMotionPolicy motion)
    {
        _motion = motion;
        _motion.Changed += OnMotionChanged;
    }

    [ObservableProperty] private bool _interfaceMotionEnabled = true;

    [ObservableProperty] private string _motionStatus = string.Empty;

    [ObservableProperty] private string _sessionNote =
        "A live session always pauses motion so frame time stays clean. This toggle is not hidden.";

    public async Task InitializeAsync()
    {
        await _motion.InitializeAsync();
        SyncFromPolicy();
    }

    partial void OnInterfaceMotionEnabledChanged(bool value)
    {
        if (_suppressToggle)
        {
            return;
        }

        _ = _motion.SetUserWantsMotionAsync(value);
    }

    private void OnMotionChanged(object? sender, EventArgs e)
    {
        var queue = App.DispatcherQueue;
        if (queue is not null && !queue.HasThreadAccess)
        {
            _ = queue.TryEnqueue(SyncFromPolicy);
            return;
        }

        SyncFromPolicy();
    }

    private void SyncFromPolicy()
    {
        _suppressToggle = true;
        InterfaceMotionEnabled = _motion.UserWantsMotion;
        _suppressToggle = false;
        MotionStatus = _motion.StatusText;
    }
}
