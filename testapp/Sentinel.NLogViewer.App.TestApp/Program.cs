using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Sentinel.NLogViewer.App.TestApp
{
    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly Random Random = new Random();
        private static Timer? _logTimer;
        private static int _messageCounter = 0;
        private static int _exceptionCounter = 0;

        static async Task Main(string[] args)
        {
            Console.WriteLine("NLogViewer Client Application Test App");
            var nlogVersion = typeof(LogManager).Assembly.GetName().Version;
            Console.WriteLine($"NLog Version: {nlogVersion}");
            
            // Check which target to use from environment variable
            var targetName = Environment.GetEnvironmentVariable("NLOG_TARGET") ?? "chainsaw";
            Console.WriteLine($"Using target: {targetName}");
            Console.WriteLine("Press any key to stop logging...");
            Console.WriteLine();
            
            // Update NLog configuration to use the specified target
            UpdateNLogTarget(targetName);

            // Start logging timer
            _logTimer = new Timer(GenerateLogMessage, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Wait for user input
            await Task.Run(() => Console.ReadKey(true));

            // Stop timer
            _logTimer?.Dispose();
            
            Console.WriteLine();
            Console.WriteLine($"Total messages logged: {_messageCounter}");
            Console.WriteLine($"Total exceptions logged: {_exceptionCounter}");
            Console.WriteLine("Shutting down...");

            LogManager.Shutdown();
        }

        private static void UpdateNLogTarget(string targetName)
        {
            try
            {
                var config = LogManager.Configuration;
                if (config == null) return;

                // Remove existing rules
                config.LoggingRules.Clear();

                // Add new rule with specified target
                var target = config.FindTargetByName(targetName);
                if (target != null)
                {
                    config.LoggingRules.Add(new NLog.Config.LoggingRule("*", NLog.LogLevel.Trace, target));
                    LogManager.Configuration = config;
                    LogManager.ReconfigExistingLoggers();
                    Console.WriteLine($"Successfully configured NLog to use target: {targetName}");
                }
                else
                {
                    Console.WriteLine($"Warning: Target '{targetName}' not found. Using default configuration.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating NLog target: {ex.Message}");
            }
        }

        private static void GenerateLogMessage(object? state)
        {
            try
            {
                // 20% chance to generate an exception
                if (Random.Next(1, 6) == 1)
                {
                    GenerateExceptionLog();
                }
                else
                {
                    GenerateNormalLog();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating log message: {ex.Message}");
            }
        }

        private static void GenerateNormalLog()
        {
            var logLevel = GetRandomLogLevel();
            var message = GenerateRandomMessage();
            var loggerName = GetRandomLoggerName();

            var logger = LogManager.GetLogger(loggerName);

            if (logLevel == LogLevel.Trace)
                logger.Trace(message);
            else if (logLevel == LogLevel.Debug)
                logger.Debug(message);
            else if (logLevel == LogLevel.Info)
                logger.Info(message);
            else if (logLevel == LogLevel.Warn)
                logger.Warn(message);
            else if (logLevel == LogLevel.Error)
                logger.Error(message);
            else if (logLevel == LogLevel.Fatal)
                logger.Fatal(message);

            Interlocked.Increment(ref _messageCounter);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {logLevel.Name}: {message}");
        }

        private static void GenerateExceptionLog()
        {
            var exceptionType = Random.Next(1, 4);
            Exception exception;
            LogLevel logLevel;

            switch (exceptionType)
            {
                case 1:
                    exception = new InvalidOperationException($"Invalid operation occurred at {DateTime.Now:HH:mm:ss}");
                    logLevel = LogLevel.Error;
                    break;
                case 2:
                    exception = new ArgumentNullException("testParameter", "Test parameter was null");
                    logLevel = LogLevel.Error;
                    break;
                case 3:
                    exception = new DivideByZeroException("Division by zero attempted");
                    logLevel = LogLevel.Fatal;
                    break;
                default:
                    exception = new Exception("Generic exception occurred");
                    logLevel = LogLevel.Error;
                    break;
            }

            var logger = LogManager.GetLogger(GetRandomLoggerName());
            
            if (logLevel == LogLevel.Fatal)
            {
                logger.Fatal(exception, $"Fatal exception: {exception.Message}");
            }
            else
            {
                logger.Error(exception, $"Error exception: {exception.Message}");
            }

            Interlocked.Increment(ref _exceptionCounter);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {logLevel.Name} (Exception): {exception.GetType().Name} - {exception.Message}");
        }

        private static LogLevel GetRandomLogLevel()
        {
            var levels = new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Info, LogLevel.Warn, LogLevel.Error, LogLevel.Fatal };
            return levels[Random.Next(levels.Length)];
        }

        private static string GenerateRandomMessage()
        {
            var messages = new[]
            {
                "Application started successfully",
                "Processing user request",
                "Database connection established",
                "Cache updated with new data",
                "Configuration loaded from file",
                "User authentication successful",
                "File uploaded successfully",
                "Background task completed",
                "Memory usage: {0} MB",
                "CPU usage: {0}%",
                "Network request sent to {0}",
                "Response received in {0}ms",
                "Validation passed for user input",
                "Session created for user {0}",
                "Data synchronized with server"
            };

            var message = messages[Random.Next(messages.Length)];
            
            // Replace placeholders with random values
            message = message.Replace("{0}", Random.Next(1, 100).ToString());
            
            return $"{message} (Message #{_messageCounter + 1})";
        }

        private static string GetRandomLoggerName()
        {
            var loggerNames = new[]
            {
                "Sentinel.NLogViewer.App.TestApp",
                "Sentinel.NLogViewer.App.TestApp.Services",
                "Sentinel.NLogViewer.App.TestApp.Services.DataService",
                "Sentinel.NLogViewer.App.TestApp.Services.NetworkService",
                "Sentinel.NLogViewer.App.TestApp.Controllers",
                "Sentinel.NLogViewer.App.TestApp.Controllers.HomeController",
                "Sentinel.NLogViewer.App.TestApp.Models",
                "Sentinel.NLogViewer.App.TestApp.Utils"
            };

            return loggerNames[Random.Next(loggerNames.Length)];
        }
    }
}
