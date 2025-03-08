var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.EnableAnnotations();
});

// Register services for dependency injection
builder.Services.AddScoped<iTextFormBuilderAPI.Interfaces.IPDFGenerationService, iTextFormBuilderAPI.Services.PDFGenerationService>();

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
