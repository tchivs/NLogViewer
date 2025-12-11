using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Sentinel.NLogViewer.Wpf.Extensions;
using Sentinel.NLogViewer.Wpf.Helper;
using Sentinel.NLogViewer.Wpf.Resolver;
using Sentinel.NLogViewer.Wpf.Targets;
using NLog;

namespace Sentinel.NLogViewer.Wpf
{
    /// <summary>
    /// Base class for search terms that can match against text
    /// </summary>
    public abstract class SearchTerm
    {
        public string Text { get; protected set; }
        
        protected SearchTerm(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }
        
        /// <summary>
        /// Determines if the search term matches the given text
        /// </summary>
        /// <param name="text">The text to match against</param>
        /// <returns>True if the text matches the search term</returns>
        public abstract bool Match(string text);
        
        /// <summary>
        /// Determines if the search term matches any of the given texts
        /// </summary>
        /// <param name="texts">The texts to match against</param>
        /// <returns>True if any text matches the search term</returns>
        public bool MatchAny(params string[] texts)
        {
            if (texts == null) return false;
            return texts.Any(text => !string.IsNullOrEmpty(text) && Match(text));
        }
        
        public override string ToString()
        {
            return Text;
        }
    }

    /// <summary>
    /// A search term that performs case-insensitive substring matching
    /// </summary>
    public class TextSearchTerm : SearchTerm
    {
        private readonly string _lowercaseText;
        
        public TextSearchTerm(string text) : base(text)
        {
            _lowercaseText = text.ToLowerInvariant();
        }
        
        public override bool Match(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (string.IsNullOrEmpty(_lowercaseText)) return false;
            return text.ToLowerInvariant().Contains(_lowercaseText);
        }
        
        public override string ToString()
        {
            return Text;
        }
    }

    /// <summary>
    /// A search term that performs regex pattern matching
    /// </summary>
    public class RegexSearchTerm : SearchTerm
    {
        private readonly Regex _regex;

        public RegexSearchTerm(string pattern) : base(pattern)
        {
            try
            {
                _regex = new Regex(pattern, RegexOptions.Compiled);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException($"Invalid regex pattern: {ex.Message}", nameof(pattern), ex);
            }
        }
        
        public override bool Match(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            return _regex.IsMatch(text);
        }
        
        public override string ToString()
        {
            return $"/{Text}/";
        }
    }

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
        [Description("Background brush for Trace level log entries")] 
        public Brush TraceBackground
        {
            get => (Brush) GetValue(TraceBackgroundProperty);
            set => SetValue(TraceBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="TraceBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TraceBackgroundProperty =
            DependencyProperty.Register(nameof(TraceBackground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#D3D3D3"))));

        /// <summary>
        /// The foreground for the trace output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Foreground brush for Trace level log entries")] 
        public Brush TraceForeground
        {
            get => (Brush) GetValue(TraceForegroundProperty);
            set => SetValue(TraceForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="TraceForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TraceForegroundProperty =
            DependencyProperty.Register(nameof(TraceForeground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#042271"))));

        /// <summary>
        /// The background for the debug output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Background brush for Debug level log entries")] 
        public Brush DebugBackground
        {
            get => (Brush) GetValue(DebugBackgroundProperty);
            set => SetValue(DebugBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="DebugBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty DebugBackgroundProperty =
            DependencyProperty.Register(nameof(DebugBackground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#90EE90"))));

        /// <summary>
        /// The foreground for the debug output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Foreground brush for Debug level log entries")] 
        public Brush DebugForeground
        {
            get => (Brush) GetValue(DebugForegroundProperty);
            set => SetValue(DebugForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="DebugForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty DebugForegroundProperty =
            DependencyProperty.Register(nameof(DebugForeground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#042271"))));

        /// <summary>
        /// The background for the info output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Background brush for Info level log entries")] 
        public Brush InfoBackground
        {
            get => (Brush) GetValue(InfoBackgroundProperty);
            set => SetValue(InfoBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="InfoBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty InfoBackgroundProperty = DependencyProperty.Register(nameof(InfoBackground),
            typeof(Brush), typeof(NLogViewer),
            new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#0000FF"))));

        /// <summary>
        /// The foreground for the info output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Foreground brush for Info level log entries")] 
        public Brush InfoForeground
        {
            get => (Brush) GetValue(InfoForegroundProperty);
            set => SetValue(InfoForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="InfoForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty InfoForegroundProperty = DependencyProperty.Register(nameof(InfoForeground),
            typeof(Brush), typeof(NLogViewer), new PropertyMetadata(Brushes.White));

        /// <summary>
        /// The background for the warn output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Background brush for Warn level log entries")] 
        public Brush WarnBackground
        {
            get => (Brush) GetValue(WarnBackgroundProperty);
            set => SetValue(WarnBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="WarnBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty WarnBackgroundProperty = DependencyProperty.Register(nameof(WarnBackground),
            typeof(Brush), typeof(NLogViewer),
            new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#FFFF00"))));

        /// <summary>
        /// The foreground for the warn output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Foreground brush for Warn level log entries")] 
        public Brush WarnForeground
        {
            get => (Brush) GetValue(WarnForegroundProperty);
            set => SetValue(WarnForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="WarnForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty WarnForegroundProperty = DependencyProperty.Register(nameof(WarnForeground),
            typeof(Brush), typeof(NLogViewer),
            new PropertyMetadata((Brush) (new BrushConverter().ConvertFrom("#324B5C"))));

        /// <summary>
        /// The background for the error output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Background brush for Error level log entries")] 
        public Brush ErrorBackground
        {
            get => (Brush) GetValue(ErrorBackgroundProperty);
            set => SetValue(ErrorBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="ErrorBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ErrorBackgroundProperty =
            DependencyProperty.Register(nameof(ErrorBackground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.Red));

        /// <summary>
        /// The foreground for the error output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Foreground brush for Error level log entries")] 
        public Brush ErrorForeground
        {
            get => (Brush) GetValue(ErrorForegroundProperty);
            set => SetValue(ErrorForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="ErrorForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ErrorForegroundProperty =
            DependencyProperty.Register(nameof(ErrorForeground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.White));

        /// <summary>
        /// The background for the fatal output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Background brush for Fatal level log entries")] 
        public Brush FatalBackground
        {
            get => (Brush) GetValue(FatalBackgroundProperty);
            set => SetValue(FatalBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="FatalBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty FatalBackgroundProperty =
            DependencyProperty.Register(nameof(FatalBackground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.Black));

        /// <summary>
        /// The foreground for the fatal output
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Foreground brush for Fatal level log entries")] 
        public Brush FatalForeground
        {
            get => (Brush) GetValue(FatalForegroundProperty);
            set => SetValue(FatalForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="FatalForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty FatalForegroundProperty =
            DependencyProperty.Register(nameof(FatalForeground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.Yellow));

        // ------------------------------------------------------------------------------------------
        // Search Term Chip Colors
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Background brush for plain text search term chips
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Background brush for plain text search term chips")] 
        public Brush TextSearchTermBackground
        {
            get => (Brush)GetValue(TextSearchTermBackgroundProperty);
            set => SetValue(TextSearchTermBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="TextSearchTermBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TextSearchTermBackgroundProperty =
            DependencyProperty.Register(nameof(TextSearchTermBackground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush)(new BrushConverter().ConvertFrom("#FFFFFBE6"))));

        /// <summary>
        /// Border brush for plain text search term chips
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Border brush for plain text search term chips")] 
        public Brush TextSearchTermBorderBrush
        {
            get => (Brush)GetValue(TextSearchTermBorderBrushProperty);
            set => SetValue(TextSearchTermBorderBrushProperty, value);
        }

        /// <summary>
        /// The <see cref="TextSearchTermBorderBrush"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TextSearchTermBorderBrushProperty =
            DependencyProperty.Register(nameof(TextSearchTermBorderBrush), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush)(new BrushConverter().ConvertFrom("#FFFFC107"))));

        /// <summary>
        /// Foreground brush for plain text search term chips
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Foreground brush for plain text search term chips")] 
        public Brush TextSearchTermForeground
        {
            get => (Brush)GetValue(TextSearchTermForegroundProperty);
            set => SetValue(TextSearchTermForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="TextSearchTermForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty TextSearchTermForegroundProperty =
            DependencyProperty.Register(nameof(TextSearchTermForeground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.Black));

        /// <summary>
        /// Background brush for regex search term chips
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Background brush for regex search term chips")] 
        public Brush RegexSearchTermBackground
        {
            get => (Brush)GetValue(RegexSearchTermBackgroundProperty);
            set => SetValue(RegexSearchTermBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="RegexSearchTermBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty RegexSearchTermBackgroundProperty =
            DependencyProperty.Register(nameof(RegexSearchTermBackground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush)(new BrushConverter().ConvertFrom("#FFFFFBE6"))));

        /// <summary>
        /// Border brush for regex search term chips
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Border brush for regex search term chips")] 
        public Brush RegexSearchTermBorderBrush
        {
            get => (Brush)GetValue(RegexSearchTermBorderBrushProperty);
            set => SetValue(RegexSearchTermBorderBrushProperty, value);
        }

        /// <summary>
        /// The <see cref="RegexSearchTermBorderBrush"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty RegexSearchTermBorderBrushProperty =
            DependencyProperty.Register(nameof(RegexSearchTermBorderBrush), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush)(new BrushConverter().ConvertFrom("#FFFFC107"))));

        /// <summary>
        /// Foreground brush for regex search term chips
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Foreground brush for regex search term chips")] 
        public Brush RegexSearchTermForeground
        {
            get => (Brush)GetValue(RegexSearchTermForegroundProperty);
            set => SetValue(RegexSearchTermForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="RegexSearchTermForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty RegexSearchTermForegroundProperty =
            DependencyProperty.Register(nameof(RegexSearchTermForeground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata(Brushes.Black));

        /// <summary>
        /// Foreground brush for the "Regex:" prefix label
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Foreground brush for the 'Regex:' prefix label")] 
        public Brush RegexPrefixForeground
        {
            get => (Brush)GetValue(RegexPrefixForegroundProperty);
            set => SetValue(RegexPrefixForegroundProperty, value);
        }

        /// <summary>
        /// The <see cref="RegexPrefixForeground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty RegexPrefixForegroundProperty =
            DependencyProperty.Register(nameof(RegexPrefixForeground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush)(new BrushConverter().ConvertFrom("#FFFF8F00"))));

        /// <summary>
        /// Background brush used to highlight matched search text in the message
        /// </summary>
        [Category("NLogViewerColors")]
        [Description("Background brush used to highlight matched search text in the message")] 
        public Brush SearchHighlightBackground
        {
            get => (Brush)GetValue(SearchHighlightBackgroundProperty);
            set => SetValue(SearchHighlightBackgroundProperty, value);
        }

        /// <summary>
        /// The <see cref="SearchHighlightBackground"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty SearchHighlightBackgroundProperty =
            DependencyProperty.Register(nameof(SearchHighlightBackground), typeof(Brush), typeof(NLogViewer),
                new PropertyMetadata((Brush)(new BrushConverter().ConvertFrom("#FFFF80C0"))));

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
        public static readonly DependencyProperty TargetNameProperty = DependencyProperty.Register(nameof(TargetName), typeof(string), typeof(NLogViewer), new PropertyMetadata(null));
        
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
        public static readonly DependencyProperty LogEventsProperty = DependencyProperty.Register(nameof(LogEvents),
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
        public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.Register(nameof(AutoScroll), typeof(bool), typeof(NLogViewer), new PropertyMetadata(true, AutoScrollChangedCallback));

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
		
        private void UpdateFilter()
        {
            if (LogEvents?.View is not CollectionView collectionView) return;

            collectionView.Filter = item =>
            {
                if (item is not LogEventInfo logEvent) return true;

                // Apply level filters first
                bool levelFilterPassed = logEvent.Level.Name switch
                {
                    "Trace" => !TraceFilter,  // If TraceFilter is true, hide Trace entries (!true = false)
                    "Debug" => !DebugFilter,  // If DebugFilter is true, hide Debug entries (!true = false)
                    "Info" => !InfoFilter,    // If InfoFilter is true, hide Info entries (!true = false)
                    "Warn" => !WarnFilter,     // If WarnFilter is true, hide Warn entries (!true = false)
                    "Error" => !ErrorFilter,   // If ErrorFilter is true, hide Error entries (!true = false)
                    "Fatal" => !FatalFilter,   // If FatalFilter is true, hide Fatal entries (!true = false)
                    _ => true
                };

                if (!levelFilterPassed) return false;

                // Apply search filters if any active search terms exist
                if (ActiveSearchTerms?.Count > 0)
                {
                    string loggerName = logEvent.LoggerName ?? string.Empty;
                    string message = MessageResolver?.Resolve(logEvent) ?? string.Empty;
                    
                    // AND logic: ALL search terms must match
                    foreach (var searchTerm in ActiveSearchTerms)
                    {
                        if (!searchTerm.MatchAny(loggerName, message))
                            return false;
                    }
                    
                    // All search terms matched
                    return true;
                }

                return true;
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
        public static readonly DependencyProperty ClearCommandProperty = DependencyProperty.Register(nameof(ClearCommand),
            typeof(ICommand), typeof(NLogViewer), new PropertyMetadata(null));

        /// <summary>
        /// Command to add a new search term
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Command to add a new search term")]
        public ICommand AddSearchTermCommand
        {
            get => (ICommand) GetValue(AddSearchTermCommandProperty);
            set => SetValue(AddSearchTermCommandProperty, value);
        }

        /// <summary>
        /// The <see cref="AddSearchTermCommand"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty AddSearchTermCommandProperty = DependencyProperty.Register(nameof(AddSearchTermCommand),
            typeof(ICommand), typeof(NLogViewer), new PropertyMetadata(null));

        /// <summary>
        /// Command to clear all search terms
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Command to clear all search terms")]
        public ICommand ClearAllSearchTermsCommand
        {
            get => (ICommand) GetValue(ClearAllSearchTermsCommandProperty);
            set => SetValue(ClearAllSearchTermsCommandProperty, value);
        }

        /// <summary>
        /// The <see cref="ClearAllSearchTermsCommand"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ClearAllSearchTermsCommandProperty = DependencyProperty.Register(nameof(ClearAllSearchTermsCommand),
            typeof(ICommand), typeof(NLogViewer), new PropertyMetadata(null));

        /// <summary>
        /// Command to add a regex search term from a provided text
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Command to add a regex search term from the provided text")]
        public ICommand AddRegexSearchTermCommand
        {
            get => (ICommand) GetValue(AddRegexSearchTermCommandProperty);
            set => SetValue(AddRegexSearchTermCommandProperty, value);
        }

        /// <summary>
        /// The <see cref="AddRegexSearchTermCommand"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty AddRegexSearchTermCommandProperty = DependencyProperty.Register(nameof(AddRegexSearchTermCommand),
            typeof(ICommand), typeof(NLogViewer), new PropertyMetadata(null));
        /// <summary>
        /// Command to remove a specific search term
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Command to remove a specific search term")]
        public ICommand RemoveSearchTermCommand
        {
            get => (ICommand) GetValue(RemoveSearchTermCommandProperty);
            set => SetValue(RemoveSearchTermCommandProperty, value);
        }

        /// <summary>
        /// The <see cref="RemoveSearchTermCommand"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty RemoveSearchTermCommandProperty = DependencyProperty.Register(nameof(RemoveSearchTermCommand),
            typeof(ICommand), typeof(NLogViewer), new PropertyMetadata(null));

        /// <summary>
        /// Command to edit a search term by copying it to the current search text
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Command to edit a search term by copying it to the current search text")]
        public ICommand EditSearchTermCommand
        {
            get => (ICommand) GetValue(EditSearchTermCommandProperty);
            set => SetValue(EditSearchTermCommandProperty, value);
        }

        /// <summary>
        /// The <see cref="EditSearchTermCommand"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty EditSearchTermCommandProperty = DependencyProperty.Register(nameof(EditSearchTermCommand),
            typeof(ICommand), typeof(NLogViewer), new PropertyMetadata(null));

        /// <summary>
        /// Command to copy text to clipboard
        /// </summary>
        [Category("NLogViewerControls")]
        [Browsable(true)]
        [Description("Command to copy text to clipboard")]
        public ICommand CopyToClipboardCommand
        {
            get => (ICommand) GetValue(CopyToClipboardCommandProperty);
            set => SetValue(CopyToClipboardCommandProperty, value);
        }

        /// <summary>
        /// The <see cref="CopyToClipboardCommand"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty CopyToClipboardCommandProperty = DependencyProperty.Register(nameof(CopyToClipboardCommand),
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
        public static readonly DependencyProperty PauseProperty = DependencyProperty.Register(nameof(Pause), typeof(bool), typeof(NLogViewer), new PropertyMetadata(false, OnPauseChanged));

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
        public static readonly DependencyProperty MaxCountProperty = DependencyProperty.Register(nameof(MaxCount), typeof(int), typeof(NLogViewer), new PropertyMetadata(5000));


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
        public static readonly DependencyProperty IdResolverProperty = DependencyProperty.Register(nameof(IdResolver), typeof(ILogEventInfoResolver), typeof(NLogViewer), new PropertyMetadata(new IdResolver()));
        
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
        public static readonly DependencyProperty TimeStampResolverProperty = DependencyProperty.Register(nameof(TimeStampResolver), typeof(ILogEventInfoResolver), typeof(NLogViewer), new PropertyMetadata(new TimeStampResolver()));
        
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
        public static readonly DependencyProperty LoggerNameResolverProperty = DependencyProperty.Register(nameof(LoggerNameResolver), typeof(ILogEventInfoResolver), typeof(NLogViewer), new PropertyMetadata(new LoggerNameResolver()));
        
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
        public static readonly DependencyProperty MessageResolverProperty = DependencyProperty.Register(nameof(MessageResolver), typeof(ILogEventInfoResolver), typeof(NLogViewer), new PropertyMetadata(new MessageResolver()));
        
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
            DependencyProperty.Register(nameof(ShowIdColumn), typeof(bool), typeof(NLogViewer), 
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
            DependencyProperty.Register(nameof(ShowLevelColumn), typeof(bool), typeof(NLogViewer), 
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
            DependencyProperty.Register(nameof(ShowTimeStampColumn), typeof(bool), typeof(NLogViewer), 
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
            DependencyProperty.Register(nameof(ShowLoggerNameColumn), typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(true, OnColumnVisibilityChanged));


        private static void OnColumnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d is NLogViewer instance)
            {
                instance.UpdateColumnVisibility();
            }
        }

        /// <summary>
        /// Updates column visibility by removing or adding columns based on the Show properties.
        /// When a column is hidden, it's removed from the grid. When shown again, it's added back at its original position.
        /// </summary>
        private void UpdateColumnVisibility()
        {
	        if (PART_ListView?.View is not AutoSizedGridView gridView) return;

	        // Update ID column visibility (original index 0)
	        UpdateColumnVisibility(gridView, _originalIdColumn, ShowIdColumn, _originalIdColumnWidth, 0);

	        // Update Level column visibility (original index 1)
	        UpdateColumnVisibility(gridView, _originalLevelColumn, ShowLevelColumn, _originalLevelColumnWidth, 1);

	        // Update TimeStamp column visibility (original index 2)
	        UpdateColumnVisibility(gridView, _originalTimeStampColumn, ShowTimeStampColumn, _originalTimeStampColumnWidth, 2);

	        // Update LoggerName column visibility (original index 3)
	        UpdateColumnVisibility(gridView, _originalLoggerNameColumn, ShowLoggerNameColumn, _originalLoggerNameColumnWidth, 3);

	        // Message column is always visible (no visibility control)
        }

        /// <summary>
        /// Helper method to update individual column visibility by removing or adding the column.
        /// Calculates the correct insertion position based on which columns are currently visible.
        /// </summary>
        /// <param name="gridView">The GridView containing the columns</param>
        /// <param name="originalColumn">The original column reference to add/remove</param>
        /// <param name="showColumn">Whether the column should be visible</param>
        /// <param name="originalWidth">The original width of the column</param>
        /// <param name="originalIndex">The original index of the column (0=ID, 1=Level, 2=TimeStamp, 3=LoggerName)</param>
        private void UpdateColumnVisibility(AutoSizedGridView gridView, GridViewColumn originalColumn, bool showColumn, double originalWidth, int originalIndex)
        {
	        if (originalColumn == null) return;

	        // Check if column is currently in the grid
	        bool columnExists = gridView.Columns.Contains(originalColumn);

	        if (showColumn && !columnExists)
	        {
		        // Column should be visible but is not in the grid - add it back
		        originalColumn.Width = originalWidth;
		        
		        // Calculate the correct insertion index based on which columns are currently visible
		        int insertionIndex = CalculateInsertionIndex(gridView, originalIndex);
		        gridView.Columns.Insert(insertionIndex, originalColumn);
	        }
	        else if (!showColumn && columnExists)
	        {
		        // Column should be hidden but is in the grid - remove it
		        gridView.Columns.Remove(originalColumn);
	        }
        }

        /// <summary>
        /// Calculates the correct insertion index for a column based on its original position
        /// and which columns are currently visible.
        /// </summary>
        /// <param name="gridView">The GridView containing the columns</param>
        /// <param name="originalIndex">The original index of the column to insert</param>
        /// <returns>The correct insertion index</returns>
        private int CalculateInsertionIndex(AutoSizedGridView gridView, int originalIndex)
        {
	        // Count how many visible columns come before this one in the original order
	        int insertionIndex = 0;
	        
	        // Check ID column (original index 0)
	        if (originalIndex > 0 && ShowIdColumn && gridView.Columns.Contains(_originalIdColumn))
	        {
		        insertionIndex++;
	        }
	        
	        // Check Level column (original index 1)
	        if (originalIndex > 1 && ShowLevelColumn && gridView.Columns.Contains(_originalLevelColumn))
	        {
		        insertionIndex++;
	        }
	        
	        // Check TimeStamp column (original index 2)
	        if (originalIndex > 2 && ShowTimeStampColumn && gridView.Columns.Contains(_originalTimeStampColumn))
	        {
		        insertionIndex++;
	        }
	        
	        // Check LoggerName column (original index 3)
	        if (originalIndex > 3 && ShowLoggerNameColumn && gridView.Columns.Contains(_originalLoggerNameColumn))
	        {
		        insertionIndex++;
	        }
	        
	        return insertionIndex;
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
            DependencyProperty.Register(nameof(TraceFilter), typeof(bool), typeof(NLogViewer), 
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
            DependencyProperty.Register(nameof(DebugFilter), typeof(bool), typeof(NLogViewer), 
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
            DependencyProperty.Register(nameof(InfoFilter), typeof(bool), typeof(NLogViewer), 
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
            DependencyProperty.Register(nameof(WarnFilter), typeof(bool), typeof(NLogViewer), 
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
            DependencyProperty.Register(nameof(ErrorFilter), typeof(bool), typeof(NLogViewer), 
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
            DependencyProperty.Register(nameof(FatalFilter), typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(false, OnFilterChanged));

        /// <summary>
        /// Current search text being typed (not yet activated)
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Current search text being typed (activated with Enter key)")]
        public string CurrentSearchText
        {
            get => (string)GetValue(CurrentSearchTextProperty);
            set => SetValue(CurrentSearchTextProperty, value);
        }

        /// <summary>
        /// The <see cref="CurrentSearchText"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty CurrentSearchTextProperty = 
            DependencyProperty.Register(nameof(CurrentSearchText), typeof(string), typeof(NLogViewer), 
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Whether to use regex pattern matching for search
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Use regex pattern matching instead of free text search")]
        public bool UseRegexSearch
        {
            get => (bool)GetValue(UseRegexSearchProperty);
            set => SetValue(UseRegexSearchProperty, value);
        }

        /// <summary>
        /// The <see cref="UseRegexSearch"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty UseRegexSearchProperty = 
            DependencyProperty.Register(nameof(UseRegexSearch), typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(false));

        /// <summary>
        /// Collection of active search terms
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(false)]
        [Description("Collection of active search terms")]
        public ObservableCollection<SearchTerm> ActiveSearchTerms
        {
            get => (ObservableCollection<SearchTerm>)GetValue(ActiveSearchTermsProperty);
            private set => SetValue(ActiveSearchTermsProperty, value);
        }

        /// <summary>
        /// The <see cref="ActiveSearchTerms"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ActiveSearchTermsProperty = 
            DependencyProperty.Register(nameof(ActiveSearchTerms), typeof(ObservableCollection<SearchTerm>), typeof(NLogViewer), 
                new PropertyMetadata(null));

        private static void OnFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            if (d is NLogViewer instance)
            {
                instance.UpdateFilter();
            }
        }


        /// <summary>
        /// Adds a new search term to the active search terms collection
        /// </summary>
        public void AddSearchTerm()
        {
            if (string.IsNullOrWhiteSpace(CurrentSearchText))
                return;

            SearchTerm searchTerm;
            
            if (UseRegexSearch)
            {
                try
                {
                    searchTerm = new RegexSearchTerm(CurrentSearchText);
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show($"Invalid regex pattern: {ex.Message}", "Regex Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                searchTerm = new TextSearchTerm(CurrentSearchText);
            }
            
            ActiveSearchTerms.Add(searchTerm);
            
            // Clear the input
            CurrentSearchText = string.Empty;
            
            // Update filter
            UpdateFilter();
        }

        /// <summary>
        /// Removes a search term from the active search terms collection
        /// </summary>
        public void RemoveSearchTerm(SearchTerm searchTerm)
        {
            ActiveSearchTerms.Remove(searchTerm);
            UpdateFilter();
        }

        /// <summary>
        /// Clears all active search terms
        /// </summary>
        public void ClearAllSearchTerms()
        {
            ActiveSearchTerms.Clear();
            UpdateFilter();
        }

        /// <summary>
        /// Adds a RegexSearchTerm from a provided text and updates the filter
        /// </summary>
        /// <param name="text">The text to use as regex pattern</param>
        public void AddRegexSearchTerm(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                var term = new RegexSearchTerm(text);
                ActiveSearchTerms.Add(term);
                UpdateFilter();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"Invalid regex pattern: {ex.Message}", "Regex Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Edits a search term by copying its text to the current search text field
        /// </summary>
        /// <param name="searchTerm">The search term to edit</param>
        public void EditSearchTerm(SearchTerm searchTerm)
        {
            if (searchTerm == null)
                return;

            // Copy the search term text to the current search text field
            CurrentSearchText = searchTerm.Text;
            
            // Set the regex checkbox based on the search term type
            UseRegexSearch = searchTerm is RegexSearchTerm;
            
            // Remove the original search term
            ActiveSearchTerms.Remove(searchTerm);
            UpdateFilter();
        }

        /// <summary>
        /// Copies text to the clipboard
        /// </summary>
        /// <param name="text">The text to copy to clipboard</param>
        public void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy to clipboard: {ex.Message}", "Clipboard Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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
            DependencyProperty.Register(nameof(ShowFilterButtons), typeof(bool), typeof(NLogViewer), 
                new PropertyMetadata(true));

        /// <summary>
        /// Controls the visibility of the search controls
        /// </summary>
        [Category("NLogViewerFilters")]
        [Browsable(true)]
        [Description("Show/hide the search controls")]
        public bool ShowSearchControls
        {
            get => (bool)GetValue(ShowSearchControlsProperty);
            set => SetValue(ShowSearchControlsProperty, value);
        }

        /// <summary>
        /// The <see cref="ShowSearchControls"/> DependencyProperty.
        /// </summary>
        public static readonly DependencyProperty ShowSearchControlsProperty = 
            DependencyProperty.Register(nameof(ShowSearchControls), typeof(bool), typeof(NLogViewer), 
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
            DependencyProperty.Register(nameof(ShowControlButtons), typeof(bool), typeof(NLogViewer), 
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
        
        // Store original column references and widths for restoration
        private GridViewColumn _originalIdColumn;
        private GridViewColumn _originalLevelColumn;
        private GridViewColumn _originalTimeStampColumn;
        private GridViewColumn _originalLoggerNameColumn;
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
            ActiveSearchTerms = new ObservableCollection<SearchTerm>();
            UpdateFilter(); // Initialize filter
            
            Loaded += _OnLoaded;
            Unloaded += _OnUnloaded;
            ClearCommand = new RelayCommand(_LogEventInfos.Clear);
            AddSearchTermCommand = new RelayCommand(AddSearchTerm);
            ClearAllSearchTermsCommand = new RelayCommand(ClearAllSearchTerms);
            RemoveSearchTermCommand = new RelayCommand<SearchTerm>(RemoveSearchTerm);
            AddRegexSearchTermCommand = new RelayCommand<string>(AddRegexSearchTerm);
            EditSearchTermCommand = new RelayCommand<SearchTerm>(EditSearchTerm);
            CopyToClipboardCommand = new RelayCommand<string>(CopyToClipboard);
            
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
            
            // Store original column references and widths for visibility management
            if (PART_ListView?.View is AutoSizedGridView gridView)
            {
                if (gridView.Columns.Count > 0) 
                {
                    _originalIdColumn = gridView.Columns[0];
                    _originalIdColumnWidth = gridView.Columns[0].Width;
                }
                if (gridView.Columns.Count > 1) 
                {
                    _originalLevelColumn = gridView.Columns[1];
                    _originalLevelColumnWidth = gridView.Columns[1].Width;
                }
                if (gridView.Columns.Count > 2) 
                {
                    _originalTimeStampColumn = gridView.Columns[2];
                    _originalTimeStampColumnWidth = gridView.Columns[2].Width;
                }
                if (gridView.Columns.Count > 3) 
                {
                    _originalLoggerNameColumn = gridView.Columns[3];
                    _originalLoggerNameColumnWidth = gridView.Columns[3].Width;
                }
                // Message column (index 4) is always visible, no need to store its reference
                
                // Apply initial column visibility
                UpdateColumnVisibility();
            }
            
            // Start listening for log events
            StartListen();
        }

        #endregion
    }
}
