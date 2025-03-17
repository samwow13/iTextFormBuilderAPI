namespace iTextFormBuilderAPI.Models.APIModels;

public class PdfRequest
{
    public string TemplateName { get; set; } = null!;
    public object Data { get; set; } = null!;
    /// <summary>
    /// When true, returns the PDF as a base64 encoded string instead of a file
    /// </summary>
    public bool ReturnAsBase64 { get; set; } = false;
}