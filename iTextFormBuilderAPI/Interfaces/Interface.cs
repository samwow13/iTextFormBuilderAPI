using iTextFormBuilderAPI.Models;
using iTextFormBuilderAPI.Models.APIModels;
using iTextFormBuilderAPI.Services;

namespace iTextFormBuilderAPI.Interfaces;

public interface IPDFGenerationService
{
    PdfResult GeneratePdf(string templateName, object data);

    //health check for the service. Returns true if the service is healthy, false otherwise.
    ServiceHealthStatus GetServiceHealth();
}
