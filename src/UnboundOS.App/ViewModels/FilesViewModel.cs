using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.App.ViewModels;

public partial class FilesViewModel(IFileBrowser browser) : ObservableObject
{
    public ObservableCollection<FilePlace> Places { get; } = [];
    public ObservableCollection<FileBrowseEntry> Entries { get; } = [];

    [ObservableProperty] private string _currentPath = string.Empty;
    [ObservableProperty] private string _parentPath = string.Empty;
    [ObservableProperty] private string _status = OsProductCopy.FilesHonesty;
    [ObservableProperty] private FileBrowseEntry? _selectedEntry;
    [ObservableProperty] private string _clipboardPath = "";

    public string Honesty => OsProductCopy.FilesHonesty;

    public async Task InitializeAsync()
    {
        await Task.Yield();
        Open(browser.OpenPlaces());
    }

    [RelayCommand]
    private void OpenPlace(FilePlace? place)
    {
        if (place is null)
        {
            return;
        }

        TryOpen(place.Path);
    }

    [RelayCommand]
    private void OpenSelected()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        if (SelectedEntry.IsDirectory)
        {
            TryOpen(SelectedEntry.FullPath);
            return;
        }

        Reveal(SelectedEntry.FullPath);
    }

    [RelayCommand]
    private void GoParent()
    {
        if (!string.IsNullOrWhiteSpace(ParentPath))
        {
            TryOpen(ParentPath);
        }
    }

    [RelayCommand]
    private void RevealInExplorer()
    {
        var path = SelectedEntry?.FullPath ?? CurrentPath;
        Reveal(path);
    }

    [RelayCommand]
    private void CopySelected()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        ClipboardPath = SelectedEntry.FullPath;
        Status = $"Ready to paste {SelectedEntry.Name} into another folder.";
    }

    [RelayCommand]
    private void PasteHere()
    {
        if (string.IsNullOrWhiteSpace(ClipboardPath))
        {
            Status = "Copy a file or folder first.";
            return;
        }

        Apply(browser.Copy(ClipboardPath, CurrentPath));
        TryOpen(CurrentPath);
    }

    [RelayCommand]
    private void MoveSelected()
    {
        if (string.IsNullOrWhiteSpace(ClipboardPath))
        {
            if (SelectedEntry is null)
            {
                return;
            }

            ClipboardPath = SelectedEntry.FullPath;
        }

        Apply(browser.Move(ClipboardPath, CurrentPath));
        ClipboardPath = "";
        TryOpen(CurrentPath);
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        Apply(browser.Delete(SelectedEntry.FullPath));
        TryOpen(CurrentPath);
    }

    [RelayCommand]
    private void EjectSelected()
    {
        var root = SelectedEntry?.FullPath ?? CurrentPath;
        Apply(browser.Eject(root));
    }

    [RelayCommand]
    private void OpenWithSelected()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        Apply(browser.OpenWith(SelectedEntry.FullPath));
    }

    private void Apply(FileOpResult result) =>
        Status = result.Ok ? result.Message : result.Message;

    private void TryOpen(string path)
    {
        try
        {
            Open(browser.OpenPath(path));
        }
        catch (Exception error)
        {
            Status = error.Message;
        }
    }

    private void Open(FileBrowsePage page)
    {
        CurrentPath = page.CurrentPath;
        ParentPath = page.ParentPath;
        Places.Clear();
        foreach (var place in page.Places)
        {
            Places.Add(place);
        }

        Entries.Clear();
        foreach (var entry in page.Entries)
        {
            Entries.Add(entry);
        }

        Status = $"{page.Entries.Count} items · {OsProductCopy.FilesHonesty}";
    }

    private void Reveal(string path)
    {
        try
        {
            var info = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = Directory.Exists(path) ? path : $"/select,\"{path}\"",
                UseShellExecute = true
            };
            _ = System.Diagnostics.Process.Start(info);
            Status = $"Opened Windows Explorer for compatibility. {OsProductCopy.FilesHonesty}";
        }
        catch (Exception error)
        {
            Status = $"Explorer did not start: {error.Message}";
        }
    }
}
