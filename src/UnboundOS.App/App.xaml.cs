using Microsoft.UI.Xaml;
using UnboundOS.App.Services;
using UnboundOS.Core;
using UnboundOS.Core.Abstractions;
using UnboundOS.Core.Overlay;
using UnboundOS.Infrastructure.Startup;

namespace UnboundOS.App;

public partial class App : Application
{
    public static Window Window { get; private set; } = null!;
    public static Microsoft.UI.Dispatching.DispatcherQueue DispatcherQueue { get; private set; } = null!;

    public static nint WindowHandle =>
        WinRT.Interop.WindowNative.GetWindowHandle(Window);

    public App()
    {
        InitializeComponent();
        AppServices.Initialize();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        if (StartupAuditCommand.IsRequested(Environment.GetCommandLineArgs()))
        {
            try
            {
                await AppServices.Get<IStartupAuditService>().AuditAsync();
            }
            catch
            {
                // Headless scheduled-task path must not throw into the shell.
            }

            Exit();
            return;
        }

        Window = new MainWindow();
        DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
        Window.Title = $"{Branding.ProductName} — {Branding.CompanyName}";

        var overlay = AppServices.Get<IDesktopOverlayHost>();
        if (overlay.IsEnabled)
        {
            _ = overlay.StartAsync();
        }

        Window.Closed += (_, _) =>
        {
            if (!overlay.IsEnabled)
            {
                return;
            }

            _ = overlay.StopAsync();
        };

        Window.Activate();
    }
}
