using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public partial class HardwareViewModel(IHardwareInventory inventory) : ObservableObject
{
    public ObservableCollection<DiskVolumeInfo> Disks { get; } = [];
    public ObservableCollection<GpuAdapterInfo> Gpus { get; } = [];
    public ObservableCollection<SensorReading> Sensors { get; } = [];

    [ObservableProperty] private string _cpuName = "—";
    [ObservableProperty] private string _memorySummary = "—";
    [ObservableProperty] private string _bios = "—";
    [ObservableProperty] private string _sourceNote = OsProductCopy.HardwareHonesty;
    [ObservableProperty] private string _status = "Reading hardware…";
    [ObservableProperty] private bool _showGpuEmpty = true;

    public string Honesty => OsProductCopy.HardwareHonesty;

    public async Task InitializeAsync()
    {
        try
        {
            var snapshot = await inventory.SampleAsync();
            CpuName = snapshot.CpuName;
            MemorySummary = snapshot.MemorySummary;
            Bios = string.IsNullOrWhiteSpace(snapshot.Bios) ? "—" : snapshot.Bios;
            SourceNote = snapshot.SourceNote;
            Disks.Clear();
            foreach (var disk in snapshot.Disks)
            {
                Disks.Add(disk);
            }

            Gpus.Clear();
            foreach (var gpu in snapshot.Gpus)
            {
                Gpus.Add(gpu);
            }

            Sensors.Clear();
            foreach (var sensor in snapshot.Sensors)
            {
                Sensors.Add(sensor);
            }

            ShowGpuEmpty = Gpus.Count == 0;
            Status = Sensors.Count == 0
                ? "Inventory loaded. Live temps/clocks need HWiNFO or LibreHardwareMonitor later."
                : $"Inventory loaded · {Sensors.Count} sensors.";
        }
        catch (Exception error)
        {
            Status = $"Hardware inventory could not complete: {error.Message}";
        }
    }
}
