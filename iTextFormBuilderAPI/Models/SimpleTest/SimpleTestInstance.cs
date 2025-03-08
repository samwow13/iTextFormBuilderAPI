using Newtonsoft.Json;

namespace iTextFormBuilderAPI.Models.SimpleTest;

/// <summary>
/// Simple test model for PDF generation
/// </summary>
public class SimpleTestInstance
{
    /// <summary>
    /// The name of the person
    /// </summary>
    [JsonProperty("personName")]
    public string? PersonName { get; set; }

    /// <summary>
    /// The current date for the report
    /// </summary>
    [JsonProperty("reportDate")]
    public string? ReportDate { get; set; }
}