using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NLogViewer.ClientApplication.Models;

namespace NLogViewer.ClientApplication.Services
{
    /// <summary>
    /// Service for loading and saving application configuration
    /// </summary>
    public class ConfigurationService
    {
        private readonly string _configPath;

        public ConfigurationService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NLogViewer.ClientApplication");
            Directory.CreateDirectory(appDataPath);
            _configPath = Path.Combine(appDataPath, "appsettings.json");
        }

        public AppConfiguration LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<AppConfiguration>(json);
                    if (config != null)
                    {
                        // Ensure Language is set, default to empty for auto-detection
                        if (string.IsNullOrEmpty(config.Language))
                        {
                            config.Language = string.Empty;
                        }
                        return config;
                    }
                }
            }
            catch (Exception)
            {
                // Fall back to default configuration
            }

            // Return default configuration with empty language for auto-detection
            return new AppConfiguration
            {
                Ports = new List<string> { "udp://0.0.0.0:4000" },
                Language = string.Empty, // Empty means auto-detect on first start
                MaxLogEntriesPerTab = 10000,
                AutoStartListening = false
            };
        }

        public void SaveConfiguration(AppConfiguration config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception)
            {
                // Log error but don't throw
            }
        }
    }

    /// <summary>
    /// Application configuration model
    /// </summary>
    public class AppConfiguration
    {
        public List<string> Ports { get; set; } = new();
        public string Language { get; set; } = string.Empty; // Empty means auto-detect
        public int MaxLogEntriesPerTab { get; set; } = 10000;
        public bool AutoStartListening { get; set; } = false;
    }
}

