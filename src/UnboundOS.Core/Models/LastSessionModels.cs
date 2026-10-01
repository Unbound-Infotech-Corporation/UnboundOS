namespace UnboundOS.Core.Models;

public sealed record LastSessionRecord(
    string GameId,
    string DisplayName,
    string LaunchUri,
    string? ExecutablePath,
    DateTimeOffset LastPlayedUtc,
    bool ResumeAfterSleep);

public sealed record GameProfile(
    string GameId,
    bool HdrPreferred,
    int FrameCap,
    bool ShaderPrecache,
    string Notes);

public sealed record CustomLibraryEntry(
    string Id,
    string DisplayName,
    string? ExecutablePath,
    string? LaunchUri);
