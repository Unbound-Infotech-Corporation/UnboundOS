using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace UnboundOS.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        RootFrame.Navigate(typeof(MainPage));
    }

    public void TryEnterFullscreen()
    {
        try
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            if (AppTitleBar is not null)
            {
                AppTitleBar.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception)
        {
            try
            {
                if (AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.SetBorderAndTitleBar(false, false);
                    presenter.Maximize();
                }
            }
            catch (Exception)
            {
                // WinUI presenter APIs are Windows-only. Never fail launch.
            }
        }
    }
}
