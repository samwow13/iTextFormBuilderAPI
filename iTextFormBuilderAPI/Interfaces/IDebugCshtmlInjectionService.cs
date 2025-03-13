namespace iTextFormBuilderAPI.Interfaces
{
    /// <summary>
    /// Service responsible for injecting debugging information into CSHTML content.
    /// </summary>
    public interface IDebugCshtmlInjectionService
    {
        /// <summary>
        /// Gets or sets whether model debugging is enabled.
        /// When true, JSON representation of the model will be displayed at the top of generated PDFs.
        /// </summary>
        bool ModelDebuggingEnabled { get; set; }

        /// <summary>
        /// Injects model debugging information at the top of the HTML content if debugging is enabled.
        /// </summary>
        /// <param name="cshtmlContent">The CSHTML content to inject debugging info into</param>
        /// <param name="model">The model object to display as JSON</param>
        /// <returns>HTML content with injected model debugging information if enabled, otherwise the original content</returns>
        string InjectModelDebugInfoIfEnabled(string cshtmlContent, object model);
    }
}
