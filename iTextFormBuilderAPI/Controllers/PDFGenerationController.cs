using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models.APIModels;
using iTextFormBuilderAPI.Models.HealthAndWellness.TestRazorDataModels;
using iTextFormBuilderAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RazorLight.Razor;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json;

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
        try
        {
            // Check if the template is for TestRazorDataAssessment
            if (request.TemplateName.Contains("TestRazorDataAssessment", StringComparison.OrdinalIgnoreCase))
            {
                // Log the incoming data for debugging
                Console.WriteLine($"Incoming data type: {request.Data.GetType().FullName}");
                
                try {
                    // Convert data to JSON string first
                    string jsonString = System.Text.Json.JsonSerializer.Serialize(request.Data);
                    Console.WriteLine($"Serialized data: {jsonString}");
                    
                    // Then deserialize to the specific model type using Newtonsoft.Json
                    var modelData = Newtonsoft.Json.JsonConvert.DeserializeObject<TestRazorDataInstance>(jsonString);
                    
                    // Use the converted data
                    if (modelData != null)
                    {
                        var result = _pdfGenerationService.GeneratePdf(request.TemplateName, modelData);
                        
                        if (!result.Success)
                        {
                            return BadRequest(new { Message = result.Message });
                        }
                        
                        if (result.PdfBytes != null && result.PdfBytes.Length > 0)
                        {
                            return File(result.PdfBytes, "application/pdf", $"{request.TemplateName}.pdf", true);
                        }
                        else
                        {
                            return BadRequest(new { Message = "Generated PDF has no content" });
                        }
                    }
                    else
                    {
                        return BadRequest(new { Message = "Failed to convert data to the required model type" });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing TestRazorDataAssessment: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    return BadRequest(new { Message = $"Error processing TestRazorDataAssessment: {ex.Message}" });
                }
            }
            
            // For other templates, use the default approach
            var defaultResult = _pdfGenerationService.GeneratePdf(request.TemplateName, request.Data);

            if (!defaultResult.Success)
            {
                return BadRequest(new { Message = defaultResult.Message });
            }

            if (defaultResult.PdfBytes != null && defaultResult.PdfBytes.Length > 0)
            {
                return File(defaultResult.PdfBytes, "application/pdf", $"{request.TemplateName}.pdf", true);
            }
            else
            {
                return BadRequest(new { Message = "Generated PDF has no content" });
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = $"Error processing request: {ex.Message}" });
        }
    }
}

/// <summary>
/// Represents an error response message.
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
}
