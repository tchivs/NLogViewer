using System.Windows;
using System.Windows.Input;
using NLogViewer.ClientApplication.ViewModels;

namespace NLogViewer.ClientApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            
            // Setup keyboard shortcuts
            this.InputBindings.Add(new KeyBinding(_viewModel.OpenFileCommand, 
                new KeyGesture(Key.O, ModifierKeys.Control)));
            this.InputBindings.Add(new KeyBinding(_viewModel.OpenSettingsCommand, 
                new KeyGesture(Key.OemComma, ModifierKeys.Control)));
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}

