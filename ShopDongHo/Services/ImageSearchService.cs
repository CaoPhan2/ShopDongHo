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
            _logger.LogInformation("[AI-LOG 1] Bắt đầu quá trình xử lý ảnh tại Service.");

            if (image == null || image.Length == 0)
            {
                _logger.LogWarning("[AI-LOG 1.1] File ảnh truyền vào bị rỗng hoặc NULL.");
                return new List<string>();
            }

            try
            {
                // 1. Log thông tin file nhận được
                _logger.LogInformation("[AI-LOG 2] Nhận file: {FileName} | Kích thước: {Length} bytes | Định dạng: {ContentType}",
                    image.FileName, image.Length, image.ContentType);

                // 2. Chuyển ảnh sang Base64
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                string base64Image = Convert.ToBase64String(ms.ToArray());
                _logger.LogInformation("[AI-LOG 3] Chuyển đổi Base64 thành công. Độ dài chuỗi mã hóa: {Base64Length} ký tự.", base64Image.Length);

                // 3. Kiểm tra API Key
                if (string.IsNullOrEmpty(_apiKey))
                {
                    _logger.LogError("[AI-LOG CRITICAL] API Key của Gemini đang bị trống! Vui lòng kiểm tra file appsettings.json.");
                    return new List<string>();
                }
                _logger.LogInformation("[AI-LOG 4] Đã tìm thấy API Key (Độ dài: {KeyLength}). Chuẩn bị gọi API Google...", _apiKey.Length);

                // 4. Cấu hình URL và Payload
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
                var payload = new
                {
                    contents = new[] {
                new {
                    parts = new object[] {
new { text = "Identify this watch. Return ONLY a comma-separated list of keywords in Vietnamese: Brand, Model, Style. No sentences, no markdown. If the image does not contain any watch, return ONLY the word 'NONE'." },
                        new { inlineData = new { mimeType = image.ContentType, data = base64Image } }
                    }
                }
            }
                };

                // 5. Gửi request
                _logger.LogInformation("[AI-LOG 5] Đang gửi HTTP POST tới Google Gemini API...");
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                // 6. ĐỌC DỮ LIỆU THÔ (RAW) TRƯỚC KHI PARSE - giúp bắt mọi loại lỗi của Google
                string rawJsonResponse = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("[AI-LOG 6] Đã nhận phản hồi từ Google. HTTP Status Code: {StatusCode}", response.StatusCode);
                _logger.LogInformation("[AI-LOG 7] Dữ liệu thô (Raw JSON) nhận được từ Google:\n{RawJson}", rawJsonResponse);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("[AI-LOG ERROR] Google Gemini từ chối yêu cầu. Mã lỗi: {StatusCode}. Xem chi tiết ở Log 7 phía trên.", response.StatusCode);
                    return new List<string>();
                }

                // 7. Phân tích cú pháp JSON khi thành công
                using JsonDocument jsonDoc = JsonDocument.Parse(rawJsonResponse);


                if (jsonDoc.RootElement.TryGetProperty("candidates", out JsonElement candidates) && candidates.GetArrayLength() > 0)
                {
                    string text = candidates[0]
              .GetProperty("content")
              .GetProperty("parts")[0]
              .GetProperty("text")
              .GetString();

                    _logger.LogInformation("[AI-LOG 8] Phân tích JSON thành công. Chữ AI trả về gốc: \"{Text}\"", text);

                    if (string.IsNullOrEmpty(text)) return new List<string>();

                    // THÊM MỚI ĐỂ CHẶN ẢNH RÁC / KHÔNG PHẢI ĐỒNG HỒ 
                    string lowerText = text.ToLower();
                    if (lowerText.Contains("không có") ||
                        lowerText.Contains("không phải") ||
                        lowerText.Contains("unknown") ||
                        lowerText.Contains("none"))
                    {
                        _logger.LogWarning("[AI-LOG 8.2] Phát hiện ảnh không hợp lệ hoặc không chứa đồng hồ. Trả về mảng rỗng.");
                        return new List<string>(); // Trả về rỗng để Controller báo lỗi ra giao diện cho khách
                    }
                

                    // Tách chuỗi thành List từ khóa (Giữ nguyên bên dưới)
                    var keywords = text.Split(new[] { ',', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(s => s.Trim())
                                       .Where(s => s.Length > 1)
                                       .ToList();

                    _logger.LogInformation("[AI-LOG 9] Danh sách từ khóa sau khi chuẩn hóa thành mảng: [{Keywords}]", string.Join(", ", keywords));
                    return keywords;
                }
                else
                {
                    _logger.LogWarning("[AI-LOG 8.1] Cấu trúc JSON thành công nhưng không tìm thấy thẻ 'candidates'.");
                    return new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI-LOG EXCEPTION] Gặp lỗi nghiêm trọng trong quá trình xử lý Service!");
                return new List<string>();
            }
        }
    }
}