using Newtonsoft.Json;
using System.Text;

namespace ShopDongHo.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;

        private const string API_KEY =
            "AIzaSyCC0YOz4KuKFYa1ahLImuqozCdghSGD5cU";

        public GeminiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> AskAI(string prompt)
        {
            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={API_KEY}";

            var body = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                }
            };

            var json =
                JsonConvert.SerializeObject(body);

            var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

            var response =
                await _httpClient.PostAsync(
                    url,
                    content
                );

            var responseString =
                await response.Content.ReadAsStringAsync();

            dynamic result =
                JsonConvert.DeserializeObject(responseString);

            return result
                .candidates[0]
                .content.parts[0]
                .text
                .ToString();
        }
    }
}