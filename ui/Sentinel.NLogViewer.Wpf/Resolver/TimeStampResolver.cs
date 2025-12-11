using NLog;

namespace Sentinel.NLogViewer.Wpf.Resolver
{
    public class TimeStampResolver : ILogEventInfoResolver
    {
        public string Resolve(LogEventInfo logEventInfo)
        {
            return logEventInfo.TimeStamp.ToString("dd-MM-yyyy hh:mm:ss.fff");
        }
    }
}