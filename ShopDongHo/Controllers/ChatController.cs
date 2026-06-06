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

        // API KEY 
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

                // lấy dữ liệu từ db
                var allCategories = await _context.Categories.AsNoTracking().Select(c => c.Name).ToListAsync();
                var allBrands = await _context.Brands.AsNoTracking().Select(b => b.Name).ToListAsync();

                // Gọi hàm lọc từ khóa thông minh (Đã ưu tiên nhận diện Thương hiệu)
                var matchedProducts = await GetProductContextData(userMessageLower);

                string productContext = "Hiện tại không có sản phẩm cụ thể nào khớp chính xác từ khóa khách hàng tìm kiếm trong kho.";
                if (matchedProducts.Any())
                {
                    var contextData = matchedProducts.Select(p => new {
                        p.Id,
                        p.Name,
                        BrandName = p.Brand?.Name ?? "Chưa rõ", 
                        Price = p.Price,
                        ImageUrl = p.Images,
                        Rating = 5,
                        Url = $"/san-pham/{p.Slug}"
                    });
                    productContext = JsonSerializer.Serialize(contextData);
                }

                // Lấy lịch sử trò chuyện theo SessionId 
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

                // TẦNG 3: SYSTEM INSTRUCTION
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
                    2. Trình bày danh sách sản phẩm theo định dạng Markdown có số thứ tự. Tên sản phẩm PHẢI in đậm (ví dụ: **Rolex Datejust Nữ**). Các đặc tính nổi bật hoặc mô tả ngắn gọn đi kèm phải viết rõ ràng, mạch lạc sau dấu gạch ngang ""-"".
                    3. Với MỖI sản phẩm được nhắc đến trong danh sách, ngay dưới dòng mô tả văn bản, bạn PHẢI chèn chuỗi JSON của sản phẩm đó nằm giữa thẻ [PRODUCT_CARD] và [/PRODUCT_CARD] ở một dòng riêng biệt.

                    Ví dụ cách trình bày danh sách kiểu khối xen kẽ văn bản để load giao diện:
                    Dạ shop em xin gợi ý các mẫu đang cực hot bên em để mình tham khảo ạ:

                    1. **Rolex Datejust Nữ** - Thiết kế tinh tế, quý phái dành cho phái đẹp.
                    [PRODUCT_CARD]{{""id"":78,""name"":""Rolex Datejust Nữ"",""brandName"":""Rolex"",""price"":8800000,""imageUrl"":""rolex-nu.jpg""}}[/PRODUCT_CARD]

                    Anh/chị quan tâm đến dòng đồng hồ nào ở trên hoặc cần em tư vấn thêm tiêu chí nào khác không ạ?

                    * Chú ý cấu trúc thuộc tính JSON bên trong thẻ:
                    - Tuyệt đối tuân thủ định dạng JSON bên trong thẻ, viết liền mạch trên 1 dòng, giữ đúng tên thuộc tính hệ thống yêu cầu.
                    - ""id"": Điền chính xác ID từ dữ liệu hệ thống kho thực tế.
                    - ""name"": Tên chính xác của sản phẩm.
                    - ""brandName"": Tên thương hiệu của sản phẩm.
                    - ""price"": Giá tiền kiểu số (không chứa dấu chấm hay ký tự đ).
                    - ""imageUrl"": Tên file ảnh hoặc đường dẫn ảnh đi kèm từ hệ thống.";

                //  GỌI GEMINI API
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

                botReply = Regex.Replace(botReply, @"<think>.*?</think>", "", RegexOptions.Singleline).Trim();

                // Trích xuất danh sách đối tượng phục vụ Frontend
                var productList = ExtractProductsFromCard(botReply);

                Console.WriteLine("--- DANH SÁCH PRODUCT LIST TRẢ VỀ ---");
                Console.WriteLine(JsonSerializer.Serialize(productList));

                // LƯU LỊCH SỬ VÀ TRẢ VỀ KẾT QUẢ
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
                System.Diagnostics.Debug.WriteLine($"[LỖI HỆ THỐNG]: {ex.Message} \n[VỊ TRÍ]: {ex.StackTrace}");

                // Trả về thông báo lỗi nhẹ nhàng, chuyên nghiệp cho khách hàng thay vì quăng lỗi thô
                return Json(new ChatResponse
                {
                    Reply = "Dạ, hệ thống kết nối của em đang gặp chút gián đoạn nhỏ. Anh/chị vui lòng thử gửi lại tin nhắn hoặc đợi em trong giây lát nhé! 🌸"
                });
            }
        }

       
        private bool IsGreeting(string text)
        {
           
            string[] keywords = { "hi", "hello", "xin chào", "xin chao", "alo shop" };
            return keywords.Any(kw => text == kw || text.StartsWith(kw + " "));
        }

        private bool IsContactInquiry(string text)
        {
            string[] keywords = { "địa chỉ", "dia chi", "ở đâu", "cửa hàng", "hotline", "số điện thoại", "liên hệ" };
            return keywords.Any(kw => text.Contains(kw));
        }

        /// <summary>
        /// Thuật toán RAG tối ưu: Ưu tiên bóc tách và khớp chính xác theo Thương hiệu trước
        /// </summary>
        private async Task<List<ProductModel>> GetProductContextData(string msg)
        {
            string msgLower = msg.ToLower().Trim();

            // tìm tên Thương hiệu xuất hiện trong tin nhắn
            var brands = await _context.Brands.AsNoTracking().ToListAsync();
            var matchedBrand = brands.FirstOrDefault(b => msgLower.Contains(b.Name.ToLower()));

            // Nếu khách đề cập trực tiếp đến một Thương hiệu (ví dụ: "Rolex"), lấy ngay các sản phẩm của thương hiệu đó
            if (matchedBrand != null)
            {
                return await _context.Products
                    .Include(p => p.Brand)
                    .Where(p => p.Brand != null && p.Brand.Id == matchedBrand.Id)
                    .Take(10) // Lấy ra tối đa 10 sản phẩm
                    .ToListAsync();
            }

            // Nếu không nhắc đến thương hiệu, tiến hành tách từ khóa tìm kiếm theo tên
            string[] noiseWords = { "tôi", "muốn", "biết", "về", "sản", "phẩm", "cho", "hỏi", "tìm", "kiếm", "mẫu", "dòng", "xem", "loại", "chi tiết", "giá" };
            var words = msgLower.Split(new[] { ' ', ',', '.', '-', '/', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(w => !noiseWords.Contains(w))
                                .ToList();

            if (!words.Any())
                return new List<ProductModel>();

            var query = _context.Products.Include(p => p.Brand).AsNoTracking().AsQueryable();
            foreach (var word in words)
            {
                query = query.Where(p => p.Name.ToLower().Contains(word));
            }

            return await query.Take(5).ToListAsync();
        }

        // tạo card sản phẩm 
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
                        // Chuẩn hóa folder ảnh tĩnh đúng cấu trúc thực tế của bạn
                        if (!string.IsNullOrEmpty(dto.ImageUrl) && !dto.ImageUrl.StartsWith("http") && !dto.ImageUrl.StartsWith("/"))
                        {
                            dto.ImageUrl = "/media/products/" + dto.ImageUrl;
                        }
                        products.Add(dto);
                    }
                }
                catch { }
            }
            return products;
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return Json(new { success = false, messages = new List<object>() });
            }

            // Lấy toàn bộ lịch sử trò chuyện của phiên này, sắp xếp từ cũ đến mới
            var history = await _context.ChatHistory
                .Where(h => h.SessionId == sessionId)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();

            var messages = new List<object>();
            foreach (var h in history)
            {
                // Trích xuất lại danh sách card sản phẩm ẩn trong đoạn chat cũ (nếu có)
                var products = ExtractProductsFromCard(h.BotReply);

                messages.Add(new
                {
                    userMessage = h.UserMessage,
                    botReply = h.BotReply,
                    products = products,
                    createdAt = h.CreatedAt.ToString("HH:mm")
                });
            }

            return Json(new { success = true, messages = messages });
        }
    }
}