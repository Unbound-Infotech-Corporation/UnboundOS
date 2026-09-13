using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public partial class ProfilesViewModel : ObservableObject
{
    private readonly IProfileStore _store;

    public ProfilesViewModel(IProfileStore store) => _store = store;

    public ObservableCollection<SessionProfile> Profiles { get; } = [];

    [ObservableProperty] private SessionProfile? _selectedProfile;
    [ObservableProperty] private string _denylistPreview = string.Empty;
    [ObservableProperty] private string _protectPreview = string.Empty;

    public async Task InitializeAsync()
    {
        Profiles.Clear();
        foreach (var profile in await _store.LoadAsync())
        {
            Profiles.Add(profile);
        }

        SelectedProfile ??= Profiles.FirstOrDefault();
    }

    partial void OnSelectedProfileChanged(SessionProfile? value)
    {
        if (value is null)
        {
            DenylistPreview = string.Empty;
            ProtectPreview = string.Empty;
            return;
        }

        DenylistPreview = string.Join(", ", value.TerminateProcessNames);
        ProtectPreview = string.Join(", ", value.ProtectProcessNames);
    }

    [RelayCommand]
    private async Task ResetDefaultsAsync()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unbound Infotech Corporation",
            "UnboundOS",
            "profiles.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        await _store.EnsureDefaultsAsync();
        await InitializeAsync();
    }
}
