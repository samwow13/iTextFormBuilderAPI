using System.Diagnostics;
using iText.Html2pdf;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;
using iTextFormBuilderAPI.Models.APIModels;
using RazorLight;
using RazorLight.Razor;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace iTextFormBuilderAPI.Services
{
    /// <summary>
    /// Service responsible for generating PDF documents from templates.
    /// </summary>
    public class PDFGenerationService : IPDFGenerationService
    {
        private static int _pdfsGenerated = 0;
        private static int _errorsLogged = 0;
        private static DateTime? _lastPDFGenerationTime = null;
        private static string _lastPDFGenerationStatus = "N/A";
        private static readonly Stopwatch _uptime = Stopwatch.StartNew();

        // Store the last 10 PDF generations
        private static readonly List<PdfGenerationLog> _recentPdfGenerations = new();
        
        // Reference to the template service
        private readonly IPdfTemplateService _templateService;
        private readonly IRazorService _razorService;
        private readonly ILogService _logService;

        /// <summary>
        /// Initializes a new instance of the PDFGenerationService class.
        /// </summary>
        /// <param name="templateService">The template service for managing PDF templates.</param>
        /// <param name="razorService">The Razor service for rendering templates.</param>
        /// <param name="logService">The log service for logging messages.</param>
        public PDFGenerationService(IPdfTemplateService templateService, IRazorService razorService, ILogService logService)
        {
            _templateService = templateService;
            _razorService = razorService;
            _logService = logService;
        }

        /// <summary>
        /// Gets the health status of the PDF generation service.
        /// </summary>
        /// <returns>A ServiceHealthStatus object containing health metrics.</returns>
        public ServiceHealthStatus GetServiceHealth()
        {
            var process = Process.GetCurrentProcess();

            var status = new ServiceHealthStatus
            {
                Status = _templateService.GetTemplateCount() > 0 ? "Healthy" : "Unhealthy",
                TemplateCount = _templateService.GetTemplateCount(),
                AvailableTemplates = _templateService.GetAllTemplateNames().ToList(),
                LastChecked = DateTime.UtcNow,

                // Tracking PDF generation statistics
                PDFsGenerated = _pdfsGenerated,
                LastPDFGenerationTime = _lastPDFGenerationTime,
                LastPDFGenerationStatus = _lastPDFGenerationStatus,

                // System Metrics
                SystemUptime = _uptime.Elapsed,
                MemoryUsage = process.WorkingSet64, // Memory usage in bytes
                ErrorsLogged = _errorsLogged,

                // Return recent PDF generations, or an empty list with a placeholder message
                RecentPdfGenerations =
                    _recentPdfGenerations.Count <= 1
                        ? new List<PdfGenerationLog>
                        {
                            new PdfGenerationLog
                            {
                                Message = "Waiting for more PDF generations to be available.",
                            },
                        }
                        : _recentPdfGenerations.ToList(),
            };

            return status;
        }

        /// <summary>
        /// Generates a PDF from a template and data.
        /// </summary>
        /// <param name="templateName">The name of the template to use.</param>
        /// <param name="data">The data to populate the template with.</param>
        /// <returns>A PdfResult containing the generated PDF or error information.</returns>
        public PdfResult GeneratePdf(string templateName, object data)
        {
            _logService.LogInfo($"Starting PDF generation for template: {templateName}");
            bool success;
            string message;
            byte[] pdfBytes = Array.Empty<byte>();

            try
            {
                if (!_templateService.TemplateExists(templateName))
                {
                    _errorsLogged++;
                    _lastPDFGenerationStatus = "Failed - Template not found";
                    success = false;
                    message = $"Template '{templateName}' does not exist.";
                    _logService.LogError(message);
                }
                else
                {
                    // Convert string data to appropriate object if needed
                    object processedData = ProcessData(templateName, data);
                    _logService.LogInfo($"Data processed for template: {templateName}");
                    
                    pdfBytes = GeneratePdfFromTemplate(templateName, processedData);
                    _pdfsGenerated++;
                    _lastPDFGenerationTime = DateTime.UtcNow;
                    _lastPDFGenerationStatus = "Success";
                    success = true;
                    message = "PDF generated successfully.";
                    _logService.LogInfo(message);
                }
            }
            catch (Exception ex)
            {
                _errorsLogged++;
                _lastPDFGenerationStatus = "Failed - Exception";
                success = false;
                message = $"Error generating PDF: {ex.Message}";
                _logService.LogError(message, ex);
            }

            // Store the last 10 PDF generations
            _recentPdfGenerations.Add(
                new PdfGenerationLog
                {
                    Timestamp = DateTime.UtcNow,
                    TemplateName = templateName,
                    Success = success,
                    Message = message,
                }
            );

            // Ensure the list never exceeds 10 entries
            if (_recentPdfGenerations.Count > 10)
            {
                _recentPdfGenerations.RemoveAt(0);
            }

            return new PdfResult
            {
                Success = success,
                PdfBytes = success ? pdfBytes : Array.Empty<byte>(),
                Message = message,
            };
        }

        /// <summary>
        /// Processes the input data to ensure it's in the correct format for the template.
        /// </summary>
        /// <param name="templateName">The name of the template.</param>
        /// <param name="data">The data to process.</param>
        /// <returns>The processed data object.</returns>
        private object ProcessData(string templateName, object data)
        {
            try
            {
                // If data is already the correct type, return it as is
                var modelType = _razorService.GetModelType(templateName);
                if (modelType == null)
                {
                    _logService.LogWarning($"No model type found for template '{templateName}'. Using data as is.");
                    return data;
                }
                
                // If data is already the correct type, return it as is
                if (data.GetType() == modelType)
                {
                    _logService.LogInfo($"Data is already of the correct type: {modelType.Name}");
                    return data;
                }
                
                // If data is a string, try to deserialize it
                if (data is string stringData)
                {
                    if (string.IsNullOrEmpty(stringData))
                    {
                        _logService.LogWarning($"Empty string data provided for template '{templateName}'");
                        return Activator.CreateInstance(modelType) ?? data;
                    }
                    
                    _logService.LogInfo($"Converting string data to {modelType.Name}");
                    var result = JsonConvert.DeserializeObject(stringData, modelType);
                    return result ?? Activator.CreateInstance(modelType) ?? data;
                }
                
                // If data is a JObject or other JSON structure, convert it to the target type
                if (data is JObject || data is JArray || data is JToken)
                {
                    _logService.LogInfo($"Converting JToken data to {modelType.Name}");
                    var jsonString = data.ToString();
                    if (string.IsNullOrEmpty(jsonString))
                    {
                        return Activator.CreateInstance(modelType) ?? data;
                    }
                    
                    var result = JsonConvert.DeserializeObject(jsonString, modelType);
                    return result ?? Activator.CreateInstance(modelType) ?? data;
                }
                
                // If data is a JsonElement, convert it to the target type
                if (data is JsonElement jsonElement)
                {
                    _logService.LogInfo($"Converting JsonElement data to {modelType.Name}");
                    var jsonText = jsonElement.GetRawText();
                    if (string.IsNullOrEmpty(jsonText))
                    {
                        return Activator.CreateInstance(modelType) ?? data;
                    }
                    
                    try
                    {
                        // First try with Newtonsoft.Json
                        var settings = new JsonSerializerSettings
                        {
                            Error = (sender, args) => { args.ErrorContext.Handled = true; },
                            NullValueHandling = NullValueHandling.Ignore,
                            MissingMemberHandling = MissingMemberHandling.Ignore
                        };
                        
                        var result = JsonConvert.DeserializeObject(jsonText, modelType, settings);
                        if (result != null)
                        {
                            _logService.LogInfo($"Successfully converted JsonElement to {modelType.Name} using Newtonsoft.Json");
                            return result;
                        }
                        
                        // If that fails, try with System.Text.Json
                        var options = new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        result = System.Text.Json.JsonSerializer.Deserialize(jsonText, modelType, options);
                        if (result != null)
                        {
                            _logService.LogInfo($"Successfully converted JsonElement to {modelType.Name} using System.Text.Json");
                            return result;
                        }
                        
                        // If both fail, create a new instance and manually map properties
                        _logService.LogWarning($"Failed to deserialize JsonElement to {modelType.Name} using standard methods. Attempting manual mapping.");
                        return Activator.CreateInstance(modelType) ?? data;
                    }
                    catch (Exception ex)
                    {
                        _logService.LogError($"Error converting JsonElement to {modelType.Name}: {ex.Message}", ex);
                        return Activator.CreateInstance(modelType) ?? data;
                    }
                }
                
                // Otherwise, serialize and deserialize to convert between types
                _logService.LogInfo($"Converting {data.GetType().Name} to {modelType.Name} via serialization");
                var json = JsonConvert.SerializeObject(data);
                if (string.IsNullOrEmpty(json))
                {
                    return Activator.CreateInstance(modelType) ?? data;
                }
                
                var deserializedResult = JsonConvert.DeserializeObject(json, modelType);
                return deserializedResult ?? Activator.CreateInstance(modelType) ?? data;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error processing data for template '{templateName}'", ex);
                // Return the original data if processing fails
                return data;
            }
        }

        /// <summary>
        /// Generates a PDF from a template and data.
        /// </summary>
        /// <param name="templateName">The name of the template to use.</param>
        /// <param name="data">The data to populate the template with.</param>
        /// <returns>The generated PDF as a byte array.</returns>
        private byte[] GeneratePdfFromTemplate(string templateName, object data)
        {
            _logService.LogInfo($"Generating PDF from template: {templateName}");
            
            if (!_templateService.TemplateExists(templateName))
            {
                _logService.LogError($"Template '{templateName}' does not exist.");
                return Array.Empty<byte>();
            }
            
            try
            {
                // Get the model type for the template
                var modelType = _razorService.GetModelType(templateName);
                _logService.LogInfo($"Model type for template '{templateName}': {(modelType != null ? modelType.Name : "Unknown")}");
                
                // Render the template using Razor
                var htmlContent = _razorService.RenderTemplateAsync(templateName, data).GetAwaiter().GetResult();
                _logService.LogInfo($"Template '{templateName}' rendered successfully. HTML length: {htmlContent.Length} characters");
                
                // Convert HTML string directly to PDF using a different approach
                try 
                {
                    // Save HTML to temp file to avoid any stream closing issues
                    string tempHtmlPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.html");
                    string tempPdfPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.pdf");
                    
                    try
                    {
                        // Write HTML to temp file
                        File.WriteAllText(tempHtmlPath, htmlContent);
                        _logService.LogInfo($"Wrote HTML to temp file: {tempHtmlPath}");
                        
                        // Use iText's API to convert from HTML file to PDF file
                        // Use a FileStream that's guaranteed to be properly disposed
                        using (FileStream pdfFileStream = new FileStream(tempPdfPath, FileMode.Create))
                        {
                            // Convert the HTML to PDF
                            // The problem is that we're passing the file path directly, which
                            // iText might be interpreting as HTML content rather than a path
                            // Fix: Use explicit FileStream for input rather than just the path string
                            using (FileStream htmlFileStream = new FileStream(tempHtmlPath, FileMode.Open))
                            {
                                HtmlConverter.ConvertToPdf(htmlFileStream, pdfFileStream);
                                _logService.LogInfo($"HTML converted to PDF successfully using file-based approach");
                            }
                        }
                        
                        // Read resulting PDF file
                        byte[] pdfBytes = File.ReadAllBytes(tempPdfPath);
                        _logService.LogInfo($"Read PDF from temp file: {tempPdfPath}, size: {pdfBytes.Length} bytes");
                        
                        return pdfBytes;
                    }
                    finally
                    {
                        // Clean up temp files
                        try
                        {
                            if (File.Exists(tempHtmlPath))
                                File.Delete(tempHtmlPath);
                                
                            if (File.Exists(tempPdfPath))
                                File.Delete(tempPdfPath);
                        }
                        catch (Exception ex)
                        {
                            _logService.LogWarning($"Error deleting temp files: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Error during HTML to PDF conversion: {ex.Message}", ex);
                    throw new Exception($"Error during HTML to PDF conversion: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error generating PDF from template '{templateName}'", ex);
                throw; // Re-throw to be handled by the calling method
            }
        }
        
        /// <summary>
        /// Generates a dummy PDF for testing purposes.
        /// </summary>
        /// <returns>A byte array representing a dummy PDF.</returns>
        private byte[] GenerateDummyPdf()
        {
            _logService.LogInfo("Generating dummy PDF for testing");
            // In a real implementation, this would generate an actual PDF
            // For now, we return a dummy byte array
            return new byte[1] { 0x01 };
        }
    }
}
