using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NLog;
using NLog.Config;
using NLog.Layouts;

namespace NLogViewer.ClientApplication.Parsers
{
    /// <summary>
    /// Parser for Log4J XML format (used by Log4JXmlTarget and ChainsawTarget)
    /// </summary>
    public class Log4JXmlParser : IDisposable
    {
        private bool _disposed;

        /// <summary>
        /// Parse a single log event from XML string
        /// </summary>
        public LogEventInfo? Parse(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                var eventElement = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "event");
                
                if (eventElement == null)
                    return null;

                return ParseEventElement(eventElement);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parse multiple log events from XML string
        /// </summary>
        public List<LogEventInfo> ParseMultiple(string xml)
        {
            var results = new List<LogEventInfo>();

            try
            {
                var doc = XDocument.Parse(xml);
                var eventElements = doc.Descendants().Where(e => e.Name.LocalName == "event");

                foreach (var eventElement in eventElements)
                {
                    var logEvent = ParseEventElement(eventElement);
                    if (logEvent != null)
                        results.Add(logEvent);
                }
            }
            catch
            {
                // Return empty list on error
            }

            return results;
        }

        /// <summary>
        /// Extract AppInfo from XML string
        /// </summary>
        public string? ExtractAppInfo(string xml)
        {
            try
            {
                var doc = XDocument.Parse(xml);
                var eventElement = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "event");
                
                if (eventElement == null)
                    return null;

                // Try to find log4j:event/log4j:app element
                var appElement = eventElement.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "app" || e.Name.LocalName == "log4japp");
                
                return appElement?.Value;
            }
            catch
            {
                return null;
            }
        }

        private LogEventInfo? ParseEventElement(XElement eventElement)
        {
            try
            {
                // Parse timestamp
                var timestampAttr = eventElement.Attribute("timestamp");
                var timestamp = timestampAttr != null && long.TryParse(timestampAttr.Value, out var ts)
                    ? DateTimeOffset.FromUnixTimeMilliseconds(ts).DateTime
                    : DateTime.Now;

                // Parse level
                var levelAttr = eventElement.Attribute("level");
                var level = ParseLogLevel(levelAttr?.Value);

                // Parse logger
                var loggerAttr = eventElement.Attribute("logger");
                var loggerName = loggerAttr?.Value ?? "Unknown";

                // Parse message
                var messageElement = eventElement.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "message");
                var message = messageElement?.Value ?? string.Empty;

                // Parse thread
                var threadElement = eventElement.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "thread");
                var thread = threadElement?.Value;

                // Create LogEventInfo
                var logEvent = new LogEventInfo(level, loggerName, message)
                {
                    TimeStamp = timestamp
                };

                if (!string.IsNullOrEmpty(thread))
                {
                    logEvent.Properties["Thread"] = thread;
                }

                // Parse exception if present
                var throwableElement = eventElement.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "throwable");
                if (throwableElement != null)
                {
                    logEvent.Exception = new Exception(throwableElement.Value);
                }

                // Parse NDC (Nested Diagnostic Context)
                var ndcElement = eventElement.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "ndc" || e.Name.LocalName == "log4jndc");
                if (ndcElement != null)
                {
                    logEvent.Properties["NDC"] = ndcElement.Value;
                }

                // Parse properties
                var properties = eventElement.Descendants()
                    .Where(e => e.Name.LocalName == "data" || e.Name.LocalName == "property");
                foreach (var prop in properties)
                {
                    var key = prop.Attribute("key")?.Value ?? prop.Attribute("name")?.Value;
                    var value = prop.Value;
                    if (!string.IsNullOrEmpty(key))
                    {
                        logEvent.Properties[key] = value;
                    }
                }

                return logEvent;
            }
            catch
            {
                return null;
            }
        }

        private LogLevel ParseLogLevel(string? levelString)
        {
            if (string.IsNullOrEmpty(levelString))
                return LogLevel.Info;

            return levelString.ToUpperInvariant() switch
            {
                "TRACE" => LogLevel.Trace,
                "DEBUG" => LogLevel.Debug,
                "INFO" => LogLevel.Info,
                "WARN" or "WARNING" => LogLevel.Warn,
                "ERROR" => LogLevel.Error,
                "FATAL" => LogLevel.Fatal,
                _ => LogLevel.Info
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}

