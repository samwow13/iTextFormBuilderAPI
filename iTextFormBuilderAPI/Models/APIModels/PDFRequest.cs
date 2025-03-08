namespace iTextFormBuilderAPI.Models.APIModels;

public class PdfRequest
{
    public string TemplateName { get; set; } = null!;
    public object Data { get; set; } = null!;
}