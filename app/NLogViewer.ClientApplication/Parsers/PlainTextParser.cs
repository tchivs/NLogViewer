using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;

namespace NLogViewer.ClientApplication.Parsers
{
    /// <summary>
    /// Parser for plain text log files
    /// </summary>
    public class PlainTextParser : IDisposable
    {
        private readonly Regex _timestampRegex = new Regex(
            @"(\d{4}-\d{2}-\d{2}[\sT]\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})?)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex _levelRegex = new Regex(
            @"\b(TRACE|DEBUG|INFO|WARN|WARNING|ERROR|FATAL)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private bool _disposed;

        public List<LogEventInfo> Parse(string[] lines)
        {
            var results = new List<LogEventInfo>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var logEvent = ParseLine(line);
                if (logEvent != null)
                {
                    results.Add(logEvent);
                }
            }

            return results;
        }

        private LogEventInfo? ParseLine(string line)
        {
            try
            {
                // Try to extract timestamp
                var timestampMatch = _timestampRegex.Match(line);
                var timestamp = timestampMatch.Success && DateTime.TryParse(timestampMatch.Value, out var dt)
                    ? dt
                    : DateTime.Now;

                // Try to extract log level
                var levelMatch = _levelRegex.Match(line);
                var level = levelMatch.Success
                    ? ParseLogLevel(levelMatch.Value)
                    : LogLevel.Info;

                // Extract logger name (try to find pattern like [LoggerName] or (LoggerName))
                var loggerName = "Unknown";
                var loggerMatch = Regex.Match(line, @"[\[\(]([^\])]+)[\]\)]");
                if (loggerMatch.Success)
                {
                    loggerName = loggerMatch.Groups[1].Value;
                }

                // Message is the entire line (or remaining part after timestamp/level)
                var message = line;

                return new LogEventInfo(level, loggerName, message)
                {
                    TimeStamp = timestamp
                };
            }
            catch
            {
                return null;
            }
        }

        private LogLevel ParseLogLevel(string levelString)
        {
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

