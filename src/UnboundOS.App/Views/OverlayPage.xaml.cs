using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;

namespace UnboundOS.App.Views;

public sealed partial class OverlayPage : Page
{
    public OverlayViewModel ViewModel { get; } = AppServices.Get<OverlayViewModel>();

    public OverlayPage()
    {
        InitializeComponent();
        EmptyWidgets.Visibility = ViewModel.Widgets.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }
}
