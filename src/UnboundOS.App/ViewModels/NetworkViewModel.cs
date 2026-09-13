using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public partial class NetworkViewModel : ObservableObject
{
    private readonly INetworkDirector _network;
    private readonly IProfileStore _profiles;

    public NetworkViewModel(INetworkDirector network, IProfileStore profiles)
    {
        _network = network;
        _profiles = profiles;
    }

    public ObservableCollection<NetworkAdapterInfo> Adapters { get; } = [];

    [ObservableProperty] private string _planSummary = "Scanning adapters…";
    [ObservableProperty] private string _streamerTip =
        "For streaming: put the game on your lowest-latency NIC and OBS upload on the second NIC so encode traffic cannot starve gameplay.";

    public async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        Adapters.Clear();
        var adapters = await _network.ListAdaptersAsync();
        var profiles = await _profiles.LoadAsync();
        var streamer = profiles.FirstOrDefault(p => p.Kind == ProfileKind.Streamer) ?? profiles.First();
        var plan = _network.RecommendPlan(adapters, streamer);
        PlanSummary = plan.Summary;

        foreach (var adapter in adapters)
        {
            Adapters.Add(adapter);
        }
    }

    [RelayCommand]
    private async Task ApplySeparationAsync()
    {
        var adapters = Adapters.ToList();
        var profiles = await _profiles.LoadAsync();
        var streamer = profiles.FirstOrDefault(p => p.Kind == ProfileKind.Streamer) ?? profiles.First();
        var plan = _network.RecommendPlan(adapters, streamer);
        var result = await _network.ApplyTrafficSeparationAsync(plan.GameAdapterId, plan.StreamAdapterId);
        PlanSummary = result.Succeeded
            ? $"{plan.Summary} — {result.Message}"
            : $"{plan.Summary} — {result.Message}";
        StreamerTip = result.Actions.Count > 0
            ? string.Join(" ", result.Actions)
            : StreamerTip;
        await RefreshAsync();
    }
}
