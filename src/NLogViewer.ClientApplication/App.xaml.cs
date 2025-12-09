using System.Windows;

namespace NLogViewer.ClientApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize localization service
            Services.LocalizationService.Instance.Initialize();
        }
    }
}


