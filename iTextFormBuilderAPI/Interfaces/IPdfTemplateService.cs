namespace iTextFormBuilderAPI.Interfaces;

/// <summary>
/// Interface for services that manage PDF templates.
/// </summary>
public interface IPdfTemplateService
{
    /// <summary>
    /// Gets all valid template names registered in the system.
    /// </summary>
    /// <returns>A collection of valid template names.</returns>
    IEnumerable<string> GetAllTemplateNames();

    /// <summary>
    /// Checks if a template with the specified name exists.
    /// </summary>
    /// <param name="templateName">The name of the template to check.</param>
    /// <returns>True if the template exists, false otherwise.</returns>
    bool TemplateExists(string templateName);

    /// <summary>
    /// Gets the full path to a template file.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <returns>The full path to the template file, or an empty string if the template doesn't exist.</returns>
    string GetTemplatePath(string templateName);

    /// <summary>
    /// Gets the count of valid templates in the system.
    /// </summary>
    /// <returns>The number of valid templates.</returns>
    int GetTemplateCount();
}
