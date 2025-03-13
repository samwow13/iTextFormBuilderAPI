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
    public double MemoryUsageInMB { get; set; }  // Renamed from MemoryUsage and changed to double for MB representation
    public int ErrorsLogged { get; set; }
    public double CpuUsage { get; set; }                          // Added for CPU monitoring
    public double AverageResponseTime { get; set; }               // Added for performance tracking
    public int ConcurrentRequestsHandled { get; set; }            // Added for load monitoring

    // Debug Information
    public bool DebugModeActive { get; set; }                   // Added for tracking debug mode status

    // Dependency Status
    public Dictionary<string, string> DependencyStatuses { get; set; } = new();  // Added for tracking external services
    public string RazorServiceStatus { get; set; } = "Unknown";   // Added for template rendering health

    // Template Insights
    public Dictionary<string, double> TemplatePerformance { get; set; } = new();  // Added for template rendering times
    public Dictionary<string, int> TemplateUsageStatistics { get; set; } = new(); // Added for usage patterns

    // Operational Status
    public string ServiceVersion { get; set; } = "1.0.0";         // Added for version tracking

    // Track last 10 PDF generations
    public List<PdfGenerationLog> RecentPdfGenerations { get; set; } = new();
}
