using System.Reflection;
using iTextFormBuilderAPI.Configuration;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Services;
using NLog;
using NLog.Web;

// Setup NLog for dependency injection
var logger = LogManager.Setup()
                .LoadConfigurationFromAppSettings()
                .GetCurrentClassLogger();

try
{
    // Starting application
    logger.Info("Starting application");
    
    // Create builder with NLog integration
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddControllers();

    // Swagger configuration
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.EnableAnnotations();

        // Register custom operation filter for examples
        c.OperationFilter<SwaggerExampleFilter>();

        // Add XML comments if they exist
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    });

    // Register services for dependency injection
    builder.Services.AddSingleton<ILogService, LogService>();
    builder.Services.AddSingleton<IPdfTemplateService, PdfTemplateService>();
    builder.Services.AddSingleton<IRazorService, RazorService>();
    builder.Services.AddSingleton<IDebugCshtmlInjectionService, DebugCshtmlInjectionService>();
    builder.Services.AddSingleton<ISystemMetricsService, SystemMetricsService>();
    builder.Services.AddScoped<IPDFGenerationService, PDFGenerationService>();

    // Build the application
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            // Add custom JavaScript for clipboard functionality
            c.HeadContent =
                @"
            <script>
            window.onload = function() {
                setTimeout(function() {
                    // Add copy buttons to example JSON blocks
                    const observer = new MutationObserver(function(mutations) {
                        mutations.forEach(function(mutation) {
                            if (mutation.addedNodes.length) {
                                checkForExamples();
                            }
                        });
                    });
                    
                    // Start observing the document for changes
                    observer.observe(document.body, { childList: true, subtree: true });
                    
                    // Initial check for examples
                    checkForExamples();
                    
                    // Also check when the example dropdown changes
                    document.body.addEventListener('click', function(e) {
                        if (e.target && (e.target.classList.contains('examples-select') || e.target.parentElement?.classList.contains('examples-select'))) {
                            setTimeout(checkForExamples, 100);
                        }
                    });
                    
                    function checkForExamples() {
                        const exampleBlocks = document.querySelectorAll('.example:not(.has-copy-button)');
                        exampleBlocks.forEach(function(block) {
                            block.classList.add('has-copy-button');
                            const exampleContent = block.querySelector('pre');
                            if (!exampleContent) return;
                            
                            const copyButton = document.createElement('button');
                            copyButton.innerText = 'Copy to Clipboard';
                            copyButton.className = 'copy-json-btn';
                            copyButton.style.backgroundColor = '#4990E2';
                            copyButton.style.color = 'white';
                            copyButton.style.border = 'none';
                            copyButton.style.borderRadius = '4px';
                            copyButton.style.padding = '7px 15px';
                            copyButton.style.margin = '10px 0';
                            copyButton.style.cursor = 'pointer';
                            copyButton.style.display = 'block';
                            
                            copyButton.addEventListener('click', function() {
                                const text = exampleContent.innerText;
                                navigator.clipboard.writeText(text).then(function() {
                                    copyButton.innerText = 'Copied!';
                                    setTimeout(function() {
                                        copyButton.innerText = 'Copy to Clipboard';
                                    }, 2000);
                                }, function() {
                                    copyButton.innerText = 'Failed to copy';
                                    setTimeout(function() {
                                        copyButton.innerText = 'Copy to Clipboard';
                                    }, 2000);
                                });
                            });
                            
                            block.parentNode.insertBefore(copyButton, block);
                        });
                    }
                }, 500);
            };
            </script>";
        });
    }

    // Commented out HTTPS redirection as per memory to avoid SSL certificate issues
    // app.UseHttpsRedirection();

    app.UseAuthorization();
    app.MapControllers();

    // Run the application
    app.Run();
}
catch (Exception ex)
{
    // Log any startup errors
    logger.Error(ex, "Application stopped due to an exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application exit
    LogManager.Shutdown();
}
