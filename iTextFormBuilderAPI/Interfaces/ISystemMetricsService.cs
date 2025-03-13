using System;
using System.Collections.Concurrent;

namespace iTextFormBuilderAPI.Interfaces
{
    /// <summary>
    /// Interface for a service that monitors system metrics and performance.
    /// </summary>
    public interface ISystemMetricsService
    {
        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        double CpuUsage { get; }

        /// <summary>
        /// Gets the current memory usage in bytes.
        /// </summary>
        long MemoryUsage { get; }

        /// <summary>
        /// Gets the system uptime.
        /// </summary>
        TimeSpan SystemUptime { get; }

        /// <summary>
        /// Gets the current number of concurrent requests being handled.
        /// </summary>
        int ConcurrentRequestsHandled { get; }

        /// <summary>
        /// Gets the average response time for all requests in milliseconds.
        /// </summary>
        double AverageResponseTime { get; }

        /// <summary>
        /// Gets the template performance metrics (average render time in milliseconds per template).
        /// </summary>
        ConcurrentDictionary<string, double> TemplatePerformance { get; }

        /// <summary>
        /// Gets the template usage statistics (count of uses per template).
        /// </summary>
        ConcurrentDictionary<string, int> TemplateUsageStatistics { get; }

        /// <summary>
        /// Records the start of a new request processing.
        /// </summary>
        void StartRequest();

        /// <summary>
        /// Records the completion of a request processing.
        /// </summary>
        /// <param name="elapsedMs">The elapsed time in milliseconds for processing the request.</param>
        void EndRequest(double elapsedMs);

        /// <summary>
        /// Records the performance metrics for a template rendering operation.
        /// </summary>
        /// <param name="templateName">The name of the template.</param>
        /// <param name="renderTimeMs">The time taken to render the template in milliseconds.</param>
        void RecordTemplatePerformance(string templateName, double renderTimeMs);
    }
}
