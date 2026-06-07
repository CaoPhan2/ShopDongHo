using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShopDongHo.Services
{
    public class ImageSearchService : IImageSearchService
    {
        private readonly string _apiKey;
        private readonly ILogger<ImageSearchService> _logger;
        private readonly HttpClient _httpClient;

        public ImageSearchService(IConfiguration configuration, ILogger<ImageSearchService> logger, HttpClient httpClient)
        {
            _apiKey = configuration["GoogleGemini:ApiKey"];
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<List<string>> ExtractKeywordsFromImageAsync(IFormFile image)
        {
            try
            {
                // 1. Chuy?n ?nh sang Base64
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                string base64Image = Convert.ToBase64String(ms.ToArray());

                // 2. C?u hình URL API Google Gemini 1.5 Flash
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

                // 3. T?o Payload g?i cho Gemini
                var payload = new
                {
                    contents = new[] {
                        new {
                            parts = new object[] {
                                new { text = "Identify this watch. Return ONLY a comma-separated list of keywords: Brand, Model. No sentences, no markdown." },
                                new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                            }
                        }
                    }
                };

                // 4. G?i yêu c?u qua HttpClient
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Gemini API l?i: {StatusCode}", response.StatusCode);
                    return new List<string>();
                }

                // 5. ??c và trích xu?t JSON
                var jsonDoc = await response.Content.ReadFromJsonAsync<JsonElement>();

                string text = jsonDoc.GetProperty("candidates")[0]
                                     .GetProperty("content")
                                     .GetProperty("parts")[0]
                                     .GetProperty("text")
                                     .GetString();

                _logger.LogInformation("Gemini AI tr? v?: {Text}", text);
                _logger.LogInformation("?? dài chu?i base64: {Length}", base64Image.Length);
                // 6. X? lý k?t qu? tr? v? thành List
                return text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                           .Select(s => s.Trim())
                           .Where(s => s.Length > 1)
                           .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "L?i k?t n?i Gemini API");
                return new List<string>();
            }
        }
    }
}