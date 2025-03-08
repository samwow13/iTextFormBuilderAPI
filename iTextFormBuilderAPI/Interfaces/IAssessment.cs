namespace iTextFormBuilderAPI.Interfaces
{
    /// <summary>
    /// Base interface for all assessment types
    /// </summary>
    public interface IAssessment
    {
        /// <summary>
        /// Gets the template file name for PDF generation
        /// </summary>
        string TemplateFileName { get; }

        /// <summary>
        /// Gets the display name for this assessment type
        /// </summary>
        string DisplayName { get; }
    }
}
