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
        // Check if the template is in our registry but don't look for the file yet
        if (
            !PdfTemplateRegistry.ValidTemplates.Contains(
                templateName,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            _logService?.LogWarning(
                $"Template '{templateName}' not found in registry (attempted paths: {Path.Combine(_templateBasePath, templateName)})"
            );
            return string.Empty;
        }

        // For templates with a directory structure like "HealthAndWellness\TestRazor",
        // we need to look for "Templates\HealthAndWellness\TestRazorDataAssessment.cshtml"
        string filePath;

        if (templateName.Contains("\\"))
        {
            // Extract the directory and filename parts
            var directory = Path.GetDirectoryName(templateName);
            var baseName = Path.GetFileName(templateName);

            // Try multiple naming patterns for the template file
            var possibleFileNames = new[]{
                $"{baseName}Template.cshtml",    // Format: TestRazorTemplate.cshtml
                $"{baseName}DataAssessment.cshtml", // Format: TestRazorDataAssessment.cshtml
                $"{baseName}Assessment.cshtml" // Format: TestRazorAssessment.cshtml
            };

            bool fileFound = false;
            filePath = string.Empty;

            _logService?.LogInfo($"Searching for template '{templateName}' in directory '{directory}' with base name '{baseName}'");

            foreach (var fileName in possibleFileNames)
            {
                var testPath = Path.Combine(_templateBasePath, directory ?? string.Empty, fileName);
                _logService?.LogInfo($"Looking for template at: {testPath}");

                if (File.Exists(testPath))
                {
                    filePath = testPath;
                    fileFound = true;
                    _logService?.LogInfo($"Found template at: {filePath}");
                    break;
                }
            }

            if (!fileFound)
            {
                _logService?.LogWarning(
                    $"No template file found for '{templateName}' in directory '{directory}' with base name '{baseName}'. Attempted paths: {string.Join(", ", possibleFileNames.Select(f => Path.Combine(_templateBasePath, directory ?? string.Empty, f)))}"
                );
                return string.Empty;
            }
        }
        else
        {
            // For flat templates (no directory), try both naming conventions
            var possibleFileNames = new[]{
                $"{templateName}Template.cshtml",
                $"{templateName}DataAssessment.cshtml",
                $"{templateName}Assessment.cshtml",
            };

            bool fileFound = false;
            filePath = string.Empty;

            foreach (var fileName in possibleFileNames)
            {
                var testPath = Path.Combine(_templateBasePath, fileName);
                _logService?.LogInfo($"Looking for template at: {testPath}");

                if (File.Exists(testPath))
                {
                    filePath = testPath;
                    fileFound = true;
                    _logService?.LogInfo($"Found template at: {filePath}");
                    break;
                }
            }

            if (!fileFound)
            {
                _logService?.LogWarning($"No template file found for '{templateName}'. Attempted paths: {string.Join(", ", possibleFileNames.Select(f => Path.Combine(_templateBasePath, f)))}");
                return string.Empty;
            }
        }

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
