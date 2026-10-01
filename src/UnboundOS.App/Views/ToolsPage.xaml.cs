using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;
using UnboundOS.Core.Models;

namespace UnboundOS.App.Views;

public sealed partial class ToolsPage : Page
{
    public ToolsViewModel ViewModel { get; } = AppServices.Get<ToolsViewModel>();

    public ToolsPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
    }

    private void Kit_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DesktopTool tool)
        {
            ViewModel.SelectToolCommand.Execute(tool);
        }
    }

    private void Utility_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is DesktopTool tool)
        {
            ViewModel.SelectToolCommand.Execute(tool);
        }
    }
}
