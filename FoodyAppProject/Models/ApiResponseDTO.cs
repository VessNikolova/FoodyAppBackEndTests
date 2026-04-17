

using System.Text.Json.Serialization;

namespace FoodyAppProject.Models
{
    internal class ApiResponseDTO
    {
        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("foodId")]
        public string? FoodId { get; set; }
    }
}
