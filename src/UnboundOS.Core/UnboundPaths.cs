namespace UnboundOS.Core;

/// <summary>LocalAppData root for UnboundOS logs, settings, and health exports.</summary>
public static class UnboundPaths
{
    public static string Root
    {
        get
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Branding.CompanyName,
                Branding.ProductName);
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
