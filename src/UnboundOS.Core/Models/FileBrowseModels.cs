namespace UnboundOS.Core.Models;

public sealed record FileBrowseEntry(
    string Name,
    string FullPath,
    bool IsDirectory,
    long? SizeBytes,
    DateTimeOffset LastWriteUtc)
{
    public string KindLabel => IsDirectory ? "FOLDER" : "FILE";

    public string SizeLabel =>
        IsDirectory || SizeBytes is null
            ? "—"
            : SizeBytes.Value switch
            {
                < 1024 => $"{SizeBytes.Value} B",
                < 1024 * 1024 => $"{SizeBytes.Value / 1024.0:0.0} KB",
                < 1024L * 1024 * 1024 => $"{SizeBytes.Value / (1024.0 * 1024):0.0} MB",
                _ => $"{SizeBytes.Value / (1024.0 * 1024 * 1024):0.0} GB"
            };
}

public sealed record FilePlace(
    string Id,
    string Label,
    string Path);

public sealed record FileBrowsePage(
    string CurrentPath,
    string ParentPath,
    IReadOnlyList<FilePlace> Places,
    IReadOnlyList<FileBrowseEntry> Entries);
