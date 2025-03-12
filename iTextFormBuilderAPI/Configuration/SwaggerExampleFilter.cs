using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;
using Newtonsoft.Json.Linq;

namespace iTextFormBuilderAPI.Configuration
{
    /// <summary>
    /// Adds example request bodies to Swagger documentation
    /// </summary>
    public class SwaggerExampleFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Only apply to the GeneratePdf operation
            if (context.MethodInfo.Name == "GeneratePdf" && operation.RequestBody != null)
            {
                // Sample JSON for the TestRazor template
                var testRazorJson = @"{
  ""templateName"": ""HealthAndWellness\\TestRazor"",
  ""data"": {
    ""user"": {
      ""id"": 1,
      ""name"": ""Alice Smith"",
      ""email"": ""alice@example.com"",
      ""is_active"": true,
      ""created_at"": ""2023-03-01T12:34:56Z""
    },
    ""preferences"": {
      ""notifications"": {
        ""email"": true,
        ""sms"": false,
        ""push"": true
      },
      ""theme"": ""dark"",
      ""language"": ""en-US""
    },
    ""orders"": [
      {
        ""order_id"": ""ORD123"",
        ""amount"": 250.75,
        ""items"": [
          {
            ""item_id"": ""ITM1"",
            ""name"": ""Product 1"",
            ""quantity"": 2,
            ""price"": 50.25
          },
          {
            ""item_id"": ""ITM2"",
            ""name"": ""Product 2"",
            ""quantity"": 1,
            ""price"": 150.25
          }
        ],
        ""status"": ""Completed""
      },
      {
        ""order_id"": ""ORD124"",
        ""amount"": 100.00,
        ""items"": [
          {
            ""item_id"": ""ITM3"",
            ""name"": ""Product 3"",
            ""quantity"": 1,
            ""price"": 100.00
          }
        ],
        ""status"": ""Pending""
      }
    ]
  }
}";

                // Create example for HealthAndWellness\TestRazor template
                var testRazorExample = new OpenApiExample
                {
                    Summary = "TestRazor Template Example",
                    Description = "Sample JSON for the HealthAndWellness\\TestRazor template",
                    Value = OpenApiAnyFactory.CreateFromJson(testRazorJson)
                };

                // Add the example to all application/json media types in the request body
                foreach (var mediaType in operation.RequestBody.Content.Where(x => x.Key == "application/json"))
                {
                    mediaType.Value.Examples = new Dictionary<string, OpenApiExample>
                    {
                        { "TestRazorExample", testRazorExample }
                    };
                }
            }
        }
    }

    /// <summary>
    /// Factory for creating OpenApiAny objects from JSON
    /// </summary>
    public static class OpenApiAnyFactory
    {
        /// <summary>
        /// Creates an IOpenApiAny object from a JSON string
        /// </summary>
        public static IOpenApiAny CreateFromJson(string json)
        {
            var jObject = JObject.Parse(json);
            return CreateFromJToken(jObject);
        }

        private static IOpenApiAny CreateFromJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    var obj = new OpenApiObject();
                    foreach (var property in token.Children<JProperty>())
                    {
                        obj.Add(property.Name, CreateFromJToken(property.Value));
                    }
                    return obj;

                case JTokenType.Array:
                    var array = new OpenApiArray();
                    foreach (var item in token.Children())
                    {
                        array.Add(CreateFromJToken(item));
                    }
                    return array;

                case JTokenType.Integer:
                    return new OpenApiInteger((int)token.Value<long>());

                case JTokenType.Float:
                    return new OpenApiDouble(token.Value<double>());

                case JTokenType.Boolean:
                    return new OpenApiBoolean(token.Value<bool>());

                case JTokenType.Date:
                    return new OpenApiDateTime(token.Value<DateTime>());

                case JTokenType.String:
                    return new OpenApiString(token.Value<string>());

                case JTokenType.Null:
                    return new OpenApiNull();

                default:
                    return new OpenApiString(token.ToString());
            }
        }
    }
}
