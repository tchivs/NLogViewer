using System;
using System.Linq;
using System.Xml.Linq;
using NLog;
using NLogViewer.ClientApplication.Parsers;
using Xunit;

namespace NLogViewer.ClientApplication.Tests.Parsers
{
    /// <summary>
    /// Unit tests for Log4JXmlParser
    /// </summary>
    public class Log4JXmlParserTests : IDisposable
    {
        private readonly Log4JXmlParser _parser;

        public Log4JXmlParserTests()
        {
            _parser = new Log4JXmlParser();
        }

        [Fact]
        public void Parse_ValidLog4JXml_ReturnsLogEventInfo()
        {
            // Arrange
            var xml = @"<log4j:event logger=""TestLogger"" level=""INFO"" timestamp=""1234567890000"" xmlns:log4j=""http://jakarta.apache.org/log4j/"">
                <log4j:message>Test message</log4j:message>
                <log4j:thread>Thread-1</log4j:thread>
            </log4j:event>";

            // Act
            var result = _parser.Parse(xml);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestLogger", result.LoggerName);
            Assert.Equal(LogLevel.Info, result.Level);
            Assert.Equal("Test message", result.Message);
        }

        [Fact]
        public void Parse_Log4JXmlWithTimestamp_ParsesTimestampCorrectly()
        {
            // Arrange
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var xml = $@"<log4j:event logger=""TestLogger"" level=""DEBUG"" timestamp=""{timestamp}"" xmlns:log4j=""http://jakarta.apache.org/log4j/"">
                <log4j:message>Debug message</log4j:message>
            </log4j:event>";

            // Act
            var result = _parser.Parse(xml);

            // Assert
            Assert.NotNull(result);
            var expectedTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
            Assert.True(Math.Abs((result.TimeStamp - expectedTime).TotalSeconds) < 1);
        }

        [Fact]
        public void Parse_Log4JXmlWithAllLevels_ParsesLevelsCorrectly()
        {
            var levels = new[] { "TRACE", "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };
            var expectedLogLevels = new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warn, LogLevel.Error, LogLevel.Fatal };

            for (int i = 0; i < levels.Length; i++)
            {
                // Arrange
                var xml = $@"<log4j:event logger=""TestLogger"" level=""{levels[i]}"" timestamp=""1234567890000"" xmlns:log4j=""http://jakarta.apache.org/log4j/"">
                    <log4j:message>Test {levels[i]}</log4j:message>
                </log4j:event>";

                // Act
                var result = _parser.Parse(xml);

                // Assert
                Assert.NotNull(result);
                Assert.Equal(expectedLogLevels[i], result.Level);
            }
        }

        [Fact]
        public void Parse_Log4JXmlWithException_ParsesException()
        {
            // Arrange
            var xml = @"<log4j:event logger=""TestLogger"" level=""ERROR"" timestamp=""1234567890000"" xmlns:log4j=""http://jakarta.apache.org/log4j/"">
                <log4j:message>Error occurred</log4j:message>
                <log4j:throwable>System.Exception: Test exception</log4j:throwable>
            </log4j:event>";

            // Act
            var result = _parser.Parse(xml);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Exception);
            Assert.Contains("Test exception", result.Exception.Message);
        }

        [Fact]
        public void Parse_Log4JXmlWithProperties_ParsesProperties()
        {
            // Arrange
            // Note: The parser reads prop.Value, so we need to put the value as element content, not as attribute
            var xml = @"<log4j:event logger=""TestLogger"" level=""INFO"" timestamp=""1234567890000"" xmlns:log4j=""http://jakarta.apache.org/log4j/"">
                <log4j:message>Test message</log4j:message>
                <log4j:data name=""UserId"">12345</log4j:data>
                <log4j:data name=""SessionId"">abc-123</log4j:data>
            </log4j:event>";

            // Act
            var result = _parser.Parse(xml);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Properties.ContainsKey("UserId"));
            Assert.Equal("12345", result.Properties["UserId"]);
            Assert.True(result.Properties.ContainsKey("SessionId"));
            Assert.Equal("abc-123", result.Properties["SessionId"]);
        }

        [Fact]
        public void Parse_InvalidXml_ReturnsNull()
        {
            // Arrange
            var invalidXml = "<invalid>xml</invalid>";

            // Act
            var result = _parser.Parse(invalidXml);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Parse_EmptyXml_ReturnsNull()
        {
            // Arrange
            var emptyXml = "";

            // Act
            var result = _parser.Parse(emptyXml);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseMultiple_ValidLog4JXml_ReturnsMultipleLogEvents()
        {
            // Arrange
            var xml = @"<log4j:events xmlns:log4j=""http://jakarta.apache.org/log4j/"">
                <log4j:event logger=""Logger1"" level=""INFO"" timestamp=""1234567890000"">
                    <log4j:message>Message 1</log4j:message>
                </log4j:event>
                <log4j:event logger=""Logger2"" level=""WARN"" timestamp=""1234567891000"">
                    <log4j:message>Message 2</log4j:message>
                </log4j:event>
            </log4j:events>";

            // Act
            var results = _parser.ParseMultiple(xml);

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count);
            Assert.Equal("Logger1", results[0].LoggerName);
            Assert.Equal("Logger2", results[1].LoggerName);
        }

        [Fact]
        public void ExtractAppInfo_ValidLog4JXml_ReturnsAppInfo()
        {
            // Arrange
            var xml = @"<log4j:event logger=""TestLogger"" level=""INFO"" timestamp=""1234567890000"" xmlns:log4j=""http://jakarta.apache.org/log4j/"">
                <log4j:app>MyApplication</log4j:app>
                <log4j:message>Test message</log4j:message>
            </log4j:event>";

            // Act
            var appInfo = _parser.ExtractAppInfo(xml);

            // Assert
            Assert.NotNull(appInfo);
            Assert.Equal("MyApplication", appInfo);
        }

        [Fact]
        public void ExtractAppInfo_Log4JXmlWithoutApp_ReturnsNull()
        {
            // Arrange
            var xml = @"<log4j:event logger=""TestLogger"" level=""INFO"" timestamp=""1234567890000"" xmlns:log4j=""http://jakarta.apache.org/log4j/"">
                <log4j:message>Test message</log4j:message>
            </log4j:event>";

            // Act
            var appInfo = _parser.ExtractAppInfo(xml);

            // Assert
            Assert.Null(appInfo);
        }

        [Fact]
        public void Parse_RealWorldLog4JXml_DoesNotThrowException()
        {
            // Arrange - Real XML from TestApp that caused an exception
            // This XML has no namespace declaration, but parser should handle it
            var xml = @"<log4j:event logger=""NLogViewer.ClientApplication.TestApp.Services.NetworkService"" level=""TRACE"" timestamp=""1765283620626"" thread=""11""><log4j:message>Application started successfully (Message #16)</log4j:message><log4j:locationInfo class=""NLogViewer.ClientApplication.TestApp.Program"" method=""GenerateNormalLog"" file=""C:\Users\dboexler\Documents\Repositories\github.com\boexler\NLogViewer\testapp\NLogViewer.ClientApplication.TestApp\Program.cs"" line=""107""/><log4j:properties><log4j:data name=""log4japp"" value=""NLogViewer.ClientApplication.TestApp(31368)""/><log4j:data name=""log4jmachinename"" value=""NB250911""/></log4j:properties></log4j:event>";

            // Act - Should not throw exception and should parse successfully
            LogEventInfo? result = null;
            Exception? parseException = null;
            try
            {
                result = _parser.Parse(xml);
            }
            catch (Exception ex)
            {
                parseException = ex;
            }

            // Assert - Should not throw exception and should parse successfully
            Assert.Null(parseException);
            Assert.NotNull(result);
            Assert.Equal("NLogViewer.ClientApplication.TestApp.Services.NetworkService", result.LoggerName);
            Assert.Equal(LogLevel.Trace, result.Level);
            Assert.Equal("Application started successfully (Message #16)", result.Message);
            // Properties should be parsed from the value attributes
            Assert.True(result.Properties.ContainsKey("log4japp"));
            Assert.Equal("NLogViewer.ClientApplication.TestApp(31368)", result.Properties["log4japp"]);
            Assert.True(result.Properties.ContainsKey("log4jmachinename"));
            Assert.Equal("NB250911", result.Properties["log4jmachinename"]);
        }

        [Fact]
        public void Parse_Log4JXmlWithoutNamespace_CanStillParse()
        {
            // Arrange - XML without namespace declaration (like the real-world example)
            var xml = @"<log4j:event logger=""TestLogger"" level=""INFO"" timestamp=""1234567890000"">
                <log4j:message>Test message</log4j:message>
            </log4j:event>";

            // Act
            var result = _parser.Parse(xml);

            // Assert
            // The parser should handle XML without namespace declaration
            // It uses LocalName which should work regardless of namespace
            Assert.NotNull(result);
            Assert.Equal("TestLogger", result.LoggerName);
            Assert.Equal("Test message", result.Message);
        }

        [Fact]
        public void Parse_Log4JXmlWithPropertiesContainer_ParsesCorrectly()
        {
            // Arrange - XML with properties in log4j:properties container
            var xml = @"<log4j:event logger=""TestLogger"" level=""INFO"" timestamp=""1234567890000"">
                <log4j:message>Test message</log4j:message>
                <log4j:properties>
                    <log4j:data name=""log4japp"" value=""MyApp(12345)""/>
                    <log4j:data name=""log4jmachinename"" value=""MyMachine""/>
                </log4j:properties>
            </log4j:event>";

            // Act
            var result = _parser.Parse(xml);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestLogger", result.LoggerName);
            Assert.Equal("Test message", result.Message);
            // Properties should be parsed from the data elements (now supports value attribute)
            Assert.True(result.Properties.ContainsKey("log4japp"));
            Assert.Equal("MyApp(12345)", result.Properties["log4japp"]);
            Assert.True(result.Properties.ContainsKey("log4jmachinename"));
            Assert.Equal("MyMachine", result.Properties["log4jmachinename"]);
        }

        [Fact]
        public void Parse_Log4JXmlWithLocationInfo_ParsesCorrectly()
        {
            // Arrange - XML with locationInfo element
            var xml = @"<log4j:event logger=""TestLogger"" level=""DEBUG"" timestamp=""1234567890000"">
                <log4j:message>Debug message</log4j:message>
                <log4j:locationInfo class=""MyClass"" method=""MyMethod"" file=""MyFile.cs"" line=""42""/>
            </log4j:event>";

            // Act
            var result = _parser.Parse(xml);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("TestLogger", result.LoggerName);
            Assert.Equal(LogLevel.Debug, result.Level);
            Assert.Equal("Debug message", result.Message);
            // LocationInfo should not cause parsing errors
        }

        public void Dispose()
        {
            _parser?.Dispose();
        }
    }
}
