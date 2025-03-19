namespace iTextFormBuilderAPI.Models;

public class PdfGenerationLog
{
    public DateTime Timestamp { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the processing time for PDF generation in milliseconds.
    /// </summary>
    public long ProcessingTimeMs { get; set; }
}
