using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models.APIModels;
using iTextFormBuilderAPI.Services;
using Microsoft.AspNetCore.Mvc;
using RazorLight.Razor;
using Swashbuckle.AspNetCore.Annotations;

namespace iTextFormBuilderAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PDFGenerationController : ControllerBase
{
    private readonly IPDFGenerationService _pdfGenerationService;

    public PDFGenerationController(IPDFGenerationService pdfGenerationService)
    {
        _pdfGenerationService = pdfGenerationService;
    }

    /// <summary>
    /// Generates a PDF based on a specified template and data.
    /// </summary>
    /// <remarks>
    /// This endpoint accepts a template name and associated data to generate a customized PDF document.
    /// The template must exist in the system, and the data structure should match what the template expects.
    /// 
    /// **Example Request (JSON Body):**
    ///
    /// ```json
    /// {
    ///   "templateName": "Invoice",
    ///   "data": { "customerName": "John Doe", "amount": 100 }
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">The request containing the template name and data.</param>
    /// <returns>A PDF file if successful, or an error message if the template is not found.</returns>
    /// <response code="200">Returns the generated PDF file.</response>
    /// <response code="400">Invalid request or template not found.</response>
    [HttpPost("generate")]
    [Produces("application/json", "application/pdf")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [SwaggerOperation(
        Summary = "Generate a PDF from a template",
        Description = "Use this endpoint to generate a PDF file using a predefined template and structured data."
    )]
    public IActionResult GeneratePdf([FromBody] PdfRequest request)
    {
        var result = _pdfGenerationService.GeneratePdf(request.TemplateName, request.Data);

        if (!result.Success)
        {
            return BadRequest(new { Message = result.Message });
        }

        return File(result.PdfBytes, "application/pdf", $"{request.TemplateName}.pdf", true);
    }
}

/// <summary>
/// Represents an error response message.
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}
