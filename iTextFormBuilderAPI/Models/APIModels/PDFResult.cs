namespace iTextFormBuilderAPI.Models.APIModels;

public class PdfResult
{
    public bool Success { get; set; }
    public byte[]? PdfBytes { get; set; }
    public string Message { get; set; } = "";
}
