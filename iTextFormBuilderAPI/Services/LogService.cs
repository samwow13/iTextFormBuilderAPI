using System.IO;
using System.Text;
using iTextFormBuilderAPI.Interfaces;

namespace iTextFormBuilderAPI.Services;

/// <summary>
/// Service for logging messages to a file in the application's root directory.
/// </summary>
public class LogService : ILogService
{
    private readonly string _logFilePath;
    private readonly object _lockObject = new object();
    
    // Static flag to track if the log has been cleared in the current session
    private static bool _logClearedThisSession = false;

    /// <summary>
    /// Initializes a new instance of the LogService class.
    /// </summary>
    public LogService()
    {
        // Determine the project root directory
        var projectRoot = Directory
            .GetParent(AppContext.BaseDirectory)
            ?.Parent?.Parent?.Parent?.FullName;

        if (projectRoot == null)
        {
            // If we can't determine the project root, use the current directory
            projectRoot = Directory.GetCurrentDirectory();
        }

        // Set the log file path directly in the root directory
        _logFilePath = Path.Combine(projectRoot, $"app_log_{DateTime.Now:yyyy-MM-dd}.log");
        
        // Log the location of the log file
        Console.WriteLine($"Log file will be written to: {_logFilePath}");
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogInfo(string message)
    {
        WriteToLog("INFO", message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogWarning(string message)
    {
        WriteToLog("WARNING", message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogError(string message)
    {
        WriteToLog("ERROR", message);
    }

    /// <summary>
    /// Logs an error message with exception details.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">The exception to log.</param>
    public void LogError(string message, Exception ex)
    {
        var fullMessage = $"{message} - Exception: {ex.Message}\nStackTrace: {ex.StackTrace}";
        WriteToLog("ERROR", fullMessage);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogDebug(string message)
    {
        WriteToLog("DEBUG", message);
    }

    /// <summary>
    /// Writes a message to the log file.
    /// </summary>
    /// <param name="level">The log level.</param>
    /// <param name="message">The message to log.</param>
    private void WriteToLog(string level, string message)
    {
        try
        {
            // Format the log entry
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}\r\n";

            // Write to the log file with a lock to prevent concurrent access issues
            lock (_lockObject)
            {
                // Clear the log file on first write of the session
                if (!_logClearedThisSession)
                {
                    // Create or overwrite the file (clearing previous content)
                    File.WriteAllText(_logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] Log file cleared for new session\r\n", Encoding.UTF8);
                    _logClearedThisSession = true;
                }
                
                // Append the new log entry
                File.AppendAllText(_logFilePath, logEntry, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            // If we can't write to the log file, write to the console
            Console.WriteLine($"Failed to write to log file: {ex.Message}");
            Console.WriteLine($"Original log message: [{level}] {message}");
        }
    }
    
    /// <summary>
    /// Resets the log clearing flag. Can be called when you want to force the log to clear again.
    /// </summary>
    public static void ResetLogClearingFlag()
    {
        _logClearedThisSession = false;
    }
}
