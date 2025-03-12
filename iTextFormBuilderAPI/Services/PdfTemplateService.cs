using System.Diagnostics;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Utilities;

namespace iTextFormBuilderAPI.Services;

/// <summary>
/// Service responsible for managing PDF templates, including tracking available templates
/// and validating template existence.
/// </summary>
public class PdfTemplateService : IPdfTemplateService
{
    private readonly string _templateBasePath;
    private readonly ILogService? _logService;

    /// <summary>
    /// Initializes a new instance of the PdfTemplateService class.
    /// </summary>
    /// <param name="logService">Optional log service for logging messages.</param>
    public PdfTemplateService(ILogService? logService = null)
    {
        _logService = logService;

        // Determine the project root directory
        var projectRoot = Directory
            .GetParent(AppContext.BaseDirectory)
            ?.Parent?.Parent?.Parent?.FullName;

        if (projectRoot == null)
        {
            Trace.WriteLine("Unable to determine project root directory.");
            _templateBasePath = string.Empty;
        }
        else
        {
            _templateBasePath = Path.Combine(projectRoot, "Templates");
        }

        // Ensure the templates directory exists
        if (!string.IsNullOrEmpty(_templateBasePath) && !Directory.Exists(_templateBasePath))
        {
            Directory.CreateDirectory(_templateBasePath);
        }
    }

    /// <summary>
    /// Gets all valid template names registered in the system.
    /// </summary>
    /// <returns>A collection of valid template names.</returns>
    public IEnumerable<string> GetAllTemplateNames()
    {
        return PdfTemplateRegistry.ValidTemplates;
    }

    /// <summary>
    /// Checks if a template with the specified name exists.
    /// </summary>
    /// <param name="templateName">The name of the template to check.</param>
    /// <returns>True if the template exists, false otherwise.</returns>
    public bool TemplateExists(string templateName)
    {
        // Only check if the template name is in our registry
        // Do not verify file existence here - let that be handled later if needed
        var exists = PdfTemplateRegistry.ValidTemplates.Contains(
            templateName,
            StringComparer.OrdinalIgnoreCase
        );

        _logService?.LogInfo($"Template '{templateName}' exists in registry: {exists}");
        return exists;
    }

    /// <summary>
    /// Gets the full path to a template file.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <returns>The full path to the template file, or an empty string if the template doesn't exist.</returns>
    public string GetTemplatePath(string templateName)
    {
        // Check if the template is in our registry before attempting to locate the file
        if (!TemplateExists(templateName))
        {
            _logService?.LogWarning(
                $"Template '{templateName}' not found in registry when attempting to get path"
            );
            return string.Empty;
        }

        // Build template file path based on whether it has directory structure
        string fileName;
        string filePath;

        if (templateName.Contains("\\"))
        {
            // For templates with a directory structure like "HealthAndWellness\TestRazor",
            // Use format: "Templates\HealthAndWellness\TestRazorTemplate.cshtml"
            var directory = Path.GetDirectoryName(templateName);
            var baseName = Path.GetFileName(templateName);
            fileName = $"{baseName}Template.cshtml";
            filePath = Path.Combine(_templateBasePath, directory ?? string.Empty, fileName);

            _logService?.LogInfo(
                $"Searching for template '{templateName}' in directory '{directory}' with base name '{baseName}'"
            );
        }
        else
        {
            // For flat templates, use format: "Templates\TestRazorTemplate.cshtml"
            fileName = $"{templateName}Template.cshtml";
            filePath = Path.Combine(_templateBasePath, fileName);
        }
        // Verify file exists and log results
        _logService?.LogInfo($"Looking for template at: {filePath}");

        if (!File.Exists(filePath))
        {
            if (templateName.Contains("\\"))
            {
                var directory = Path.GetDirectoryName(templateName);
                var baseName = Path.GetFileName(templateName);
                _logService?.LogWarning(
                    $"No template file found for '{templateName}' in directory '{directory}' with base name '{baseName}'. Attempted path: {filePath}"
                );
            }
            else
            {
                _logService?.LogWarning(
                    $"No template file found for '{templateName}'. Attempted path: {filePath}"
                );
            }
            return string.Empty;
        }

        _logService?.LogInfo($"Found template at: {filePath}");
        return filePath;
    }

    /// <summary>
    /// Gets the count of valid templates in the system.
    /// </summary>
    /// <returns>The number of valid templates.</returns>
    public int GetTemplateCount()
    {
        return PdfTemplateRegistry.ValidTemplates.Count;
    }
}
