using NLog;

namespace Sentinel.NLogViewer.Wpf.Resolver
{
    public class LoggerNameResolver : ILogEventInfoResolver
    {
        public string Resolve(LogEventInfo logEventInfo)
        {
            return logEventInfo.LoggerName;
        }
    }
}