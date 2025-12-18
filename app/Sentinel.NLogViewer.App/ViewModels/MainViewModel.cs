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
using System.Windows.Threading;
using Sentinel.NLogViewer.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using NLog;
using NLog.Config;
using Sentinel.NLogViewer.App.Models;
using Sentinel.NLogViewer.App.Services;
using Sentinel.NLogViewer.App;
using Sentinel.NLogViewer.Wpf.Targets;

namespace Sentinel.NLogViewer.App.ViewModels;

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
		// Buffer events for 250ms, filter empty batches, group by AppInfo.Id, and process each group as a batch
		_udpReceiverService.Log4JEventObservable
			.Buffer(TimeSpan.FromMilliseconds(250))
			.Where(list => list.Count > 0)
			.SelectMany(list => list.GroupBy(le => le.AppInfo.Id).Select(g => g.ToList()))
			.ObserveOn(new DispatcherSynchronizationContext(System.Windows.Application.Current.Dispatcher))
			.Subscribe(OnLogEvent);
		//_fileParserService.LogParsed += OnLogReceived;

		// Load configuration
		LoadConfiguration();
	}

	/// <summary>
	/// Processes a batch of log events grouped by AppInfo.Id
	/// </summary>
	/// <param name="logEvents">List of log events for a specific AppInfo.Id</param>
	private void OnLogEvent(IList<LogEvent> logEvents)
	{
		// We're already on the Dispatcher thread, no need to invoke
		if (logEvents == null || logEvents.Count == 0)
			return;

		// All events in this batch have the same AppInfo.Id (they were grouped)
		var firstEvent = logEvents[0];
		var appInfoId = firstEvent.AppInfo.Id;
		
		// Find or create tab for this AppInfo
		var tab = LogTabs.FirstOrDefault(t => t.TargetName == appInfoId);
		if (tab == null)
		{
			tab = new LogTabViewModel
			{
				Header = firstEvent.AppInfo.ToString(),
				TargetName = appInfoId,
				MaxCount = _configService.LoadConfiguration().MaxLogEntriesPerTab
			};
			LogTabs.Add(tab);
			SelectedTab = tab;
		}
		
		// Add all log events from the batch to the tab
		foreach (var logEvent in logEvents)
		{
			tab.LogEventInfos.Add(logEvent.LogEventInfo);
		}
		
		// Update log count
		tab.LogCount += logEvents.Count;
		LastLogTimestamp = DateTime.Now.ToString("HH:mm:ss");
		StatusMessage = $"Received {logEvents.Count} log(s) from {firstEvent.AppInfo.AppName.Name}";
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
			var filePath = dialog.FileName;
			var extension = Path.GetExtension(filePath).ToLowerInvariant();

			// For text files, check format detection
			if (extension == ".txt" || extension == ".log" || extension == "")
			{
				HandleTextFileWithFormatDetection(filePath);
			}
			else
			{
				// For XML/JSON, parse directly
				ParseFileDirectly(filePath);
			}
		}
	}

	/// <summary>
	/// Handles text file opening with format detection and optional mapping dialog
	/// </summary>
	private void HandleTextFileWithFormatDetection(string filePath)
	{
		Task.Run(() =>
		{
			try
			{
				// Get format configuration
				var format = _fileParserService.GetTextFileFormat(filePath);

				// If format detection failed or auto-mapping is incomplete, show dialog
				if (format == null || !format.ColumnMapping.IsValid())
				{
					// Read sample lines for the dialog
					var sampleLines = File.ReadLines(filePath).Take(20).ToList();

					// If format is null, create a basic one for detection
					if (format == null)
					{
						var detector = App.ServiceProvider?.GetRequiredService<TextFileFormatDetector>();
						format = detector?.DetectFormat(filePath);
					}

					// Show dialog on UI thread
					System.Windows.Application.Current.Dispatcher.Invoke(() =>
					{
						if (format != null)
						{
							var viewModel = new ColumnMappingViewModel(format, sampleLines, filePath);
							var mappingWindow = new ColumnMappingWindow(viewModel);
							var mainWindow = System.Windows.Application.Current.MainWindow;

							if (mappingWindow.ShowDialog(mainWindow) == true)
							{
								var finalFormat = mappingWindow.FinalFormat;

								// Save format if requested
								if (mappingWindow.SaveForPattern && !string.IsNullOrEmpty(mappingWindow.FilePattern))
								{
									_fileParserService.SaveTextFileFormat(mappingWindow.FilePattern, finalFormat);
								}

								// Parse with the configured format
								ParseFileWithFormat(filePath, finalFormat);
							}
							else
							{
								// User cancelled, try parsing with detected format or fallback
								ParseFileDirectly(filePath);
							}
						}
						else
						{
							// Format detection completely failed, use fallback parsing
							ParseFileDirectly(filePath);
						}
					});
				}
				else
				{
					// Format is valid, parse directly
					ParseFileDirectly(filePath);
				}
			}
			catch (Exception ex)
			{
				System.Windows.Application.Current.Dispatcher.Invoke(() =>
				{
					StatusMessage = $"Error detecting file format: {ex.Message}";
					// Fallback to direct parsing
					ParseFileDirectly(filePath);
				});
			}
		});
	}

	/// <summary>
	/// Parses a file directly without format configuration
	/// </summary>
	private void ParseFileDirectly(string filePath)
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

				var logEvents = await _fileParserService.ParseFileAsync(filePath, progress);

				// Batch-add logs to CacheTarget for better performance
				System.Windows.Application.Current.Dispatcher.Invoke(() =>
				{
					ProcessParsedLogs(logEvents, filePath);
					IsLoading = false;
					LoadingProgress = string.Empty;
					StatusMessage = $"Loaded {logEvents.Count} log entries from {Path.GetFileName(filePath)}";
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

	/// <summary>
	/// Parses a file with a specific format configuration
	/// </summary>
	private void ParseFileWithFormat(string filePath, TextFileFormat format)
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

				// Temporarily save format for this parsing session
				_fileParserService.SaveTextFileFormat(Path.GetFileName(filePath), format);

				var logEvents = await _fileParserService.ParseFileAsync(filePath, progress);

				// Batch-add logs to CacheTarget for better performance
				System.Windows.Application.Current.Dispatcher.Invoke(() =>
				{
					ProcessParsedLogs(logEvents, filePath);
					IsLoading = false;
					LoadingProgress = string.Empty;
					StatusMessage = $"Loaded {logEvents.Count} log entries from {Path.GetFileName(filePath)}";
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

	/// <summary>
	/// Processes parsed log events and adds them to the appropriate tab
	/// </summary>
	private void ProcessParsedLogs(List<LogEventInfo> logEvents, string fileName)
	{
		// We're already on the Dispatcher thread, no need to invoke
		if (logEvents == null || logEvents.Count == 0)
			return;
		
		// Find or create tab for this file
		var tab = LogTabs.FirstOrDefault(t => t.TargetName == $"File_{Path.GetFileName(fileName)}");
		if (tab == null)
		{
			tab = new LogTabViewModel
			{
				Header = Path.GetFileName(fileName),
				TargetName = $"File_{Path.GetFileName(fileName)}",
				MaxCount = int.MaxValue
			};
			LogTabs.Add(tab);
			SelectedTab = tab;
		}

		// Add all log events in batches to avoid UI freezing
		const int batchSize = 500;
		for (int i = 0; i < logEvents.Count; i += batchSize)
		{
			var batch = logEvents.Skip(i).Take(batchSize).ToList();
			
			// Add batch directly to the collection
			foreach (var logEvent in batch)
			{
				tab.LogEventInfos.Add(logEvent);
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