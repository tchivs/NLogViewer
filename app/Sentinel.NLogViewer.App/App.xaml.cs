using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentinel.NLogViewer.App.Services;
using Sentinel.NLogViewer.App.ViewModels;

namespace Sentinel.NLogViewer.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;

        public App()
        {
            // Build the host with dependency injection
            var hostBuilder = Host.CreateApplicationBuilder();
            
            // Register services
            ConfigureServices(hostBuilder.Services);
            
            _host = hostBuilder.Build();
            
            // Initialize localization service BEFORE XAML is loaded
            // This ensures the correct culture is set for resource loading
            var localizationService = _host.Services.GetRequiredService<LocalizationService>();
            localizationService.Initialize();
        }

        /// <summary>
        /// Configures the dependency injection container
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // Register services as singletons
            services.AddSingleton<ConfigurationService>();
            services.AddSingleton<LocalizationService>();
            services.AddSingleton<TextFileFormatDetector>();
            services.AddSingleton<TextFileFormatConfigService>();
            
            // Register services as scoped (one per window/view)
            services.AddScoped<UdpLogReceiverService>();
            services.AddScoped<LogFileParserService>();
            
            // Register parsers as transient (new instance each time)
            services.AddTransient<Parsers.Log4JEventParser>();
            services.AddTransient<Parsers.PlainTextParser>();
            services.AddTransient<Parsers.JsonLogParser>();
            
            // Register ViewModels as scoped
            services.AddScoped<MainViewModel>();
            services.AddScoped<SettingsViewModel>();
            services.AddScoped<LanguageSelectionViewModel>();
            
            // Register Windows as transient (new window each time)
            services.AddTransient<MainWindow>();
            services.AddTransient<SettingsWindow>();
            services.AddTransient<LanguageSelectionWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Create and show the main window using DI
            // Create a scope for the main window and its dependencies
            if (_host != null)
            {
                var scope = _host.Services.CreateScope();
                var mainWindow = scope.ServiceProvider.GetRequiredService<MainWindow>();
                MainWindow = mainWindow;
                
                // Store the scope so it's disposed when the window closes
                mainWindow.Closed += (s, args) => scope.Dispose();
                
                mainWindow.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Dispose the host and all registered services
            _host?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Gets the service provider for dependency injection
        /// </summary>
        public static IServiceProvider ServiceProvider => ((App)Current)._host?.Services;
    }
}