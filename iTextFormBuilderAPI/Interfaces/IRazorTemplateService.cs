using RazorLight;
using RazorLight.Razor;

namespace iTextFormBuilderAPI.Interfaces
{
    /// <summary>
    /// Interface for services that manage Razor templates
    /// </summary>
    public interface IRazorTemplateService
    {
        /// <summary>
        /// Retrieves a RazorLight project item from the template store
        /// </summary>
        /// <param name="templateKey">Key of the template to retrieve</param>
        /// <returns>RazorLight project item containing the template content</returns>
        Task<RazorLightProjectItem> GetTemplateAsync(string templateKey);

        /// <summary>
        /// Adds a template to the template store
        /// </summary>
        /// <param name="key">Key of the template to add</param>
        /// <param name="template">Content of the template to add</param>
        void AddTemplate(string key, string template);

        /// <summary>
        /// Loads a template from the filesystem
        /// </summary>
        /// <param name="templateName">Name of the template file (without extension)</param>
        /// <param name="templateType">Type of template (e.g., "cshtml")</param>
        /// <returns>True if template was loaded successfully, false otherwise</returns>
        bool LoadTemplateFromFile(string templateName, string templateType = "cshtml");
        
        /// <summary>
        /// Checks if a template exists in the service
        /// </summary>
        /// <param name="templateName">Name of the template to check</param>
        /// <returns>True if the template exists, false otherwise</returns>
        bool TemplateExists(string templateName);
        
        /// <summary>
        /// Gets the content of a template
        /// </summary>
        /// <param name="templateName">Name of the template to get content for</param>
        /// <returns>The content of the template</returns>
        string GetTemplateContent(string templateName);
        
        /// <summary>
        /// Gets a RazorLight engine configured to use this template service
        /// </summary>
        /// <returns>A configured RazorLight engine</returns>
        IRazorLightEngine GetRazorEngine();
    }
}
