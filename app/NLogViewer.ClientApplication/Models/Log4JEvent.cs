using System.Collections.Generic;

namespace NLogViewer.ClientApplication.Models;

/// <summary>
/// Represents a Log4J logging event parsed from XML format.
/// This class models the structure defined in the log4j.dtd specification for log4j:event elements.
/// </summary>
public class Log4JEvent
{
	/// <summary>
	/// Gets or sets the name of the logger that generated this event.
	/// Corresponds to the "logger" attribute in the log4j:event element.
	/// </summary>
	public string Logger { get; set; }

	/// <summary>
	/// Gets or sets the log level of this event.
	/// Corresponds to the "level" attribute in the log4j:event element.
	/// </summary>
	public Log4JLevel Level { get; set; }

	/// <summary>
	/// Gets or sets the timestamp of the event in milliseconds since epoch (Unix timestamp).
	/// Corresponds to the "timestamp" attribute in the log4j:event element.
	/// </summary>
	public long Timestamp { get; set; }

	/// <summary>
	/// Gets or sets the name of the thread that generated this event.
	/// Corresponds to the "thread" attribute in the log4j:event element.
	/// </summary>
	public string Thread { get; set; }

	/// <summary>
	/// Gets or sets the log message content.
	/// Extracted from the log4j:message child element of the log4j:event.
	/// </summary>
	public string Message { get; set; }

	/// <summary>
	/// Gets or sets the exception or throwable information (stack trace) associated with this log event.
	/// Extracted from the log4j:throwable child element of the log4j:event. May be null if no exception information is included.
	/// </summary>
	public string Throwable { get; set; }

	/// <summary>
	/// Gets or sets the location information (class, method, file, line) where the log event was generated.
	/// Extracted from the log4j:locationInfo child element. May be null if location info is not included.
	/// </summary>
	public Log4JLocationInfo LocationInfo { get; set; }

	/// <summary>
	/// Gets or sets a dictionary of additional properties associated with this log event.
	/// Extracted from the log4j:properties/log4j:data elements. Keys are property names, values are property values.
	/// </summary>
	public Dictionary<string, string> Properties { get; set; } = new();
}