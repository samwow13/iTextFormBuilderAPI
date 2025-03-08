using System.Diagnostics;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;
using iTextFormBuilderAPI.Models.APIModels;
using iTextFormBuilderAPI.Utilities;
using RazorLight;
using RazorLight.Razor;
using iText.Html2pdf;


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

        if (!PdfTemplateRegistry.ValidTemplates.Contains(templateName, StringComparer.OrdinalIgnoreCase))
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
    private string InjectGlobalStyles(string cshtmlContent)
    {
        try
        {
            if (!File.Exists(_globalStylesPath))
            {
                Trace.WriteLine($"Global styles file not found: {_globalStylesPath}");
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
                Trace.WriteLine("No </head> tag found in HTML content");
                return cshtmlContent;
            }

            // Inject the CSS content within a style tag before the </head>
            var styleTag = $"<style>{cssContent}</style>";
            return cshtmlContent.Insert(headEndIndex, styleTag);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error injecting global styles: {ex}");
            return cshtmlContent;
        }
    }


    private byte[] GeneratePdfFromTemplate(string templateName, object data)
    {
        if (!TemplateExists(templateName))
        {
            return Array.Empty<byte>();
        }

        var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
        var templatePath = Path.Combine(projectRoot, "Templates", $"{templateName}.cshtml");
        var templateContent = File.ReadAllText(templatePath);

        var engine = new RazorLightEngineBuilder()
            .UseMemoryCachingProvider()
            .UseFileSystemProject(projectRoot)
            .EnableDebugMode()
            .Build();

        try
        {
            // Inject global styles into the template content
            templateContent = InjectGlobalStyles(templateContent);

            // Render the template into HTML
            string htmlContent = engine.CompileRenderStringAsync(templateName, templateContent, data).Result;

            // Log the HTML content for debugging
            Trace.WriteLine($"Generated HTML Content: {htmlContent}");

            // Convert the HTML content to a PDF using iText7.pdfhtml
            using (var ms = new MemoryStream())
            {
                HtmlConverter.ConvertToPdf(htmlContent, ms);
                return ms.ToArray();
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"Error rendering template: {ex}");
            return Array.Empty<byte>();
        }
    }



    private bool TemplateExists(string templateName)
    {
        var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;

        if (projectRoot == null)
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