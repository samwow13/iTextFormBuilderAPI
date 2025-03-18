using System.Text;
using iTextFormBuilderAPI.Interfaces;
using NLog;

namespace iTextFormBuilderAPI.Services;

/// <summary>
/// Service for logging messages using NLog while maintaining the original LogService interface.
/// </summary>
public class LogService : ILogService
{
    private readonly NLog.ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the LogService class.
    /// </summary>
    public LogService()
    {
        // Get a logger with the name of the current class
        _logger = LogManager.GetLogger(typeof(LogService).FullName);

        // Log the initialization of the service
        _logger.Info("LogService initialized. Using NLog for logging.");
    }

    /// <summary>
    /// Logs an information message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogInfo(string message)
    {
        _logger.Info(message);
    }

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogWarning(string message)
    {
        _logger.Warn(message);
    }

    /// <summary>
    /// Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogError(string message)
    {
        _logger.Error(message);
    }

    /// <summary>
    /// Logs an error message with exception details.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="ex">The exception to log.</param>
    public void LogError(string message, Exception ex)
    {
        _logger.Error(ex, message);
    }

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void LogDebug(string message)
    {
        _logger.Debug(message);
    }

    /// <summary>
    /// Resets the log clearing flag. This method is kept for backwards compatibility but does nothing with NLog.
    /// </summary>
    public static void ResetLogClearingFlag()
    {
        // This method does nothing with NLog as it's not needed
        // It's kept for backwards compatibility
        LogManager
            .GetLogger(typeof(LogService).FullName)
            .Info("ResetLogClearingFlag called, but this has no effect when using NLog.");
    }
}
