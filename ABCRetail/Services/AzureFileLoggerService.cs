using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ABCRetail.Services
{
    public class AzureFileLoggerService : ILogger
    {
        private readonly string _categoryName;
        private readonly ShareClient? _shareClient;
        private readonly ShareDirectoryClient? _logsDirectoryClient;
        private readonly string? _logFileName;
        private readonly object _lock = new object();

        public AzureFileLoggerService(string categoryName, string connectionString)
        {
            _categoryName = categoryName;
            
            try
            {
                // Create share service client from connection string
                var shareServiceClient = new ShareServiceClient(connectionString);
                
                // Get or create the logs share
                _shareClient = shareServiceClient.GetShareClient("logs");
                
                // Ensure the share exists
                try
                {
                    _shareClient.CreateIfNotExists();
                    Console.WriteLine($"✅ Share 'logs' created or already exists");
                }
                catch (Exception shareEx)
                {
                    Console.WriteLine($"⚠️ Could not create share 'logs': {shareEx.Message}");
                }
                
                // Work directly with the share root (no subdirectory)
                _logsDirectoryClient = _shareClient.GetRootDirectoryClient();
                
                // Create log file name with date
                _logFileName = $"application-{DateTime.UtcNow:yyyy-MM-dd}.log";
                
                Console.WriteLine($"✅ AzureFileLoggerService initialized for category: {categoryName}");
                Console.WriteLine($"📁 Working with share: {_shareClient.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to initialize AzureFileLoggerService: {ex.Message}");
                _shareClient = null!;
                _logsDirectoryClient = null!;
                _logFileName = null!;
            }
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            try
            {
                var logMessage = FormatLogMessage(logLevel, eventId, state, exception, formatter);
                WriteLogToFile(logMessage);
            }
            catch (Exception ex)
            {
                // Fallback to console if file logging fails
                Console.WriteLine($"❌ File logging failed: {ex.Message}");
                Console.WriteLine($"📝 Original log: {formatter(state, exception)}");
            }
        }

        private string FormatLogMessage<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var level = logLevel.ToString().ToUpper();
            var category = _categoryName;
            var message = formatter(state, exception);
            
            var logEntry = $"[{timestamp}] [{level}] [{category}] {message}";
            
            if (exception != null)
            {
                logEntry += $"\nException: {exception.Message}";
                if (exception.StackTrace != null)
                {
                    logEntry += $"\nStackTrace: {exception.StackTrace}";
                }
            }
            
            return logEntry + Environment.NewLine;
        }

        private void WriteLogToFile(string logMessage)
        {
            if (_logsDirectoryClient == null || _logFileName == null)
                return;

            lock (_lock)
            {
                try
                {
                    // Create a unique filename with timestamp to avoid conflicts
                    var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss-fff");
                    var uniqueFileName = $"log-{timestamp}.txt";
                    
                    // Get file client for the unique filename
                    var logFileClient = _logsDirectoryClient.GetFileClient(uniqueFileName);
                    
                    // Convert message to bytes
                    var logBytes = Encoding.UTF8.GetBytes(logMessage);
                    
                    // Create the file and write the log message
                    logFileClient.Create(logBytes.Length);
                    using var stream = logFileClient.OpenWrite(false, 0);
                    stream.Write(logBytes, 0, logBytes.Length);
                    
                    Console.WriteLine($"📝 Log written to Azure File: {uniqueFileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to write log to Azure File: {ex.Message}");
                    Console.WriteLine($"📝 Fallback: Writing to console instead");
                }
            }
        }
    }

    public class AzureFileLoggerProvider : ILoggerProvider
    {
        private readonly string _connectionString;
        private readonly Dictionary<string, AzureFileLoggerService> _loggers = new();

        public AzureFileLoggerProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (!_loggers.ContainsKey(categoryName))
            {
                _loggers[categoryName] = new AzureFileLoggerService(categoryName, _connectionString);
            }
            return _loggers[categoryName];
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }
}
