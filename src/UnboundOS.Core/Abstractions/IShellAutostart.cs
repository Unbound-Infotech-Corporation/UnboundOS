namespace UnboundOS.Core.Abstractions;

/// <summary>
/// Optional HKCU Run autostart for the UnboundOS app. Never sets Shell=
/// and never replaces Explorer.
/// </summary>
public interface IShellAutostart
{
    bool IsEnabled { get; }

    Task<(bool Succeeded, string Message)> SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}
