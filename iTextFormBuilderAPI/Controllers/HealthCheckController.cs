using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;

namespace iTextFormBuilderAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HealthCheckController : ControllerBase
{
    private readonly IPDFGenerationService _pdfGenerationService;

    public HealthCheckController(IPDFGenerationService pdfGenerationService)
    {
        _pdfGenerationService = pdfGenerationService;
    }

    /// <summary>
    /// Retrieves the health status of the PDF Generation Service.
    /// </summary>
    /// <returns>Returns service health status along with relevant system metrics.</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get Service Health Status",
        Description = "Checks the health of the PDF Generation Service and provides various system metrics."
    )]
    [ProducesResponseType(typeof(ServiceHealthStatus), 200)]
    [ProducesResponseType(typeof(ServiceHealthStatus), 500)]
    public IActionResult GetHealthStatus()
    {
        ServiceHealthStatus healthStatus = _pdfGenerationService.GetServiceHealth();

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
}
