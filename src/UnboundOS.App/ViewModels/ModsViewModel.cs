using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public partial class ModsViewModel(
    IModCatalogService catalog,
    IModProfileStore profileStore,
    IModProfileManager profileManager,
    IExternalModHandoff handoff) : ObservableObject
{
    public ObservableCollection<ModGame> Games { get; } = [];
    public ObservableCollection<InstalledMod> Mods { get; } = [];
    public ObservableCollection<ModProfile> Profiles { get; } = [];

    [ObservableProperty] private ModGame? _selectedGame;
    [ObservableProperty] private InstalledMod? _selectedMod;
    [ObservableProperty] private ModProfile? _selectedProfile;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _status = "Scanning Steam libraries…";
    [ObservableProperty] private string _capabilitySummary = string.Empty;
    [ObservableProperty] private string _modDetails = "Select a mod to inspect dependencies and conflicts.";
    [ObservableProperty] private string _profileName = "My Mod Set";

    public bool CanApplyProfile =>
        SelectedGame?.Capabilities is { CanToggle: true } or { CanReorder: true };

    public bool CanOpenWorkshop =>
        SelectedGame?.SteamAppId is not null;

    public bool CanEditLoadOrder =>
        SelectedGame?.Capabilities.CanReorder == true;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            Games.Clear();
            foreach (var game in await catalog.DiscoverAsync())
            {
                Games.Add(game);
            }

            var savedProfiles = await profileStore.LoadAsync();
            Profiles.Clear();
            foreach (var profile in savedProfiles)
            {
                Profiles.Add(profile);
            }

            SelectedGame ??= Games.FirstOrDefault();
            Status = SelectedGame?.Provider == ModProvider.BuiltIn
                ? "No local Workshop content found. Showing a safe, read-only preview."
                : $"Found {Games.Count} game(s) with local Workshop content.";
        }
        catch (Exception error)
        {
            Status = $"Workshop discovery could not complete: {error.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedGameChanged(ModGame? value)
    {
        Mods.Clear();
        var orderedMods = value is null
            ? Enumerable.Empty<InstalledMod>()
            : value.Mods.OrderBy(mod => mod.LoadOrder);
        foreach (var mod in orderedMods)
        {
            Mods.Add(mod);
        }

        CapabilitySummary = value?.Capabilities.Explanation ?? "Select a game.";
        SelectedProfile = Profiles.FirstOrDefault(profile => profile.GameId == value?.GameId);
        OnPropertyChanged(nameof(CanApplyProfile));
        OnPropertyChanged(nameof(CanOpenWorkshop));
        OnPropertyChanged(nameof(CanEditLoadOrder));
    }

    partial void OnSelectedModChanged(InstalledMod? value)
    {
        ModDetails = value is null
            ? "Select a mod to inspect dependencies and conflicts."
            : $"Provider: {value.Provider}  •  Update: {value.UpdateState}\n" +
              $"Dependencies: {JoinOrNone(value.Dependencies)}\n" +
              $"Conflicts: {JoinOrNone(value.Conflicts)}\n" +
              $"Location: {(string.IsNullOrWhiteSpace(value.InstallPath) ? "Preview only" : value.InstallPath)}";
    }

    [RelayCommand]
    private async Task RefreshAsync() => await InitializeAsync();

    [RelayCommand]
    private void ToggleSelected()
    {
        if (SelectedMod is null)
        {
            return;
        }

        var index = Mods.IndexOf(SelectedMod);
        var updated = SelectedMod with { IsEnabled = !SelectedMod.IsEnabled };
        Mods[index] = updated;
        SelectedMod = updated;
        Status = CanApplyProfile
            ? "Profile draft changed. Save and apply when ready."
            : "Profile draft changed in UnboundOS. This game is discovery-only; files were not changed.";
    }

    [RelayCommand]
    private void MoveUp() => MoveSelected(-1);

    [RelayCommand]
    private void MoveDown() => MoveSelected(1);

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (SelectedGame is null || string.IsNullOrWhiteSpace(ProfileName))
        {
            Status = "Choose a game and enter a profile name.";
            return;
        }

        var profile = new ModProfile(
            Guid.NewGuid().ToString("N"),
            SelectedGame.GameId,
            ProfileName.Trim(),
            Mods.Select((mod, index) => new ModProfileEntry(mod.Id, mod.IsEnabled, index)).ToArray(),
            DateTimeOffset.UtcNow,
            $"Created in UnboundOS for {SelectedGame.DisplayName}");

        var all = (await profileStore.LoadAsync()).ToList();
        all.RemoveAll(existing =>
            existing.GameId == profile.GameId &&
            existing.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));
        all.Add(profile);
        await profileStore.SaveAsync(all);

        Profiles.Clear();
        foreach (var saved in all.OrderBy(item => item.Name))
        {
            Profiles.Add(saved);
        }

        SelectedProfile = profile;
        Status = $"Saved profile '{profile.Name}'.";
    }

    [RelayCommand]
    private async Task ApplyProfileAsync()
    {
        if (SelectedGame is null || SelectedProfile is null)
        {
            Status = "Select a game and saved profile first.";
            return;
        }

        IsBusy = true;
        try
        {
            var result = await profileManager.ApplyAsync(SelectedGame, SelectedProfile);
            Status = result.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RestoreLatestAsync()
    {
        if (SelectedGame is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var result = await profileManager.RestoreLatestAsync(SelectedGame);
            Status = result.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenWorkshopAsync()
    {
        if (SelectedGame?.SteamAppId is { } appId)
        {
            await handoff.OpenWorkshopAsync(appId);
            Status = "Opened this game's Workshop in Steam.";
        }
    }

    [RelayCommand]
    private async Task OpenSelectedItemAsync()
    {
        if (SelectedMod is { Provider: ModProvider.SteamWorkshop })
        {
            await handoff.OpenWorkshopItemAsync(SelectedMod.Id);
            Status = "Opened the Workshop item in Steam.";
        }
    }

    private void MoveSelected(int delta)
    {
        if (SelectedMod is null)
        {
            return;
        }

        var from = Mods.IndexOf(SelectedMod);
        var to = Math.Clamp(from + delta, 0, Mods.Count - 1);
        if (from == to)
        {
            return;
        }

        Mods.Move(from, to);
        Status = CanEditLoadOrder
            ? "Load-order draft changed. Save the profile to keep it."
            : "Draft reordered for planning only. This game has no load-order adapter.";
    }

    private static string JoinOrNone(IReadOnlyList<string> values) =>
        values.Count == 0 ? "None declared" : string.Join(", ", values);
}
