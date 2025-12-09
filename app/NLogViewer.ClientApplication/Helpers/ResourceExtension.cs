using System;
using System.Windows.Markup;
using NLogViewer.ClientApplication.Services;

namespace NLogViewer.ClientApplication.Helpers
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

            return LocalizationService.Instance.GetString(Key, DefaultValue);
        }
    }
}

