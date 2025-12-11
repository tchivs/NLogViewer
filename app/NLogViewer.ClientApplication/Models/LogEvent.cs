using NLog;

namespace NLogViewer.ClientApplication.Models;

public class LogEvent
{

	public AppInfo AppInfo { get; set; }
	public LogEventInfo LogEventInfo { get; set; }
}