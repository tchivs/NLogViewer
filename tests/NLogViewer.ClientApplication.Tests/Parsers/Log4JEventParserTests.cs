using System;
using NLogViewer.ClientApplication.Models;
using NLogViewer.ClientApplication.Parsers;
using Xunit;

namespace NLogViewer.ClientApplication.Tests.Parsers
{
    /// <summary>
    /// Unit tests for Log4JEventParser
    /// </summary>
    public class Log4JEventParserTests : IDisposable
    {
        private readonly Log4JEventParser _parser;

        public Log4JEventParserTests()
        {
            _parser = new Log4JEventParser();
        }

        [Fact]
        public void Parse_ValidLog4JXml_ReturnsLog4JEvent()
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
            Assert.Equal("TestLogger", result.Logger);
            Assert.Equal(Log4JLevel.Info, result.Level);
            Assert.Equal("Test message", result.Message);
            Assert.Equal("Thread-1", result.Thread);
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
            Assert.Equal(timestamp, result.Timestamp);
        }

        [Fact]
        public void Parse_Log4JXmlWithAllLevels_ParsesLevelsCorrectly()
        {
            var levels = new[] { "TRACE", "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };
            var expectedLogLevels = new[] { Log4JLevel.Trace, Log4JLevel.Debug, Log4JLevel.Info, Log4JLevel.Warn, Log4JLevel.Error, Log4JLevel.Fatal };

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
            Assert.NotNull(result.Throwable);
            Assert.Contains("Test exception", result.Throwable);
        }

        [Fact]
        public void Parse_Log4JXmlWithProperties_ParsesProperties()
        {
            // Arrange
            // Note: The parser reads the value attribute from log4j:data elements within log4j:properties
            var xml = @"<log4j:event logger=""TestLogger"" level=""INFO"" timestamp=""1234567890000"" xmlns:log4j=""http://jakarta.apache.org/log4j/"">
                <log4j:message>Test message</log4j:message>
                <log4j:properties>
                    <log4j:data name=""UserId"" value=""12345""/>
                    <log4j:data name=""SessionId"" value=""abc-123""/>
                </log4j:properties>
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
        public void Parse_InvalidXml_ThrowsFormatException()
        {
            // Arrange
            var invalidXml = "<invalid>xml</invalid>";

            // Act & Assert
            Assert.Throws<FormatException>(() => _parser.Parse(invalidXml));
        }

        [Fact]
        public void Parse_EmptyXml_ThrowsArgumentException()
        {
            // Arrange
            var emptyXml = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _parser.Parse(emptyXml));
        }


        [Fact]
        public void Parse_RealWorldLog4JXml_DoesNotThrowException()
        {
            // Arrange - Real XML from TestApp that caused an exception
            // This XML has no namespace declaration, but parser should handle it
            var xml = @"<log4j:event logger=""NLogViewer.ClientApplication.TestApp.Services.NetworkService"" level=""TRACE"" timestamp=""1765283620626"" thread=""11""><log4j:message>Application started successfully (Message #16)</log4j:message><log4j:locationInfo class=""NLogViewer.ClientApplication.TestApp.Program"" method=""GenerateNormalLog"" file=""C:\Users\dboexler\Documents\Repositories\github.com\boexler\NLogViewer\testapp\NLogViewer.ClientApplication.TestApp\Program.cs"" line=""107""/><log4j:properties><log4j:data name=""log4japp"" value=""NLogViewer.ClientApplication.TestApp(31368)""/><log4j:data name=""log4jmachinename"" value=""NB250911""/></log4j:properties></log4j:event>";

            // Act - Should not throw exception and should parse successfully
            Log4JEvent? result = null;
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
            Assert.Equal("NLogViewer.ClientApplication.TestApp.Services.NetworkService", result.Logger);
            Assert.Equal(Log4JLevel.Trace, result.Level);
            Assert.Equal("Application started successfully (Message #16)", result.Message);
            Assert.Equal("11", result.Thread);
            Assert.Equal(1765283620626L, result.Timestamp);
            // Properties should be parsed from the value attributes
            Assert.True(result.Properties.ContainsKey("log4japp"));
            Assert.Equal("NLogViewer.ClientApplication.TestApp(31368)", result.Properties["log4japp"]);
            Assert.True(result.Properties.ContainsKey("log4jmachinename"));
            Assert.Equal("NB250911", result.Properties["log4jmachinename"]);
            // LocationInfo should be parsed
            Assert.NotNull(result.LocationInfo);
            Assert.Equal("NLogViewer.ClientApplication.TestApp.Program", result.LocationInfo.Class);
            Assert.Equal("GenerateNormalLog", result.LocationInfo.Method);
            Assert.Equal(107, result.LocationInfo.Line);
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
            // It automatically adds the namespace if missing
            Assert.NotNull(result);
            Assert.Equal("TestLogger", result.Logger);
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
            Assert.Equal("TestLogger", result.Logger);
            Assert.Equal("Test message", result.Message);
            // Properties should be parsed from the data elements (value attribute)
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
            Assert.Equal("TestLogger", result.Logger);
            Assert.Equal(Log4JLevel.Debug, result.Level);
            Assert.Equal("Debug message", result.Message);
            // LocationInfo should be parsed correctly
            Assert.NotNull(result.LocationInfo);
            Assert.Equal("MyClass", result.LocationInfo.Class);
            Assert.Equal("MyMethod", result.LocationInfo.Method);
            Assert.Equal("MyFile.cs", result.LocationInfo.File);
            Assert.Equal(42, result.LocationInfo.Line);
        }

        public void Dispose()
        {
            // Log4JEventParser doesn't implement IDisposable, so nothing to dispose
        }
    }
}
