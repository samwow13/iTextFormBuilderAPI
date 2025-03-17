using iTextFormBuilderAPI.Interfaces;
using Moq;
using System.IO.Abstractions.TestingHelpers;

namespace iTextFormBuilderAPI.Tests.TestHelpers
{
    /// <summary>
    /// Provides utility methods and common setup for tests.
    /// </summary>
    public static class TestUtils
    {
        /// <summary>
        /// Creates a mock file system for template testing.
        /// </summary>
        /// <param name="mockProjectRoot">The root directory path for the mock project.</param>
        /// <returns>A configured MockFileSystem with template directories and files.</returns>
        public static MockFileSystem CreateMockTemplateFileSystem(string mockProjectRoot)
        {
            var mockFileSystem = new MockFileSystem();
            var templatesPath = Path.Combine(mockProjectRoot, "Templates");
            
            // Create the templates directory
            mockFileSystem.AddDirectory(templatesPath);
            
            // Add a global styles file
            mockFileSystem.AddFile(
                Path.Combine(templatesPath, "globalStyles.css"), 
                new MockFileData(".test { color: black; }"));
            
            // Add a standard template
            mockFileSystem.AddFile(
                Path.Combine(templatesPath, "TestTemplate.cshtml"), 
                new MockFileData("@model dynamic\n<html><body>Test template content</body></html>"));
            
            // Add a template in a subdirectory
            var healthDirPath = Path.Combine(templatesPath, "HealthAndWellness");
            mockFileSystem.AddDirectory(healthDirPath);
            mockFileSystem.AddFile(
                Path.Combine(healthDirPath, "TestRazorTemplate.cshtml"), 
                new MockFileData("@model iTextFormBuilderAPI.Models.HealthAndWellness.TestRazorDataModels.TestRazorDataInstance\n<html><body>Test Razor template content</body></html>"));
            
            return mockFileSystem;
        }

        /// <summary>
        /// Creates a mock log service for testing.
        /// </summary>
        /// <returns>A mock ILogService.</returns>
        public static Mock<ILogService> CreateMockLogService()
        {
            var mockLogService = new Mock<ILogService>();
            mockLogService.Setup(l => l.LogInfo(It.IsAny<string>()));
            mockLogService.Setup(l => l.LogWarning(It.IsAny<string>()));
            mockLogService.Setup(l => l.LogError(It.IsAny<string>()));
            mockLogService.Setup(l => l.LogError(It.IsAny<string>(), It.IsAny<Exception>()));
            
            return mockLogService;
        }
    }
}
