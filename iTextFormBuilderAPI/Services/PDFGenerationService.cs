using System.Diagnostics;
using System.Text;
using iText.Html2pdf;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;
using iTextFormBuilderAPI.Models.APIModels;
using System.IO;

namespace iTextFormBuilderAPI.Services
{
    /// <summary>
    /// Service responsible for generating PDF documents from templates.
    /// </summary>
    public class PDFGenerationService(IPdfTemplateService templateService, IRazorService razorService, ILogService logService) : IPDFGenerationService
    {
        private static int _pdfsGenerated = 0;
        private static int _errorsLogged = 0;
        private static DateTime? _lastPDFGenerationTime = null;
        private static string _lastPDFGenerationStatus = "N/A";
        private static readonly Stopwatch _uptime = Stopwatch.StartNew();
        private readonly string _globalStylesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "globalStyles.css");

        // Store the last 10 PDF generations
        private static readonly List<PdfGenerationLog> _recentPdfGenerations = [];

        // Services injected through the primary constructor
        private readonly IPdfTemplateService _templateService = templateService;
        private readonly IRazorService _razorService = razorService;
        private readonly ILogService _logService = logService;

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
            byte[] pdfBytes = [];

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

                    pdfBytes = GeneratePdfFromTemplate(templateName, processedData)
                        .GetAwaiter()
                        .GetResult();
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
                PdfBytes = success ? pdfBytes : [],
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
                // Get the model type for the template
                var modelType = _razorService.GetModelType(templateName);
                if (modelType == null)
                {
                    _logService.LogWarning(
                        $"No model type found for template '{templateName}'. Using data as is."
                    );
                    return data;
                }
                return data;
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error processing data for template '{templateName}'", ex);
                return data;
            }
        }

        /// <summary>
        /// Generates a PDF from a template and data.
        /// </summary>
        /// <param name="templateName">The name of the template to use.</param>
        /// <param name="data">The data to populate the template with.</param>
        /// <returns>The generated PDF as a byte array.</returns>
        private async Task<byte[]> GeneratePdfFromTemplate(string templateName, object data)
        {
            _logService.LogInfo($"Generating PDF from template: {templateName}");

            if (!_templateService.TemplateExists(templateName))
            {
                _logService.LogError($"Template '{templateName}' does not exist.");
                return [];
            }

            try
            {
                // Get the model type for the template
                var modelType = _razorService.GetModelType(templateName);
                _logService.LogInfo(
                    $"Model type for template '{templateName}': {(modelType != null ? modelType.Name : "Unknown")}"
                );

                // Render the template using Razor
                var htmlContent = await _razorService.RenderTemplateAsync(templateName, data);
                _logService.LogInfo(
                    $"Template '{templateName}' rendered successfully. HTML length: {htmlContent.Length} characters"
                );

                // Inject global styles into the HTML content
                htmlContent = InjectGlobalStyles(htmlContent);
                _logService.LogInfo("Global styles injected into HTML content");

                // Convert HTML directly to PDF using memory streams
                await using var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(htmlContent));
                await using var pdfStream = new MemoryStream();

                HtmlConverter.ConvertToPdf(htmlStream, pdfStream);
                _logService.LogInfo($"HTML converted to PDF successfully");

                // Get the PDF bytes
                return pdfStream.ToArray();
            }
            catch (Exception ex)
            {
                _logService.LogError(
                    $"Error generating PDF from template '{templateName}': {ex.Message}",
                    ex
                );
                throw; // Re-throw to be handled by the calling method
            }
        }

        /// <summary>
        /// Injects global CSS styles into the HTML content before PDF generation.
        /// </summary>
        /// <param name="htmlContent">The HTML content to inject styles into</param>
        /// <returns>HTML content with injected global styles</returns>
        /// <remarks>
        /// Reads styles from globalStyles.css and injects them into the head section.
        /// If the styles file is not found or head tag is missing, returns original content.
        /// </remarks>
        private string InjectGlobalStyles(string htmlContent)
        {
            try
            {
                if (!File.Exists(_globalStylesPath))
                {
                    _logService.LogWarning($"Global styles file not found: {_globalStylesPath}");
                    return htmlContent;
                }

                var cssContent = File.ReadAllText(_globalStylesPath);

                // Find the closing head tag
                const string headEndTag = "</head>";
                var headEndIndex = htmlContent.IndexOf(
                    headEndTag,
                    StringComparison.OrdinalIgnoreCase
                );

                if (headEndIndex == -1)
                {
                    _logService.LogWarning("No </head> tag found in HTML content");
                    return htmlContent;
                }

                // Inject the CSS content within a style tag before the </head>
                var styleTag = $"<style>{cssContent}</style>";
                return htmlContent.Insert(headEndIndex, styleTag);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error injecting global styles: {ex.Message}", ex);
                return htmlContent;
            }
        }
    }
}
