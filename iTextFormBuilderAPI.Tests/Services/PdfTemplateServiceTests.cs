using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Services;
using iTextFormBuilderAPI.Utilities;
using Moq;
using System.Reflection;
using Xunit;

namespace iTextFormBuilderAPI.Tests.Services
{
    /// <summary>
    /// Tests for the PdfTemplateService class.
    /// </summary>
    public class PdfTemplateServiceTests
    {
        private readonly Mock<ILogService> _mockLogService;
        private readonly MockFileSystem _mockFileSystem;
        private readonly string _mockProjectRoot;
        private readonly string _mockTemplatesPath;
        private readonly Mock<IPdfTemplateService> _mockPdfTemplateService;

        /// <summary>
        /// Initializes a new instance of the PdfTemplateServiceTests class.
        /// </summary>
        public PdfTemplateServiceTests()
        {
            // Setup the mock log service
            _mockLogService = new Mock<ILogService>();

            // Setup mock file system
            _mockFileSystem = new MockFileSystem();
            
            // Create a mock project root path
            _mockProjectRoot = "C:\\MockProject";
            _mockTemplatesPath = Path.Combine(_mockProjectRoot, "Templates");

            // Create the mock templates directory
            _mockFileSystem.AddDirectory(_mockTemplatesPath);
            
            // Add some test template files
            _mockFileSystem.AddFile(
                Path.Combine(_mockTemplatesPath, "TestTemplate.cshtml"), 
                new MockFileData("<html><body>Test template content</body></html>"));
            
            // Add a template in a subdirectory
            var subDirPath = Path.Combine(_mockTemplatesPath, "HealthAndWellness");
            _mockFileSystem.AddDirectory(subDirPath);
            _mockFileSystem.AddFile(
                Path.Combine(subDirPath, "TestRazorTemplate.cshtml"), 
                new MockFileData("<html><body>Test Razor template content</body></html>"));

            // Setup mock IPdfTemplateService
            _mockPdfTemplateService = new Mock<IPdfTemplateService>();
        }

        /// <summary>
        /// Tests that GetAllTemplateNames returns the correct template names.
        /// </summary>
        [Fact]
        public void GetAllTemplateNames_ReturnsCorrectTemplates()
        {
            // Arrange - Setup the mock IPdfTemplateService
            var expectedTemplates = new List<string> { "Test", "HealthAndWellness\\TestRazor" };
            _mockPdfTemplateService.Setup(s => s.GetAllTemplateNames()).Returns(expectedTemplates);

            // Act
            var templates = _mockPdfTemplateService.Object.GetAllTemplateNames().ToList();

            // Assert
            Assert.Equal(2, templates.Count);
            Assert.Contains("Test", templates);
            Assert.Contains("HealthAndWellness\\TestRazor", templates);
        }

        /// <summary>
        /// Tests that TemplateExists returns true for a valid template.
        /// </summary>
        [Fact]
        public void TemplateExists_ValidTemplate_ReturnsTrue()
        {
            // Arrange - Setup the mock IPdfTemplateService
            _mockPdfTemplateService.Setup(s => s.TemplateExists("Test")).Returns(true);

            // Act
            bool exists = _mockPdfTemplateService.Object.TemplateExists("Test");

            // Assert
            Assert.True(exists);
        }

        /// <summary>
        /// Tests that TemplateExists returns false for an invalid template.
        /// </summary>
        [Fact]
        public void TemplateExists_InvalidTemplate_ReturnsFalse()
        {
            // Arrange - Setup the mock IPdfTemplateService
            _mockPdfTemplateService.Setup(s => s.TemplateExists("NonExistentTemplate")).Returns(false);

            // Act
            var exists = _mockPdfTemplateService.Object.TemplateExists("NonExistentTemplate");

            // Assert
            Assert.False(exists);
        }

        /// <summary>
        /// Tests that GetTemplateCount returns the correct number of templates.
        /// </summary>
        [Fact]
        public void GetTemplateCount_ReturnsCorrectCount()
        {
            // Arrange - Setup the mock IPdfTemplateService
            _mockPdfTemplateService.Setup(s => s.GetTemplateCount()).Returns(2);

            // Act
            var count = _mockPdfTemplateService.Object.GetTemplateCount();

            // Assert
            Assert.Equal(2, count);
        }

        /// <summary>
        /// Tests that GetTemplatePath returns the correct path for a valid template.
        /// </summary>
        [Fact]
        public void GetTemplatePath_ValidTemplate_ReturnsPath()
        {
            // Arrange - Setup the mock IPdfTemplateService
            var expectedPath = Path.Combine(_mockTemplatesPath, "TestTemplate.cshtml");
            _mockPdfTemplateService.Setup(s => s.GetTemplatePath("Test")).Returns(expectedPath);

            // Act
            var path = _mockPdfTemplateService.Object.GetTemplatePath("Test");

            // Assert
            Assert.NotEmpty(path);
            Assert.Contains("TestTemplate.cshtml", path);
        }

        /// <summary>
        /// Tests that GetTemplatePath returns an empty string for an invalid template.
        /// </summary>
        [Fact]
        public void GetTemplatePath_InvalidTemplate_ReturnsEmptyString()
        {
            // Arrange - Setup the mock IPdfTemplateService
            _mockPdfTemplateService.Setup(s => s.GetTemplatePath("NonExistentTemplate")).Returns(string.Empty);

            // Act
            var path = _mockPdfTemplateService.Object.GetTemplatePath("NonExistentTemplate");

            // Assert
            Assert.Equal(string.Empty, path);
        }
    }
}
