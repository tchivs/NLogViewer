using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using NLog;
using NLog.Config;
using NLogViewer.ClientApplication.Models;
using NLogViewer.ClientApplication.Services;
using DJ.Targets;

namespace NLogViewer.ClientApplication.ViewModels
{
    /// <summary>
    /// Main ViewModel for the application
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly UdpLogReceiverService _udpReceiverService;
        private readonly LogFileParserService _fileParserService;
        private readonly ConfigurationService _configService;
        private bool _isListening;
        private string _listeningStatus = "Stopped";
        private string _statusMessage = "Ready";
        private string _lastLogTimestamp = string.Empty;
        private LogTabViewModel? _selectedTab;

        public MainViewModel()
        {
            _configService = new ConfigurationService();
            _udpReceiverService = new UdpLogReceiverService();
            _fileParserService = new LogFileParserService();

            LogTabs = new ObservableCollection<LogTabViewModel>();

            // Initialize commands
            StartListeningCommand = new RelayCommand(StartListening, () => !_isListening);
            StopListeningCommand = new RelayCommand(StopListening, () => _isListening);
            OpenFileCommand = new RelayCommand(OpenFile);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ExitCommand = new RelayCommand(() => System.Windows.Application.Current.Shutdown());
            AboutCommand = new RelayCommand(ShowAbout);

            // Subscribe to log events
            _udpReceiverService.LogReceived += OnLogReceived;
            _fileParserService.LogParsed += OnLogReceived;

            // Load configuration
            LoadConfiguration();
        }

        public ObservableCollection<LogTabViewModel> LogTabs { get; }

        public LogTabViewModel? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab != value)
                {
                    _selectedTab = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsListening
        {
            get => _isListening;
            private set
            {
                if (_isListening != value)
                {
                    _isListening = value;
                    OnPropertyChanged();
                    ((RelayCommand)StartListeningCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)StopListeningCommand).RaiseCanExecuteChanged();
                }
            }
        }

        public string ListeningStatus
        {
            get => _listeningStatus;
            set
            {
                if (_listeningStatus != value)
                {
                    _listeningStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LastLogTimestamp
        {
            get => _lastLogTimestamp;
            set
            {
                if (_lastLogTimestamp != value)
                {
                    _lastLogTimestamp = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand StartListeningCommand { get; }
        public ICommand StopListeningCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand AboutCommand { get; }

        private void LoadConfiguration()
        {
            var config = _configService.LoadConfiguration();
            
            if (config.AutoStartListening)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(500); // Small delay to ensure UI is ready
                    System.Windows.Application.Current.Dispatcher.Invoke(StartListening);
                });
            }
        }

        private void StartListening()
        {
            try
            {
                var config = _configService.LoadConfiguration();
                _udpReceiverService.StartListening(config.Ports);
                IsListening = true;
                ListeningStatus = "Listening";
                StatusMessage = $"Listening on {config.Ports.Count} port(s)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error starting listener: {ex.Message}";
                ListeningStatus = "Error";
            }
        }

        private void StopListening()
        {
            try
            {
                _udpReceiverService.StopListening();
                IsListening = false;
                ListeningStatus = "Stopped";
                StatusMessage = "Stopped listening";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error stopping listener: {ex.Message}";
            }
        }

        private void OpenFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Log Files (*.xml;*.log;*.txt;*.json)|*.xml;*.log;*.txt;*.json|XML Files (*.xml)|*.xml|Text Files (*.txt;*.log)|*.txt;*.log|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                Title = "Open Log File"
            };

            if (dialog.ShowDialog() == true)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _fileParserService.ParseFileAsync(dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            StatusMessage = $"Error parsing file: {ex.Message}";
                        });
                    }
                });
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow();
            var result = settingsWindow.ShowDialog(System.Windows.Application.Current.MainWindow);
            if (result == true)
            {
                StatusMessage = "Settings saved";
                // Reload configuration if needed
                LoadConfiguration();
            }
        }

        private void ShowAbout()
        {
            // TODO: Show about dialog
            StatusMessage = "About dialog not yet implemented";
        }

        private void OnLogReceived(object? sender, LogReceivedEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var appInfo = new AppInfo
                {
                    Name = e.AppInfo ?? "Unknown",
                    Sender = e.Sender ?? "Local"
                };

                // Find or create tab for this AppInfo
                var tab = LogTabs.FirstOrDefault(t => t.TargetName == appInfo.Id);
                if (tab == null)
                {
                    tab = new LogTabViewModel
                    {
                        Header = appInfo.ToString(),
                        TargetName = appInfo.Id,
                        MaxCount = _configService.LoadConfiguration().MaxLogEntriesPerTab
                    };
                    LogTabs.Add(tab);
                    SelectedTab = tab;

                    // Create CacheTarget for this tab
                    CreateCacheTargetForTab(tab);
                }

                // Write log event to the CacheTarget
                WriteLogToCacheTarget(tab.TargetName, e.LogEvent);

                // Update log count
                tab.LogCount++;
                LastLogTimestamp = DateTime.Now.ToString("HH:mm:ss");
                StatusMessage = $"Received log from {appInfo.Name}";
            });
        }

        private void CreateCacheTargetForTab(LogTabViewModel tab)
        {
            try
            {
                var config = LogManager.Configuration ?? new LoggingConfiguration();
                var target = new CacheTarget
                {
                    Name = tab.TargetName,
                    MaxCount = tab.MaxCount
                };

                config.AddTarget(tab.TargetName, target);
                config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, target));
                LogManager.Configuration = config;
                LogManager.ReconfigExistingLoggers();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating CacheTarget: {ex.Message}");
            }
        }

        private void WriteLogToCacheTarget(string targetName, LogEventInfo logEvent)
        {
            try
            {
                // Get or create the CacheTarget
                var target = CacheTarget.GetInstance(targetName: targetName);
                
                // Use NLog's public API to write the log event
                // Create a logger that writes to this specific target
                var logger = LogManager.GetLogger(targetName);
                logger.Log(logEvent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to CacheTarget: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            StopListening();
            _udpReceiverService.LogReceived -= OnLogReceived;
            _fileParserService.LogParsed -= OnLogReceived;
            _udpReceiverService?.Dispose();
            _fileParserService?.Dispose();
        }
    }

    /// <summary>
    /// Simple RelayCommand implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

