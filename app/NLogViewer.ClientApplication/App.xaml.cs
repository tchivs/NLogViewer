using System.Globalization;
using System.Threading;
using System.Windows;

namespace NLogViewer.ClientApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // Initialize localization service BEFORE XAML is loaded
            // This ensures the correct culture is set for resource loading
            Services.LocalizationService.Instance.Initialize();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
        }
    }
}

