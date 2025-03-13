using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Diagnostics;

namespace iTextFormBuilderAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HealthCheckController : ControllerBase
{
    private readonly IPDFGenerationService _pdfGenerationService;
    private readonly ISystemMetricsService _metricsService;
    private readonly IRazorService _razorService;
    private readonly Stopwatch _requestTimer = new();

    /// <summary>
    /// Initializes a new instance of the HealthCheckController.
    /// </summary>
    /// <param name="pdfGenerationService">The PDF generation service.</param>
    /// <param name="metricsService">The system metrics service.</param>
    /// <param name="razorService">The Razor templating service.</param>
    public HealthCheckController(
        IPDFGenerationService pdfGenerationService, 
        ISystemMetricsService metricsService,
        IRazorService razorService)
    {
        _pdfGenerationService = pdfGenerationService;
        _metricsService = metricsService;
        _razorService = razorService;
    }

    /// <summary>
    /// Retrieves the health status of the PDF Generation Service.
    /// </summary>
    /// <returns>Returns service health status along with relevant system metrics.</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get Service Health Status",
        Description = "Checks the health of the PDF Generation Service and provides various system metrics including CPU usage, response times, and template performance."
    )]
    [ProducesResponseType(typeof(ServiceHealthStatus), 200)]
    [ProducesResponseType(typeof(ServiceHealthStatus), 500)]
    public IActionResult GetHealthStatus()
    {
        _requestTimer.Start();
        _metricsService.StartRequest();
        
        try
        {
            // Get health status from the PDF generation service
            ServiceHealthStatus healthStatus = _pdfGenerationService.GetServiceHealth();

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
                LastPDFGenerationStatus = $"Error during health check: {ex.Message}"
            };
            
            return StatusCode(500, errorStatus);
        }
    }
}
