using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Network;

public sealed class NetworkDirector : INetworkDirector
{
    public async Task<IReadOnlyList<NetworkAdapterInfo>> ListAdaptersAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var metricsByName = await ReadInterfaceMetricsByNameAsync(ct).ConfigureAwait(false);

        var list = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n =>
                n.NetworkInterfaceType is not NetworkInterfaceType.Loopback
                && n.NetworkInterfaceType is not NetworkInterfaceType.Tunnel)
            .Select(n => Map(n, metricsByName))
            .OrderByDescending(a => a.IsUp)
            .ThenByDescending(a => a.SpeedMbps)
            .ToList();

        return list;
    }

    public NetworkPlan RecommendPlan(IReadOnlyList<NetworkAdapterInfo> adapters, SessionProfile profile)
    {
        var up = adapters.Where(a => a.IsUp).ToList();
        if (up.Count == 0)
        {
            return new NetworkPlan { Summary = "No active adapters detected." };
        }

        var wired = up.Where(a => !a.IsWireless).OrderByDescending(a => a.SpeedMbps).ToList();
        var game = wired.FirstOrDefault() ?? up.OrderByDescending(a => a.SpeedMbps).First();
        NetworkAdapterInfo? stream = null;

        if (!string.IsNullOrWhiteSpace(profile.PreferredGameAdapterId))
        {
            game = up.FirstOrDefault(a => a.Id == profile.PreferredGameAdapterId) ?? game;
        }

        if (!string.IsNullOrWhiteSpace(profile.PreferredStreamAdapterId))
        {
            stream = up.FirstOrDefault(a => a.Id == profile.PreferredStreamAdapterId);
        }

        stream ??= wired.FirstOrDefault(a => a.Id != game.Id)
                   ?? up.FirstOrDefault(a => a.Id != game.Id);

        foreach (var adapter in adapters)
        {
            adapter.RoleHint = adapter.Id == game.Id
                ? "Game path"
                : stream is not null && adapter.Id == stream.Id
                    ? "Stream / bulk path"
                    : "Standby";
        }

        var summary = stream is null
            ? $"Single-path mode on {game.Name}. Add a second NIC for stream isolation."
            : $"Game → {game.Name} (metric low). Stream/bulk → {stream.Name} (metric higher).";

        return new NetworkPlan
        {
            GameAdapterId = game.Id,
            StreamAdapterId = stream?.Id,
            Summary = summary
        };
    }

    public async Task<IReadOnlyDictionary<string, int>> CaptureMetricsAsync(CancellationToken ct = default)
    {
        var adapters = await ListAdaptersAsync(ct).ConfigureAwait(false);
        return adapters.ToDictionary(a => a.Id, a => a.InterfaceMetric);
    }

    public async Task<NetworkMutationResult> ApplyTrafficSeparationAsync(
        string? gameAdapterId,
        string? streamAdapterId,
        bool allowElevation = true,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(gameAdapterId))
        {
            return new NetworkMutationResult
            {
                Succeeded = false,
                Message = "No game adapter selected — nothing to apply."
            };
        }

        var adapters = await ListAdaptersAsync(ct).ConfigureAwait(false);
        var game = adapters.FirstOrDefault(a => a.Id == gameAdapterId);
        var stream = adapters.FirstOrDefault(a => a.Id == streamAdapterId);
        var actions = new List<string>();
        var elevationRequired = false;
        var anyFailed = false;

        if (game is not null)
        {
            var set = await SetMetricAsync(game.Name, 5, allowElevation, ct).ConfigureAwait(false);
            actions.Add(set.Detail);
            elevationRequired |= set.ElevationRequired;
            anyFailed |= !set.Ok;
        }
        else
        {
            actions.Add("Game adapter id was not found among current NICs.");
            anyFailed = true;
        }

        if (stream is not null)
        {
            var set = await SetMetricAsync(stream.Name, 50, allowElevation, ct).ConfigureAwait(false);
            actions.Add(set.Detail);
            elevationRequired |= set.ElevationRequired;
            anyFailed |= !set.Ok;
        }

        // Verify metrics actually moved when we claimed success.
        if (!anyFailed)
        {
            var after = await ListAdaptersAsync(ct).ConfigureAwait(false);
            var gameAfter = after.FirstOrDefault(a => a.Id == gameAdapterId);
            if (gameAfter is not null && gameAfter.InterfaceMetric > 10)
            {
                anyFailed = true;
                elevationRequired = true;
                actions.Add(
                    $"Verify failed: {gameAfter.Name} still reports metric {gameAfter.InterfaceMetric}. " +
                    "Approve the UAC prompt or run UnboundOS as Administrator.");
            }
        }

        return new NetworkMutationResult
        {
            Succeeded = !anyFailed,
            ElevationRequired = elevationRequired,
            Message = anyFailed
                ? (elevationRequired
                    ? "NIC metrics were not changed. Administrator / UAC approval is required."
                    : "NIC metric apply did not fully succeed.")
                : "NIC interface metrics updated in Windows.",
            Actions = actions
        };
    }

    public async Task<NetworkMutationResult> RestoreMetricsAsync(
        IReadOnlyDictionary<string, int> originalMetrics,
        bool allowElevation = true,
        CancellationToken ct = default)
    {
        var adapters = await ListAdaptersAsync(ct).ConfigureAwait(false);
        var actions = new List<string>();
        var elevationRequired = false;
        var anyFailed = false;

        foreach (var pair in originalMetrics)
        {
            var adapter = adapters.FirstOrDefault(a => a.Id == pair.Key);
            if (adapter is null)
            {
                continue;
            }

            var metric = pair.Value <= 0 ? 25 : pair.Value;
            var set = await SetMetricAsync(adapter.Name, metric, allowElevation, ct).ConfigureAwait(false);
            actions.Add(set.Detail);
            elevationRequired |= set.ElevationRequired;
            anyFailed |= !set.Ok;
        }

        return new NetworkMutationResult
        {
            Succeeded = !anyFailed,
            ElevationRequired = elevationRequired,
            Message = anyFailed
                ? "Could not restore all NIC metrics."
                : "Restored original NIC metrics.",
            Actions = actions
        };
    }

    private static NetworkAdapterInfo Map(NetworkInterface nic, IReadOnlyDictionary<string, int> metricsByName)
    {
        var props = nic.GetIPProperties();
        var ipv4 = props.UnicastAddresses
            .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
            ?.Address.ToString();

        // GetIPv4Properties().Index is the interface INDEX, not the route metric.
        var indexFallback = props.GetIPv4Properties()?.Index ?? 0;
        var metric = metricsByName.TryGetValue(nic.Name, out var resolved) ? resolved : indexFallback;
        var speed = nic.Speed > 0 ? nic.Speed / 1_000_000 : 0;
        var wireless = nic.NetworkInterfaceType is NetworkInterfaceType.Wireless80211;

        return new NetworkAdapterInfo
        {
            Id = nic.Id,
            Name = nic.Name,
            Description = nic.Description,
            IPv4Address = ipv4,
            SpeedMbps = speed,
            IsUp = nic.OperationalStatus == OperationalStatus.Up,
            IsWireless = wireless,
            InterfaceMetric = metric
        };
    }

    internal static async Task<IReadOnlyDictionary<string, int>> ReadInterfaceMetricsByNameAsync(CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = "interface ipv4 show interfaces",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process is null)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return ParseInterfaceMetrics(output);
    }

    /// <summary>
    /// Parses `netsh interface ipv4 show interfaces` rows:
    /// Idx  Met  MTU  State  Name (name may contain spaces).
    /// </summary>
    internal static IReadOnlyDictionary<string, int> ParseInterfaceMetrics(string netshOutput)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in netshOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0
                || line.StartsWith("Idx", StringComparison.OrdinalIgnoreCase)
                || line.StartsWith("---", StringComparison.Ordinal))
            {
                continue;
            }

            var match = Regex.Match(
                line,
                @"^(?<idx>\d+)\s+(?<met>\d+)\s+(?<mtu>\d+)\s+(?<state>\S+)\s+(?<name>.+)$",
                RegexOptions.CultureInvariant);
            if (!match.Success)
            {
                continue;
            }

            if (int.TryParse(match.Groups["met"].Value, out var metric))
            {
                result[match.Groups["name"].Value.Trim()] = metric;
            }
        }

        return result;
    }

    private static async Task<(bool Ok, bool ElevationRequired, string Detail)> SetMetricAsync(
        string interfaceName,
        int metric,
        bool allowElevation,
        CancellationToken ct)
    {
        var (exitCode, stderr) = await RunNetshAsync(
                $"interface ipv4 set interface name=\"{interfaceName}\" metric={metric}",
                ct)
            .ConfigureAwait(false);

        if (exitCode == 0)
        {
            return (true, false, $"Set {interfaceName} metric → {metric}.");
        }

        if (!allowElevation)
        {
            return (false, true,
                $"Denied setting {interfaceName} metric={metric} (exit {exitCode}). Elevation required.");
        }

        // Retry with UAC elevation so the change actually lands in Windows.
        try
        {
            var elevatedCode = await RunNetshElevatedAsync(
                    $"interface ipv4 set interface name=\"{interfaceName}\" metric={metric}",
                    ct)
                .ConfigureAwait(false);

            if (elevatedCode == 0)
            {
                return (true, true, $"Set {interfaceName} metric → {metric} (elevated).");
            }

            return (false, true,
                $"Elevated netsh failed for {interfaceName} (exit {elevatedCode}). {Trim(stderr)}");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return (false, true,
                $"UAC declined for {interfaceName}. Approve the prompt or run UnboundOS as Administrator.");
        }
    }

    private static async Task<(int ExitCode, string StdErr)> RunNetshAsync(
        string arguments,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process is null)
        {
            return (-1, "Failed to start netsh.");
        }

        var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return (process.ExitCode, stderr);
    }

    private static async Task<int> RunNetshElevatedAsync(string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "netsh",
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process is null)
        {
            return -1;
        }

        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return process.ExitCode;
    }

    private static string Trim(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
}
