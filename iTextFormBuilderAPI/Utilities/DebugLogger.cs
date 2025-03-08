using System;
using System.IO;
using System.Text;

namespace iTextFormBuilderAPI.Utilities
{
    /// <summary>
    /// Simple debug logger to write diagnostic information to a file
    /// </summary>
    public static class DebugLogger
    {
        private static readonly string LogFilePath = Path.Combine(
            Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? string.Empty,
            "debug_logs.txt"
        );

        /// <summary>
        /// Log a message to the debug log file
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void Log(string message)
        {
            try
            {
                // Create timestamp
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"[{timestamp}] {message}{Environment.NewLine}";

                // Append to log file
                File.AppendAllText(LogFilePath, logEntry);
            }
            catch (Exception ex)
            {
                // If logging fails, write to console as fallback
                Console.WriteLine($"Error writing to debug log: {ex.Message}");
            }
        }

        /// <summary>
        /// Log an exception to the debug log file
        /// </summary>
        /// <param name="ex">Exception to log</param>
        /// <param name="context">Additional context information</param>
        public static void LogException(Exception ex, string context = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"EXCEPTION in {context}:");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine($"Inner Exception: {ex.InnerException.Message}");
                sb.AppendLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
            }

            Log(sb.ToString());
        }

        /// <summary>
        /// Clear the debug log file
        /// </summary>
        public static void ClearLog()
        {
            try
            {
                File.WriteAllText(LogFilePath, $"Log started at {DateTime.Now}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing debug log: {ex.Message}");
            }
        }
    }
}
