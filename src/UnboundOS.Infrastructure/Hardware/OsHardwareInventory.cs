using Microsoft.Win32;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Hardware;

/// <summary>
/// First-slice hardware inventory from OS APIs and registry. Not a HWiNFO clone.
/// Live sensors / LibreHardwareMonitor are later (docs/os-spec.md §6).
/// </summary>
public sealed class OsHardwareInventory : IHardwareInventory
{
    public Task<HardwareSnapshot> SampleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cpu = ReadCpuName();
        var logical = Environment.ProcessorCount;
        var disks = ReadDisks();
        var gpus = ReadGpus();
        var memoryBytes = ReadTotalMemoryBytes();
        var memorySummary = memoryBytes is null
            ? $"{logical} logical processors"
            : $"{memoryBytes.Value / (1024.0 * 1024 * 1024):0.0} GB reported · {logical} logical processors";

        var snapshot = new HardwareSnapshot(
            cpu,
            logical,
            memorySummary,
            [],
            disks,
            gpus,
            ReadBios(),
            [],
            OsProductCopy.HardwareHonesty);

        return Task.FromResult(snapshot);
    }

    private static string ReadCpuName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            if (key?.GetValue("ProcessorNameString") is string name && !string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }
        }
        catch
        {
            // Linux CI / missing hive.
        }

        return Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU";
    }

    private static long? ReadTotalMemoryBytes()
    {
        try
        {
            var info = GC.GetGCMemoryInfo();
            return info.TotalAvailableMemoryBytes > 0 ? info.TotalAvailableMemoryBytes : null;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<DiskVolumeInfo> ReadDisks()
    {
        var disks = new List<DiskVolumeInfo>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady)
                {
                    continue;
                }

                disks.Add(new DiskVolumeInfo(
                    drive.Name,
                    string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.DriveType.ToString() : drive.VolumeLabel,
                    $"{drive.TotalSize / (1024.0 * 1024 * 1024):0.0} GB",
                    $"{drive.AvailableFreeSpace / (1024.0 * 1024 * 1024):0.0} GB free",
                    drive.DriveFormat));
            }
            catch
            {
                // Skip locked volumes.
            }
        }

        return disks;
    }

    private static IReadOnlyList<GpuAdapterInfo> ReadGpus()
    {
        var found = new List<GpuAdapterInfo>();
        try
        {
            using var cls = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
            if (cls is null)
            {
                return found;
            }

            foreach (var name in cls.GetSubKeyNames())
            {
                if (string.IsNullOrEmpty(name) || !char.IsDigit(name[0]))
                {
                    continue;
                }

                using var sub = cls.OpenSubKey(name);
                var desc = sub?.GetValue("DriverDesc") as string;
                if (string.IsNullOrWhiteSpace(desc))
                {
                    continue;
                }

                var version = sub?.GetValue("DriverVersion") as string;
                found.Add(new GpuAdapterInfo(desc.Trim(), version));
            }
        }
        catch
        {
            // Missing on Linux CI.
        }

        return found;
    }

    private static string? ReadBios()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
            var vendor = key?.GetValue("BIOSVendor") as string;
            var version = key?.GetValue("BIOSVersion") as string;
            if (string.IsNullOrWhiteSpace(vendor) && string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            return string.Join(" · ", new[] { vendor, version }.Where(part => !string.IsNullOrWhiteSpace(part)));
        }
        catch
        {
            return null;
        }
    }
}
