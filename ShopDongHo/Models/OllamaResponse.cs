using System.Text.Json.Serialization;

namespace ShopDongHo.Models
{
    public class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; }
    }
}
