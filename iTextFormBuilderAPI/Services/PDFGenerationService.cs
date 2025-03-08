using System.Diagnostics;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;
using iTextFormBuilderAPI.Models.APIModels;
using iTextFormBuilderAPI.Utilities;
using RazorLight;
using RazorLight.Razor;
using iText.Html2pdf;
using Newtonsoft.Json;
using System.Text;

namespace iTextFormBuilderAPI.Services;

// This comment replaces the removed RazorLightInMemoryProject class

public class PDFGenerationService : IPDFGenerationService
{
    private static int _pdfsGenerated = 0;
    private static int _errorsLogged = 0;
    private static DateTime? _lastPDFGenerationTime = null;
    private static string _lastPDFGenerationStatus = "N/A";
    private static readonly Stopwatch _uptime = Stopwatch.StartNew();
    private readonly string _globalStylesPath;
    private readonly IRazorTemplateService _razorTemplateService;

    // Store the last 10 PDF generations
    private static readonly List<PdfGenerationLog> _recentPdfGenerations = new();

    /// <summary>
    /// Initializes a new instance of the PDFGenerationService
    /// </summary>
    /// <param name="razorTemplateService">Service for managing Razor templates</param>
    public PDFGenerationService(IRazorTemplateService razorTemplateService)
    {
        _razorTemplateService = razorTemplateService;
        
        // Initialize the global styles path
        string? baseDirectoryPath = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
        _globalStylesPath = !string.IsNullOrEmpty(baseDirectoryPath)
            ? Path.Combine(baseDirectoryPath, "Templates", "globalStyles.css")
            : Path.Combine(AppContext.BaseDirectory, "Templates", "globalStyles.css");
        
        // Initialize templates from the file system
        InitializeTemplates();
    }
    
    /// <summary>
    /// Initializes templates from the file system
    /// </summary>
    private void InitializeTemplates()
    {
        // Load templates from the registry
        foreach (var templateName in PdfTemplateRegistry.ValidTemplates)
        {
            _razorTemplateService.LoadTemplateFromFile(templateName);
        }
    }

    public ServiceHealthStatus GetServiceHealth()
    {
        var process = Process.GetCurrentProcess();

        var status = new ServiceHealthStatus
        {
            Status = PdfTemplateRegistry.ValidTemplates.Count > 0 ? "Healthy" : "Unhealthy",
            TemplateCount = PdfTemplateRegistry.ValidTemplates.Count,
            AvailableTemplates = PdfTemplateRegistry.ValidTemplates.ToList(),
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
            RecentPdfGenerations = _recentPdfGenerations.Count <= 1
                ? new List<PdfGenerationLog> { new PdfGenerationLog { Message = "Waiting for more PDF generations to be available." } }
                : _recentPdfGenerations.ToList()
        };

        return status;
    }

    public PdfResult GeneratePdf(string templateName, object data)
    {
        bool success;
        string message;

        // Clear the debug log
        DebugLogger.ClearLog();
        DebugLogger.Log($"Starting PDF generation for template: {templateName}");
        DebugLogger.Log($"Data: {JsonConvert.SerializeObject(data, Formatting.Indented)}");

        // Check if the template exists in our service or can be loaded
        if (!_razorTemplateService.TemplateExists(templateName) && 
            !_razorTemplateService.LoadTemplateFromFile(templateName))
        {
            _errorsLogged++;
            _lastPDFGenerationStatus = "Failed - Template not found";
            success = false;
            message = $"Template '{templateName}' does not exist.";
            DebugLogger.Log($"Error: {message}");
        }
        else
        {
            try
            {
                byte[] pdfBytes = GeneratePdfFromTemplate(templateName, data);
                _pdfsGenerated++;
                _lastPDFGenerationTime = DateTime.UtcNow;
                _lastPDFGenerationStatus = "Success";
                success = true;
                message = "PDF generated successfully.";
                DebugLogger.Log($"Success: PDF generated with {pdfBytes.Length} bytes");

                // Store the last 10 PDF generations
                _recentPdfGenerations.Add(new PdfGenerationLog
                {
                    Timestamp = DateTime.UtcNow,
                    TemplateName = templateName,
                    Success = success,
                    Message = message
                });

                // Ensure the list never exceeds 10 entries
                if (_recentPdfGenerations.Count > 10)
                {
                    _recentPdfGenerations.RemoveAt(0);
                }

                return new PdfResult
                {
                    Success = success,
                    PdfBytes = pdfBytes,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _errorsLogged++;
                _lastPDFGenerationStatus = "Failed - Exception";
                success = false;
                message = $"Exception generating PDF: {ex.Message}";
                DebugLogger.LogException(ex, "GeneratePdf");
            }
        }

        // Store the last 10 PDF generations
        _recentPdfGenerations.Add(new PdfGenerationLog
        {
            Timestamp = DateTime.UtcNow,
            TemplateName = templateName,
            Success = success,
            Message = message
        });

        // Ensure the list never exceeds 10 entries
        if (_recentPdfGenerations.Count > 10)
        {
            _recentPdfGenerations.RemoveAt(0);
        }

        return new PdfResult
        {
            Success = success,
            PdfBytes = success ? new byte[1] : Array.Empty<byte>(), // Dummy PDF data
            Message = message
        };
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
        if (!File.Exists(_globalStylesPath))
        {
            DebugLogger.Log($"Global styles file not found at: {_globalStylesPath}");
            return htmlContent;
        }

        DebugLogger.Log($"Injecting global styles from: {_globalStylesPath}");

        try
        {
            string globalStyles = File.ReadAllText(_globalStylesPath);
            DebugLogger.Log($"Global styles loaded, length: {globalStyles.Length} characters");

            // If there's no head tag, we can't inject styles
            if (!htmlContent.Contains("<head>"))
            {
                DebugLogger.Log("No head tag found in HTML, cannot inject styles");
                return htmlContent;
            }

            // Insert the global styles inside the head tag
            string styleTag = $"<style>{globalStyles}</style>";
            string modifiedHtml = htmlContent.Replace("<head>", $"<head>{Environment.NewLine}{styleTag}");

            DebugLogger.Log("Global styles injected successfully");
            return modifiedHtml;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException(ex, "InjectGlobalStyles");
            return htmlContent; // Return original content if there was an error
        }
    }

    private byte[] GeneratePdfFromTemplate(string templateName, object data)
    {
        DebugLogger.Log($"GeneratePdfFromTemplate called for template: {templateName}");
        
        // Get the template content from our service
        var templateContent = _razorTemplateService.GetTemplateContent(templateName);
        if (string.IsNullOrEmpty(templateContent))
        {
            DebugLogger.Log("Template content is empty");
            return Array.Empty<byte>();
        }
        DebugLogger.Log($"Template content length: {templateContent.Length} characters");
        DebugLogger.Log($"Template content (first 500 chars): {templateContent.Substring(0, Math.Min(500, templateContent.Length))}");

        // Handle the model type to match the template type
        var templateModel = GetTemplateModel(templateName, data);
        if (templateModel == null)
        {
            DebugLogger.Log("Template model is null");
            return Array.Empty<byte>();
        }

        var typedModel = Convert.ChangeType(templateModel, templateModel.GetType());
        DebugLogger.Log($"Model data: {JsonConvert.SerializeObject(typedModel, Formatting.Indented)}");

        // Get the RazorLight engine from our service
        var engine = _razorTemplateService.GetRazorEngine();

        try
        {
            // Generate HTML from the template using RazorLight
            DebugLogger.Log("Rendering template with RazorLight...");
            string htmlContent = engine.CompileRenderStringAsync(templateName, templateContent, typedModel).Result;
            DebugLogger.Log($"HTML content (first 1000 chars): {htmlContent.Substring(0, Math.Min(1000, htmlContent.Length))}");

            // Save HTML to a file for inspection
            string? baseDirectoryPath = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
            string htmlFilePath = !string.IsNullOrEmpty(baseDirectoryPath)
                ? Path.Combine(baseDirectoryPath, "debug_output.html")
                : Path.Combine(AppContext.BaseDirectory, "debug_output.html");
                
            File.WriteAllText(htmlFilePath, htmlContent);
            DebugLogger.Log($"Saved HTML content to: {htmlFilePath}");

            // Inject global styles into the HTML content
            htmlContent = InjectGlobalStyles(htmlContent);

            // Convert the HTML content to a PDF using iText7.pdfhtml
            DebugLogger.Log("Converting HTML to PDF...");
            using (var ms = new MemoryStream())
            {
                HtmlConverter.ConvertToPdf(htmlContent, ms);
                var pdfBytes = ms.ToArray();
                if (pdfBytes.Length > 0)
                {
                    DebugLogger.Log($"PDF conversion complete. PDF size: {pdfBytes.Length} bytes");
                    
                    // Save PDF to a file for inspection
                    string pdfFilePath = !string.IsNullOrEmpty(baseDirectoryPath)
                        ? Path.Combine(baseDirectoryPath, "debug_output.pdf")
                        : Path.Combine(AppContext.BaseDirectory, "debug_output.pdf");
                        
                    File.WriteAllBytes(pdfFilePath, pdfBytes);
                    DebugLogger.Log($"Saved PDF content to: {pdfFilePath}");
                    
                    return pdfBytes;
                }
                else
                {
                    DebugLogger.Log("PDF conversion returned empty PDF");
                    return Array.Empty<byte>();
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException(ex, "GeneratePdfFromTemplate");
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Gets the appropriate model type for the template
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="data">Data object from the client</param>
    /// <returns>A model instance for the specified template</returns>
    private object? GetTemplateModel(string templateName, object data)
    {
        try
        {
            DebugLogger.Log($"Getting model for template: {templateName}");

            // Try to cast JSON data to a dynamic object
            dynamic? jsonData = data is string jsonString
                ? JsonConvert.DeserializeObject<dynamic>(jsonString)
                : data;

            if (jsonData == null)
            {
                DebugLogger.Log("JSON data is null after deserialization");
                return null;
            }

            // Map template name to model type
            string modelType = $"iTextFormBuilderAPI.Models.{templateName}Instance";
            DebugLogger.Log($"Looking for model type: {modelType}");

            // Get the model type by name
            Type? type = Type.GetType(modelType);
            if (type == null)
            {
                // Try to find the type in current assembly if not found by name
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.FullName == modelType);
            }

            if (type == null)
            {
                DebugLogger.Log($"Model type not found: {modelType}");
                return null;
            }

            DebugLogger.Log($"Model type found: {type.FullName}");

            // Convert JSON to the model type
            var json = JsonConvert.SerializeObject(jsonData);
            var model = JsonConvert.DeserializeObject(json, type);

            DebugLogger.Log($"Model created: {model != null}");
            return model;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException(ex, "GetTemplateModel");
            return data; // Return original data instead of null to avoid null reference
        }
    }
}