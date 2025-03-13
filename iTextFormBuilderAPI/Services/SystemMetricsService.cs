using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using iTextFormBuilderAPI.Interfaces;

namespace iTextFormBuilderAPI.Services
{
    /// <summary>
    /// Service for monitoring system metrics like CPU usage, memory, and request counts.
    /// </summary>
    public class SystemMetricsService : ISystemMetricsService, IDisposable
    {
        private readonly ILogService _logService;
        private readonly object? _cpuCounter;
        private readonly Stopwatch _uptime = Stopwatch.StartNew();
        private readonly ConcurrentDictionary<string, double> _responseTimesMs = new();
        private readonly ConcurrentDictionary<string, int> _templateUsageCounts = new();
        private readonly Timer _aggregationTimer;
        
        private double _cpuUsage = 0;
        private int _currentConcurrentRequests = 0;
        private double _cumulativeResponseTimeMs = 0;
        private int _totalResponseCount = 0;
        private bool _disposedValue;
        private bool _canUseCpuCounter = false;

        /// <summary>
        /// Initializes a new instance of the SystemMetricsService class.
        /// </summary>
        /// <param name="logService">The log service for logging messages.</param>
        public SystemMetricsService(ILogService logService)
        {
            _logService = logService;
            _logService.LogInfo("Initializing SystemMetricsService");

            try
            {
                // Try to use CPU counter through reflection to avoid direct dependency on PerformanceCounter
                var counterType = Type.GetType("System.Diagnostics.PerformanceCounter, System.Diagnostics.PerformanceCounter");
                if (counterType != null)
                {
                    _cpuCounter = Activator.CreateInstance(counterType, "Processor", "% Processor Time", "_Total");
                    
                    // Call NextValue method using reflection
                    var nextValueMethod = counterType.GetMethod("NextValue");
                    if (nextValueMethod != null && _cpuCounter != null)
                    {
                        nextValueMethod.Invoke(_cpuCounter, null); // First call always returns 0, so call it immediately
                        _canUseCpuCounter = true;
                    }
                }
                else
                {
                    _logService.LogWarning("PerformanceCounter type not found. CPU usage monitoring will be disabled.");
                }
            }
            catch (Exception ex)
            {
                _logService.LogWarning($"Failed to initialize CPU counter: {ex.Message}");
            }

            // Timer to periodically sample CPU usage every 5 seconds
            _aggregationTimer = new Timer(UpdateMetrics, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Gets the current CPU usage percentage.
        /// </summary>
        public double CpuUsage => _cpuUsage;

        /// <summary>
        /// Gets the current memory usage in megabytes.
        /// </summary>
        public double MemoryUsageInMB => Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0);

        /// <summary>
        /// Gets the system uptime.
        /// </summary>
        public TimeSpan SystemUptime => _uptime.Elapsed;

        /// <summary>
        /// Gets the current number of concurrent requests being handled.
        /// </summary>
        public int ConcurrentRequestsHandled => _currentConcurrentRequests;

        /// <summary>
        /// Gets the average response time for all requests in milliseconds.
        /// </summary>
        public double AverageResponseTime => _totalResponseCount > 0 
            ? _cumulativeResponseTimeMs / _totalResponseCount 
            : 0;

        /// <summary>
        /// Gets the template performance metrics (average render time in milliseconds per template).
        /// </summary>
        public ConcurrentDictionary<string, double> TemplatePerformance => _responseTimesMs;

        /// <summary>
        /// Gets the template usage statistics (count of uses per template).
        /// </summary>
        public ConcurrentDictionary<string, int> TemplateUsageStatistics => _templateUsageCounts;

        /// <summary>
        /// Records the start of a new request processing.
        /// </summary>
        public void StartRequest()
        {
            Interlocked.Increment(ref _currentConcurrentRequests);
        }

        /// <summary>
        /// Records the completion of a request processing.
        /// </summary>
        /// <param name="elapsedMs">The elapsed time in milliseconds for processing the request.</param>
        public void EndRequest(double elapsedMs)
        {
            Interlocked.Decrement(ref _currentConcurrentRequests);
            Interlocked.Increment(ref _totalResponseCount);
            Interlocked.Exchange(ref _cumulativeResponseTimeMs, _cumulativeResponseTimeMs + elapsedMs);
        }

        /// <summary>
        /// Records the performance metrics for a template rendering operation.
        /// </summary>
        /// <param name="templateName">The name of the template.</param>
        /// <param name="renderTimeMs">The time taken to render the template in milliseconds.</param>
        public void RecordTemplatePerformance(string templateName, double renderTimeMs)
        {
            // Update average render time for this template
            _responseTimesMs.AddOrUpdate(
                templateName,
                renderTimeMs, // If new, use this value
                (_, oldValue) => (oldValue + renderTimeMs) / 2 // If exists, compute new average
            );

            // Increment usage count for this template
            _templateUsageCounts.AddOrUpdate(
                templateName,
                1, // If new, set to 1
                (_, oldValue) => oldValue + 1 // If exists, increment
            );
        }

        /// <summary>
        /// Updates metrics periodically (CPU usage).
        /// </summary>
        private void UpdateMetrics(object? state)
        {
            try
            {
                if (_canUseCpuCounter && _cpuCounter != null)
                {
                    var type = _cpuCounter.GetType();
                    var nextValueMethod = type.GetMethod("NextValue");
                    if (nextValueMethod != null)
                    {
                        var result = nextValueMethod.Invoke(_cpuCounter, null);
                        if (result != null)
                        {
                            _cpuUsage = Convert.ToDouble(result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.LogError("Error updating system metrics", ex);
            }
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _aggregationTimer?.Dispose();
                    
                    if (_canUseCpuCounter && _cpuCounter != null)
                    {
                        // Try to dispose the counter using reflection
                        try 
                        {
                            var type = _cpuCounter.GetType();
                            var disposeMethod = type.GetMethod("Dispose", Type.EmptyTypes);
                            disposeMethod?.Invoke(_cpuCounter, null);
                        }
                        catch (Exception ex)
                        {
                            _logService.LogWarning($"Error disposing CPU counter: {ex.Message}");
                        }
                    }
                }

                _disposedValue = true;
            }
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
