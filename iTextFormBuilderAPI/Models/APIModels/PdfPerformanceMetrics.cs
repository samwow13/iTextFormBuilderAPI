using System;

namespace iTextFormBuilderAPI.Models.APIModels
{
    /// <summary>
    /// Contains performance metrics for PDF generation and template rendering.
    /// </summary>
    public class PdfPerformanceMetrics
    {
        /// <summary>
        /// Gets or sets the timestamp when the metric was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the name of the template being measured.
        /// </summary>
        public string TemplateName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total rendering time in milliseconds.
        /// </summary>
        public double RenderingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the total PDF conversion time in milliseconds.
        /// </summary>
        public double ConversionTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the total PDF generation time (rendering + conversion) in milliseconds.
        /// </summary>
        public double TotalProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the size of the generated PDF in bytes.
        /// </summary>
        public long OutputSizeBytes { get; set; }
    }
}
