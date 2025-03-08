using iTextFormBuilderAPI.Services;
using iTextFormBuilderAPI.Models.APIModels;
using iTextFormBuilderAPI.Models;

namespace iTextFormBuilderAPI.Interfaces;

public interface IPDFGenerationService
{
    PdfResult GeneratePdf(string templateName, object data);

    //health check for the service. Returns true if the service is healthy, false otherwise.
    ServiceHealthStatus GetServiceHealth();
}
