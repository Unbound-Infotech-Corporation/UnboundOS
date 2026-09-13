using UnboundOS.Infrastructure.Network;

namespace UnboundOS.Tests;

public sealed class NetworkDirectorMetricTests
{
    [Fact]
    public void ParseInterfaceMetrics_ReadsMetNotIdx()
    {
        const string sample =
            """
            Idx     Met         MTU          State                Name
            ---  ----------  ----------  ------------  ---------------------------
              1          75  4294967295  connected     Loopback Pseudo-Interface 1
             10          20        1500  connected     Ethernet
              4          30        1500  disconnected  Wi-Fi
              3           5        1500  disconnected  Ethernet 2
            """;

        var parsed = NetworkDirector.ParseInterfaceMetrics(sample);

        Assert.Equal(20, parsed["Ethernet"]);
        Assert.Equal(30, parsed["Wi-Fi"]);
        Assert.Equal(5, parsed["Ethernet 2"]);
        Assert.False(parsed.ContainsKey("10"));
    }

    [Fact]
    public async Task CaptureMetricsAsync_MatchesNetshMetricNotInterfaceIndex()
    {
        var director = new NetworkDirector();
        var adapters = await director.ListAdaptersAsync();
        var captured = await director.CaptureMetricsAsync();

        var up = adapters.FirstOrDefault(a => a.IsUp && !a.IsWireless);
        Assert.NotNull(up);
        Assert.True(captured.TryGetValue(up.Id, out var metric));
        Assert.Equal(up.InterfaceMetric, metric);
        Assert.NotEqual(0, metric);
    }
}
