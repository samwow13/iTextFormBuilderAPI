using iTextFormBuilderAPI.Interfaces;
using RazorLight;
using RazorLight.Razor;
using System.Diagnostics;

namespace iTextFormBuilderAPI.Services
{
    /// <summary>
    /// In-memory template service implementation for RazorLight template engine.
    /// Stores and manages templates in memory for PDF generation while also supporting loading from file system.
    /// </summary>
    public class RazorTemplateService : IRazorTemplateService
    {
        private readonly Dictionary<string, string> _templates = new Dictionary<string, string>();
        private readonly string _templateBasePath;

        /// <summary>
        /// Initializes a new instance of the RazorTemplateService
        /// </summary>
        public RazorTemplateService()
        {
            // Set default template path to the Templates folder in the application root
            string? baseDirectoryPath = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
            _templateBasePath = !string.IsNullOrEmpty(baseDirectoryPath)
                ? Path.Combine(baseDirectoryPath, "Templates")
                : Path.Combine(AppContext.BaseDirectory, "Templates");
            
            // Log the template base path for debugging purposes
            Debug.WriteLine($"Template base path: {_templateBasePath}");
        }

        /// <summary>
        /// Retrieves a RazorLight project item from the in-memory project.
        /// </summary>
        /// <param name="templateKey">Key of the template to retrieve</param>
        /// <returns>RazorLight project item containing the template content</returns>
        public Task<RazorLightProjectItem> GetTemplateAsync(string templateKey)
        {
            string content = string.Empty;
            if (!string.IsNullOrEmpty(templateKey))
            {
                _templates.TryGetValue(templateKey, out content);
                // If content is null, return empty string instead
                content ??= string.Empty;
            }

            return Task.FromResult<RazorLightProjectItem>(
                new TextSourceRazorProjectItem(templateKey, content)
            );
        }

        /// <summary>
        /// Adds a template to the in-memory project.
        /// </summary>
        /// <param name="key">Key of the template to add</param>
        /// <param name="template">Content of the template to add</param>
        public void AddTemplate(string key, string template)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.WriteLine("Cannot add template with null or empty key");
                return;
            }

            _templates[key] = template ?? string.Empty;
            Debug.WriteLine($"Template added: {key}");
        }

        /// <summary>
        /// Loads a template from the filesystem
        /// </summary>
        /// <param name="templateName">Name of the template file (without extension)</param>
        /// <param name="templateType">Type of template (e.g., "cshtml")</param>
        /// <returns>True if template was loaded successfully, false otherwise</returns>
        public bool LoadTemplateFromFile(string templateName, string templateType = "cshtml")
        {
            if (string.IsNullOrEmpty(templateName))
            {
                Debug.WriteLine("Cannot load template with null or empty name");
                return false;
            }

            try
            {
                // Find the template file based on various potential locations
                string templatePath = FindTemplateFile(templateName, templateType);
                
                if (string.IsNullOrEmpty(templatePath))
                {
                    Debug.WriteLine($"Template file not found: {templateName}.{templateType}");
                    return false;
                }

                // Read the template content and add it to the in-memory store
                string templateContent = File.ReadAllText(templatePath);
                string templateKey = $"{templateName}.{templateType}";
                AddTemplate(templateKey, templateContent);
                
                Debug.WriteLine($"Template loaded from file: {templatePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading template {templateName}.{templateType}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a template exists in the service
        /// </summary>
        /// <param name="templateName">Name of the template to check</param>
        /// <returns>True if the template exists, false otherwise</returns>
        public bool TemplateExists(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return false;
            }

            // First check if it's already in our in-memory collection
            string templateKey = $"{templateName}.cshtml";
            if (_templates.ContainsKey(templateKey))
            {
                return true;
            }

            // If not in memory, check if it exists on disk
            string templatePath = FindTemplateFile(templateName, "cshtml");
            return !string.IsNullOrEmpty(templatePath);
        }

        /// <summary>
        /// Gets the content of a template
        /// </summary>
        /// <param name="templateName">Name of the template to get content for</param>
        /// <returns>The content of the template</returns>
        public string GetTemplateContent(string templateName)
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return string.Empty;
            }

            string templateKey = $"{templateName}.cshtml";

            // First check if template is already loaded in memory
            if (_templates.TryGetValue(templateKey, out string content) && !string.IsNullOrEmpty(content))
            {
                return content;
            }

            // If not in memory, try to load from file
            if (LoadTemplateFromFile(templateName, "cshtml"))
            {
                return _templates[templateKey] ?? string.Empty;
            }

            // Template not found
            return string.Empty;
        }

        /// <summary>
        /// Gets a RazorLight engine configured to use this template service
        /// </summary>
        /// <returns>A configured RazorLight engine</returns>
        public IRazorLightEngine GetRazorEngine()
        {
            var project = new RazorLightEmbeddedResourcesProject(this);
            
            return new RazorLightEngineBuilder()
                .UseMemoryCachingProvider()
                .UseProject(project)
                .EnableDebugMode()
                .Build();
        }

        /// <summary>
        /// Helper class to create a RazorLight project from our service
        /// </summary>
        private class RazorLightEmbeddedResourcesProject : RazorLightProject
        {
            private readonly RazorTemplateService _service;

            public RazorLightEmbeddedResourcesProject(RazorTemplateService service)
            {
                _service = service;
            }

            public override Task<RazorLightProjectItem> GetItemAsync(string templateKey)
            {
                return _service.GetTemplateAsync(templateKey);
            }

            public override Task<IEnumerable<RazorLightProjectItem>> GetImportsAsync(string templateKey)
            {
                return Task.FromResult<IEnumerable<RazorLightProjectItem>>(
                    Array.Empty<RazorLightProjectItem>()
                );
            }
        }

        /// <summary>
        /// Finds the template file in various potential locations
        /// </summary>
        /// <param name="templateName">Template name without extension</param>
        /// <param name="templateType">Template file extension</param>
        /// <returns>Full path to the template file if found, empty string otherwise</returns>
        private string FindTemplateFile(string templateName, string templateType)
        {
            if (string.IsNullOrEmpty(templateName) || string.IsNullOrEmpty(templateType))
            {
                return string.Empty;
            }

            string fileName = $"{templateName}.{templateType}";
            string baseDirectory = AppContext.BaseDirectory;
            string? parentDirectory = Directory.GetParent(baseDirectory)?.FullName ?? string.Empty;

            List<string> possibleLocations = new List<string>
            {
                // Primary location: Templates folder in the application root
                Path.Combine(_templateBasePath, fileName),
                
                // Alternative location: Templates folder in the current directory
                Path.Combine(Directory.GetCurrentDirectory(), "Templates", fileName),
                
                // Alternative location: Check if it's directly in the base path directory
                Path.Combine(parentDirectory, fileName),
                
                // Last resort: Check in the bin directory
                Path.Combine(baseDirectory, "Templates", fileName)
            };

            // Check each possible location until we find the file
            foreach (string path in possibleLocations)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty; // Not found
        }
    }
}
