using NLog;

namespace Sentinel.NLogViewer.Wpf.Resolver
{
    public class IdResolver : ILogEventInfoResolver
    {
        public string Resolve(LogEventInfo logEventInfo)
        {
            return logEventInfo.SequenceID.ToString();
        }
    }
}