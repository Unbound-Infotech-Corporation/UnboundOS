using UnboundOS.Core.Models;

namespace UnboundOS.Core.Shell;

/// <summary>Controller-first file cursor. A opens, B goes up, filter uses the on-screen keyboard.</summary>
public sealed record GamepadFilePicker(
    IReadOnlyList<FileBrowseEntry> Entries,
    int Index,
    string Filter)
{
    public IReadOnlyList<FileBrowseEntry> Visible =>
        string.IsNullOrWhiteSpace(Filter)
            ? Entries
            : Entries.Where(entry => entry.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase)).ToArray();

    public FileBrowseEntry? Selected =>
        Visible.Count == 0 ? null : Visible[Math.Clamp(Index, 0, Visible.Count - 1)];

    public GamepadFilePicker Move(int delta)
    {
        if (Visible.Count == 0)
        {
            return this with { Index = 0 };
        }

        var next = ((Index + delta) % Visible.Count + Visible.Count) % Visible.Count;
        return this with { Index = next };
    }

    public GamepadFilePicker WithFilter(string filter)
    {
        var next = this with { Filter = filter ?? "", Index = 0 };
        return next;
    }

    public GamepadFilePicker Type(OnScreenKey key) =>
        WithFilter(OnScreenKeyboardLayout.Apply(Filter, key));
}
