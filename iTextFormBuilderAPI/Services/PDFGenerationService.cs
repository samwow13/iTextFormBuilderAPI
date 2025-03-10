using System.Diagnostics;
using iText.Html2pdf;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models;
using iTextFormBuilderAPI.Models.APIModels;
using iTextFormBuilderAPI.Utilities;
using RazorLight;
using RazorLight.Razor;

namespace iTextFormBuilderAPI.Services;

public class PDFGenerationService : IPDFGenerationService
{
    private static int _pdfsGenerated = 0;
    private static int _errorsLogged = 0;
    private static DateTime? _lastPDFGenerationTime = null;
    private static string _lastPDFGenerationStatus = "N/A";
    private static readonly Stopwatch _uptime = Stopwatch.StartNew();


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

    public PdfResult GeneratePdf(string templateName, object data)
    {
        bool success;
        string message;

        if (
            !PdfTemplateRegistry.ValidTemplates.Contains(
                templateName,
                StringComparer.OrdinalIgnoreCase
            )
        )
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
            PdfBytes = success ? new byte[1] : Array.Empty<byte>(), // Dummy PDF data
            Message = message,
        };
    }

    private byte[] GeneratePdfFromTemplate(string templateName, object data)
    {
        if (!TemplateExists(templateName))
        {
            return Array.Empty<byte>();
        }
        return Array.Empty<byte>();
    }

    private bool TemplateExists(string templateName)
    {
        var projectRoot = Directory
            .GetParent(AppContext.BaseDirectory)
            ?.Parent?.Parent?.Parent?.FullName;

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
