using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;

namespace UnboundOS.App.Views;

public sealed partial class SwitcherPage : Page
{
    public SwitcherViewModel ViewModel { get; } = AppServices.Get<SwitcherViewModel>();

    public SwitcherPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
    }
}
