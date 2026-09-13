using System.Diagnostics;
using System.Text.RegularExpressions;
using UnboundOS.Core.Abstractions;

namespace UnboundOS.Infrastructure.Power;

public sealed class WindowsPowerPlanService : IPowerPlanService
{
    // Well-known Windows power scheme GUIDs.
    private static readonly Guid HighPerformanceGuid = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");
    private static readonly Guid UltimatePerformanceGuid = new("e9a42b02-d5df-448d-aa00-03f14749eb61");

    public async Task<string?> GetActiveSchemeGuidAsync(CancellationToken ct = default)
    {
        var (exitCode, stdout, _) = await RunPowerCfgAsync("/getactivescheme", ct).ConfigureAwait(false);
        if (exitCode != 0)
        {
            return null;
        }

        return ParseGuid(stdout);
    }

    public async Task<(bool Succeeded, string Message)> ActivateHighPerformanceAsync(CancellationToken ct = default)
    {
        var (listCode, listOut, listErr) = await RunPowerCfgAsync("/list", ct).ConfigureAwait(false);
        if (listCode != 0)
        {
            return (false, $"Could not list power plans: {Trim(listErr)}");
        }

        var target = ResolveHighPerformanceGuid(listOut);
        if (target is null)
        {
            return (false, "No High performance / Ultimate performance power plan found on this PC.");
        }

        var (setCode, _, setErr) = await RunPowerCfgAsync($"/setactive {target}", ct).ConfigureAwait(false);
        if (setCode != 0)
        {
            return (false, $"powercfg failed to activate plan: {Trim(setErr)}");
        }

        var active = await GetActiveSchemeGuidAsync(ct).ConfigureAwait(false);
        if (!string.Equals(active, target, StringComparison.OrdinalIgnoreCase))
        {
            return (false, "powercfg reported success but the active scheme did not change.");
        }

        var label = target.Equals(UltimatePerformanceGuid.ToString(), StringComparison.OrdinalIgnoreCase)
            ? "Ultimate Performance"
            : "High performance";
        return (true, $"Switched Windows power plan to {label}.");
    }

    public async Task<(bool Succeeded, string Message)> RestoreSchemeAsync(string? schemeGuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(schemeGuid))
        {
            return (true, "No prior power plan to restore.");
        }

        var (setCode, _, setErr) = await RunPowerCfgAsync($"/setactive {schemeGuid}", ct).ConfigureAwait(false);
        if (setCode != 0)
        {
            return (false, $"Could not restore power plan: {Trim(setErr)}");
        }

        return (true, "Restored previous Windows power plan.");
    }

    internal static string? ResolveHighPerformanceGuid(string powercfgListOutput)
    {
        var schemes = ParseSchemes(powercfgListOutput);
        var byName = schemes.FirstOrDefault(s =>
            s.Name.Contains("Ultimate Performance", StringComparison.OrdinalIgnoreCase)
            || s.Name.Contains("High performance", StringComparison.OrdinalIgnoreCase));
        if (byName.Guid is not null)
        {
            return byName.Guid;
        }

        if (schemes.Any(s => s.Guid.Equals(UltimatePerformanceGuid.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return UltimatePerformanceGuid.ToString();
        }

        if (schemes.Any(s => s.Guid.Equals(HighPerformanceGuid.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return HighPerformanceGuid.ToString();
        }

        return null;
    }

    internal static IReadOnlyList<(string Guid, string Name)> ParseSchemes(string output)
    {
        var list = new List<(string Guid, string Name)>();
        foreach (var raw in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Regex.Match(
                raw,
                @"Power Scheme GUID:\s*(?<guid>[0-9a-fA-F\-]{36})\s*\((?<name>[^)]+)\)",
                RegexOptions.CultureInvariant);
            if (match.Success)
            {
                list.Add((match.Groups["guid"].Value, match.Groups["name"].Value.Trim()));
            }
        }

        return list;
    }

    private static string? ParseGuid(string output)
    {
        var match = Regex.Match(
            output,
            @"([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
            RegexOptions.CultureInvariant);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunPowerCfgAsync(
        string arguments,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powercfg",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process is null)
        {
            return (-1, string.Empty, "Failed to start powercfg.");
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return (process.ExitCode, stdout, stderr);
    }

    private static string Trim(string value) =>
        string.IsNullOrWhiteSpace(value) ? "(no details)" : value.Trim();
}
