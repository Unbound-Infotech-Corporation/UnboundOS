using Microsoft.Win32;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Models;

namespace UnboundOS.Infrastructure.Startup;

/// <summary>
/// Read-only Windows startup inventory. Missing hives/folders are skipped.
/// Tests inject a fake inventory instead of this type.
/// </summary>
public sealed class WindowsStartupInventory : IStartupInventory
{
    public IReadOnlyList<StartupCandidate> List()
    {
        var items = new List<StartupCandidate>();
        AddRunKey(items, Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", StartupSource.RunKeyUser);
        AddRunKey(items, Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", StartupSource.RunKeyMachine);
        AddStartupFolder(items);
        AddScheduledTasks(items);
        return items;
    }

    private static void AddRunKey(
        List<StartupCandidate> items,
        RegistryKey hive,
        string subKey,
        StartupSource source)
    {
        try
        {
            using var key = hive.OpenSubKey(subKey);
            if (key is null)
            {
                return;
            }

            foreach (var name in key.GetValueNames())
            {
                var command = key.GetValue(name)?.ToString() ?? string.Empty;
                items.Add(new StartupCandidate(
                    StartupPolicy.MakeId(source, name, command),
                    source,
                    name,
                    command,
                    subKey));
            }
        }
        catch
        {
            // Hive missing (Linux CI) or access denied.
        }
    }

    private static void AddStartupFolder(List<StartupCandidate> items)
    {
        var folders = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
        };

        foreach (var folder in folders.Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path)))
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(folder))
                {
                    var name = Path.GetFileName(file);
                    items.Add(new StartupCandidate(
                        StartupPolicy.MakeId(StartupSource.StartupFolder, name, file),
                        StartupSource.StartupFolder,
                        name,
                        file,
                        folder));
                }
            }
            catch
            {
                // Ignore unreadable folders.
            }
        }
    }

    private static void AddScheduledTasks(List<StartupCandidate> items)
    {
        try
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                "System32",
                "Tasks");
            if (!Directory.Exists(root))
            {
                return;
            }

            foreach (var file in EnumerateTaskFiles(root).Take(64))
            {
                var name = Path.GetRelativePath(root, file);
                items.Add(new StartupCandidate(
                    StartupPolicy.MakeId(StartupSource.ScheduledTask, name, file),
                    StartupSource.ScheduledTask,
                    name,
                    file,
                    "Scheduled Tasks"));
            }
        }
        catch
        {
            // Tasks folder is often ACL'd; report-only when readable.
        }
    }

    private static IEnumerable<string> EnumerateTaskFiles(string root)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(root);
        }
        catch
        {
            files = [];
        }

        foreach (var file in files)
        {
            yield return file;
        }

        IEnumerable<string> dirs;
        try
        {
            dirs = Directory.EnumerateDirectories(root);
        }
        catch
        {
            yield break;
        }

        foreach (var dir in dirs)
        {
            IEnumerable<string> nested;
            try
            {
                nested = Directory.EnumerateFiles(dir);
            }
            catch
            {
                continue;
            }

            foreach (var file in nested)
            {
                yield return file;
            }
        }
    }
}
