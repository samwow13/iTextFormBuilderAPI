namespace iTextFormBuilderAPI.Interfaces;

/// <summary>
/// Interface for services that render Razor templates.
/// </summary>
public interface IRazorService
{
    /// <summary>
    /// Renders a Razor template with the specified model.
    /// </summary>
    /// <param name="templateName">The name of the template to render.</param>
    /// <param name="model">The model to pass to the template.</param>
    /// <returns>The rendered HTML as a string.</returns>
    Task<string> RenderTemplateAsync(string templateName, object model);
    
    /// <summary>
    /// Initializes the Razor engine.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InitializeAsync();
    
    /// <summary>
    /// Gets the type of the model for the specified template.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <returns>The type of the model, or null if the type cannot be determined.</returns>
    Type? GetModelType(string templateName);
}
