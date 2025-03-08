namespace iTextFormBuilderAPI.Models;

public class ServiceHealthStatus
{
    public string Status { get; set; } = "Unhealthy";
    public int TemplateCount { get; set; }
    public List<string> AvailableTemplates { get; set; } = new();
    public DateTime LastChecked { get; set; }

    // PDF Generation Tracking
    public int PDFsGenerated { get; set; }
    public DateTime? LastPDFGenerationTime { get; set; }
    public string LastPDFGenerationStatus { get; set; } = "N/A";

    // System Metrics
    public TimeSpan SystemUptime { get; set; }
    public long MemoryUsage { get; set; }
    public int ErrorsLogged { get; set; }

    // Track last 10 PDF generations
    public List<PdfGenerationLog> RecentPdfGenerations { get; set; } = new();
}
