using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using NLog;
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

	/// <summary>
	/// Parses a log file asynchronously with progress reporting
	/// </summary>
	/// <param name="filePath">Path to the log file</param>
	/// <param name="progress">Progress reporter for parsing status</param>
	/// <returns>List of parsed log events</returns>
	public async Task<List<LogEventInfo>> ParseFileAsync(string filePath, IProgress<(int current, int total)>? progress = null)
	{
		var extension = Path.GetExtension(filePath).ToLowerInvariant();
		var fileName = Path.GetFileName(filePath);

		return await Task.Run(() =>
		{
			try
			{
				switch (extension)
				{
					case ".xml":
						return ParseXmlFile(filePath, fileName, progress);
					case ".json":
						return ParseJsonFile(filePath, fileName, progress);
					case ".txt":
					case ".log":
					default:
						return ParseTextFile(filePath, fileName, progress);
				}
			}
			catch (Exception ex)
			{
				throw new Exception($"Error parsing file {fileName}: {ex.Message}", ex);
			}
		});
	}

	/// <summary>
	/// Parses an XML file with progress reporting
	/// </summary>
	private List<LogEventInfo> ParseXmlFile(string filePath, string fileName, IProgress<(int current, int total)>? progress)
	{
		var content = File.ReadAllText(filePath);
		var log4JEvents = ParseMultipleLog4JEvents(content, progress);
		
		var results = new List<LogEventInfo>();
		foreach (var log4JEvent in log4JEvents)
		{
			var logEvent = log4JEvent.ToLogEvent("File");
			results.Add(logEvent.LogEventInfo);
		}
		
		return results;
	}

	/// <summary>
	/// Parses multiple Log4J events from XML content with progress reporting.
	/// Extracts all log4j:event elements and parses each one separately.
	/// </summary>
	private List<Log4JEvent> ParseMultipleLog4JEvents(string xmlContent, IProgress<(int current, int total)>? progress)
	{
		var results = new List<Log4JEvent>();

		try
		{
			// Try to parse as a document with multiple events
			var doc = XDocument.Parse(xmlContent);
			var ns = XNamespace.Get("http://jakarta.apache.org/log4j/");
			
			// Find all event elements (could be in eventSet or standalone)
			var eventElements = doc.Descendants(ns + "event").ToList();
			var total = eventElements.Count;
			var current = 0;
			
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
						current++;
						progress?.Report((current, total));
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
					progress?.Report((1, 1));
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
				progress?.Report((1, 1));
			}
			catch
			{
				// Return empty list on error
			}
		}

		return results;
	}

	/// <summary>
	/// Parses a JSON file with progress reporting
	/// </summary>
	private List<LogEventInfo> ParseJsonFile(string filePath, string fileName, IProgress<(int current, int total)>? progress)
	{
		var content = File.ReadAllText(filePath);
		var logEvents = _jsonParser.Parse(content);
		
		// Report progress if parser supports it
		if (logEvents.Count > 0)
		{
			progress?.Report((logEvents.Count, logEvents.Count));
		}
		
		return logEvents;
	}

	/// <summary>
	/// Parses a text file line by line with progress reporting
	/// </summary>
	private List<LogEventInfo> ParseTextFile(string filePath, string fileName, IProgress<(int current, int total)>? progress)
	{
		var results = new List<LogEventInfo>();
		const int batchSize = 1000; // Process in batches for progress updates
		
		// Get total line count for progress calculation
		var totalLines = File.ReadLines(filePath).Count();
		
		using var reader = new StreamReader(filePath);
		string? line;
		int currentLine = 0;
		var batch = new List<string>(batchSize);
		
		while ((line = reader.ReadLine()) != null)
		{
			currentLine++;
			batch.Add(line);
			
			// Process batch when full or at end
			if (batch.Count >= batchSize || currentLine == totalLines)
			{
				var logEvents = _textParser.Parse(batch.ToArray());
				results.AddRange(logEvents);
				batch.Clear();
				
				// Report progress
				progress?.Report((currentLine, totalLines));
			}
		}
		
		return results;
	}

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