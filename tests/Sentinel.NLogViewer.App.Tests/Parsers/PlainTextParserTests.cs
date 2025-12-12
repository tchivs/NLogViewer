using System;
using System.Linq;
using NLog;
using Sentinel.NLogViewer.App.Parsers;
using Xunit;

namespace Sentinel.NLogViewer.App.Tests.Parsers
{
    /// <summary>
    /// Unit tests for PlainTextParser
    /// </summary>
    public class PlainTextParserTests : IDisposable
    {
        private readonly PlainTextParser _parser;

        public PlainTextParserTests()
        {
            _parser = new PlainTextParser();
        }

        [Fact]
        public void Parse_LineWithTimestampAndLevel_ParsesCorrectly()
        {
            // Arrange
            var lines = new[]
            {
                "2024-01-15 10:30:45 INFO [MyLogger] Application started successfully"
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Single(results);
            var logEvent = results[0];
            Assert.Equal(LogLevel.Info, logEvent.Level);
            Assert.Equal("MyLogger", logEvent.LoggerName);
            Assert.Contains("Application started", logEvent.Message);
        }

        [Fact]
        public void Parse_LineWithDifferentLevels_ParsesLevelsCorrectly()
        {
            // Arrange
            var lines = new[]
            {
                "2024-01-15 10:30:45 TRACE [Logger] Trace message",
                "2024-01-15 10:30:46 DEBUG [Logger] Debug message",
                "2024-01-15 10:30:47 INFO [Logger] Info message",
                "2024-01-15 10:30:48 WARN [Logger] Warn message",
                "2024-01-15 10:30:49 ERROR [Logger] Error message",
                "2024-01-15 10:30:50 FATAL [Logger] Fatal message"
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Equal(6, results.Count);
            Assert.Equal(LogLevel.Trace, results[0].Level);
            Assert.Equal(LogLevel.Debug, results[1].Level);
            Assert.Equal(LogLevel.Info, results[2].Level);
            Assert.Equal(LogLevel.Warn, results[3].Level);
            Assert.Equal(LogLevel.Error, results[4].Level);
            Assert.Equal(LogLevel.Fatal, results[5].Level);
        }

        [Fact]
        public void Parse_LineWithIsoTimestamp_ParsesTimestamp()
        {
            // Arrange
            var lines = new[]
            {
                "2024-01-15T10:30:45.123Z INFO [Logger] Message with ISO timestamp"
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Single(results);
            var logEvent = results[0];
            Assert.True(logEvent.TimeStamp.Year == 2024);
            Assert.True(logEvent.TimeStamp.Month == 1);
            Assert.True(logEvent.TimeStamp.Day == 15);
        }

        [Fact]
        public void Parse_LineWithoutTimestamp_UsesCurrentTime()
        {
            // Arrange
            var before = DateTime.Now;
            var lines = new[]
            {
                "INFO [Logger] Message without timestamp"
            };

            // Act
            var results = _parser.Parse(lines);
            var after = DateTime.Now;

            // Assert
            Assert.Single(results);
            var logEvent = results[0];
            Assert.True(logEvent.TimeStamp >= before.AddSeconds(-1));
            Assert.True(logEvent.TimeStamp <= after.AddSeconds(1));
        }

        [Fact]
        public void Parse_LineWithoutLevel_DefaultsToInfo()
        {
            // Arrange
            var lines = new[]
            {
                "2024-01-15 10:30:45 [Logger] Message without level"
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Single(results);
            Assert.Equal(LogLevel.Info, results[0].Level);
        }

        [Fact]
        public void Parse_LineWithLoggerInBrackets_ExtractsLoggerName()
        {
            // Arrange
            var lines = new[]
            {
                "2024-01-15 10:30:45 INFO [MyApp.Services.DataService] Processing request"
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Single(results);
            Assert.Equal("MyApp.Services.DataService", results[0].LoggerName);
        }

        [Fact]
        public void Parse_LineWithLoggerInParentheses_ExtractsLoggerName()
        {
            // Arrange
            var lines = new[]
            {
                "2024-01-15 10:30:45 INFO (MyApp.Controllers.HomeController) Processing request"
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Single(results);
            Assert.Equal("MyApp.Controllers.HomeController", results[0].LoggerName);
        }

        [Fact]
        public void Parse_LineWithoutLogger_DefaultsToUnknown()
        {
            // Arrange
            var lines = new[]
            {
                "2024-01-15 10:30:45 INFO Simple log message"
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Single(results);
            Assert.Equal("Unknown", results[0].LoggerName);
        }

        [Fact]
        public void Parse_EmptyLines_IgnoresEmptyLines()
        {
            // Arrange
            var lines = new[]
            {
                "",
                "   ",
                "2024-01-15 10:30:45 INFO [Logger] Valid message",
                "",
                null
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Single(results);
            Assert.Contains("Valid message", results[0].Message);
        }

        [Fact]
        public void Parse_MultipleLines_ParsesAllLines()
        {
            // Arrange
            var lines = new[]
            {
                "2024-01-15 10:30:45 INFO [Logger1] First message",
                "2024-01-15 10:30:46 WARN [Logger2] Second message",
                "2024-01-15 10:30:47 ERROR [Logger3] Third message"
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Equal(3, results.Count);
            Assert.Equal("Logger1", results[0].LoggerName);
            Assert.Equal("Logger2", results[1].LoggerName);
            Assert.Equal("Logger3", results[2].LoggerName);
        }

        [Fact]
        public void Parse_LineWithWarningKeyword_ParsesAsWarn()
        {
            // Arrange
            var lines = new[]
            {
                "2024-01-15 10:30:45 WARNING [Logger] Warning message"
            };

            // Act
            var results = _parser.Parse(lines);

            // Assert
            Assert.Single(results);
            Assert.Equal(LogLevel.Warn, results[0].Level);
        }

        public void Dispose()
        {
            _parser?.Dispose();
        }
    }
}


