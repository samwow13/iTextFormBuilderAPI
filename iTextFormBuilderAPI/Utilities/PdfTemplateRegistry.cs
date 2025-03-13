namespace iTextFormBuilderAPI.Utilities;

public static class PdfTemplateRegistry
{
    /// <summary>
    /// Set of valid PDF templates. Please add additional templates to the PDFGenerationController swagger documentation.
    /// </summary>
    public static readonly HashSet<string> ValidTemplates = new() { "Hotline\\HotlineTesting" };
}
