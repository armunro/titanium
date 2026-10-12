using System.Windows;
using Titanium.Desktop.ViewModels;

namespace Titanium.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (s, e) => await viewModel.LoadDataAsync();
    }
}
