using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public partial class SessionViewModel : ObservableObject
{
    private readonly ISessionEngine _session;
    private readonly IProfileStore _profiles;
    private readonly IProcessGuardian _guardian;

    public SessionViewModel(ISessionEngine session, IProfileStore profiles, IProcessGuardian guardian)
    {
        _session = session;
        _profiles = profiles;
        _guardian = guardian;
    }

    public ObservableCollection<SessionProfile> Profiles { get; } = [];
    public ObservableCollection<string> ActionLog { get; } = [];
    public ObservableCollection<string> RunningSuspects { get; } = [];

    [ObservableProperty] private SessionProfile? _selectedProfile;
    [ObservableProperty] private string _headline = "Enter a focused gaming session";
    [ObservableProperty] private string _detail = "UnboundOS snapshots NIC metrics, switches to High Performance power, clears denylist junk, and keeps protect-list apps alive.";
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _canEnter = true;
    [ObservableProperty] private bool _canExit;

    public async Task InitializeAsync()
    {
        Profiles.Clear();
        foreach (var profile in await _profiles.LoadAsync())
        {
            Profiles.Add(profile);
        }

        SelectedProfile ??= Profiles.FirstOrDefault();
        SyncButtons();
        await RefreshSuspectsAsync();
    }

    [RelayCommand]
    private async Task EnterSessionAsync()
    {
        if (SelectedProfile is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _session.EnterAsync(SelectedProfile);
            PushResult(result);
            Headline = result.Succeeded ? "Session unbound" : "Session blocked";
            Detail = result.Message;
        }
        finally
        {
            IsBusy = false;
            SyncButtons();
            await RefreshSuspectsAsync();
        }
    }

    [RelayCommand]
    private async Task ExitSessionAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _session.ExitAsync();
            PushResult(result);
            Headline = result.Succeeded ? "Desktop posture restored" : "Exit incomplete";
            Detail = result.Message;
        }
        finally
        {
            IsBusy = false;
            SyncButtons();
            await RefreshSuspectsAsync();
        }
    }

    [RelayCommand]
    private async Task RefreshSuspectsAsync()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        RunningSuspects.Clear();
        foreach (var name in await _guardian.ListRunningSuspectsAsync(SelectedProfile.TerminateProcessNames))
        {
            RunningSuspects.Add(name);
        }
    }

    partial void OnSelectedProfileChanged(SessionProfile? value) => _ = RefreshSuspectsAsync();

    private void PushResult(SessionMutationResult result)
    {
        ActionLog.Clear();
        foreach (var action in result.Actions)
        {
            ActionLog.Add(action);
        }

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            ActionLog.Insert(0, result.Message);
        }
    }

    private void SyncButtons()
    {
        CanEnter = _session.State is SessionState.Idle or SessionState.Faulted;
        CanExit = _session.State is SessionState.Active or SessionState.Faulted;
    }
}
