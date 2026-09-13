namespace UnboundOS.Core.Models;

public sealed class NetworkAdapterInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? IPv4Address { get; init; }
    public long SpeedMbps { get; init; }
    public bool IsUp { get; init; }
    public bool IsWireless { get; init; }
    public int InterfaceMetric { get; init; }
    public string RoleHint { get; set; } = "Unassigned";
}

public sealed class NetworkPlan
{
    public string? GameAdapterId { get; init; }
    public string? StreamAdapterId { get; init; }
    public string Summary { get; init; } = string.Empty;
}
