using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NLogViewer.ClientApplication.Models;
using NLogViewer.ClientApplication.Parsers;

namespace NLogViewer.ClientApplication.Services;

/// <summary>
/// Service for parsing log files
/// </summary>
public class LogFileParserService(
	Log4JEventParser xmlParser,
	PlainTextParser textParser,
	JsonLogParser jsonParser)
	: IDisposable
{
	private readonly Log4JEventParser _xmlParser = xmlParser ?? throw new ArgumentNullException(nameof(xmlParser));
	private readonly PlainTextParser _textParser = textParser ?? throw new ArgumentNullException(nameof(textParser));
	private readonly JsonLogParser _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
	private bool _disposed;

	//public event EventHandler<LogReceivedEventArgs>? LogParsed;

	public async Task ParseFileAsync(string filePath)
	{
		var extension = Path.GetExtension(filePath).ToLowerInvariant();
		var fileName = Path.GetFileName(filePath);

		await Task.Run(() =>
		{
			try
			{
				switch (extension)
				{
					case ".xml":
						ParseXmlFile(filePath, fileName);
						break;
					case ".json":
						ParseJsonFile(filePath, fileName);
						break;
					case ".txt":
					case ".log":
					default:
						ParseTextFile(filePath, fileName);
						break;
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Error parsing file {fileName}: {ex.Message}", ex);
			}
		});
	}

	private void ParseXmlFile(string filePath, string fileName)
	{
		var content = File.ReadAllText(filePath);
		var log4JEvents = ParseMultipleLog4JEvents(content);

		foreach (var log4JEvent in log4JEvents)
		{
			var logEvent = log4JEvent.ToLogEvent("File");
			//OnLogParsed(new LogReceivedEventArgs
			//{
			//	LogEvent = logEvent.LogEventInfo,
			//	AppInfo = logEvent.AppInfo?.AppName?.Name ?? fileName,
			//	Sender = "File"
			//});
		}
	}

	/// <summary>
	/// Parses multiple Log4J events from XML content.
	/// Extracts all log4j:event elements and parses each one separately.
	/// </summary>
	private List<Log4JEvent> ParseMultipleLog4JEvents(string xmlContent)
	{
		var results = new List<Log4JEvent>();

		try
		{
			// Try to parse as a document with multiple events
			var doc = XDocument.Parse(xmlContent);
			var ns = XNamespace.Get("http://jakarta.apache.org/log4j/");
			
			// Find all event elements (could be in eventSet or standalone)
			var eventElements = doc.Descendants(ns + "event").ToList();
			
			if (eventElements.Any())
			{
				// Parse each event element separately
				foreach (var eventElement in eventElements)
				{
					try
					{
						var eventXml = eventElement.ToString();
						var log4JEvent = _xmlParser.Parse(eventXml);
						results.Add(log4JEvent);
					}
					catch
					{
						// Skip invalid events
					}
				}
			}
			else
			{
				// Try parsing as a single event
				try
				{
					var log4JEvent = _xmlParser.Parse(xmlContent);
					results.Add(log4JEvent);
				}
				catch
				{
					// If parsing fails, return empty list
				}
			}
		}
		catch
		{
			// If document parsing fails, try parsing as a single event
			try
			{
				var log4JEvent = _xmlParser.Parse(xmlContent);
				results.Add(log4JEvent);
			}
			catch
			{
				// Return empty list on error
			}
		}

		return results;
	}

	private void ParseJsonFile(string filePath, string fileName)
	{
		var content = File.ReadAllText(filePath);
		var logEvents = _jsonParser.Parse(content);

		foreach (var logEvent in logEvents)
		{
			//OnLogParsed(new LogReceivedEventArgs
			//{
			//	LogEvent = logEvent,
			//	AppInfo = fileName,
			//	Sender = "File"
			//});
		}
	}

	private void ParseTextFile(string filePath, string fileName)
	{
		var lines = File.ReadAllLines(filePath);
		var logEvents = _textParser.Parse(lines);

		foreach (var logEvent in logEvents)
		{
			//OnLogParsed(new LogReceivedEventArgs
			//{
			//	LogEvent = logEvent,
			//	AppInfo = fileName,
			//	Sender = "File"
			//});
		}
	}

	//protected virtual void OnLogParsed(LogReceivedEventArgs e)
	//{
	//	LogParsed?.Invoke(this, e);
	//}

	public void Dispose()
	{
		if (!_disposed)
		{
			// Log4JEventParser doesn't implement IDisposable
			_textParser?.Dispose();
			_jsonParser?.Dispose();
			_disposed = true;
		}
	}
}