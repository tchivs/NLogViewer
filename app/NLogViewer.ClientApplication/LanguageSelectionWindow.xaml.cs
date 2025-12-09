using System.Windows;
using NLogViewer.ClientApplication.ViewModels;

namespace NLogViewer.ClientApplication
{
    /// <summary>
    /// Interaction logic for LanguageSelectionWindow.xaml
    /// </summary>
    public partial class LanguageSelectionWindow : Window
    {
        private readonly LanguageSelectionViewModel _viewModel;

        public LanguageSelectionWindow()
        {
            InitializeComponent();
            _viewModel = new LanguageSelectionViewModel();
            DataContext = _viewModel;
        }

        /// <summary>
        /// Gets the selected language code
        /// </summary>
        public string? SelectedLanguageCode => _viewModel.SelectedLanguage?.Code;

        /// <summary>
        /// Shows the dialog with the owner window
        /// </summary>
        public bool? ShowDialog(Window owner)
        {
            Owner = owner;
            return ShowDialog();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

