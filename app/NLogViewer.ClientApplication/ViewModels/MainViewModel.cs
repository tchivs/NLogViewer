using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DJ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NLog;
using NLog.Config;
using NLogViewer.ClientApplication.Models;
using NLogViewer.ClientApplication.Services;
using DJ.Targets;

namespace NLogViewer.ClientApplication.ViewModels;

/// <summary>
/// Main ViewModel for the application
/// </summary>
public class MainViewModel : INotifyPropertyChanged, IDisposable
{
	private readonly UdpLogReceiverService _udpReceiverService;
	private readonly LogFileParserService _fileParserService;
	private readonly ConfigurationService _configService;
	private readonly LocalizationService _localizationService;
	private bool _isListening;
	private string _listeningStatus;
	private string _statusMessage = "Ready";
	private string _lastLogTimestamp = string.Empty;
	private LogTabViewModel? _selectedTab;
	private string _currentLanguageFlag = "🇬🇧";
	private bool _isLoading;
	private string _loadingProgress = string.Empty;

	private readonly CompositeDisposable _subscriptions = new();


	public MainViewModel(
		UdpLogReceiverService udpReceiverService,
		LogFileParserService fileParserService,
		ConfigurationService configService,
		LocalizationService localizationService)
	{
		_udpReceiverService = udpReceiverService ?? throw new ArgumentNullException(nameof(udpReceiverService));
		_fileParserService = fileParserService ?? throw new ArgumentNullException(nameof(fileParserService));
		_configService = configService ?? throw new ArgumentNullException(nameof(configService));
		_localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

		LogTabs = new ObservableCollection<LogTabViewModel>();

		// Initialize commands
		StartListeningCommand = new RelayCommand(StartListening, () => !_isListening);
		StopListeningCommand = new RelayCommand(StopListening, () => _isListening);
		OpenFileCommand = new RelayCommand(OpenFile);
		OpenSettingsCommand = new RelayCommand(OpenSettings);
		ExitCommand = new RelayCommand(() => System.Windows.Application.Current.Shutdown());
		AboutCommand = new RelayCommand(ShowAbout);
		ChangeLanguageCommand = new RelayCommand<string>(ChangeLanguage);

		// Initialize current language flag
		UpdateLanguageFlag();

		// Initialize listening status with translated value
		ListeningStatus = _localizationService.GetString("Status_Stopped", "Stopped");

		// Subscribe to log events
		_udpReceiverService.Log4JEventObservable.Subscribe(OnLogEvent);
		//_fileParserService.LogParsed += OnLogReceived;

		// Load configuration
		LoadConfiguration();
	}

	private void OnLogEvent(LogEvent logEvent)
	{
		// Write log event to the CacheTarget
		WriteLogToCacheTarget(logEvent.AppInfo.Id, logEvent.LogEventInfo);

		// check if we need a new tab

		System.Windows.Application.Current.Dispatcher.Invoke(() =>
		{
			// Find or create tab for this AppInfo
			var tab = LogTabs.FirstOrDefault(t => t.TargetName == logEvent.AppInfo.Id);
			if (tab == null)
			{
				tab = new LogTabViewModel
				{
					Header = logEvent.AppInfo.ToString(),
					TargetName = logEvent.AppInfo.Id,
					MaxCount = _configService.LoadConfiguration().MaxLogEntriesPerTab
				};
				LogTabs.Add(tab);
				SelectedTab = tab;

				// Create CacheTarget for this tab
				CreateCacheTargetForTab(tab);
			}
			
			// Update log count
			tab.LogCount++;
			LastLogTimestamp = DateTime.Now.ToString("HH:mm:ss");
			StatusMessage = $"Received log from {logEvent.AppInfo.AppName.Name}";
		});
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
	public ICommand ChangeLanguageCommand { get; }

	/// <summary>
	/// Gets the flag emoji for the current language
	/// </summary>
	public string CurrentLanguageFlag
	{
		get => _currentLanguageFlag;
		private set
		{
			if (_currentLanguageFlag != value)
			{
				_currentLanguageFlag = value;
				OnPropertyChanged();
			}
		}
	}

	/// <summary>
	/// Gets the list of available languages with flags
	/// </summary>
	public Dictionary<string, string> AvailableLanguages => Services.LocalizationService.AvailableLanguages;

	/// <summary>
	/// Gets or sets whether the application is currently loading a file
	/// </summary>
	public bool IsLoading
	{
		get => _isLoading;
		private set
		{
			if (_isLoading != value)
			{
				_isLoading = value;
				OnPropertyChanged();
			}
		}
	}

	/// <summary>
	/// Gets or sets the loading progress message
	/// </summary>
	public string LoadingProgress
	{
		get => _loadingProgress;
		private set
		{
			if (_loadingProgress != value)
			{
				_loadingProgress = value;
				OnPropertyChanged();
			}
		}
	}

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
			ListeningStatus = _localizationService.GetString("Status_Listening", "Listening");
			StatusMessage = _localizationService.GetString("Status_ListeningOnPorts", $"Listening on {config.Ports.Count} port(s)");
		}
		catch (Exception ex)
		{
			StatusMessage = _localizationService.GetString("Error_StartingListener", $"Error starting listener: {ex.Message}");
			ListeningStatus = _localizationService.GetString("Status_Error", "Error");
		}
	}

	private void StopListening()
	{
		try
		{
			_udpReceiverService.StopListening();
			IsListening = false;
			ListeningStatus = _localizationService.GetString("Status_Stopped", "Stopped");
			StatusMessage = _localizationService.GetString("Status_StoppedListening", "Stopped listening");
		}
		catch (Exception ex)
		{
			StatusMessage = _localizationService.GetString("Error_StoppingListener", $"Error stopping listener: {ex.Message}");
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
			IsLoading = true;
			LoadingProgress = "Starting...";
			StatusMessage = "Parsing file...";
			
			Task.Run(async () =>
			{
				try
				{
					var progress = new Progress<(int current, int total)>(p =>
					{
						var percentage = p.total > 0 ? (p.current * 100 / p.total) : 0;
						System.Windows.Application.Current.Dispatcher.Invoke(() =>
						{
							LoadingProgress = $"Processing: {p.current} / {p.total} ({percentage}%)";
						});
					});
					
					var logEvents = await _fileParserService.ParseFileAsync(dialog.FileName, progress);
					
					// Batch-add logs to CacheTarget for better performance
					System.Windows.Application.Current.Dispatcher.Invoke(() =>
					{
						ProcessParsedLogs(logEvents, dialog.FileName);
						IsLoading = false;
						LoadingProgress = string.Empty;
						StatusMessage = $"Loaded {logEvents.Count} log entries from {System.IO.Path.GetFileName(dialog.FileName)}";
					});
				}
				catch (Exception ex)
				{
					System.Windows.Application.Current.Dispatcher.Invoke(() =>
					{
						IsLoading = false;
						LoadingProgress = string.Empty;
						StatusMessage = $"Error parsing file: {ex.Message}";
					});
				}
			});
		}
	}

	/// <summary>
	/// Processes parsed log events and adds them to the appropriate tab
	/// </summary>
	private void ProcessParsedLogs(List<LogEventInfo> logEvents, string fileName)
	{
		if (logEvents.Count == 0)
			return;
		
		// Find or create tab for this file
		var tab = LogTabs.FirstOrDefault(t => t.TargetName == $"File_{System.IO.Path.GetFileName(fileName)}");
		if (tab == null)
		{
			tab = new LogTabViewModel
			{
				Header = System.IO.Path.GetFileName(fileName),
				TargetName = $"File_{System.IO.Path.GetFileName(fileName)}",
				MaxCount = int.MaxValue
			};
			LogTabs.Add(tab);
			SelectedTab = tab;
			
			// Create CacheTarget for this tab
			CreateCacheTargetForTab(tab);
		}
		
		// Batch-write logs to CacheTarget for better performance
		var target = CacheTarget.GetInstance(defaultMaxCount: int.MaxValue, targetName: tab.TargetName);
		var logger = LogManager.GetLogger(tab.TargetName);

		const int batchSize = 500; // Write in batches to avoid UI freezing
		for (int i = 0; i < logEvents.Count; i += batchSize)
		{
			var batch = logEvents.Skip(i).Take(batchSize).ToList();
			
			// Write batch synchronously on UI thread to maintain order
			foreach (var logEvent in batch)
			{
				logger.Log(logEvent);
			}
			
			// Update progress during batch writing
			if (i + batchSize < logEvents.Count)
			{
				var progress = (i + batchSize) * 100 / logEvents.Count;
				LoadingProgress = $"Adding to view: {progress}%";
			}
		}
		
		tab.LogCount = logEvents.Count;
		LastLogTimestamp = DateTime.Now.ToString("HH:mm:ss");
	}

	private void OpenSettings()
	{
		using var scope = App.ServiceProvider.CreateScope();
		var settingsWindow = scope.ServiceProvider.GetRequiredService<SettingsWindow>();
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

	private void ChangeLanguage(string? languageCode)
	{
		if (string.IsNullOrEmpty(languageCode))
			return;

		try
		{
			_localizationService.SetLanguage(languageCode);
			UpdateLanguageFlag();
                
			// Notify that language has changed - this will require app restart for full effect
			StatusMessage = "Language changed. Please restart the application for full effect.";
		}
		catch (Exception ex)
		{
			StatusMessage = $"Error changing language: {ex.Message}";
		}
	}

	private void UpdateLanguageFlag()
	{
		CurrentLanguageFlag = _localizationService.CurrentLanguageFlag;
	}

	private void CreateCacheTargetForTab(LogTabViewModel tab)
	{
		try
		{
			var config = LogManager.Configuration ?? new LoggingConfiguration();
			var target = new CacheTarget(int.MaxValue)
			{
				Name = tab.TargetName,
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
		//_udpReceiverService.LogReceived -= OnLogReceived;
		//_fileParserService.LogParsed -= OnLogReceived;
		_udpReceiverService?.Dispose();
		_fileParserService?.Dispose();
		_subscriptions.Dispose();
	}
}