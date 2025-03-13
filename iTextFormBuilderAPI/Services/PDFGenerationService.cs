using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using iText.Html2pdf;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;
using iTextFormBuilderAPI.Models.APIModels;
using Newtonsoft.Json;
using System.Reflection;

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
        private readonly string _globalStylesPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Templates",
            "globalStyles.css"
        );

        // Store the last 10 PDF generations
        private static readonly List<PdfGenerationLog> _recentPdfGenerations = [];

        // Services injected through the constructor
        private readonly IPdfTemplateService _templateService;
        private readonly IRazorService _razorService;
        private readonly ILogService _logService;
        private readonly IDebugCshtmlInjectionService _debugService;
        private readonly ISystemMetricsService _metricsService;

        /// <summary>
        /// Initializes a new instance of the PDFGenerationService class.
        /// </summary>
        /// <param name="templateService">Service for managing PDF templates.</param>
        /// <param name="razorService">Service for rendering Razor templates.</param>
        /// <param name="logService">Service for logging messages.</param>
        /// <param name="debugService">Service for injecting debug information into rendered templates.</param>
        /// <param name="metricsService">Service for tracking system and performance metrics.</param>
        public PDFGenerationService(
            IPdfTemplateService templateService,
            IRazorService razorService,
            ILogService logService,
            IDebugCshtmlInjectionService debugService,
            ISystemMetricsService metricsService
        )
        {
            _templateService = templateService;
            _razorService = razorService;
            _logService = logService;
            _debugService = debugService;
            _metricsService = metricsService;
        }

        /// <summary>
        /// Gets or sets whether model debugging is enabled.
        /// When true, JSON representation of the model will be displayed at the top of generated PDFs.
        /// </summary>
        public bool ModelDebuggingEnabled
        {
            get => _debugService.ModelDebuggingEnabled;
            set => _debugService.ModelDebuggingEnabled = value;
        }

        /// <summary>
        /// Gets the health status of the PDF generation service.
        /// </summary>
        /// <returns>A ServiceHealthStatus object containing health metrics.</returns>
        public ServiceHealthStatus GetServiceHealth()
        {
            var process = Process.GetCurrentProcess();
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionString = version != null ? version.ToString() : "1.0.0";

            // Check dependency statuses
            var dependencyStatuses = new Dictionary<string, string>();
            var razorServiceStatus = CheckRazorServiceStatus();
            
            // Add more dependency checks as needed
            dependencyStatuses.Add("TemplateService", _templateService.GetTemplateCount() > 0 ? "Healthy" : "Unhealthy");
            dependencyStatuses.Add("RazorService", razorServiceStatus);

            var status = new ServiceHealthStatus
            {
                Status = _templateService.GetTemplateCount() > 0 && razorServiceStatus == "Healthy" ? "Healthy" : "Unhealthy",
                TemplateCount = _templateService.GetTemplateCount(),
                AvailableTemplates = _templateService.GetAllTemplateNames().ToList(),
                LastChecked = DateTime.UtcNow,

                // Tracking PDF generation statistics
                PDFsGenerated = _pdfsGenerated,
                LastPDFGenerationTime = _lastPDFGenerationTime,
                LastPDFGenerationStatus = _lastPDFGenerationStatus,

                // System Metrics
                SystemUptime = _metricsService.SystemUptime,
                MemoryUsageInMB = _metricsService.MemoryUsageInMB,
                ErrorsLogged = _errorsLogged,
                CpuUsage = _metricsService.CpuUsage,
                AverageResponseTime = _metricsService.AverageResponseTime,
                ConcurrentRequestsHandled = _metricsService.ConcurrentRequestsHandled,

                // Dependency Status
                DependencyStatuses = dependencyStatuses,
                RazorServiceStatus = razorServiceStatus,

                // Template Insights
                TemplatePerformance = _metricsService.TemplatePerformance.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                TemplateUsageStatistics = _metricsService.TemplateUsageStatistics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),

                // Operational Status
                ServiceVersion = versionString,
                
                // Recent logs
                RecentPdfGenerations = _recentPdfGenerations
            };

            return status;
        }

        /// <summary>
        /// Checks the status of the Razor service.
        /// </summary>
        /// <returns>The status of the Razor service.</returns>
        private string CheckRazorServiceStatus()
        {
            try
            {
                // Simple test to see if the Razor service can be initialized
                var canInit = _razorService.IsInitialized();
                if (!canInit)
                {
                    _razorService.InitializeAsync().GetAwaiter().GetResult();
                }
                return "Healthy";
            }
            catch (Exception ex)
            {
                _logService.LogError("Error checking Razor service status", ex);
                return "Unhealthy";
            }
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
            
            var stopwatch = Stopwatch.StartNew();
            _metricsService.StartRequest();
            
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
            finally
            {
                stopwatch.Stop();
                _metricsService.EndRequest(stopwatch.ElapsedMilliseconds);
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

                // Track template rendering time
                Stopwatch templateRenderTimer = new Stopwatch();
                templateRenderTimer.Start();
                
                // Render the template using Razor
                var cshtmlContent = await _razorService.RenderTemplateAsync(templateName, data);
                
                // Stop timer and record performance
                templateRenderTimer.Stop();
                _metricsService.RecordTemplatePerformance(templateName, templateRenderTimer.ElapsedMilliseconds);
                
                _logService.LogInfo(
                    $"Template '{templateName}' rendered successfully. HTML length: {cshtmlContent.Length} characters, Render time: {templateRenderTimer.ElapsedMilliseconds}ms"
                );

                // If debugging is enabled, inject model display code
                cshtmlContent = _debugService.InjectModelDebugInfoIfEnabled(cshtmlContent, data);

                // Inject global styles into the HTML content
                cshtmlContent = InjectGlobalStyles(cshtmlContent);
                _logService.LogInfo("Global styles injected into HTML content");

                // Process image paths to embed images in the PDF
                string templatesPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Templates"
                );
                cshtmlContent = ProcessImagePaths(cshtmlContent, templatesPath);
                _logService.LogInfo("Image paths processed and embedded into HTML content");

                // Track PDF conversion time
                Stopwatch pdfConversionTimer = new Stopwatch();
                pdfConversionTimer.Start();
                
                // Convert HTML directly to PDF using memory streams
                await using var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(cshtmlContent));
                await using var pdfStream = new MemoryStream();

                HtmlConverter.ConvertToPdf(htmlStream, pdfStream);
                
                // Stop timer and log performance
                pdfConversionTimer.Stop();
                _logService.LogInfo($"HTML converted to PDF successfully. Conversion time: {pdfConversionTimer.ElapsedMilliseconds}ms");

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
        /// Injects global CSS styles into the CSHTML content before PDF generation.
        /// </summary>
        /// <param name="cshtmlContent">The CSHTML content to inject styles into</param>
        /// <returns>HTML content with injected global styles</returns>
        /// <remarks>
        /// Reads styles from globalStyles.css and injects them into the head section.
        /// If the styles file is not found or head tag is missing, returns original content.
        /// </remarks>
        private string InjectGlobalStyles(string cshtmlContent)
        {
            try
            {
                if (!File.Exists(_globalStylesPath))
                {
                    _logService.LogWarning($"Global styles file not found: {_globalStylesPath}");
                    return cshtmlContent;
                }

                var cssContent = File.ReadAllText(_globalStylesPath);

                // Find the closing head tag
                const string headEndTag = "</head>";
                var headEndIndex = cshtmlContent.IndexOf(
                    headEndTag,
                    StringComparison.OrdinalIgnoreCase
                );

                if (headEndIndex == -1)
                {
                    _logService.LogWarning("No </head> tag found in HTML content");
                    return cshtmlContent;
                }

                // Inject the CSS content within a style tag before the </head>
                var styleTag = $"<style>{cssContent}</style>";
                return cshtmlContent.Insert(headEndIndex, styleTag);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error injecting global styles: {ex.Message}", ex);
                return cshtmlContent;
            }
        }

        /// <summary>
        /// Processes image paths in HTML content to convert them to base64 data URIs.
        /// </summary>
        /// <param name="cshtmlContent">The HTML content containing image tags</param>
        /// <param name="basePath">Base path for resolving relative image paths</param>
        /// <returns>HTML content with image paths converted to base64 data URIs</returns>
        /// <remarks>
        /// This ensures images are embedded in the PDF rather than requiring external files.
        /// If an image cannot be processed, the original img tag is preserved.
        /// </remarks>
        private string ProcessImagePaths(string cshtmlContent, string basePath)
        {
            try
            {
                // Find all img tags with src attributes
                var imgPattern = @"<img[^>]*src\s*=\s*[""']([^""']*)[""'][^>]*>";
                return Regex.Replace(
                    cshtmlContent,
                    imgPattern,
                    match =>
                    {
                        var imgTag = match.Value;
                        var srcPath = match.Groups[1].Value;

                        // Remove leading '/' or './' if present
                        srcPath = srcPath.TrimStart('/', '.');

                        // Construct full path
                        var fullPath = Path.Combine(basePath, srcPath);

                        if (File.Exists(fullPath))
                        {
                            try
                            {
                                // Read image and convert to base64
                                var imageBytes = File.ReadAllBytes(fullPath);
                                var base64String = Convert.ToBase64String(imageBytes);
                                var mimeType =
                                    "image/" + Path.GetExtension(fullPath).TrimStart('.').ToLower();

                                // Replace src with data URI
                                return imgTag.Replace(
                                    match.Groups[1].Value,
                                    $"data:{mimeType};base64,{base64String}"
                                );
                            }
                            catch (Exception ex)
                            {
                                _logService.LogWarning($"Error processing image {fullPath}: {ex.Message}");
                                return imgTag; // Keep original tag if processing fails
                            }
                        }

                        _logService.LogWarning($"Image not found: {fullPath}");
                        return imgTag; // Keep original tag if file not found
                    }
                );
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error in ProcessImagePaths: {ex.Message}", ex);
                return cshtmlContent; // Return original content if processing fails
            }
        }
    }
}
