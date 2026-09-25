using System.Management;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Hardware;

/// <summary>
/// Best-effort CPU/GPU/package temps. Returns nulls when no sensor path answers.
/// Does not invent readings. LibreHardwareMonitor remains a later image slice.
/// </summary>
public sealed class OsThermalProbe : IThermalProbe
{
    public const string UnavailableNote =
        "Temps bind when sysfs, WMI thermal zones, or a later LibreHardwareMonitor path answers. UnboundOS does not invent readings.";

    public Task<ThermalSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var linux = ReadLinuxSysfs();
        if (linux.HasAny)
        {
            return Task.FromResult(linux);
        }

        var windows = ReadWindowsWmi();
        if (windows.HasAny)
        {
            return Task.FromResult(windows);
        }

        return Task.FromResult(ThermalSnapshot.Unavailable(UnavailableNote));
    }

    private static ThermalSnapshot ReadLinuxSysfs()
    {
        double? cpu = null;
        double? gpu = null;
        double? pkg = null;
        try
        {
            var root = "/sys/class/thermal";
            if (Directory.Exists(root))
            {
                foreach (var zone in Directory.EnumerateDirectories(root, "thermal_zone*"))
                {
                    var type = ReadText(Path.Combine(zone, "type"));
                    var temp = ReadMilliC(Path.Combine(zone, "temp"));
                    if (temp is null)
                    {
                        continue;
                    }

                    if (type.Contains("pkg", StringComparison.OrdinalIgnoreCase) ||
                        type.Contains("x86_pkg", StringComparison.OrdinalIgnoreCase))
                    {
                        pkg ??= temp;
                        cpu ??= temp;
                    }
                    else if (type.Contains("cpu", StringComparison.OrdinalIgnoreCase) ||
                             type.Contains("acpitz", StringComparison.OrdinalIgnoreCase) ||
                             type.Contains("k10temp", StringComparison.OrdinalIgnoreCase) ||
                             type.Contains("coretemp", StringComparison.OrdinalIgnoreCase))
                    {
                        cpu ??= temp;
                    }
                    else if (type.Contains("gpu", StringComparison.OrdinalIgnoreCase) ||
                             type.Contains("amdgpu", StringComparison.OrdinalIgnoreCase) ||
                             type.Contains("nouveau", StringComparison.OrdinalIgnoreCase))
                    {
                        gpu ??= temp;
                    }
                }
            }

            var hwmon = "/sys/class/hwmon";
            if (Directory.Exists(hwmon))
            {
                foreach (var dir in Directory.EnumerateDirectories(hwmon, "hwmon*"))
                {
                    var name = ReadText(Path.Combine(dir, "name"));
                    var temp = ReadMilliC(Path.Combine(dir, "temp1_input"));
                    if (temp is null)
                    {
                        continue;
                    }

                    if (name.Contains("k10temp", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("coretemp", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("zenpower", StringComparison.OrdinalIgnoreCase))
                    {
                        cpu ??= temp;
                    }
                    else if (name.Contains("amdgpu", StringComparison.OrdinalIgnoreCase) ||
                             name.Contains("nvidia", StringComparison.OrdinalIgnoreCase) ||
                             name.Contains("nouveau", StringComparison.OrdinalIgnoreCase) ||
                             name.Contains("i915", StringComparison.OrdinalIgnoreCase))
                    {
                        gpu ??= temp;
                    }
                }
            }
        }
        catch
        {
            // Missing sysfs or permission.
        }

        return cpu is null && gpu is null && pkg is null
            ? ThermalSnapshot.Unavailable(UnavailableNote)
            : new ThermalSnapshot(cpu, gpu, pkg, "sysfs thermal / hwmon");
    }

    private static ThermalSnapshot ReadWindowsWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\wmi",
                "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            double? cpu = null;
            foreach (ManagementObject row in searcher.Get())
            {
                var value = row["CurrentTemperature"];
                if (value is null)
                {
                    continue;
                }

                var raw = Convert.ToDouble(value);
                var celsius = raw > 200 ? raw / 10.0 - 273.15 : raw;
                if (celsius is >= 1 and <= 120)
                {
                    cpu = celsius;
                    break;
                }
            }

            return cpu is null
                ? ThermalSnapshot.Unavailable(UnavailableNote)
                : new ThermalSnapshot(cpu, null, cpu, "WMI MSAcpi_ThermalZoneTemperature");
        }
        catch
        {
            return ThermalSnapshot.Unavailable(UnavailableNote);
        }
    }

    private static string ReadText(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static double? ReadMilliC(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var text = File.ReadAllText(path).Trim();
            if (!double.TryParse(text, out var milli))
            {
                return null;
            }

            var celsius = milli > 200 ? milli / 1000.0 : milli;
            return celsius is >= 1 and <= 120 ? celsius : null;
        }
        catch
        {
            return null;
        }
    }
}
