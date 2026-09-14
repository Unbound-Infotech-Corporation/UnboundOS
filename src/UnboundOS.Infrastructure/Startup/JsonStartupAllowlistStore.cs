using System.Text.Json;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Startup;

public sealed class JsonStartupAllowlistStore : IStartupAllowlistStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;

    public JsonStartupAllowlistStore(string? rootDirectory = null)
    {
        var root = rootDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unbound Infotech Corporation",
            "UnboundOS");
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "startup-allowlist.json");
    }

    public string FilePath => _path;

    public async Task<StartupAllowlist> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
        {
            return StartupAllowlist.Empty;
        }

        await using var stream = File.OpenRead(_path);
        var loaded = await JsonSerializer.DeserializeAsync<StartupAllowlist>(stream, JsonOptions, ct)
            .ConfigureAwait(false);
        return loaded ?? StartupAllowlist.Empty;
    }

    public async Task SaveAsync(StartupAllowlist allowlist, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(allowlist);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, allowlist, JsonOptions, ct).ConfigureAwait(false);
    }
}
