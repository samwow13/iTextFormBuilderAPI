using System.Text.Json;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models.APIModels;
using iTextFormBuilderAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RazorLight.Razor;
using Swashbuckle.AspNetCore.Annotations;

namespace iTextFormBuilderAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PDFGenerationController : ControllerBase
{
    private readonly IPDFGenerationService _pdfGenerationService;
    private readonly IRazorService _razorService;
    private readonly ILogService _logService;

    /// <summary>
    /// Initializes a new instance of the PDFGenerationController class.
    /// </summary>
    /// <param name="pdfGenerationService">The PDF generation service.</param>
    /// <param name="razorService">The Razor templating service.</param>
    /// <param name="logService">The logging service.</param>
    public PDFGenerationController(
        IPDFGenerationService pdfGenerationService,
        IRazorService razorService,
        ILogService logService
    )
    {
        _pdfGenerationService = pdfGenerationService;
        _razorService = razorService;
        _logService = logService;
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
    ///   "data": { "customerName": "John Doe", "amount": 100 },
    ///   "returnAsBase64": false
    /// }
    /// ```
    ///
    /// **Available templates:**
    /// - Hotline\\HotlineTesting
    /// </remarks>
    /// <param name="request">The request containing the template name and data.</param>
    /// <returns>A PDF file if ReturnAsBase64 is false, or a JSON response with base64 string if ReturnAsBase64 is true.</returns>
    /// <response code="200">Returns the generated PDF file or base64 string.</response>
    /// <response code="400">Invalid request or template not found.</response>
    [HttpPost("generate")]
    [Produces("application/json", "application/pdf")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(Base64PdfResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [SwaggerOperation(
        Summary = "Generate a PDF from a template",
        Description = "Use this endpoint to generate a PDF file using a predefined template and structured data. Optionally returns as base64 string."
    )]
    public IActionResult GeneratePdf([FromBody] PdfRequest request)
    {
        try
        {
            _logService.LogInfo(
                $"Processing PDF generation request for template: {request.TemplateName}"
            );
            _logService.LogInfo($"Incoming data type: {request.Data.GetType().FullName}");
            _logService.LogInfo($"Return as base64: {request.ReturnAsBase64}");

            // Try to obtain the model type for this template
            var modelType = _razorService.GetModelType(request.TemplateName);

            if (modelType != null)
            {
                _logService.LogInfo(
                    $"Found model type {modelType.FullName} for template {request.TemplateName}"
                );

                try
                {
                    // Convert data to JSON string first
                    string jsonString = System.Text.Json.JsonSerializer.Serialize(request.Data);
                    _logService.LogDebug($"Serialized data: {jsonString}");

                    // Then deserialize to the specific model type using Newtonsoft.Json
                    var modelData = JsonConvert.DeserializeObject(jsonString, modelType);

                    // Use the converted data
                    if (modelData != null)
                    {
                        var result = _pdfGenerationService.GeneratePdf(
                            request.TemplateName,
                            modelData
                        );

                        if (!result.Success)
                        {
                            return BadRequest(new ErrorResponse { Message = result.Message });
                        }

                        if (result.PdfBytes != null && result.PdfBytes.Length > 0)
                        {
                            if (request.ReturnAsBase64)
                            {
                                // Return the PDF as a base64 string
                                string base64String = Convert.ToBase64String(result.PdfBytes);
                                return Ok(new Base64PdfResponse
                                {
                                    Base64Data = base64String,
                                    FileName = $"{request.TemplateName}.pdf"
                                });
                            }
                            else
                            {
                                // Return as file download
                                return File(
                                    result.PdfBytes,
                                    "application/pdf",
                                    $"{request.TemplateName}.pdf",
                                    true
                                );
                            }
                        }
                        else
                        {
                            return BadRequest(
                                new ErrorResponse { Message = "Generated PDF has no content" }
                            );
                        }
                    }
                    else
                    {
                        return BadRequest(
                            new ErrorResponse
                            {
                                Message = "Failed to convert data to the required model type",
                            }
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError(
                        $"Error converting data to model type {modelType.FullName}",
                        ex
                    );
                    return BadRequest(
                        new ErrorResponse { Message = $"Error processing data: {ex.Message}" }
                    );
                }
            }
            else
            {
                _logService.LogInfo(
                    $"No specific model type found for template {request.TemplateName}, using default approach"
                );

                // Notify the user that we're proceeding without a specific model type
                // This could affect data binding and template rendering
                _logService.LogWarning(
                    $"Model injection will not be completed for template '{request.TemplateName}' as the expected model type was not found"
                );

                // Continue with default approach but provide warning in result message
                var defaultResult = _pdfGenerationService.GeneratePdf(
                    request.TemplateName,
                    request.Data
                );

                if (!defaultResult.Success)
                {
                    return BadRequest(new ErrorResponse { Message = defaultResult.Message });
                }

                if (defaultResult.PdfBytes != null && defaultResult.PdfBytes.Length > 0)
                {
                    // Add a warning header to inform the client about the model mismatch
                    Response.Headers.Append(
                        "X-Model-Warning",
                        "Model type not found for template. Data may not be properly bound."
                    );

                    if (request.ReturnAsBase64)
                    {
                        // Return the PDF as a base64 string
                        string base64String = Convert.ToBase64String(defaultResult.PdfBytes);
                        return Ok(new Base64PdfResponse
                        {
                            Base64Data = base64String,
                            FileName = $"{request.TemplateName}.pdf"
                        });
                    }
                    else
                    {
                        // Return as file download
                        return File(
                            defaultResult.PdfBytes,
                            "application/pdf",
                            $"{request.TemplateName}.pdf",
                            true
                        );
                    }
                }
                else
                {
                    return BadRequest(
                        new ErrorResponse
                        {
                            Message =
                                "Generated PDF has no content. This may be due to the model type not being found for the template.",
                        }
                    );
                }
            }

            // This code is now handled in the else block above and will not be executed
        }
        catch (Exception ex)
        {
            _logService.LogError("Error in GeneratePdf endpoint", ex);
            return BadRequest(
                new ErrorResponse { Message = $"Error processing request: {ex.Message}" }
            );
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

/// <summary>
/// Represents a response containing a PDF as a base64 encoded string.
/// </summary>
public class Base64PdfResponse
{
    /// <summary>
    /// The base64 encoded PDF data.
    /// </summary>
    public string Base64Data { get; set; } = string.Empty;

    /// <summary>
    /// The suggested filename for the PDF.
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
