using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.EnableAnnotations();
});

// Register services for dependency injection
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddSingleton<IPdfTemplateService, PdfTemplateService>();
builder.Services.AddSingleton<IRazorService, RazorService>();
builder.Services.AddScoped<IPDFGenerationService, PDFGenerationService>();

// Initialize the Razor service
builder.Services.AddHostedService<iTextFormBuilderAPI.Services.RazorInitializationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Commented out HTTPS redirection as per memory to avoid SSL certificate issues
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
