using System.ComponentModel;
using System.Runtime.CompilerServices;
using NLog;
using Sentinel.NLogViewer.Wpf.Targets;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Sentinel.NLogViewer.App.Models
{
    /// <summary>
    /// ViewModel for a log tab in the TabControl
    /// </summary>
    public class LogTabViewModel : INotifyPropertyChanged, ICacheTarget
    {
        private string _header = string.Empty;
        private string _targetName = string.Empty;
        private int _logCount;
        private int _maxCount = 10000;

        public string Header
        {
            get => _header;
            set
            {
                if (_header != value)
                {
                    _header = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TargetName
        {
            get => _targetName;
            set
            {
                if (_targetName != value)
                {
                    _targetName = value;
                    OnPropertyChanged();
                }
            }
        }

        public int LogCount
        {
            get => _logCount;
            set
            {
                if (_logCount != value)
                {
                    _logCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int MaxCount
        {
            get => _maxCount;
            set
            {
                if (_maxCount != value)
                {
                    _maxCount = value;
                    OnPropertyChanged();
                }
            }
        }

		public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly ReplaySubject<LogEventInfo> _cacheSubject = new(10000);

        /// <summary>
        /// Observable stream of log events. Replays buffered events to new subscribers so nothing is lost before subscription.
        /// </summary>
        public IObservable<LogEventInfo> Cache => _cacheSubject.AsObservable();

        /// <summary>
        /// Pushes a log event into the cache. Subscribers (e.g. NLogViewer) receive it immediately or via replay when they subscribe later.
        /// </summary>
        /// <param name="logEvent">The log event to add.</param>
        public void AddLogEvent(LogEventInfo logEvent)
        {
            _cacheSubject.OnNext(logEvent);
            LogCount++;
        }
    }
}

