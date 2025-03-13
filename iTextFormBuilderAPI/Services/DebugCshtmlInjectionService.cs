using System.Text.RegularExpressions;
using iTextFormBuilderAPI.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace iTextFormBuilderAPI.Services
{
    /// <summary>
    /// Service responsible for injecting debugging information into CSHTML content.
    /// </summary>
    public class DebugCshtmlInjectionService : IDebugCshtmlInjectionService
    {
        private readonly ILogService _logService;
        private readonly IConfiguration _configuration;
        private bool _modelDebuggingEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugCshtmlInjectionService"/> class.
        /// </summary>
        /// <param name="logService">The logging service</param>
        /// <param name="configuration">The application configuration</param>
        public DebugCshtmlInjectionService(ILogService logService, IConfiguration configuration)
        {
            _logService = logService;
            _configuration = configuration;
            _modelDebuggingEnabled = _configuration.GetValue<bool>("Debug:ModelDebuggingEnabled");
        }

        /// <summary>
        /// Gets or sets whether model debugging is enabled.
        /// When true, JSON representation of the model will be displayed at the top of generated PDFs.
        /// This value is read from application configuration.
        /// </summary>
        public bool ModelDebuggingEnabled
        {
            get => _modelDebuggingEnabled;
            set => _modelDebuggingEnabled = value;
        }

        /// <summary>
        /// Injects model debugging information at the top of the HTML content if debugging is enabled.
        /// </summary>
        /// <param name="cshtmlContent">The CSHTML content to inject debugging info into</param>
        /// <param name="model">The model object to display as JSON</param>
        /// <returns>HTML content with injected model debugging information if enabled, otherwise the original content</returns>
        public string InjectModelDebugInfoIfEnabled(string cshtmlContent, object model)
        {
            // Only inject debugging info if enabled
            if (!ModelDebuggingEnabled)
                return cshtmlContent;

            try
            {
                return InjectModelDebugInfo(cshtmlContent, model);
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error injecting model debug info: {ex.Message}", ex);
                return cshtmlContent; // Return original content if injection fails
            }
        }

        /// <summary>
        /// Injects model debugging information at the top of the HTML content.
        /// </summary>
        /// <param name="cshtmlContent">The CSHTML content to inject debugging info into</param>
        /// <param name="model">The model object to display as JSON</param>
        /// <returns>HTML content with injected model debugging information</returns>
        /// <remarks>
        /// This adds a formatted JSON representation of the model at the top of the document,
        /// which is useful for debugging purposes to verify data is being passed correctly.
        /// </remarks>
        private string InjectModelDebugInfo(string cshtmlContent, object model)
        {
            try
            {
                // Create the debugging HTML block with the model JSON
                var modelJson = JsonConvert.SerializeObject(model, Formatting.Indented);

                // Apply JSON syntax highlighting
                var highlightedJson = ApplyJsonSyntaxHighlighting(modelJson);

                // For PDFs, we need to ensure all content is visible without scrollbars
                // Use word-wrap and white-space styling to ensure long lines wrap properly
                var debuggingHtml =
                    $@"<div style=""background-color: #1e1e1e; padding: 20px; border-radius: 6px; margin-bottom: 25px; font-family: 'Consolas', 'Monaco', monospace;"">
<strong style=""display: block; margin-bottom: 15px; font-size: 18px; color: #ffffff;"">
Model Values (Debug Mode)</strong>
<pre style=""margin: 0; white-space: pre-wrap; word-wrap: break-word; font-size: 14px; line-height: 1.5; color: #d4d4d4;"">{highlightedJson}</pre>
</div>";

                // Find the <body> tag to insert after
                var bodyStartTagPattern = "<body[^>]*>";
                var match = Regex.Match(
                    cshtmlContent,
                    bodyStartTagPattern,
                    RegexOptions.IgnoreCase
                );

                if (match.Success)
                {
                    // Insert after the <body> tag
                    int insertPosition = match.Index + match.Length;
                    return cshtmlContent.Insert(insertPosition, debuggingHtml);
                }
                else
                {
                    // If no body tag found, insert at the top of the document
                    _logService.LogWarning(
                        "No <body> tag found in HTML content for model debugging. Adding to the beginning."
                    );
                    return debuggingHtml + cshtmlContent;
                }
            }
            catch (Exception ex)
            {
                _logService.LogError($"Error injecting model debug info: {ex.Message}", ex);
                return cshtmlContent; // Return original content if injection fails
            }
        }

        /// <summary>
        /// Applies syntax highlighting to a JSON string to improve readability.
        /// </summary>
        /// <param name="jsonString">The JSON string to highlight</param>
        /// <returns>HTML string with syntax highlighting applied</returns>
        private static string ApplyJsonSyntaxHighlighting(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return string.Empty;

            // Replace special characters to prevent HTML injection
            jsonString = jsonString.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

            // Apply syntax highlighting using regex patterns
            // Color property names
            jsonString = Regex.Replace(
                jsonString,
                "\"([^\"]+)\":",
                "<span style=\"color: #9cdcfe;\">\"$1\"</span>:"
            );

            // Color string values
            jsonString = Regex.Replace(
                jsonString,
                ": \"([^\"]*)\"(,|\n|$)",
                ": <span style=\"color: #ce9178;\">\"$1\"</span>$2"
            );

            // Color numbers
            jsonString = Regex.Replace(
                jsonString,
                "(: |\\[|,)(-?\\d+(?:\\.\\d+)?)",
                "$1<span style=\"color: #b5cea8;\">$2</span>"
            );

            // Color booleans and null
            jsonString = Regex.Replace(
                jsonString,
                "\\b(true|false|null)\\b",
                "<span style=\"color: #569cd6;\">$1</span>"
            );

            // Color brackets and braces
            jsonString = Regex.Replace(
                jsonString,
                "([\\[\\]{}])",
                "<span style=\"color: #d4d4d4;\">$1</span>"
            );

            return jsonString;
        }
    }
}
