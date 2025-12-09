using System.Windows;
using NLogViewer.ClientApplication.ViewModels;

namespace NLogViewer.ClientApplication
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            DataContext = _viewModel;

            _viewModel.CancelCommand = new ViewModels.RelayCommand(() => DialogResult = false);
            _viewModel.SaveCommand = new ViewModels.RelayCommand(() =>
            {
                _viewModel.Save();
                DialogResult = true;
                Close();
            });
        }

        public bool? ShowDialog(Window owner)
        {
            Owner = owner;
            return ShowDialog();
        }
    }
}

