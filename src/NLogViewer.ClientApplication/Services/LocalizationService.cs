using System;
using System.Globalization;
using System.Resources;
using System.Threading;
using System.Windows;

namespace NLogViewer.ClientApplication.Services
{
    /// <summary>
    /// Service for managing application localization
    /// </summary>
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        private ResourceManager? _resourceManager;
        private CultureInfo _currentCulture;

        public static LocalizationService Instance => _instance ??= new LocalizationService();

        private LocalizationService()
        {
            _currentCulture = CultureInfo.CurrentCulture;
        }

        public void Initialize()
        {
            try
            {
                // Try to load resources
                _resourceManager = new ResourceManager(
                    "NLogViewer.ClientApplication.Resources.Resources",
                    typeof(LocalizationService).Assembly);

                // Set culture based on configuration or system default
                var configService = new ConfigurationService();
                var config = configService.LoadConfiguration();
                
                if (!string.IsNullOrEmpty(config.Language))
                {
                    SetLanguage(config.Language);
                }
            }
            catch (Exception)
            {
                // If resources are not available, continue with default culture
            }
        }

        public void SetLanguage(string languageCode)
        {
            try
            {
                _currentCulture = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentCulture = _currentCulture;
                Thread.CurrentThread.CurrentUICulture = _currentCulture;
                CultureInfo.DefaultThreadCurrentCulture = _currentCulture;
                CultureInfo.DefaultThreadCurrentUICulture = _currentCulture;
            }
            catch
            {
                // Invalid language code, use default
                _currentCulture = CultureInfo.InvariantCulture;
            }
        }

        public string GetString(string key, string defaultValue = "")
        {
            try
            {
                return _resourceManager?.GetString(key, _currentCulture) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        public CultureInfo CurrentCulture => _currentCulture;
    }
}

