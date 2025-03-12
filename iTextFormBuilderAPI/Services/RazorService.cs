using System.Reflection;
using iTextFormBuilderAPI.Interfaces;
using RazorLight;

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

            // Create the engine
            _engine = CreateRazorLightEngine();

            _logService.LogInfo("RazorLight engine initialized successfully.");
        }
        catch (Exception ex)
        {
            _logService.LogError("Failed to initialize RazorLight engine", ex);
            throw;
        }
    }

    /// <summary>
    /// Creates and configures the RazorLight engine.
    /// </summary>
    /// <returns>A configured RazorLightEngine instance.</returns>
    private RazorLightEngine CreateRazorLightEngine()
    {
        // Get the project root directory
        var projectRoot = GetProjectRootDirectory();
        var templatesDirectory = Path.Combine(projectRoot, "Templates");

        _logService.LogInfo($"Templates directory: {templatesDirectory}");

        // Create the engine with the template path
        return new RazorLightEngineBuilder()
            .UseFileSystemProject(templatesDirectory)
            .UseMemoryCachingProvider()
            .Build();
    }

    /// <summary>
    /// Gets the project root directory.
    /// </summary>
    /// <returns>The project root directory path.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the project root directory cannot be determined.</exception>
    private static string GetProjectRootDirectory()
    {
        var projectRoot = Directory
            .GetParent(AppContext.BaseDirectory)
            ?.Parent?.Parent?.Parent?.FullName;

        if (string.IsNullOrEmpty(projectRoot))
        {
            throw new InvalidOperationException("Unable to determine project root directory.");
        }

        return projectRoot;
    }

    /// <summary>
    /// Renders a Razor template with the specified model.
    /// </summary>
    /// <param name="templateName">The name of the template to render.</param>
    /// <param name="model">The model to pass to the template.</param>
    /// <returns>The rendered HTML as a string.</returns>
    public async Task<string> RenderTemplateAsync(string templateName, object model)
    {
        // Ensure engine is initialized
        await EnsureEngineInitializedAsync();

        try
        {
            _logService.LogInfo($"Rendering template: {templateName}");

            // Get template path and validate
            var templateFilePath = ValidateTemplate(templateName);

            // Validate model type
            ValidateModelType(templateName, model);

            // Render the template
            return await RenderTemplateContentAsync(templateName, templateFilePath, model);
        }
        catch (Exception ex)
        {
            _logService.LogError($"Error rendering template {templateName}", ex);
            throw;
        }
    }

    /// <summary>
    /// Ensures the RazorLight engine is initialized.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the engine could not be initialized.</exception>
    private async Task EnsureEngineInitializedAsync()
    {
        if (_engine == null)
        {
            await InitializeAsync();
            if (_engine == null)
            {
                throw new InvalidOperationException("RazorLight engine could not be initialized.");
            }
        }
    }

    /// <summary>
    /// Validates the template exists and returns its file path.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <returns>The template file path.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the template file path is empty.</exception>
    private string ValidateTemplate(string templateName)
    {
        var templateFilePath = _templateService.GetTemplatePath(templateName);
        if (string.IsNullOrEmpty(templateFilePath))
        {
            throw new FileNotFoundException($"Template path for '{templateName}' is empty.");
        }
        return templateFilePath;
    }

    /// <summary>
    /// Validates the model type for a template.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <param name="model">The model object.</param>
    private void ValidateModelType(string templateName, object model)
    {
        var expectedModelType = GetModelType(templateName);
        if (expectedModelType != null && model.GetType() != expectedModelType)
        {
            _logService.LogWarning(
                $"Model type mismatch for template {templateName}. Expected {expectedModelType.Name}, got {model.GetType().Name}"
            );
        }
    }

    /// <summary>
    /// Renders the template with the provided model.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <param name="templateFilePath">The template file path.</param>
    /// <param name="model">The model to pass to the template.</param>
    /// <returns>The rendered HTML as a string.</returns>
    private async Task<string> RenderTemplateContentAsync(
        string templateName,
        string templateFilePath,
        object model
    )
    {
        // Read the template content
        var templateContent = await File.ReadAllTextAsync(templateFilePath);
        var templateKey = GetTemplateKey(templateName);

        // Render the template
        var result = await _engine!.CompileRenderStringAsync(templateKey, templateContent, model);
        _logService.LogInfo($"Successfully rendered template: {templateName}");

        return result;
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

            // Get potential model types
            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();
            var potentialModelTypes = FindPotentialModelTypes(types);
            LogPotentialModelTypes(potentialModelTypes);

            // Map template names to model types
            MapTemplatesToModelTypes(assembly, types);

            _logService.LogInfo($"Initialized {_templateModelTypes.Count} template model types.");
        }
        catch (Exception ex)
        {
            _logService.LogError("Error initializing template model types", ex);
        }
    }

    /// <summary>
    /// Finds potential model types from the assembly types.
    /// </summary>
    /// <param name="types">All types in the assembly.</param>
    /// <returns>A list of potential model types.</returns>
    private static List<Type> FindPotentialModelTypes(Type[] types)
    {
        return types
            .Where(t => t.Name.EndsWith("Instance", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Logs information about potential model types.
    /// </summary>
    /// <param name="potentialModelTypes">The list of potential model types.</param>
    private void LogPotentialModelTypes(List<Type> potentialModelTypes)
    {
        _logService.LogInfo($"Found {potentialModelTypes.Count} potential model types:");
        foreach (var type in potentialModelTypes)
        {
            _logService.LogInfo(
                $"  - {type.FullName} (name: {type.Name}, namespace: {type.Namespace})"
            );
        }
    }

    /// <summary>
    /// Maps template names to their corresponding model types.
    /// </summary>
    /// <param name="assembly">The executing assembly.</param>
    /// <param name="types">All types in the assembly.</param>
    private void MapTemplatesToModelTypes(Assembly assembly, Type[] types)
    {
        foreach (var templateName in _templateService.GetAllTemplateNames())
        {
            try
            {
                MapSingleTemplateToModelType(templateName, assembly, types);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error processing template '{templateName}'", ex);
            }
        }
    }

    /// <summary>
    /// Maps a single template to its corresponding model type by extracting parts of the template name,
    /// building the full type name, and then searching for the model type in the given types.
    /// </summary>
    /// <param name="templateName">The template file name with its path.</param>
    /// <param name="assembly">The assembly where the model types are defined.</param>
    /// <param name="types">An array of available types to search for the model.</param>
    private void MapSingleTemplateToModelType(string templateName, Assembly assembly, Type[] types)
    {
        var baseName = GetBaseName(templateName);
        var directoryPath = GetDirectoryPath(templateName);

        _logService.LogInfo(
            $"Looking for model for template '{templateName}' with baseName '{baseName}' and directoryPath '{directoryPath ?? "<none>"}'"
        );

        var modelName = GetModelName(baseName);
        var modelNamespace = GetModelNamespace(assembly, directoryPath!);
        var fullTypeName = GetFullTypeName(modelNamespace, modelName);

        _logService.LogInfo($"Looking for model type with full name: {fullTypeName}");

        var modelType = FindModelType(types, fullTypeName, modelName, modelNamespace);
        if (modelType != null)
        {
            _templateModelTypes[templateName] = modelType;
            _logService.LogInfo(
                $"Mapped template '{templateName}' to model type '{modelType.FullName}'."
            );
        }
        else
        {
            _logService.LogWarning(
                $"Could not find model type for template '{templateName}'. Looked for '{modelName}'"
            );
        }
    }

    /// <summary>
    /// Extracts the base name from the template file name without its extension.
    /// </summary>
    /// <param name="templateName">The full template file name.</param>
    /// <returns>The base name of the template.</returns>
    private static string GetBaseName(string templateName)
    {
        return Path.GetFileNameWithoutExtension(templateName);
    }

    /// <summary>
    /// Extracts the directory path from the template name and converts any directory separators to periods,
    /// which are more suitable for namespaces.
    /// </summary>
    /// <param name="templateName">The full template file name.</param>
    /// <returns>The directory path formatted as a namespace segment, or null if none exists.</returns>
    private static string GetDirectoryPath(string templateName)
    {
        return Path.GetDirectoryName(templateName)?.Replace("\\", ".")!;
    }

    /// <summary>
    /// Constructs the model name by appending 'Instance' to the base name.
    /// </summary>
    /// <param name="baseName">The base name of the template.</param>
    /// <returns>The constructed model name.</returns>
    private static string GetModelName(string baseName)
    {
        return $"{baseName}Instance";
    }

    /// <summary>
    /// Builds the model namespace using the assembly name and the optional directory path.
    /// </summary>
    /// <param name="assembly">The assembly containing the model definitions.</param>
    /// <param name="directoryPath">The directory path from the template.</param>
    /// <returns>The full namespace for the model.</returns>
    private static string GetModelNamespace(Assembly assembly, string directoryPath)
    {
        return !string.IsNullOrEmpty(directoryPath)
            ? $"{assembly.GetName().Name}.Models.{directoryPath}"
            : $"{assembly.GetName().Name}.Models";
    }

    /// <summary>
    /// Constructs the full type name by combining the model namespace and model name.
    /// </summary>
    /// <param name="modelNamespace">The namespace of the model.</param>
    /// <param name="modelName">The name of the model.</param>
    /// <returns>The full type name.</returns>
    private static string GetFullTypeName(string modelNamespace, string modelName)
    {
        return $"{modelNamespace}.{modelName}";
    }

    /// <summary>
    /// Searches through the provided types for one that matches the full type name or that has the expected model name
    /// and a namespace that starts with the expected model namespace.
    /// </summary>
    /// <param name="types">The list of types to search.</param>
    /// <param name="fullTypeName">The full type name to search for.</param>
    /// <param name="modelName">The model name to match.</param>
    /// <param name="modelNamespace">The expected starting namespace.</param>
    /// <returns>The matching model type, or null if no match is found.</returns>
    private static Type? FindModelType(
        Type[] types,
        string fullTypeName,
        string modelName,
        string modelNamespace
    )
    {
        return types.FirstOrDefault(t =>
            string.Equals(t.FullName, fullTypeName, StringComparison.OrdinalIgnoreCase)
            || (
                string.Equals(t.Name, modelName, StringComparison.OrdinalIgnoreCase)
                && t.Namespace?.StartsWith(modelNamespace) == true
            )
        );
    }

    /// <summary>
    /// Gets the template key for the specified template name.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <returns>The template key.</returns>
    private static string GetTemplateKey(string templateName)
    {
        // Use the template name as the key, replacing backslashes with dots
        return templateName.Replace("\\", ".");
    }
}
