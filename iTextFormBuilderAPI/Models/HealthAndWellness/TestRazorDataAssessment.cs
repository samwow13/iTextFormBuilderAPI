using iTextFormBuilderAPI.Interfaces;

namespace iTextFormBuilderAPI.Models.HealthAndWellness.TestRazorDataModels
{
    public class TestRazorDataAssessment : IAssessment
    {
        public string TemplateFileName => "HealthAndWellness/TestRazorDataAssessment.cshtml";
        public string DisplayName => "Test Razor Data";
    }
}
