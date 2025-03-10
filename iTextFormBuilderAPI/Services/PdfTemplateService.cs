using System.Diagnostics;
using System.IO;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Utilities;

namespace iTextFormBuilderAPI.Services;

/// <summary>
/// Service responsible for managing PDF templates, including tracking available templates
/// and validating template existence.
/// </summary>
public class PdfTemplateService : iTextFormBuilderAPI.Interfaces.IPdfTemplateService
{
    private readonly string _templateBasePath;

    /// <summary>
    /// Initializes a new instance of the PdfTemplateService class.
    /// </summary>
    public PdfTemplateService()
    {
        // Determine the project root directory
        var projectRoot = System.IO.Directory
            .GetParent(System.AppContext.BaseDirectory)
            ?.Parent?.Parent?.Parent?.FullName;

        if (projectRoot == null)
        {
            System.Diagnostics.Trace.WriteLine("Unable to determine project root directory.");
            _templateBasePath = string.Empty;
        }
        else
        {
            _templateBasePath = System.IO.Path.Combine(projectRoot, "Templates");
        }

        // Ensure the templates directory exists
        if (!string.IsNullOrEmpty(_templateBasePath) && !System.IO.Directory.Exists(_templateBasePath))
        {
            System.IO.Directory.CreateDirectory(_templateBasePath);
        }
    }

    /// <summary>
    /// Gets all valid template names registered in the system.
    /// </summary>
    /// <returns>A collection of valid template names.</returns>
    public System.Collections.Generic.IEnumerable<string> GetAllTemplateNames()
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
        // First check if the template name is in our registry
        if (!PdfTemplateRegistry.ValidTemplates.Contains(templateName, System.StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // Then verify the template file actually exists
        if (string.IsNullOrEmpty(_templateBasePath))
        {
            return false;
        }

        var templatePath = System.IO.Path.Combine(_templateBasePath, $"{templateName}.cshtml");
        return System.IO.File.Exists(templatePath);
    }

    /// <summary>
    /// Gets the full path to a template file.
    /// </summary>
    /// <param name="templateName">The name of the template.</param>
    /// <returns>The full path to the template file, or an empty string if the template doesn't exist.</returns>
    public string GetTemplatePath(string templateName)
    {
        if (!TemplateExists(templateName))
        {
            return string.Empty;
        }

        return System.IO.Path.Combine(_templateBasePath, $"{templateName}.cshtml");
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
