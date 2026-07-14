using System.Windows;
using AnayPublisherStudio.Presentation.ViewModels;

namespace AnayPublisherStudio.Presentation;

/// <summary>Application shell window. The DataContext is the injected MainViewModel.</summary>
public partial class MainWindow : Window
{
    /// <summary>Creates the shell and binds it to the main view-model.</summary>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
