using System.Windows;
using Sentinel.NLogViewer.App.ViewModels;

namespace Sentinel.NLogViewer.App;

/// <summary>
/// Debug-only window for configuring and controlling the test log generator.
/// </summary>
public partial class TestLoggingWindow : Window
{
    public TestLoggingWindow(TestLoggingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new System.ArgumentNullException(nameof(viewModel));
    }
}
