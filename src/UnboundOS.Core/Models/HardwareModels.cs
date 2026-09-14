namespace UnboundOS.Core.Models;

public sealed record MemoryModuleInfo(string Bank, string CapacityLabel, string? SpeedLabel);

public sealed record DiskVolumeInfo(string Name, string Label, string SizeLabel, string FreeLabel, string Format);

public sealed record GpuAdapterInfo(string Name, string? DriverVersion);

public sealed record SensorReading(string Name, string Value, string Group);

public sealed record HardwareSnapshot(
    string CpuName,
    int LogicalProcessors,
    string MemorySummary,
    IReadOnlyList<MemoryModuleInfo> MemoryModules,
    IReadOnlyList<DiskVolumeInfo> Disks,
    IReadOnlyList<GpuAdapterInfo> Gpus,
    string? Bios,
    IReadOnlyList<SensorReading> Sensors,
    string SourceNote)
{
    public static HardwareSnapshot Empty(string note) =>
        new("Unknown", Environment.ProcessorCount, "—", [], [], [], null, [], note);
}

public sealed record SetupCleanupResult(bool Succeeded, int RemovedCount, string Message);
