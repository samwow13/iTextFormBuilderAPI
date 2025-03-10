using iTextFormBuilderAPI.Interfaces;
using iTextFormBuilderAPI.Models.HealthAndWellness.TestRazorDataModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace iTextFormBuilderAPI.Controllers;

/// <summary>
/// Controller for testing Razor template rendering.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RazorTestController : ControllerBase
{
    private readonly IRazorService _razorService;
    private readonly ILogService _logService;

    /// <summary>
    /// Initializes a new instance of the RazorTestController class.
    /// </summary>
    /// <param name="razorService">The Razor service for rendering templates.</param>
    /// <param name="logService">The log service for logging messages.</param>
    public RazorTestController(IRazorService razorService, ILogService logService)
    {
        _razorService = razorService;
        _logService = logService;
    }

    /// <summary>
    /// Renders a template with test data and returns the HTML.
    /// </summary>
    /// <param name="templateName">The name of the template to render.</param>
    /// <returns>The rendered HTML as a string.</returns>
    [HttpGet("render/{templateName}")]
    [Produces("text/html")]
    public async Task<IActionResult> RenderTemplate(string templateName)
    {
        try
        {
            _logService.LogInfo($"Rendering template: {templateName}");
            
            // Create test data based on the template name
            object model;
            if (templateName.Equals("HealthAndWellness\\TestRazorDataAssessment", StringComparison.OrdinalIgnoreCase))
            {
                model = CreateTestRazorDataInstance();
            }
            else
            {
                return BadRequest($"No test data available for template '{templateName}'.");
            }
            
            // Render the template
            var html = await _razorService.RenderTemplateAsync(templateName, model);
            
            // Return the HTML
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logService.LogError($"Error rendering template {templateName}", ex);
            return StatusCode(500, $"Error rendering template: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Creates a test instance of the TestRazorDataInstance class with sample data.
    /// </summary>
    /// <returns>A TestRazorDataInstance with sample data.</returns>
    private TestRazorDataInstance CreateTestRazorDataInstance()
    {
        return new TestRazorDataInstance
        {
            User = new User
            {
                Id = 12345,
                Name = "John Doe",
                Email = "john.doe@example.com",
                IsActive = true,
                CreatedAt = DateTime.Now.AddDays(-30)
            },
            Preferences = new Preferences
            {
                Theme = "Dark",
                Language = "English",
                Notifications = new Notifications
                {
                    Email = true,
                    Sms = false,
                    Push = true
                }
            },
            Orders = new List<Order>
            {
                new Order
                {
                    OrderId = "ORD-001",
                    Amount = 125.50m,
                    Status = "Completed",
                    Items = new List<Item>
                    {
                        new Item { ItemId = "ITEM-001", Name = "Product A", Quantity = 2, Price = 50.00m },
                        new Item { ItemId = "ITEM-002", Name = "Product B", Quantity = 1, Price = 25.50m }
                    }
                },
                new Order
                {
                    OrderId = "ORD-002",
                    Amount = 75.25m,
                    Status = "Pending",
                    Items = new List<Item>
                    {
                        new Item { ItemId = "ITEM-003", Name = "Product C", Quantity = 3, Price = 25.00m },
                        new Item { ItemId = "ITEM-004", Name = "Product D", Quantity = 1, Price = 0.25m }
                    }
                }
            }
        };
    }
}
