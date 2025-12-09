using System;
using NLog;

namespace NLogViewer.ClientApplication.Services
{
    /// <summary>
    /// Event arguments for log received events
    /// </summary>
    public class LogReceivedEventArgs : EventArgs
    {
        public LogEventInfo LogEvent { get; set; }
        public string? AppInfo { get; set; }
        public string? Sender { get; set; }
    }
}


