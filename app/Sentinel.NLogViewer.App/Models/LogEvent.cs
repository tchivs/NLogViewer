using NLog;

namespace Sentinel.NLogViewer.App.Models;

public class LogEvent
{

	public AppInfo AppInfo { get; set; }
	public LogEventInfo LogEventInfo { get; set; }
}