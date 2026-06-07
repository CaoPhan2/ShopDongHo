using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ShopDongHo.Models;
using ShopDongHo.Repository;

namespace ShopDongHo.Controllers
{
    [Route("Chat")]
    public class ChatController : Controller
    {
        private readonly DataContext _context;
        private readonly HttpClient _httpClient;

        public ChatController(DataContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpPost("Ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Message))
                return BadRequest(new { success = false, message = "Tin nhắn trống." });

            string sessionId = string.IsNullOrEmpty(req.SessionId) ? "session_default" : req.SessionId;

            try
            {
                // STEP 1: RETRIEVAL - RAG tinh lọc sản phẩm theo từ khóa để tránh quá tải ngữ cảnh cho AI
                string userMessage = req.Message.ToLower();

                var query = _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .AsQueryable();

                if (userMessage.Contains("rolex"))
                {
                    query = query.Where(p => p.Brand.Name.ToLower().Contains("rolex") || p.Name.ToLower().Contains("rolex"));
                }
                else if (userMessage.Contains("casio") || userMessage.Contains("edifice") || userMessage.Contains("g-shock") || userMessage.Contains("vintage"))
                {
                    query = query.Where(p => p.Brand.Name.ToLower().Contains("casio") || p.Name.ToLower().Contains("casio"));
                }
                else if (userMessage.Contains("seiko"))
                {
                    query = query.Where(p => p.Brand.Name.ToLower().Contains("seiko") || p.Name.ToLower().Contains("seiko"));
                }
                else if (userMessage.Contains("nữ") || userMessage.Contains("bạn gái") || userMessage.Contains("tặng") || userMessage.Contains("cơm") || userMessage.Contains("ăn"))
                {
                    query = query.Where(p => p.Category.Name.ToLower().Contains("nữ") || p.Name.ToLower().Contains("nữ") || p.Category.Name.ToLower().Contains("nam") || p.Name.ToLower().Contains("nam"));
                }

                var filteredProducts = await query.Take(10).ToListAsync();
                if (!filteredProducts.Any())
                {
                    filteredProducts = await _context.Products.Include(p => p.Brand).Include(p => p.Category).Take(5).ToListAsync();
                }

                string productCatalogText = "";
                foreach (var p in filteredProducts)
                {
                    productCatalogText += $"[MÃ ID THẬT: {p.Id} | Tên đúng: {p.Name} | Giá: {p.Price:N0}đ | Thương Hiệu đúng: {p.Brand?.Name} | Danh Mục đúng: {p.Category?.Name} | Mô tả gốc: {p.Description}]\n";
                }

                var history = await _context.Set<ChatHistory>()
                    .Where(c => c.SessionId == sessionId)
                    .OrderByDescending(c => c.CreatedAt).Take(3).OrderBy(c => c.CreatedAt).ToListAsync();

                // STEP 2: AUGMENTED - Viết lại Prompt mẫu chuẩn hóa dữ liệu thật (Bỏ ngoặc vuông lửng lơ)
                string systemPrompt = $@"Bạn là trợ lý bán hàng AI Pro cao cấp và rất duyên dáng tại cửa hàng C&P Store.
Đây là danh sách sản phẩm ĐANG CÓ THỰC TẾ tại shop:
{productCatalogText}

QUY TẮC ỨNG XỬ VÀ BỐ CỤC PHẢN HỒI (TUÂN THỦ 100%):
1. ĐIỀU HƯỚNG THÔNG MINH: Nếu khách hỏi câu không liên quan đến mua bán cửa hàng (ví dụ: rủ đi ăn cơm, hỏi thời tiết, đùa vui, nói chuyện phiếm...), bạn phải trả lời một cách khôn khéo, vui vẻ đáp lễ họ trước, sau đó lập tức lôi kéo họ vào xem hoặc mua sản phẩm phù hợp tại shop. 
   - Ví dụ: ""Dạ, được ăn cơm cùng anh/chị thì vinh hạnh cho em quá ạ! Mà trước khi đi ăn, anh/chị có muốn tham khảo một mẫu đồng hồ thật thời trang của shop để lên đồ thêm bảnh bao/xinh đẹp không ạ? Em vừa về mẫu này đeo đi tiệc hay đi ăn là hết sảy luôn...""

2. BỐ CỤC GIỚI THIỆU SẢN PHẨM: Khi chọn sản phẩm để tư vấn, bạn PHẢI điền thông tin thật vào đúng cấu trúc mẫu sau (Tuyệt đối không giữ lại các chữ chỉ dẫn nằm trong ngoặc vuông):

### **1.Sản phẩm : Tên_Sản_Phẩm_Thật **
⌚ Tên sản phẩm: Tên_Sản_Phẩm_Thật
💰 Giá tiền: Số_Tiền_Thậtđ
🆔 Mã sản phẩm: ID Số_ID_Thật
📂 Danh mục: Tên_Danh_Mục_Thật
📝 Mô tả chi tiết: [Biên soạn đoạn văn mô tả tóm tắt sản phẩm khoảng 80-100 từ dựa trên dữ liệu gốc. Câu văn thu hút người đọc].
👇 Click thẻ phía dưới để đến sản phẩm chi tiết:
[CARD_PRODUCT: Số_ID_Thật]

3. CẢNH BÁO QUY CÁCH NGHIÊM NGẶT:
   - Thẻ ""📂 Danh mục:"" bắt buộc phải xuống hàng nằm riêng biệt, không nằm cùng dòng với mã sản phẩm.
   - Dòng tiêu đề đầu tiên bắt buộc phải in đậm toàn bộ tên sản phẩm kèm theo giá tiền bằng cách đặt trong cặp dấu sao đôi như mẫu trên.
   - Tuyệt đối KHÔNG được viết dấu ký tự kết thúc như ### dư thừa sau phần mô tả.
   - Chỉ dùng số ID THẬT và Tên chính xác có trong danh sách được cấp ở trên.";

                var messages = new List<object> { new { role = "system", content = systemPrompt } };
                foreach (var h in history)
                {
                    messages.Add(new { role = "user", content = h.UserMessage });

                    string cleanReply = h.BotReply ?? "";
                    if (cleanReply.Contains("[PRODUCTS_DATA]"))
                    {
                        cleanReply = cleanReply.Substring(0, cleanReply.IndexOf("[PRODUCTS_DATA]")).Trim();
                    }
                    messages.Add(new { role = "assistant", content = cleanReply });
                }
                messages.Add(new { role = "user", content = req.Message });

                // STEP 3: GENERATION
                var payload = new { model = "qwen2.5:3b", messages = messages, stream = false };
                var response = await _httpClient.PostAsync("http://localhost:11434/api/chat",
                    new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                    return Json(new { success = false, message = "AI đang bận, vui lòng thử lại." });

                var resContent = await response.Content.ReadAsStringAsync();
                string aiResponseText = JsonDocument.Parse(resContent).RootElement.GetProperty("message").GetProperty("content").GetString();

                // STEP 4: PHÂN TÁCH BÓC TÁCH ID SẢN PHẨM
                string botReply = aiResponseText;
                var recommendedIds = new List<long>();

                var cardMatches = Regex.Matches(aiResponseText, @"\[card_product:\s*(\d+)\s*\]", RegexOptions.IgnoreCase);
                foreach (Match m in cardMatches)
                {
                    if (long.TryParse(m.Groups[1].Value, out long id) && !recommendedIds.Contains(id))
                        recommendedIds.Add(id);
                }

                // Sửa lỗi ảo giác ID bằng cơ chế quét Tên sản phẩm xuất hiện trong lời thoại
                var dbProducts = filteredProducts.Where(p => recommendedIds.Contains(p.Id)).ToList();
                foreach (var p in filteredProducts)
                {
                    if (aiResponseText.ToLower().Contains(p.Name.ToLower()) && !dbProducts.Any(x => x.Id == p.Id))
                    {
                        dbProducts.Add(p);
                    }
                }

                // Đóng gói dữ liệu kèm theo trường thương hiệu (brandName) trả về Frontend
                var mappedProducts = dbProducts.Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    brandName = p.Brand?.Name ?? "Chính hãng",
                    imageUrl = "/images/products/" + p.Images,
                    url = "/san-pham/" + p.Id
                }).ToList();

                // STEP 5: SAVE HISTORY
                _context.Set<ChatHistory>().Add(new ChatHistory
                {
                    SessionId = sessionId,
                    UserMessage = req.Message,
                    BotReply = aiResponseText,
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    reply = aiResponseText,
                    products = mappedProducts
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống RAG: " + ex.Message });
            }
        }

        [HttpGet("GetHistory")]
        public async Task<IActionResult> GetHistory(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) sessionId = "session_default";

            var data = await _context.Set<ChatHistory>()
                .Where(c => c.SessionId == sessionId)
                .OrderBy(c => c.CreatedAt).ToListAsync();

            var formattedMessages = data.Select(h => {
                string cleanReply = h.BotReply ?? "";
                var cardMatches = Regex.Matches(cleanReply, @"\[card_product:\s*(\d+)\s*\]", RegexOptions.IgnoreCase);
                var historyIds = new List<long>();
                foreach (Match m in cardMatches)
                {
                    if (long.TryParse(m.Groups[1].Value, out long id) && !historyIds.Contains(id))
                        historyIds.Add(id);
                }

                var dbHistProducts = _context.Products.Include(p => p.Brand).Where(p => historyIds.Contains(p.Id)).Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    brandName = p.Brand != null ? p.Brand.Name : "Chính hãng",
                    imageUrl = "/images/products/" + p.Images,
                    url = "/san-pham/" + p.Id
                }).ToList<object>();

                return new
                {
                    userMessage = h.UserMessage,
                    botReply = cleanReply,
                    createdAt = h.CreatedAt.ToString("HH:mm"),
                    products = dbHistProducts
                };
            });

            return Json(new { success = true, messages = formattedMessages });
        }
    }
}