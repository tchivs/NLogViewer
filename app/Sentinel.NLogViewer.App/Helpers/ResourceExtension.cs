using System;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Markup;
using Sentinel.NLogViewer.App.Services;

namespace Sentinel.NLogViewer.App.Helpers
{
    /// <summary>
    /// Markup extension for accessing localized resources in XAML
    /// </summary>
    public class ResourceExtension : MarkupExtension
    {
        /// <summary>
        /// The resource key to look up
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Default value if resource is not found
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        public ResourceExtension()
        {
        }

        public ResourceExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (string.IsNullOrEmpty(Key))
                return DefaultValue;

            // Get LocalizationService from DI container
            var localizationService = App.ServiceProvider?.GetService<LocalizationService>();
            
            if (localizationService != null)
            {
                return localizationService.GetString(Key, DefaultValue);
            }
            
            // Fallback if DI is not available
            return DefaultValue;
        }
    }
}

