namespace UnboundOS.Core.Models;

public sealed class NetworkMutationResult
{
    public bool Succeeded { get; init; }
    public bool ElevationRequired { get; init; }
    public string Message { get; init; } = string.Empty;
    public IReadOnlyList<string> Actions { get; init; } = [];
}
