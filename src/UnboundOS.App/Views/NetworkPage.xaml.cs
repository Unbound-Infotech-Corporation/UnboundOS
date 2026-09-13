using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;

namespace UnboundOS.App.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkViewModel ViewModel { get; } = AppServices.Get<NetworkViewModel>();

    public NetworkPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
    }
}
