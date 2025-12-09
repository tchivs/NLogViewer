using System;
using System.IO;
using System.Threading.Tasks;
using NLog;
using NLogViewer.ClientApplication.Parsers;

namespace NLogViewer.ClientApplication.Services
{
    /// <summary>
    /// Service for parsing log files
    /// </summary>
    public class LogFileParserService : IDisposable
    {
        private readonly Parsers.Log4JXmlParser _xmlParser;
        private readonly Parsers.PlainTextParser _textParser;
        private readonly Parsers.JsonLogParser _jsonParser;
        private bool _disposed;

        public LogFileParserService(
            Parsers.Log4JXmlParser xmlParser,
            Parsers.PlainTextParser textParser,
            Parsers.JsonLogParser jsonParser)
        {
            _xmlParser = xmlParser ?? throw new ArgumentNullException(nameof(xmlParser));
            _textParser = textParser ?? throw new ArgumentNullException(nameof(textParser));
            _jsonParser = jsonParser ?? throw new ArgumentNullException(nameof(jsonParser));
        }

        public event EventHandler<LogReceivedEventArgs>? LogParsed;

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
            var logEvents = _xmlParser.ParseMultiple(content);

            foreach (var logEvent in logEvents)
            {
                var appInfo = _xmlParser.ExtractAppInfo(content);
                OnLogParsed(new LogReceivedEventArgs
                {
                    LogEvent = logEvent,
                    AppInfo = appInfo ?? fileName,
                    Sender = "File"
                });
            }
        }

        private void ParseJsonFile(string filePath, string fileName)
        {
            var content = File.ReadAllText(filePath);
            var logEvents = _jsonParser.Parse(content);

            foreach (var logEvent in logEvents)
            {
                OnLogParsed(new LogReceivedEventArgs
                {
                    LogEvent = logEvent,
                    AppInfo = fileName,
                    Sender = "File"
                });
            }
        }

        private void ParseTextFile(string filePath, string fileName)
        {
            var lines = File.ReadAllLines(filePath);
            var logEvents = _textParser.Parse(lines);

            foreach (var logEvent in logEvents)
            {
                OnLogParsed(new LogReceivedEventArgs
                {
                    LogEvent = logEvent,
                    AppInfo = fileName,
                    Sender = "File"
                });
            }
        }

        protected virtual void OnLogParsed(LogReceivedEventArgs e)
        {
            LogParsed?.Invoke(this, e);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _xmlParser?.Dispose();
                _textParser?.Dispose();
                _jsonParser?.Dispose();
                _disposed = true;
            }
        }
    }
}

