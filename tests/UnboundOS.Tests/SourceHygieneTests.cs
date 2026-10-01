using System.Text.RegularExpressions;

namespace UnboundOS.Tests;

/// <summary>
/// Linux-safe checks for Windows-only compile footguns (WinUI is not built here).
/// </summary>
public sealed class SourceHygieneTests
{
    private static readonly Regex UsingDiscard = new(
        @"using\s+var\s+_\s*=",
        RegexOptions.CultureInvariant);

    [Fact]
    public void Sources_DoNotDeclareUsingVarDiscard()
    {
        var hits = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (UsingDiscard.IsMatch(lines[i]))
                {
                    hits.Add($"{Relative(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.True(
            hits.Count == 0,
            "using var _ collides with discard assignments (_ = ...) in the same scope (CS1656). Name the using variable. "
            + Environment.NewLine
            + string.Join(Environment.NewLine, hits));
    }

    [Fact]
    public void InstallScripts_UseProfileEnvironment_NotHardcodedUsers()
    {
        var scripts = Path.Combine(RepoRoot(), "scripts");
        foreach (var name in new[] { "Install-UnboundOS.ps1", "Uninstall-UnboundOS.ps1", "Publish-UnboundOS.ps1", "Restore-ExplorerShell.ps1" })
        {
            var text = File.ReadAllText(Path.Combine(scripts, name));
            Assert.DoesNotContain("akind", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"C:\Users\", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"HKLM:\Software\Microsoft\Windows NT\CurrentVersion\Winlogon", text, StringComparison.OrdinalIgnoreCase);
        }

        var install = File.ReadAllText(Path.Combine(scripts, "Install-UnboundOS.ps1"));
        Assert.Contains("$env:LOCALAPPDATA", install, StringComparison.Ordinal);
        Assert.Contains("$env:APPDATA", install, StringComparison.Ordinal);
        Assert.Contains("Unbound Infotech Corporation", install, StringComparison.Ordinal);
        Assert.Contains("-XboxModeHome", install, StringComparison.Ordinal);
        Assert.Contains("-ReplaceShell", install, StringComparison.Ordinal);
        Assert.Contains("-RestoreExplorer", install, StringComparison.Ordinal);
        Assert.Contains("windows.gamingApp", File.ReadAllText(Path.Combine(RepoRoot(), "packaging", "fse", "Package.appxmanifest")), StringComparison.Ordinal);
        Assert.Contains("Microsoft.appCategory.gamingHome_8wekyb3d8bbwe", File.ReadAllText(Path.Combine(RepoRoot(), "packaging", "fse", "CustomCapability.SCCD")), StringComparison.Ordinal);
        Assert.DoesNotContain("XFSET", install, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HKCU:\\Software\\Microsoft\\Windows NT\\CurrentVersion\\Winlogon", install, StringComparison.Ordinal);

        var publish = File.ReadAllText(Path.Combine(scripts, "Publish-UnboundOS.ps1"));
        Assert.Contains("PublishTrimmed=false", publish, StringComparison.Ordinal);
        Assert.Contains("PublishReadyToRun=true", publish, StringComparison.Ordinal);
    }

    [Fact]
    public void UnboundPaths_FollowsLocalAppData_NotAHardcodedProfile()
    {
        var root = UnboundOS.Core.UnboundPaths.Root;
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Assert.StartsWith(local, root, StringComparison.Ordinal);
        Assert.DoesNotContain("akind", root, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unbound Infotech Corporation", root, StringComparison.Ordinal);
        Assert.Contains("UnboundOS", root, StringComparison.Ordinal);
    }

    [Fact]
    public void Sources_DoNotWriteMachineWinlogonShell()
    {
        var hits = new List<string>();
        foreach (var file in Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            if (text.Contains("Registry.LocalMachine", StringComparison.Ordinal) &&
                text.Contains("Winlogon", StringComparison.Ordinal) &&
                text.Contains("SetValue", StringComparison.Ordinal))
            {
                hits.Add(Relative(file));
            }
        }

        Assert.True(hits.Count == 0, "HKLM Winlogon Shell writes are forbidden. " + string.Join(", ", hits));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "UnboundOS.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("UnboundOS.sln not found above " + AppContext.BaseDirectory);
    }

    private static string Relative(string path) =>
        Path.GetRelativePath(RepoRoot(), path).Replace('\\', '/');
}
