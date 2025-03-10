using System.Diagnostics;
using iText.Html2pdf;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;
using iTextFormBuilderAPI.Models.APIModels;
using RazorLight;
using RazorLight.Razor;

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

        /// <summary>
        /// Initializes a new instance of the PDFGenerationService class.
        /// </summary>
        /// <param name="templateService">The template service for managing PDF templates.</param>
        public PDFGenerationService(IPdfTemplateService templateService)
        {
            _templateService = templateService;
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
            bool success;
            string message;

            if (!_templateService.TemplateExists(templateName))
            {
                _errorsLogged++;
                _lastPDFGenerationStatus = "Failed - Template not found";
                success = false;
                message = $"Template '{templateName}' does not exist.";
            }
            else
            {
                byte[] pdfBytes = GeneratePdfFromTemplate(templateName, data);
                _pdfsGenerated++;
                _lastPDFGenerationTime = DateTime.UtcNow;
                _lastPDFGenerationStatus = "Success";
                success = true;
                message = "PDF generated successfully.";
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
                PdfBytes = success ? GenerateDummyPdf() : Array.Empty<byte>(), // Dummy PDF data for now
                Message = message,
            };
        }

        /// <summary>
        /// Generates a PDF from a template and data.
        /// </summary>
        /// <param name="templateName">The name of the template to use.</param>
        /// <param name="data">The data to populate the template with.</param>
        /// <returns>The generated PDF as a byte array.</returns>
        private byte[] GeneratePdfFromTemplate(string templateName, object data)
        {
            if (!_templateService.TemplateExists(templateName))
            {
                return Array.Empty<byte>();
            }
            
            // In a real implementation, this would use the template path from the template service
            // to load the template and generate the PDF
            // string templatePath = _templateService.GetTemplatePath(templateName);
            
            return GenerateDummyPdf();
        }
        
        /// <summary>
        /// Generates a dummy PDF for testing purposes.
        /// </summary>
        /// <returns>A byte array representing a dummy PDF.</returns>
        private byte[] GenerateDummyPdf()
        {
            // In a real implementation, this would generate an actual PDF
            // For now, we return a dummy byte array
            return new byte[1] { 0x01 };
        }
    }
}
