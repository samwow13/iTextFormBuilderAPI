using iTextFormBuilderAPI.Interfaces;
using Moq;
using System.IO.Abstractions.TestingHelpers;

namespace iTextFormBuilderAPITests.TestHelpers
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

            mockFileSystem.AddDirectory(mockProjectRoot);
            mockFileSystem.AddDirectory(templatesPath);

            mockFileSystem.AddFile(
                Path.Combine(templatesPath, "HealthTestTemplate.cshtml"),
                new MockFileData("@model iTextFormBuilderAPI.Models.HealthAndWellness.TestRazorDataModels.TestRazorDataInstance\n<html><body>Test template content</body></html>")
            );

            mockFileSystem.AddFile(
                Path.Combine(templatesPath, "TestRazorDataAssessment", "TestRazorDataAssessment.cshtml"),
                new MockFileData("@model iTextFormBuilderAPI.Models.HealthAndWellness.TestRazorDataModels.TestRazorDataInstance\n<html><body>Test Razor template content</body></html>")
            );

            return mockFileSystem;
        }

        /// <summary>
        /// Creates a mock log service for testing.
        /// </summary>
        /// <returns>A configured Mock<ILogService>.</returns>
        public static Mock<ILogService> CreateMockLogService()
        {
            return new Mock<ILogService>();
        }
    }
}
