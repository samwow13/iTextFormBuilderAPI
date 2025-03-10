using System.Reflection;
using iTextFormBuilderAPI.Interfaces;
using RazorLight;
using RazorLight.Razor;

namespace iTextFormBuilderAPI.Services;

/// <summary>
/// Service for rendering Razor templates using RazorLight.
/// </summary>
public class RazorService : IRazorService
{
    private readonly IPdfTemplateService _templateService;
    private readonly ILogService _logService;
    private RazorLightEngine? _engine;
    private readonly Dictionary<string, Type> _templateModelTypes;

    /// <summary>
    /// Initializes a new instance of the RazorService class.
    /// </summary>
    /// <param name="templateService">The template service for managing PDF templates.</param>
    /// <param name="logService">The log service for logging messages.</param>
    public RazorService(IPdfTemplateService templateService, ILogService logService)
    {
        _templateService = templateService;
        _logService = logService;
        _templateModelTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        
        // Initialize the template model types dictionary
        InitializeTemplateModelTypes();
    }

    /// <summary>
    /// Initializes the Razor engine.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        try
        {
            _logService.LogInfo("Initializing RazorLight engine...");
            
            // Create a new RazorLight engine that uses the file system
            var projectRoot = Directory
                .GetParent(AppContext.BaseDirectory)
                ?.Parent?.Parent?.Parent?.FullName;

            if (projectRoot == null)
            {
                throw new InvalidOperationException("Unable to determine project root directory.");
            }

            var templatesDirectory = Path.Combine(projectRoot, "Templates");
            
            _logService.LogInfo($"Templates directory: {templatesDirectory}");
            
            // Create the engine with the template path
            _engine = new RazorLightEngineBuilder()
                .UseFileSystemProject(templatesDirectory)
                .UseMemoryCachingProvider()
                .Build();
            
            // Test the engine by compiling a template
            foreach (var templateName in _templateService.GetAllTemplateNames())
            {
                try
                {
                    var templateFilePath = _templateService.GetTemplatePath(templateName);
                    if (!string.IsNullOrEmpty(templateFilePath) && File.Exists(templateFilePath))
                    {
                        _logService.LogInfo($"Compiling template: {templateName}");
                        var templateSource = await File.ReadAllTextAsync(templateFilePath);
                        var templateKey = GetTemplateKey(templateName);
                        
                        // Compile the template to ensure it's valid
                        await _engine.CompileRenderStringAsync(templateKey, templateSource, new { });
                        _logService.LogInfo($"Successfully compiled template: {templateName}");
                    }
                    else
                    {
                        _logService.LogWarning($"Template not found: {templateName}");
                    }
                }
                catch (Exception ex)
                {
                    _logService.LogError($"Error compiling template {templateName}", ex);
                }
            }
            
            _logService.LogInfo("RazorLight engine initialized successfully.");
        }
        catch (Exception ex)
        {
            _logService.LogError("Failed to initialize RazorLight engine", ex);
            throw;
        }
    }

    /// <summary>
    /// Renders a Razor template with the specified model.
    /// </summary>
    /// <param name="templateName">The name of the template to render.</param>
    /// <param name="model">The model to pass to the template.</param>
    /// <returns>The rendered HTML as a string.</returns>
    public async Task<string> RenderTemplateAsync(string templateName, object model)
    {
        if (_engine == null)
        {
            await InitializeAsync();
            if (_engine == null)
            {
                throw new InvalidOperationException("RazorLight engine could not be initialized.");
            }
        }

        try
        {
            _logService.LogInfo($"Rendering template: {templateName}");
            
            // Verify the template exists
            if (!_templateService.TemplateExists(templateName))
            {
                throw new FileNotFoundException($"Template '{templateName}' does not exist.");
            }

            // Get the template path
            var templateFilePath = _templateService.GetTemplatePath(templateName);
            if (string.IsNullOrEmpty(templateFilePath))
            {
                throw new FileNotFoundException($"Template path for '{templateName}' is empty.");
            }

            // Verify the model type is correct
            var expectedModelType = GetModelType(templateName);
            if (expectedModelType != null && model.GetType() != expectedModelType)
            {
                _logService.LogWarning($"Model type mismatch for template {templateName}. Expected {expectedModelType.Name}, got {model.GetType().Name}");
            }

            // Read the template content
            var templateContent = await File.ReadAllTextAsync(templateFilePath);
            var templateKey = GetTemplateKey(templateName);

            // Render the template
            var result = await _engine.CompileRenderStringAsync(templateKey, templateContent, model);
            _logService.LogInfo($"Successfully rendered template: {templateName}");
            
            return result;
        }
        catch (Exception ex)
        {
            _logService.LogError($"Error rendering template {templateName}", ex);
            throw;
        }
    }

    /// <summary>
    /// Gets the type of the model for the specified template.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <returns>The type of the model, or null if the type cannot be determined.</returns>
    public Type? GetModelType(string templateName)
    {
        if (_templateModelTypes.TryGetValue(templateName, out var modelType))
        {
            return modelType;
        }
        
        return null;
    }

    /// <summary>
    /// Initializes the template model types dictionary.
    /// </summary>
    private void InitializeTemplateModelTypes()
    {
        try
        {
            _logService.LogInfo("Initializing template model types...");
            
            // Get all types in the assembly
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();
            
            // Map template names to model types based on naming convention
            foreach (var templateName in _templateService.GetAllTemplateNames())
            {
                // Extract the base name without path
                var baseName = Path.GetFileNameWithoutExtension(templateName.Replace("\\", "."));
                
                // Look for a type named [BaseName]Instance
                var instanceTypeName = $"{baseName}Instance";
                var modelType = types.FirstOrDefault(t => string.Equals(t.Name, instanceTypeName, StringComparison.OrdinalIgnoreCase));
                
                if (modelType != null)
                {
                    _templateModelTypes[templateName] = modelType;
                    _logService.LogInfo($"Mapped template '{templateName}' to model type '{modelType.FullName}'.");
                }
                else
                {
                    _logService.LogWarning($"Could not find model type for template '{templateName}'. Expected type name: '{instanceTypeName}'.");
                }
            }
            
            _logService.LogInfo($"Initialized {_templateModelTypes.Count} template model types.");
        }
        catch (Exception ex)
        {
            _logService.LogError("Error initializing template model types", ex);
        }
    }

    /// <summary>
    /// Gets the template key for the specified template name.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <returns>The template key.</returns>
    private string GetTemplateKey(string templateName)
    {
        // Use the template name as the key, replacing backslashes with dots
        return templateName.Replace("\\", ".");
    }
}
