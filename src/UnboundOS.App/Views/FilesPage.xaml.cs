using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;
using UnboundOS.Core.Models;

namespace UnboundOS.App.Views;

public sealed partial class FilesPage : Page
{
    public FilesViewModel ViewModel { get; } = AppServices.Get<FilesViewModel>();

    public FilesPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.InitializeAsync();
    }

    private void Place_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: FilePlace place })
        {
            ViewModel.OpenPlaceCommand.Execute(place);
        }
    }

    private void Entries_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is FileBrowseEntry entry)
        {
            ViewModel.SelectedEntry = entry;
            ViewModel.OpenSelectedCommand.Execute(null);
        }
    }
}
