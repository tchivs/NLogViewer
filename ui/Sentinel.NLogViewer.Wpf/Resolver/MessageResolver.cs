using System.Text;
using NLog;

namespace Sentinel.NLogViewer.Wpf.Resolver
{
    public class MessageResolver : ILogEventInfoResolver
    {
        public string Resolve(LogEventInfo logEventInfo)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(logEventInfo.FormattedMessage);

            if (logEventInfo.Exception != null)
            {
                builder.AppendLine().Append(logEventInfo.Exception);
            }

            return builder.ToString();
        }
    }
}