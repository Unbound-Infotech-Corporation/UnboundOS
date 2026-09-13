using UnboundOS.Infrastructure.Power;

namespace UnboundOS.Tests;

public class PowerPlanServiceTests
{
    [Fact]
    public void ParseSchemes_ReadsGuidAndName()
    {
        const string sample = """
            Existing Power Schemes (* Active)
            -----------------------------------
            Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced) *
            Power Scheme GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (High performance)
            Power Scheme GUID: a1841308-3541-4fab-bc81-f71556f20b4a  (Power saver)
            """;

        var schemes = WindowsPowerPlanService.ParseSchemes(sample);
        Assert.Equal(3, schemes.Count);
        Assert.Contains(schemes, s => s.Name == "High performance");
    }

    [Fact]
    public void ResolveHighPerformanceGuid_PrefersNamedHighPerformance()
    {
        const string sample = """
            Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)
            Power Scheme GUID: 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c  (High performance)
            """;

        var guid = WindowsPowerPlanService.ResolveHighPerformanceGuid(sample);
        Assert.Equal("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", guid);
    }

    [Fact]
    public void ResolveHighPerformanceGuid_PrefersUltimateWhenPresent()
    {
        const string sample = """
            Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)
            Power Scheme GUID: e9a42b02-d5df-448d-aa00-03f14749eb61  (Ultimate Performance)
            """;

        var guid = WindowsPowerPlanService.ResolveHighPerformanceGuid(sample);
        Assert.Equal("e9a42b02-d5df-448d-aa00-03f14749eb61", guid);
    }
}
