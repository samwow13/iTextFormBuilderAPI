namespace iTextFormBuilderAPI.Utilities;

public static class PdfTemplateRegistry
{
    public static readonly HashSet<string> ValidTemplates = new()
    {
        "Invoice", "Receipt", "Report", "HealthAndWellness\\TestRazorDataAssessment", "SimpleTest"
    };
}