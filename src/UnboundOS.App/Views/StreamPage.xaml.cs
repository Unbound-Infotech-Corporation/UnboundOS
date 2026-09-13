using Microsoft.UI.Xaml.Controls;
using UnboundOS.App.Services;
using UnboundOS.App.ViewModels;

namespace UnboundOS.App.Views;

public sealed partial class StreamPage : Page
{
    public StreamViewModel ViewModel { get; } = AppServices.Get<StreamViewModel>();

    public StreamPage() => InitializeComponent();
}
