using UnboundOS.Core.Models;

namespace UnboundOS.Core.Abstractions;

public interface IShellSettingsStore
{
    Task<ShellSettings> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(ShellSettings settings, CancellationToken ct = default);
}
