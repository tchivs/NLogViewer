using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using NLogViewer.ClientApplication.ViewModels;

namespace NLogViewer.ClientApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            DataContext = _viewModel;
            
            // Setup keyboard shortcuts
            this.InputBindings.Add(new KeyBinding(_viewModel.OpenFileCommand, 
                new KeyGesture(Key.O, ModifierKeys.Control)));
            this.InputBindings.Add(new KeyBinding(_viewModel.OpenSettingsCommand, 
                new KeyGesture(Key.OemComma, ModifierKeys.Control)));
        }

        /// <summary>
        /// Opens the language selection window when the language button is clicked
        /// </summary>
        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
	        using var scope = App.ServiceProvider.CreateScope();
	        var languageWindow = scope.ServiceProvider.GetRequiredService<LanguageSelectionWindow>();
	        var result = languageWindow.ShowDialog(this);

	        if (result == true && !string.IsNullOrEmpty(languageWindow.SelectedLanguageCode))
	        {
		        _viewModel.ChangeLanguageCommand.Execute(languageWindow.SelectedLanguageCode);
	        }
		}

        protected override void OnClosed(System.EventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}

