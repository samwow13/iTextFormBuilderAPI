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

/// <summary>
/// In-memory project implementation for RazorLight template engine.
/// Stores and manages templates in memory for PDF generation.
/// </summary>
public class RazorLightInMemoryProject : RazorLightProject
{
    private readonly Dictionary<string, string> _templates = new Dictionary<string, string>();

    /// <summary>
    /// Retrieves a RazorLight project item from the in-memory project.
    /// </summary>
    /// <param name="templateKey">Key of the template to retrieve</param>
    /// <returns>RazorLight project item containing the template content</returns>
    public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
    {
        return Task.FromResult<RazorLightProjectItem>(
            new TextSourceRazorProjectItem(
                templateKey,
                _templates.TryGetValue(templateKey, out string template)
                    ? template
                    : string.Empty
            )
        );
    }

    /// <summary>
    /// Retrieves a list of imports for a given template key.
    /// </summary>
    /// <param name="templateKey">Key of the template to retrieve imports for</param>
    /// <returns>Empty list of RazorLight project items (no imports are used in this implementation)</returns>
    public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey)
    {
        return Task.FromResult<IEnumerable<RazorLightProjectItem>>(
            Array.Empty<RazorLightProjectItem>()
        );
    }

    /// <summary>
    /// Adds a template to the in-memory project.
    /// </summary>
    /// <param name="key">Key of the template to add</param>
    /// <param name="template">Content of the template to add</param>
    public void AddTemplate(string key, string template)
    {
        _templates[key] = template;
    }
}

public class PDFGenerationService : IPDFGenerationService
{
    private static int _pdfsGenerated = 0;
    private static int _errorsLogged = 0;
    private static DateTime? _lastPDFGenerationTime = null;
    private static string _lastPDFGenerationStatus = "N/A";
    private static readonly Stopwatch _uptime = Stopwatch.StartNew();
    private readonly string _globalStylesPath = Path.Combine(
        Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? string.Empty,
        "Templates\\globalStyles.css"
    );

    // Store the last 10 PDF generations
    private static readonly List<PdfGenerationLog> _recentPdfGenerations = new();

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

        if (!PdfTemplateRegistry.ValidTemplates.Contains(templateName, StringComparer.OrdinalIgnoreCase))
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
        try
        {
            if (!File.Exists(_globalStylesPath))
            {
                Trace.WriteLine($"Global styles file not found: {_globalStylesPath}");
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
                Trace.WriteLine("No </head> tag found in HTML content");
                return htmlContent;
            }

            // Inject the CSS content within a style tag before the </head>
            var styleTag = $"<style>{cssContent}</style>";
            return htmlContent.Insert(headEndIndex, styleTag);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error injecting global styles: {ex}");
            return htmlContent;
        }
    }

    private byte[] GeneratePdfFromTemplate(string templateName, object data)
    {
        DebugLogger.Log($"GeneratePdfFromTemplate called for template: {templateName}");
        
        if (!TemplateExists(templateName))
        {
            DebugLogger.Log("Template does not exist");
            return Array.Empty<byte>();
        }

        var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? string.Empty;
        var templatePath = Path.Combine(projectRoot, "Templates", $"{templateName}.cshtml");
        DebugLogger.Log($"Template path: {templatePath}");
        
        if (!File.Exists(templatePath))
        {
            DebugLogger.Log($"Template file not found at path: {templatePath}");
            return Array.Empty<byte>();
        }
        
        var templateContent = File.ReadAllText(templatePath);
        DebugLogger.Log($"Template content length: {templateContent.Length} characters");
        DebugLogger.Log($"Template content (first 500 chars): {templateContent.Substring(0, Math.Min(500, templateContent.Length))}");

        // Create a strongly-typed model instance based on the template name
        object typedModel = CreateTypedModel(templateName, data);
        if (typedModel == null)
        {
            DebugLogger.Log("Failed to create typed model");
            return Array.Empty<byte>();
        }

        DebugLogger.Log($"Model type: {typedModel.GetType().FullName}");
        DebugLogger.Log($"Model data: {JsonConvert.SerializeObject(typedModel, Formatting.Indented)}");

        // Create a simple memory project for RazorLight
        var project = new RazorLightInMemoryProject();
        project.AddTemplate(templateName, templateContent);

        var engine = new RazorLightEngineBuilder()
            .UseMemoryCachingProvider()
            .UseProject(project)
            .EnableDebugMode()
            .Build();

        try
        {
            // Render the template into HTML
            DebugLogger.Log("Starting template rendering...");
            string htmlContent = engine.CompileRenderAsync(templateName, typedModel).Result;
            DebugLogger.Log($"HTML content length: {htmlContent.Length} characters");
            DebugLogger.Log($"HTML content (first 1000 chars): {htmlContent.Substring(0, Math.Min(1000, htmlContent.Length))}");

            // Save HTML to a file for inspection
            string htmlFilePath = Path.Combine(projectRoot, "debug_output.html");
            File.WriteAllText(htmlFilePath, htmlContent);
            DebugLogger.Log($"Saved HTML content to: {htmlFilePath}");

            // Convert the HTML content to a PDF using iText7.pdfhtml
            DebugLogger.Log("Converting HTML to PDF...");
            using (var ms = new MemoryStream())
            {
                try
                {
                    HtmlConverter.ConvertToPdf(htmlContent, ms);
                    byte[] pdfBytes = ms.ToArray();
                    DebugLogger.Log($"PDF conversion complete. PDF size: {pdfBytes.Length} bytes");
                    
                    // Save PDF to a file for inspection
                    string pdfFilePath = Path.Combine(projectRoot, "debug_output.pdf");
                    File.WriteAllBytes(pdfFilePath, pdfBytes);
                    DebugLogger.Log($"Saved PDF content to: {pdfFilePath}");
                    
                    return pdfBytes;
                }
                catch (Exception ex)
                {
                    DebugLogger.LogException(ex, "PDF Conversion");
                    return Array.Empty<byte>();
                }
            }
        }
        catch (Exception ex)
        {
            DebugLogger.LogException(ex, "Template Rendering");
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Creates a strongly-typed model instance based on the template name and data.
    /// </summary>
    /// <param name="templateName">Name of the template</param>
    /// <param name="data">Raw data object</param>
    /// <returns>Strongly-typed model instance</returns>
    private object CreateTypedModel(string templateName, object data)
    {
        try
        {
            // Convert data to JSON
            string json = JsonConvert.SerializeObject(data);

            // Determine the model type based on the template name
            if (templateName.Equals("HealthAndWellness\\TestRazorDataAssessment", StringComparison.OrdinalIgnoreCase))
            {
                // For TestRazorDataAssessment template, use TestRazorDataInstance model
                return JsonConvert.DeserializeObject<iTextFormBuilderAPI.Models.HealthAndWellness.TestRazorDataModels.TestRazorDataInstance>(json) ?? data;
            }
            // For SimpleTest template, no model needed
            else if (templateName.Equals("SimpleTest", StringComparison.OrdinalIgnoreCase))
            {
                // Simple test doesn't need a model, return empty object
                DebugLogger.Log("Using empty model for SimpleTest template");
                return new object();
            }

            // Add more template-to-model mappings as needed

            // If no specific mapping is found, return the original data
            DebugLogger.Log($"No model type mapping found for template: {templateName}");
            return data;
        }
        catch (Exception ex)
        {
            DebugLogger.LogException(ex, "CreateTypedModel");
            return data; // Return original data instead of null to avoid null reference
        }
    }

    private bool TemplateExists(string templateName)
    {
        var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName ?? string.Empty;

        if (string.IsNullOrEmpty(projectRoot))
        {
            Trace.WriteLine("Unable to determine project root directory.");
            return false;
        }

        var templatePath = Path.Combine(projectRoot, "Templates", $"{templateName}.cshtml");

        if (!File.Exists(templatePath))
        {
            Trace.WriteLine($"Template file not found: {templatePath}");
            return false;
        }

        return true;
    }
}