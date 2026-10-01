using System.Text.Json;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Library;

public sealed class JsonCustomLibraryStore : ICustomLibraryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _path;

    public JsonCustomLibraryStore(string? path = null)
    {
        _path = path ?? Path.Combine(UnboundPaths.Root, "custom-library.json");
    }

    public Task<IReadOnlyList<CustomLibraryEntry>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(_path))
        {
            return Task.FromResult<IReadOnlyList<CustomLibraryEntry>>([]);
        }

        try
        {
            var items = JsonSerializer.Deserialize<List<CustomLibraryEntry>>(File.ReadAllText(_path), JsonOptions);
            return Task.FromResult<IReadOnlyList<CustomLibraryEntry>>(items ?? []);
        }
        catch (Exception)
        {
            return Task.FromResult<IReadOnlyList<CustomLibraryEntry>>([]);
        }
    }

    public async Task AddAsync(CustomLibraryEntry entry, CancellationToken cancellationToken = default)
    {
        var items = (await ListAsync(cancellationToken).ConfigureAwait(false)).ToList();
        items.RemoveAll(item => string.Equals(item.Id, entry.Id, StringComparison.OrdinalIgnoreCase));
        items.Add(entry);
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(items, JsonOptions));
    }
}
