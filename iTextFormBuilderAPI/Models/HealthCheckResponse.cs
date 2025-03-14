using System;
using System.Text.Json.Serialization;

namespace iTextFormBuilderAPI.Models
{
    /// <summary>
    /// Represents a basic health check response for the API.
    /// </summary>
    public class HealthCheckResponse
    {
        /// <summary>
        /// Gets or sets the identifier for the health check response.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "iTextFormBuilderAPI";

        /// <summary>
        /// Gets or sets the type of the response.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "HealthCheck";

        /// <summary>
        /// Gets or sets the data object containing additional health check information.
        /// </summary>
        [JsonPropertyName("data")]
        public HealthCheckData Data { get; set; } = new HealthCheckData();
    }

    /// <summary>
    /// Contains additional data for the health check response.
    /// </summary>
    public class HealthCheckData
    {
        /// <summary>
        /// Gets or sets the version of the API.
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.00";
    }
}
