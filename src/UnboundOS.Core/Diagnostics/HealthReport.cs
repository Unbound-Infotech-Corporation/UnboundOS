namespace UnboundOS.Core.Diagnostics;

public sealed record HealthCheck(
    string Id,
    string Title,
    bool Ok,
    string Detail);

public sealed record HealthReport(
    DateTimeOffset CollectedAt,
    IReadOnlyList<HealthCheck> Checks,
    string LogPath)
{
    public bool NetworkUp => Find("network")?.Ok == true;

    public bool GpuDriverPresent => Find("gpu")?.Ok == true;

    public bool ShellAutostart => Find("autostart")?.Ok == true;

    public bool UpdateGuardKnown => Find("updates") is not null;

    public HealthCheck? Find(string id) =>
        Checks.FirstOrDefault(check => string.Equals(check.Id, id, StringComparison.OrdinalIgnoreCase));

    public string ToLogText()
    {
        var lines = new List<string>
        {
            $"{Branding.ProductName} health {CollectedAt:u}",
            $"Log: {LogPath}",
            string.Empty
        };

        foreach (var check in Checks)
        {
            var mark = check.Ok ? "OK" : "CHECK";
            lines.Add($"{mark}  {check.Title}: {check.Detail}");
        }

        lines.Add(string.Empty);
        lines.Add(OsProductCopy.AutostartHonesty);
        lines.Add(OsProductCopy.ShellReplacementHonesty);
        lines.Add(OsProductCopy.AnticheatHonesty);
        lines.Add(OsProductCopy.FilesHonesty);
        lines.Add(OsProductCopy.UpdateGuardHonesty);
        lines.Add(OsProductCopy.HealthHonesty);
        return string.Join(Environment.NewLine, lines);
    }
}
