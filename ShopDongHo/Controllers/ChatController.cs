using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDongHo.Models;
using ShopDongHo.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShopDongHo.Controllers
{
    public class ChatController : Controller
    {
        private readonly DataContext _context;

        // Khởi tạo HttpClient kèm cấu hình Bypass SSL để tránh lỗi chặn kết nối HTTPS trên Localhost
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        });

        // API KEY DUY NHẤT SỬ DỤNG CHO HỆ THỐNG
        private const string API_KEY = "";
        private const string GEMINI_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=" + API_KEY;

        public ChatController(DataContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Message))
            {
                return Json(new ChatResponse { Reply = "Dạ, C&P Store xin kính chào quý khách! Em có thể giúp gì cho anh/chị hôm nay ạ? 🌸" });
            }

            string userMessage = req.Message.Trim();
            string userMessageLower = userMessage.ToLower();

            try
            {
                // TẦNG 1: XỬ LÝ NHANH KHÔNG CẦN AI (Chào hỏi & Liên hệ)
                if (IsGreeting(userMessageLower))
                {
                    return Json(new ChatResponse
                    {
                        Reply = "Dạ, C&P Store em xin chào anh/chị ạ! Chúc anh/chị một ngày tốt lành. Anh/chị đang cần tìm dòng đồng hồ hay phụ kiện dây đeo nào để em hỗ trợ tư vấn tốt nhất ạ? ✨"
                    });
                }

                var contact = await _context.Contact.AsNoTracking().FirstOrDefaultAsync();
                if (IsContactInquiry(userMessageLower))
                {
                    string reply = "Dạ, C&P Store xin gửi thông tin liên hệ của cửa hàng để anh/chị tiện ghé qua ạ:\n";
                    if (contact != null)
                    {
                        reply += $"📍 **Địa chỉ:** {contact.Address}\n" +
                                 $"📞 **Số điện thoại:** {contact.Phone}\n" +
                                 $"✉️ **Email:** {contact.Email}\n" +
                                 $"📝 **Thông tin thêm:** {contact.Description}";
                    }
                    else
                    {
                        reply += "📍 Địa chỉ: [Chưa cập nhật cấu hình hệ thống]\n📞 Hotline: 1900 xxxx";
                    }
                    return Json(new ChatResponse { Reply = reply });
                }

                // TẦNG 2: RAG - TRUY VẤN NGỮ CẢNH DỮ LIỆU TỪ DATABASE
                var allCategories = await _context.Categories.AsNoTracking().Select(c => c.Name).ToListAsync();
                var allBrands = await _context.Brands.AsNoTracking().Select(b => b.Name).ToListAsync();

                // Gọi hàm lọc từ khóa thông minh độc lập (Đã loại bỏ bảng Ratings trống)
                var matchedProducts = await GetProductContextData(userMessageLower);

                string productContext = "Hiện tại không có sản phẩm cụ thể nào khớp chính xác từ khóa khách hàng tìm kiếm trong kho.";
                if (matchedProducts.Any())
                {
                    var contextData = matchedProducts.Select(p => new {
                        p.Id, // Kiểu long tự động map an toàn sang định dạng số JSON
                        p.Name,
                        BrandName = p.Brand?.Name ?? "Chưa rõ",
                        Price = p.Price,
                        ImageUrl = p.Images, // Khớp chuẩn xác với trường Images (string) trong ProductModel của bạn
                        Rating = 5, // Gán cứng mặc định 5 sao vì DB chưa có dữ liệu Ratings thực tế, giúp UI luôn đẹp
                        Url = $"/san-pham/{p.Slug}"
                    });
                    productContext = JsonSerializer.Serialize(contextData);
                }

                // Lấy lịch sử trò chuyện theo SessionId (3 lượt gần nhất)
                var history = await _context.ChatHistory
                    .Where(h => h.SessionId == req.SessionId)
                    .OrderByDescending(h => h.CreatedAt)
                    .Take(3)
                    .OrderBy(h => h.CreatedAt)
                    .ToListAsync();

                var contentsList = new List<object>();
                foreach (var h in history)
                {
                    contentsList.Add(new { role = "user", parts = new[] { new { text = h.UserMessage } } });
                    contentsList.Add(new { role = "model", parts = new[] { new { text = h.BotReply } } });
                }

                contentsList.Add(new { role = "user", parts = new[] { new { text = $"[KHO SẢN PHẨM THỰC TẾ TRONG HỆ THỐNG]:\n{productContext}\n\n[TIN NHẮN MỚI CỦA KHÁCH HÀNG]: {userMessage}" } } });

                // TẦNG 3: SYSTEM INSTRUCTION (Ép cấu trúc khối Inline Card & Sửa lỗi chuỗi lồng nhau của C#)
                string storeName = "C&P Store";
                string categoryContext = string.Join(", ", allCategories);
                string brandContext = string.Join(", ", allBrands);
                string systemInstruction = $@"
Bạn là trợ lý AI bán hàng chuyên nghiệp, am hiểu sâu sắc về các dòng sản phẩm của cửa hàng {storeName}.

DANH MỤC CÓ SẴN: {categoryContext}
THƯƠNG HIỆU CÓ SẴN: {brandContext}
KHO SẢN PHẨM THỰC TẾ (CHỈ ĐƯỢC DÙNG THÔNG TIN TRONG NÀY, KHÔNG TỰ BỊA GIÁ HAY SẢN PHẨM): {productContext}

QUY TẮC PHẢN HỒI (BẮT BUỘC):
1. Luôn tư vấn bằng giọng điệu thân thiện, tự nhiên, lễ phép và chuyên nghiệp (Ví dụ: ""Dạ, shop em xin chào anh/chị ạ..."", ""Dạ anh/chị...""). Cuối câu trả lời luôn có câu hỏi gợi mở để tương tác tiếp với khách.
2. Trình bày danh sách sản phẩm theo định dạng Markdown có số thứ tự. Tên sản phẩm PHẢI in đậm (ví dụ: **HP Spectre x360**). Các đặc tính nổi bật hoặc mô tả ngắn gọn đi kèm phải viết rõ ràng, mạch lạc sau dấu gạch ngang ""-"".
3. Với MỖI sản phẩm được nhắc đến trong danh sách, ngay dưới dòng mô tả văn bản, bạn PHẢI chèn chuỗi JSON của sản phẩm đó nằm giữa thẻ [PRODUCT_CARD] và [/PRODUCT_CARD] ở một dòng riêng biệt.

Ví dụ cách trình bày danh sách kiểu khối xen kẽ văn bản để load giao diện:
Dạ shop em xin gợi ý các mẫu đang cực hot bên em để mình tham khảo ạ:

1. **ASUS ROG Zephyrus G14** - Chiếc laptop gaming đỉnh cao sở hữu thiết kế mỏng nhẹ, di động.
[PRODUCT_CARD]{{""id"":12,""name"":""ASUS ROG Zephyrus G14"",""price"":38000000,""imageUrl"":""asus-g14.jpg""}}[/PRODUCT_CARD]

4. **HP Spectre x360** - Chiếc laptop 2-in-1 đa năng với thiết kế sang trọng, màn hình cảm ứng xoay gập linh hoạt, lý tưởng cho cả công việc và giải trí.
[PRODUCT_CARD]{{""id"":15,""name"":""HP Spectre x360"",""price"":36000000,""imageUrl"":""hp-spectre.jpg""}}[/PRODUCT_CARD]

Anh/chị quan tâm đến dòng laptop nào ở trên hoặc cần em tư vấn thêm tiêu chí nào khác không ạ?

* Chú ý cấu trúc thuộc tính JSON bên trong thẻ:
- Tuyệt đối tuân thủ định dạng JSON bên trong thẻ, viết liền mạch trên 1 dòng, giữ đúng tên thuộc tính hệ thống yêu cầu.
- ""id"": Điền chính xác ID từ dữ liệu hệ thống kho thực tế.
- ""name"": Tên chính xác của sản phẩm.
- ""price"": Giá tiền kiểu số (không chứa dấu chấm hay ký tự đ).
- ""imageUrl"": Tên file ảnh hoặc đường dẫn ảnh đi kèm từ hệ thống.";

                // TẦNG 4: GỌI GEMINI API & SỬ DỤNG PARSE THỦ CÔNG CHỐNG LỖI PHIÊN BẢN
                var payload = new
                {
                    contents = contentsList,
                    systemInstruction = new { parts = new[] { new { text = systemInstruction } } },
                    generationConfig = new { temperature = 0.3, maxOutputTokens = 2000 }
                };

                var response = await _httpClient.PostAsJsonAsync(GEMINI_URL, payload);
                if (!response.IsSuccessStatusCode)
                {
                    string errReason = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Gemini lỗi HTTP {response.StatusCode}: {errReason}");
                }

                // Đọc chuỗi text thô trước rồi mới phân tích để đảm bảo an toàn tuyệt đối
                string responseString = await response.Content.ReadAsStringAsync();
                string botReply = "";

                using (JsonDocument doc = JsonDocument.Parse(responseString))
                {
                    JsonElement root = doc.RootElement;
                    if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        var firstCandidate = candidates[0];
                        if (firstCandidate.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts))
                        {
                            var textParts = new List<string>();
                            foreach (var part in parts.EnumerateArray())
                            {
                                if (part.TryGetProperty("text", out var textElement))
                                {
                                    textParts.Add(textElement.GetString() ?? "");
                                }
                            }
                            botReply = string.Join("\n", textParts);
                        }
                    }
                }

                if (string.IsNullOrEmpty(botReply))
                {
                    throw new Exception($"Gemini trả về cấu trúc trống. Nội dung thô: {responseString}");
                }

                // Loại bỏ khối suy nghĩ ngầm nếu dùng model có suy nghĩ
                botReply = Regex.Replace(botReply, @"<think>.*?</think>", "", RegexOptions.Singleline).Trim();

                // Trích xuất danh sách đối tượng phục vụ Frontend
                var productList = ExtractProductsFromCard(botReply);
                string jsonProductList = System.Text.Json.JsonSerializer.Serialize(productList, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                // In ra cửa sổ Console
                Console.WriteLine("--- DANH SÁCH PRODUCT LIST TRẢ VỀ ---");
                Console.WriteLine(productList);
                // TẦNG 5: LƯU LỊCH SỬ VÀ TRẢ VỀ KẾT QUẢ
                // LƯU Ý: Nếu Database bảng ChatHistory bị lỗi độ dài cột (nvarchar ngắn quá), hãy chạy Migration tăng lên nvarchar(max).
                await _context.ChatHistory.AddAsync(new ChatHistory
                {
                    SessionId = req.SessionId,
                    UserMessage = userMessage,
                    BotReply = botReply,
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return Json(new ChatResponse
                {
                    Reply = botReply,
                    Products = productList
                });
            }
            catch (Exception ex)
            {
                // Thay vì ẩn lỗi, gán trực tiếp thông tin ngoại lệ (Exception) trả về giao diện 
                // để bạn biết chính xác dòng nào trong code, bảng nào trong database hoặc lỗi phân tích gì đang làm sập hệ thống.
                string detailedError = $"[LỖI HỆ THỐNG]: {ex.Message} \n[VỊ TRÍ]: {ex.StackTrace}";
                System.Diagnostics.Debug.WriteLine(detailedError);

                return Json(new ChatResponse { Reply = detailedError });
            }
        }

        #region Các hàm hỗ trợ xử lý thuật toán RAG & Trích xuất thẻ
        private bool IsGreeting(string text)
        {
            string[] keywords = { "hi", "hello", "helo", "hêlo", "chào", "chao", "xin chào", "xin chao", "alo", "alô", "shop ơi", "cho hỏi" };
            return keywords.Any(kw => text.Contains(kw));
        }

        private bool IsContactInquiry(string text)
        {
            string[] keywords = { "địa chỉ", "dia chi", "ở đâu", "cửa hàng", "hotline", "số điện thoại", "liên hệ" };
            return keywords.Any(kw => text.Contains(kw));
        }

        /// <summary>
        /// Thuật toán tách từ khóa thông minh, loại bỏ bảng trống Ratings hoàn toàn an toàn
        /// </summary>
        private async Task<List<ProductModel>> GetProductContextData(string msg)
        {
            string msgLower = msg.ToLower().Trim();

            // 1. Quét tìm kiếm Thương hiệu độc lập xuất hiện trong câu nói của khách
            var brands = await _context.Brands.AsNoTracking().Select(b => b.Name.ToLower()).ToListAsync();
            string matchedBrand = brands.FirstOrDefault(b => msgLower.Contains(b));

            // 2. Định nghĩa danh sách các từ gây nhiễu cần bóc tách loại bỏ
            string[] noiseWords = { "tôi", "muốn", "biết", "về", "sản", "phẩm", "đồng", "hồ", "cho", "hỏi", "tìm", "kiếm", "mẫu", "dòng", "xem", "loại", "chi tiết", "giá" };

            var words = msgLower.Split(new[] { ' ', ',', '.', '-', '/', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(w => !noiseWords.Contains(w))
                                .ToList();

            if (!words.Any() && string.IsNullOrEmpty(matchedBrand))
                return new List<ProductModel>();

            // Chỉ nạp bảng liên kết thương hiệu Brand, bỏ hẳn Ratings để tránh lỗi và tăng tốc độ SQL
            var query = _context.Products.Include(p => p.Brand).AsNoTracking().AsQueryable();

            // 3. Thực hiện xây dựng câu lệnh SQL truy vấn động
            if (!string.IsNullOrEmpty(matchedBrand))
            {
                query = query.Where(p => p.Brand != null && p.Brand.Name.ToLower() == matchedBrand);
            }

            foreach (var word in words)
            {
                if (word == matchedBrand) continue;
                query = query.Where(p => p.Name.ToLower().Contains(word));
            }

            return await query.Take(20).ToListAsync();
        }

        private List<ProductItemDto> ExtractProductsFromCard(string text)
        {
            var products = new List<ProductItemDto>();
            var matches = Regex.Matches(text, @"\[PRODUCT_CARD\](.*?)\[/PRODUCT_CARD\]", RegexOptions.Singleline);
            foreach (Match match in matches)
            {
                try
                {
                    string jsonString = match.Groups[1].Value.Trim();
                    var dto = JsonSerializer.Deserialize<ProductItemDto>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (dto != null)
                    {
                        if (!string.IsNullOrEmpty(dto.ImageUrl) && !dto.ImageUrl.StartsWith("http") && !dto.ImageUrl.StartsWith("/"))
                        {
                            dto.ImageUrl = "/contents/images/" + dto.ImageUrl;
                        }
                        products.Add(dto);
                    }
                }
                catch { }
            }
            return products;
        }
        #endregion
    }

    #region Hệ thống DTO đồng bộ dữ liệu giao diện khối
    public class ChatRequest
    {
        public string Message { get; set; }
        public string SessionId { get; set; }
    }

    public class ChatResponse
    {
        public string Reply { get; set; }
        public List<ProductItemDto> Products { get; set; } = new List<ProductItemDto>();
    }

    public class ProductItemDto
    {
        public long Id { get; set; } // Khớp 100% với kiểu dữ liệu long trong Model của anh/chị
        public int Index { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public bool HasVerify { get; set; }
        public int Rating { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
    }
    #endregion
}