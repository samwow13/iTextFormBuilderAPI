using iTextFormBuilderAPI.Interfaces;

namespace iTextFormBuilderAPI.Services;

/// <summary>
/// Background service that initializes the Razor engine on application startup.
/// </summary>
public class RazorInitializationService : BackgroundService
{
    private readonly IRazorService _razorService;
    private readonly ILogService _logService;

    /// <summary>
    /// Initializes a new instance of the RazorInitializationService class.
    /// </summary>
    /// <param name="razorService">The Razor service to initialize.</param>
    /// <param name="logService">The log service for logging messages.</param>
    public RazorInitializationService(IRazorService razorService, ILogService logService)
    {
        _razorService = razorService;
        _logService = logService;
    }

    /// <summary>
    /// Executes the background service.
    /// </summary>
    /// <param name="stoppingToken">A token that signals when the service should stop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logService.LogInfo("Initializing Razor service...");

        try
        {
            // Initialize the Razor engine
            await _razorService.InitializeAsync();
            _logService.LogInfo("Razor service initialized successfully.");
        }
        catch (Exception ex)
        {
            _logService.LogError("Failed to initialize Razor service", ex);
        }
    }
}
