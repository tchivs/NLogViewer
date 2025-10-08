using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DJ.Extensions;
using DJ.Helper;
using DJ.Resolver;
using DJ.Targets;
using NLog;

namespace DJ
{
    /// <summary>
    /// A customizable control for viewing NLog events with filtering and styling capabilities.
    /// </summary>
    public partial class NLogViewer : Control
    {
        // ##############################################################################################################################
        // Dependency Properties
        // ##############################################################################################################################

        #region Dependency Properties
            
        // ##########################################################################################
        // Colors
        // ##########################################################################################

        #region Colors

        /// <summary>
        /// The background for the trace output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush TraceBackground
        {
            get => (Brush) GetValue(TraceBackgroundProperty);
            set => SetValue(TraceBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="TraceBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TraceBackgroundProperty =
            DependencyProperty.Register("TraceBackground", typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#D3D3D3"))));

        /// <summary>
        /// The foreground for the trace output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush TraceForeground
        {
            get => (Brush) GetValue(TraceForegroundProperty);
            set => SetValue(TraceForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="TraceForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TraceForegroundProperty =
            DependencyProperty.Register("TraceForeground", typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#042271"))));

        /// <summary>
        /// The background for the debug output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush DebugBackground
        {
            get => (Brush) GetValue(DebugBackgroundProperty);
            set => SetValue(DebugBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="DebugBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty DebugBackgroundProperty =
            DependencyProperty.Register("DebugBackground", typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#90EE90"))));

        /// <summary>
        /// The foreground for the debug output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush DebugForeground
        {
            get => (Brush) GetValue(DebugForegroundProperty);
            set => SetValue(DebugForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="DebugForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty DebugForegroundProperty =
            DependencyProperty.Register("DebugForeground", typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#042271"))));

        /// <summary>
        /// The background for the info output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush InfoBackground
        {
            get => (Brush) GetValue(InfoBackgroundProperty);
            set => SetValue(InfoBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="InfoBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty InfoBackgroundProperty = DependencyProperty.Register("InfoBackground",
            typeof(Brush), typeof(NLogViewer),
            new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#0000FF"))));

        /// <summary>
        /// The foreground for the info output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush InfoForeground
        {
            get => (Brush) GetValue(InfoForegroundProperty);
            set => SetValue(InfoForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="InfoForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty InfoForegroundProperty = DependencyProperty.Register("InfoForeground",
            typeof(Brush), typeof(NLogViewer), new PropertyMetadata(Brushes.White));

        /// <summary>
        /// The background for the warn output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush WarnBackground
        {
            get => (Brush) GetValue(WarnBackgroundProperty);
            set => SetValue(WarnBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="WarnBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty WarnBackgroundProperty = DependencyProperty.Register("WarnBackground",
            typeof(Brush), typeof(NLogViewer),
            new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#FFFF00"))));

        /// <summary>
        /// The foreground for the warn output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush WarnForeground
        {
            get => (Brush) GetValue(WarnForegroundProperty);
            set => SetValue(WarnForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="WarnForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty WarnForegroundProperty = DependencyProperty.Register("WarnForeground",
            typeof(Brush), typeof(NLogViewer),
            new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#324B5C"))));

        /// <summary>
        /// The background for the error output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush ErrorBackground
        {
            get => (Brush) GetValue(ErrorBackgroundProperty);
            set => SetValue(ErrorBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="ErrorBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ErrorBackgroundProperty =
            DependencyProperty.Register("ErrorBackground", typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.Red));

        /// <summary>
        /// The foreground for the error output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush ErrorForeground
        {
            get => (Brush) GetValue(ErrorForegroundProperty);
            set => SetValue(ErrorForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="ErrorForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ErrorForegroundProperty =
            DependencyProperty.Register("ErrorForeground", typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.White));

        /// <summary>
        /// The background for the fatal output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush FatalBackground
        {
            get => (Brush) GetValue(FatalBackgroundProperty);
            set => SetValue(FatalBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="FatalBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty FatalBackgroundProperty =
            DependencyProperty.Register("FatalBackground", typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.Black));

        /// <summary>
        /// The foreground for the fatal output
        /// </summary>
        [Category("NLogViewerColors")]
        public Brush FatalForeground
        {
            get => (Brush) GetValue(FatalForegroundProperty);
            set => SetValue(FatalForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="FatalForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty FatalForegroundProperty =
            DependencyProperty.Register("FatalForeground", typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.Yellow));

        #endregion
        
        // ##########################################################################################
        // NLogViewer
        // ##########################################################################################

        #region NLogViewer

        /// <summary>
        /// Is looking if any target with this name is configured and tries to link it
        /// </summary>
        [Category("NLogViewer")]
        public string TargetName
        {
            get => (string)GetValue(TargetNameProperty);
            set => SetValue(TargetNameProperty, value);
        }

        /// <summary>
        /// The <see cref="TargetName"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TargetNameProperty = DependencyProperty.Register("TargetName", typeof(string), typeof(NLogViewer), new PropertyMetadata(null));
        
        /// <summary>
        /// Private DP to bind to the gui
        /// </summary>
        [Category("NLogViewer")]
        public CollectionViewSource LogEvents
        {
            get => (CollectionViewSource) GetValue(LogEventsProperty);
            private set => SetValue(LogEventsProperty, value);
        }

        /// <summary>
        /// The <see cref="LogEvents"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty LogEventsProperty = DependencyProperty.Register("LogEvents",
            typeof(CollectionViewSource), typeof(NLogViewer), new PropertyMetadata(null));
        
        /// <summary>
        /// Automatically scroll to the newest entry
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Automatically scroll to the newest log entry when new entries are added")]
        public bool AutoScroll
        {
            get => (bool)GetValue(AutoScrollProperty);
            set => SetValue(AutoScrollProperty, value);
        }

        /// <summary>
        /// The <see cref="AutoScroll"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.Register("AutoScroll", typeof(bool), typeof(NLogViewer), new PropertyMetadata(true, AutoScrollChangedCallback));

        private static void AutoScrollChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d is NLogViewer instance)
            {
                instance.OnAutoScrollChanged();
            }
        }

        protected virtual void OnAutoScrollChanged()
        {
            if (AutoScroll)
                PART_ListView?.ScrollToEnd();
        }

        private void UpdateColumnVisibility()
        {
            if (PART_ListView?.View is not AutoSizedGridView gridView) return;

            // Update ID column visibility
            if (gridView.Columns.Count > 0)
            {
                gridView.Columns[0].Width = ShowIdColumn ? _originalIdColumnWidth : 0;
            }

            // Update Level column visibility
            if (gridView.Columns.Count > 1)
            {
                gridView.Columns[1].Width = ShowLevelColumn ? _originalLevelColumnWidth : 0;
            }

            // Update TimeStamp column visibility
            if (gridView.Columns.Count > 2)
            {
                gridView.Columns[2].Width = ShowTimeStampColumn ? _originalTimeStampColumnWidth : 0;
            }

            // Update LoggerName column visibility
            if (gridView.Columns.Count > 3)
            {
                gridView.Columns[3].Width = ShowLoggerNameColumn ? _originalLoggerNameColumnWidth : 0;
            }

            // Message column is always visible (no visibility control)
        }

        private void UpdateFilter()
        {
            if (LogEvents?.View is not CollectionView collectionView) return;

            collectionView.Filter = item =>
            {
                if (item is not LogEventInfo logEvent) return true;

                return logEvent.Level.Name switch
                {
                    "Trace" => !TraceFilter,  // If TraceFilter is true, hide Trace entries (!true = false)
                    "Debug" => !DebugFilter,  // If DebugFilter is true, hide Debug entries (!true = false)
                    "Info" => !InfoFilter,    // If InfoFilter is true, hide Info entries (!true = false)
                    "Warn" => !WarnFilter,     // If WarnFilter is true, hide Warn entries (!true = false)
                    "Error" => !ErrorFilter,   // If ErrorFilter is true, hide Error entries (!true = false)
                    "Fatal" => !FatalFilter,   // If FatalFilter is true, hide Fatal entries (!true = false)
                    _ => true
                };
            };
            
            // Refresh the view to apply the filter
            collectionView.Refresh();
        }
        
        /// <summary>
        /// Delete all entries
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Command to clear all log entries from the viewer")]
        public ICommand ClearCommand
        {
            get => (ICommand) GetValue(ClearCommandProperty);
            set => SetValue(ClearCommandProperty, value);
        }

        /// <summary>
        /// The <see cref="ClearCommand"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ClearCommandProperty = DependencyProperty.Register("ClearCommand",
            typeof(ICommand), typeof(NLogViewer), new PropertyMetadata(null));
        
        /// <summary>
        /// Stop logging
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Pause or resume logging to improve performance when not actively monitoring")]
        public bool Pause
        {
            get => (bool)GetValue(PauseProperty);
            set => SetValue(PauseProperty, value);
        }

        /// <summary>
        /// The <see cref="Pause"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty PauseProperty = DependencyProperty.Register("Pause", typeof(bool), typeof(NLogViewer), new PropertyMetadata(false, OnPauseChanged));

        private static void OnPauseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is NLogViewer viewer && !DesignerProperties.GetIsInDesignMode(viewer))
            {
                if ((bool)e.NewValue)
                {
                    // Pause: Stop listening for better performance
                    viewer.StopListen();
                }
                else
                {
                    // Resume: Start listening again
                    viewer.StartListen();
                }
            }
        }
        
        /// <summary>
        /// The maximum number of entries before automatic cleaning is performed. There is a hysteresis of 100 entries which must be exceeded.
        /// Example: <see cref="MaxCount"/> is '1000'. Then after '1100' entries, everything until '1000' is deleted.
        /// If set to '0' or less, it is deactivated
        /// </summary>
        [Category("NLogViewer")]
        public int MaxCount
        {
            get => (int)GetValue(MaxCountProperty);
            set => SetValue(MaxCountProperty, value);
        }

        /// <summary>
        /// The <see cref="MaxCount"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty MaxCountProperty = DependencyProperty.Register("MaxCount", typeof(int), typeof(NLogViewer), new PropertyMetadata(5000));


        #endregion

        // ##########################################################################################
        // Resolver
        // ##########################################################################################

        #region Resolver
        
        /// <summary>
        /// The <see cref="ILogEventInfoResolver"/> to format the id
        /// </summary>
        [Category("NLogViewerResolver")]
        public ILogEventInfoResolver IdResolver
        {
            get => (ILogEventInfoResolver)GetValue(IdResolverProperty);
            set => SetValue(IdResolverProperty, value);
        }

        /// <summary>
        /// The <see cref="IdResolver"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty IdResolverProperty = DependencyProperty.Register("IdResolver", typeof(ILogEventInfoResolver), typeof(NLogViewer), new PropertyMetadata(new IdResolver()));
        
        /// <summary>
        /// The <see cref="ILogEventInfoResolver"/> to format the timestamp output
        /// </summary>
        [Category("NLogViewerResolver")]
        public ILogEventInfoResolver TimeStampResolver
        {
            get => (ILogEventInfoResolver)GetValue(TimeStampResolverProperty);
            set => SetValue(TimeStampResolverProperty, value);
        }

        /// <summary>
        /// The <see cref="TimeStampResolver"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TimeStampResolverProperty = DependencyProperty.Register("TimeStampResolver", typeof(ILogEventInfoResolver), typeof(NLogViewer), new PropertyMetadata(new TimeStampResolver()));
        
        /// <summary>
        /// The <see cref="ILogEventInfoResolver"/> to format the loggername
        /// </summary>
        [Category("NLogViewerResolver")]
        public ILogEventInfoResolver LoggerNameResolver
        {
            get => (ILogEventInfoResolver)GetValue(LoggerNameResolverProperty);
            set => SetValue(LoggerNameResolverProperty, value);
        }

        /// <summary>
        /// The <see cref="LoggerNameResolver"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty LoggerNameResolverProperty = DependencyProperty.Register("LoggerNameResolver", typeof(ILogEventInfoResolver), typeof(NLogViewer), new PropertyMetadata(new LoggerNameResolver()));
        
        /// <summary>
        /// The <see cref="ILogEventInfoResolver"/> to format the message
        /// </summary>
        [Category("NLogViewerResolver")]
        public ILogEventInfoResolver MessageResolver
        {
            get => (ILogEventInfoResolver)GetValue(MessageResolverProperty);
            set => SetValue(MessageResolverProperty, value);
        }

        /// <summary>
        /// The <see cref="MessageResolver"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty MessageResolverProperty = DependencyProperty.Register("MessageResolver", typeof(ILogEventInfoResolver), typeof(NLogViewer), new PropertyMetadata(new MessageResolver()));
        
        #endregion

        // ##########################################################################################
        // Column Visibility
        // ##########################################################################################

        #region Column Visibility

        /// <summary>
        /// Controls the visibility of the ID column
        /// </summary>
        [Category("NLogViewerColumns")]
        public bool ShowIdColumn
        {
            get => (bool)GetValue(ShowIdColumnProperty);
            set => SetValue(ShowIdColumnProperty, value);
        }

        /// <summary>
        /// The <see cref="ShowIdColumn"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ShowIdColumnProperty = 
            DependencyProperty.Register("ShowIdColumn", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(true, OnColumnVisibilityChanged));

        /// <summary>
        /// Controls the visibility of the Level column
        /// </summary>
        [Category("NLogViewerColumns")]
        public bool ShowLevelColumn
        {
            get => (bool)GetValue(ShowLevelColumnProperty);
            set => SetValue(ShowLevelColumnProperty, value);
        }

        /// <summary>
        /// The <see cref="ShowLevelColumn"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ShowLevelColumnProperty = 
            DependencyProperty.Register("ShowLevelColumn", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(true, OnColumnVisibilityChanged));

        /// <summary>
        /// Controls the visibility of the TimeStamp column
        /// </summary>
        [Category("NLogViewerColumns")]
        public bool ShowTimeStampColumn
        {
            get => (bool)GetValue(ShowTimeStampColumnProperty);
            set => SetValue(ShowTimeStampColumnProperty, value);
        }

        /// <summary>
        /// The <see cref="ShowTimeStampColumn"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ShowTimeStampColumnProperty = 
            DependencyProperty.Register("ShowTimeStampColumn", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(true, OnColumnVisibilityChanged));

        /// <summary>
        /// Controls the visibility of the LoggerName column
        /// </summary>
        [Category("NLogViewerColumns")]
        public bool ShowLoggerNameColumn
        {
            get => (bool)GetValue(ShowLoggerNameColumnProperty);
            set => SetValue(ShowLoggerNameColumnProperty, value);
        }

        /// <summary>
        /// The <see cref="ShowLoggerNameColumn"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ShowLoggerNameColumnProperty = 
            DependencyProperty.Register("ShowLoggerNameColumn", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(true, OnColumnVisibilityChanged));


        private static void OnColumnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d is NLogViewer instance)
            {
                instance.UpdateColumnVisibility();
            }
        }

        #endregion

        // ##########################################################################################
        // Filter Properties
        // ##########################################################################################

        #region Filter Properties

        /// <summary>
        /// Filter for Trace log level
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Hide/show Trace level log entries")]
        public bool TraceFilter
        {
            get => (bool)GetValue(TraceFilterProperty);
            set => SetValue(TraceFilterProperty, value);
        }

        /// <summary>
        /// The <see cref="TraceFilter"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TraceFilterProperty = 
            DependencyProperty.Register("TraceFilter", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(false, OnFilterChanged));

        /// <summary>
        /// Filter for Debug log level
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Hide/show Debug level log entries")]
        public bool DebugFilter
        {
            get => (bool)GetValue(DebugFilterProperty);
            set => SetValue(DebugFilterProperty, value);
        }

        /// <summary>
        /// The <see cref="DebugFilter"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty DebugFilterProperty = 
            DependencyProperty.Register("DebugFilter", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(false, OnFilterChanged));

        /// <summary>
        /// Filter for Info log level
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Hide/show Info level log entries")]
        public bool InfoFilter
        {
            get => (bool)GetValue(InfoFilterProperty);
            set => SetValue(InfoFilterProperty, value);
        }

        /// <summary>
        /// The <see cref="InfoFilter"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty InfoFilterProperty = 
            DependencyProperty.Register("InfoFilter", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(false, OnFilterChanged));

        /// <summary>
        /// Filter for Warn log level
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Hide/show Warn level log entries")]
        public bool WarnFilter
        {
            get => (bool)GetValue(WarnFilterProperty);
            set => SetValue(WarnFilterProperty, value);
        }

        /// <summary>
        /// The <see cref="WarnFilter"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty WarnFilterProperty = 
            DependencyProperty.Register("WarnFilter", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(false, OnFilterChanged));

        /// <summary>
        /// Filter for Error log level
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Hide/show Error level log entries")]
        public bool ErrorFilter
        {
            get => (bool)GetValue(ErrorFilterProperty);
            set => SetValue(ErrorFilterProperty, value);
        }

        /// <summary>
        /// The <see cref="ErrorFilter"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ErrorFilterProperty = 
            DependencyProperty.Register("ErrorFilter", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(false, OnFilterChanged));

        /// <summary>
        /// Filter for Fatal log level
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Hide/show Fatal level log entries")]
        public bool FatalFilter
        {
            get => (bool)GetValue(FatalFilterProperty);
            set => SetValue(FatalFilterProperty, value);
        }

        /// <summary>
        /// The <see cref="FatalFilter"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty FatalFilterProperty = 
            DependencyProperty.Register("FatalFilter", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(false, OnFilterChanged));

        private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d is NLogViewer instance)
            {
                instance.UpdateFilter();
            }
        }

        #endregion

        // ##########################################################################################
        // Filter Visibility
        // ##########################################################################################

        #region Filter Visibility

        /// <summary>
        /// Controls the visibility of the filter buttons
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Show/hide the filter buttons")]
        public bool ShowFilterButtons
        {
            get => (bool)GetValue(ShowFilterButtonsProperty);
            set => SetValue(ShowFilterButtonsProperty, value);
        }

        /// <summary>
        /// The <see cref="ShowFilterButtons"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ShowFilterButtonsProperty = 
            DependencyProperty.Register("ShowFilterButtons", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(true));

        #endregion

        // ##########################################################################################
        // Controls Visibility
        // ##########################################################################################

        #region Controls Visibility

        /// <summary>
        /// Controls the visibility of the control buttons (Auto-Scroll, Clear, Pause)
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Show/hide the control buttons (Auto-Scroll, Clear, Pause)")]
        public bool ShowControlButtons
        {
            get => (bool)GetValue(ShowControlButtonsProperty);
            set => SetValue(ShowControlButtonsProperty, value);
        }

        /// <summary>
        /// The <see cref="ShowControlButtons"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ShowControlButtonsProperty = 
            DependencyProperty.Register("ShowControlButtons", typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(true));

        #endregion

        #endregion

        // ##############################################################################################################################
        // Properties
        // ##############################################################################################################################

        #region Properties

        // ##########################################################################################
        // Public Properties
        // ##########################################################################################



        // ##########################################################################################
        // Private Properties
        // ##########################################################################################

        private ObservableCollection<LogEventInfo> _LogEventInfos { get; } = new ObservableCollection<LogEventInfo>();
        private IDisposable _Subscription;
        private Window _ParentWindow;
        private bool _isListening = false;
        
        // Store original column widths for restoration
        private double _originalIdColumnWidth = 40;
        private double _originalLevelColumnWidth = double.NaN; // Auto
        private double _originalTimeStampColumnWidth = double.NaN; // Auto
        private double _originalLoggerNameColumnWidth = double.NaN; // Auto
        
        /// <summary>
        /// Reference to the ListView template part for programmatic access
        /// </summary>
        private ListView PART_ListView => GetTemplateChild("PART_ListView") as ListView;
        
        #endregion

        // ##############################################################################################################################
        // Public Methods
        // ##############################################################################################################################

        #region Public Methods

        /// <summary>
        /// Starts listening for log events by subscribing to the cache target.
        /// This method should be called when the control needs to resume listening for logs,
        /// such as when undocking from a docking system or when the window loads again.
        /// </summary>
        public void StartListen()
        {
            if (_isListening || DesignerProperties.GetIsInDesignMode(this))
                return;

			// Ensure we have a parent window reference
			// add hook to parent window to dispose subscription
			// use case:
			// NLogViewer is used in a new window inside of a TabControl. If you switch the TabItems,
			// the unloaded event is called and would dispose the subscription, even if the control is still alive.
			if (_ParentWindow == null && Window.GetWindow(this) is { } window)
            {
                _ParentWindow = window;
                _ParentWindow.Closed += _ParentWindowOnClosed;
            }

            if (_ParentWindow == null)
                return;

            var target = CacheTarget.GetInstance(targetName: TargetName);
            
            _Subscription = target.Cache.SubscribeOn(Scheduler.Default)
                .Buffer(TimeSpan.FromMilliseconds(100))
                .Where(x => x.Any())
                .ObserveOn(new DispatcherSynchronizationContext(_ParentWindow.Dispatcher))
                .Subscribe(infos =>
                {
                    using (LogEvents.DeferRefresh())
                    {
                        foreach (LogEventInfo info in infos)
                        {
                            _LogEventInfos.Add(info);
                        }
                        if (MaxCount >= 0 & _LogEventInfos.Count - 100 > MaxCount)
                        {
                            for (int i = 0; i < _LogEventInfos.Count - MaxCount; i++)
                            {
                                _LogEventInfos.RemoveAt(0);
                            }
                        }   
                    }

                    if (AutoScroll)
                    {
                        PART_ListView?.ScrollToEnd();
                    }
                });

            _isListening = true;
        }

        /// <summary>
        /// Stops listening for log events by disposing the subscription.
        /// This method should be called when the control needs to stop listening for logs,
        /// such as when docking in a docking system or when the window is unloaded.
        /// </summary>
        public void StopListen()
        {
            if (!_isListening)
                return;

            _Subscription?.Dispose();
            _Subscription = null;
            _isListening = false;
        }

        #endregion

        // ##############################################################################################################################
        // Constructor
        // ##############################################################################################################################

        #region Constructor

        static NLogViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(NLogViewer), new FrameworkPropertyMetadata(typeof(NLogViewer)));
        }

        public NLogViewer()
        {
            DataContext = this;
            
            // save instance UID
            Uid = GetHashCode().ToString();

            if (DesignerProperties.GetIsInDesignMode(this))
                return;

            LogEvents = new CollectionViewSource {Source = _LogEventInfos};
            UpdateFilter(); // Initialize filter
            
            Loaded += _OnLoaded;
            Unloaded += _OnUnloaded;
            ClearCommand = new ActionCommand(_LogEventInfos.Clear);
            
            // Filter commands are no longer needed - ToggleButtons handle the binding directly
        }

        private void _OnUnloaded(object sender, RoutedEventArgs e)
        {
            // look in logical and visual tree if the control has been removed
            // If there is no parent window found before, we have a special case (https://github.com/dojo90/NLogViewer/issues/30) and just dispose it anyway
            if (_ParentWindow.FindChildByUid<NLogViewer>(Uid) == null)
            {
                _Dispose();
            }
        }
        
        private void _ParentWindowOnClosed(object sender, EventArgs e)
        {
            _Dispose();
        }

        private void _Dispose()
        {
            StopListen();
        }
        
        private void _OnLoaded(object sender, RoutedEventArgs e)
        {
            // removed loaded handler to prevent duplicate subscribing
            Loaded -= _OnLoaded;

            PART_ListView?.ScrollToEnd();
            
            // Store original column widths for visibility management
            if (PART_ListView?.View is AutoSizedGridView gridView)
            {
                if (gridView.Columns.Count > 0) _originalIdColumnWidth = gridView.Columns[0].Width;
                if (gridView.Columns.Count > 1) _originalLevelColumnWidth = gridView.Columns[1].Width;
                if (gridView.Columns.Count > 2) _originalTimeStampColumnWidth = gridView.Columns[2].Width;
                if (gridView.Columns.Count > 3) _originalLoggerNameColumnWidth = gridView.Columns[3].Width;
                // Message column (index 4) is always visible, no need to store its width
                
                // Apply initial column visibility
                UpdateColumnVisibility();
            }
            
            // Start listening for log events
            StartListen();
        }

        #endregion
    }
}
