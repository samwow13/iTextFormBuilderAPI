using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Diagnostics;

namespace iTextFormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthCheckController : ControllerBase
    {
        private readonly IPDFGenerationService _pdfGenerationService;
        private readonly ISystemMetricsService _metricsService;
        private readonly IRazorService _razorService;
        private readonly IDebugCshtmlInjectionService _debugService;
        private readonly Stopwatch _requestTimer = new Stopwatch();

        /// <summary>
        /// Initializes a new instance of the HealthCheckController.
        /// </summary>
        /// <param name="pdfGenerationService">The PDF generation service.</param>
        /// <param name="metricsService">The system metrics service.</param>
        /// <param name="razorService">The Razor templating service.</param>
        /// <param name="debugService">The debug CSHTML injection service.</param>
        public HealthCheckController(
            IPDFGenerationService pdfGenerationService,
            ISystemMetricsService metricsService,
            IRazorService razorService,
            IDebugCshtmlInjectionService debugService)
        {
            _pdfGenerationService = pdfGenerationService;
            _metricsService = metricsService;
            _razorService = razorService;
            _debugService = debugService;
        }

        /// <summary>
        /// Retrieves a basic health check status of the API.
        /// </summary>
        /// <returns>Returns a simple health status in the specified format.</returns>
        [HttpGet("healthcheck")]
        [SwaggerOperation(
            Summary = "Get Basic Health Status",
            Description = "Returns the health check in the required format.")]
        [ProducesResponseType(typeof(HealthCheckResponse), 200)]
        public IActionResult GetHealthCheck()
        {
            // Return the exact format specified
            var healthResponse = new HealthCheckResponse
            {
                Id = "iTextFormBuilderAPI",
                Type = "HealthCheck",
                Data = new HealthCheckData { Version = "1.00" }
            };

            return Ok(healthResponse);
        }

        /// <summary>
        /// Retrieves detailed API status including PDF Generation Service health.
        /// </summary>
        /// <returns>Returns service health status along with relevant system metrics.</returns>
        [HttpGet("APIStatusReport")]
        [SwaggerOperation(
            Summary = "Get Detailed API Status",
            Description = "Checks the health of the PDF Generation Service and provides various system metrics including CPU usage, response times, and template performance.")]
        [ProducesResponseType(typeof(ServiceHealthStatus), 200)]
        [ProducesResponseType(typeof(ServiceHealthStatus), 500)]
        public IActionResult GetAPIStatus()
        {
            _requestTimer.Start();
            _metricsService.StartRequest();

            try
            {
                // Get health status from the PDF generation service
                var healthStatus = _pdfGenerationService.GetServiceHealth();

                // Add debug mode status
                healthStatus.DebugModeActive = _debugService.ModelDebuggingEnabled;

                // Record template performance for the health check itself (monitoring overhead)
                _requestTimer.Stop();
                _metricsService.EndRequest(_requestTimer.ElapsedMilliseconds);

                // Determine HTTP response based on health status
                if (healthStatus.Status == "Healthy")
                {
                    return Ok(healthStatus);
                }
                else
                {
                    return StatusCode(500, healthStatus);
                }
            }
            catch (Exception ex)
            {
                _requestTimer.Stop();
                _metricsService.EndRequest(_requestTimer.ElapsedMilliseconds);

                // Create a minimal health status with error information
                var errorStatus = new ServiceHealthStatus
                {
                    Status = "Error",
                    LastChecked = DateTime.UtcNow,
                    LastPDFGenerationStatus = $"Error during health check: {ex.Message}",
                    DebugModeActive = _debugService.ModelDebuggingEnabled
                };

                return StatusCode(500, errorStatus);
            }
        }
    }
}
