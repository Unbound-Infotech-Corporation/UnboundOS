using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;
using UnboundOS.Core.Shell;

namespace UnboundOS.App.ViewModels;

public partial class GuideViewModel : ObservableObject
{
    private readonly IGuideMenu _guide;
    private readonly ILastSessionResume _resume;
    private readonly ILibraryLaunchService _launch;
    private readonly IProfileStore _profiles;

    public GuideViewModel(
        IGuideMenu guide,
        ILastSessionResume resume,
        ILibraryLaunchService launch,
        IProfileStore profiles)
    {
        _guide = guide;
        _resume = resume;
        _launch = launch;
        _profiles = profiles;
    }

    public ObservableCollection<GuideAction> Actions { get; } = [];

    [ObservableProperty] private GuideAction? _selectedAction;
    [ObservableProperty] private string _status = OsProductCopy.GuideHonesty;
    [ObservableProperty] private string _resumeStatus = "No last game.";
    [ObservableProperty] private LastSessionRecord? _lastSession;
    [ObservableProperty] private bool _perfOverlayOn;

    public string Honesty => OsProductCopy.GuideHonesty;
    public string ControllerHonesty => OsProductCopy.ControllerHonesty;

    public async Task InitializeAsync()
    {
        Actions.Clear();
        foreach (var action in _guide.Actions)
        {
            Actions.Add(action);
        }

        SelectedAction ??= Actions.FirstOrDefault();
        LastSession = await _resume.LoadAsync();
        ResumeStatus = LastSession is null
            ? "No last game. Launch from Games to remember one."
            : LastSessionPolicy.ShouldOfferResume(LastSession, DateTimeOffset.UtcNow)
                ? $"Resume {LastSession.DisplayName} after sleep?"
                : $"{LastSession.DisplayName} is outside the 24h resume window.";
    }

    [RelayCommand]
    private async Task InvokeAsync()
    {
        if (SelectedAction is null)
        {
            return;
        }

        if (SelectedAction.Id == "perf")
        {
            PerfOverlayOn = !PerfOverlayOn;
            Status = PerfOverlayOn
                ? "Performance overlay on (in-shell telemetry). " + OsProductCopy.GuideHonesty
                : OsProductCopy.GuideHonesty;
            return;
        }

        var result = await _guide.InvokeAsync(SelectedAction.Id);
        Status = result.Message;
    }

    [RelayCommand]
    private async Task ResumeLastAsync()
    {
        if (LastSession is null || !LastSessionPolicy.ShouldOfferResume(LastSession, DateTimeOffset.UtcNow))
        {
            ResumeStatus = "Nothing to resume.";
            return;
        }

        var profiles = await _profiles.LoadAsync();
        var profile = profiles.FirstOrDefault() ?? new SessionProfile
        {
            Id = "resume",
            Name = "Resume"
        };
        var game = new LibraryGame(
            LastSession.GameId,
            LastSession.DisplayName,
            GameStore.Custom,
            Path.GetDirectoryName(LastSession.ExecutablePath),
            LastSession.LaunchUri,
            LastSession.ExecutablePath,
            Path.GetFileNameWithoutExtension(LastSession.ExecutablePath),
            null,
            []);
        var result = await _launch.LaunchAsync(game, profile);
        ResumeStatus = result.Message;
    }
}
