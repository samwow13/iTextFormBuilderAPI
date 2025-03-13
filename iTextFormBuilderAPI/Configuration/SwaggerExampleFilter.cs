using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;

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
                // Sample JSON for the Hotline\HotlineTesting template
                var testRazorJson =
                    @"{
  ""templateName"": ""Hotline\\HotlineTesting"",
  ""data"": {
    ""model"": {
      ""id"": 1001,
      ""name"": ""Hotline Assessment Test"",
      ""description"": ""This is a test instance for Hotline Assessment 1."",
      ""created_at"": ""2023-09-15T08:00:00Z"",
      ""is_active"": true
    },
    ""properties"": {
      ""sections"": [
        {
          ""title"": ""Personal Information"",
          ""fields"": [
            {
              ""name"": ""Full Name"",
              ""type"": ""string"",
              ""value"": ""John Doe""
            },
            {
              ""name"": ""Age"",
              ""type"": ""number"",
              ""value"": 30
            }
          ]
        },
        {
          ""title"": ""Assessment Details"",
          ""fields"": [
            {
              ""name"": ""Score"",
              ""type"": ""number"",
              ""value"": 85.5
            },
            {
              ""name"": ""Passed"",
              ""type"": ""boolean"",
              ""value"": true
            },
            {
              ""name"": ""Comments"",
              ""type"": ""string"",
              ""value"": ""Satisfactory performance.""
            }
          ]
        }
      ]
    }
  }
}";

                // Create example for Hotline\HotlineTesting template
                var testRazorExample = new OpenApiExample
                {
                    Summary = "HotlineTesting Template Example",
                    Description = "Sample JSON for the Hotline\\HotlineTesting template",
                    Value = OpenApiAnyFactory.CreateFromJson(testRazorJson),
                };

                // Add the example to all application/json media types in the request body
                foreach (
                    var mediaType in operation.RequestBody.Content.Where(x =>
                        x.Key == "application/json"
                    )
                )
                {
                    mediaType.Value.Examples = new Dictionary<string, OpenApiExample>
                    {
                        { "HotlineTestingExample", testRazorExample },
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
