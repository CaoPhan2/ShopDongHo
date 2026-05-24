using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDongHo.Models;
using ShopDongHo.Repository;
using System.Text.RegularExpressions;

namespace ShopDongHo.Controllers
{
    public class ChatController : Controller
    {
        private readonly DataContext _context;

        public ChatController(DataContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest req)   
        {
            string userMessage = req.Message.ToLower();
            var priceData = ExtractPrice(userMessage);
            var products = new List<ProductModel>();

            if (priceData.type == "less")
            {
                products = await _context.Products
                    .Where(p => p.Price <= priceData.maxPrice).OrderByDescending(p => p.Price).Take(5)
                    .ToListAsync();
            }
            else if (priceData.type == "greater")
            {
                products = await _context.Products
                    .Where(p => p.Price >= priceData.minPrice).OrderBy(p => p.Price).Take(5)
                    .ToListAsync();
            }
            else if (priceData.type == "range")
            {
                products = await _context.Products
                    .Where(p =>
                        p.Price >= priceData.minPrice &&
                        p.Price <= priceData.maxPrice
                    ).Take(5)
                    .ToListAsync();
            }
            else if (priceData.type == "exact")
            {
                // Tìm sản phẩm gần đúng giá
                products = await _context.Products
                    .Where(p =>
                        p.Price >= priceData.minPrice - 100000 &&      //vd: priceData = 400k thì sẽ lấy các sản phẩm từ 300k đến 500k kkk
                        p.Price <= priceData.maxPrice + 100000
                    )
                    .OrderBy(p => p.Price)
                    .Take(5)
                    .ToListAsync();

                // Nếu không có thì lấy gần nhất
                if (!products.Any())
                {
                    products = await _context.Products
                        .OrderBy(p =>
                            Math.Abs((double)(p.Price - priceData.minPrice))
                        )
                        .Take(5)
                        .ToListAsync();
                }
            }


            // TÌM THEO TỪ KHÓA

            else
            {
                var words = userMessage.ToLower().Split(' ');

                products = await _context.Products
                    .Where(p =>
                        words.Any(word =>
                            p.Name.ToLower().Contains(word) ||
                            p.Description.ToLower().Contains(word) ||
                            p.Brand.Name.ToLower().Contains(word) ||
                            p.Category.Name.ToLower().Contains(word)
                        )
                    )
                    .Take(5)
                    .ToListAsync();
            }
            if (!products.Any())
            {
                return Json(new
                {
                    reply = "Hiện tại shop chưa có sản phẩm phù hợp 😊"
                });
            }
            // BUILD PRODUCT INFO

            string productInfo = "";

            foreach (var p in products)
            {
                productInfo += $@"
                    ID: {p.Id}
                    Tên: {p.Name}
                    Giá: {p.Price:N0}đ
                    Mô tả: {p.Description}

                    ";

            }

            var sample = await _context.Products.FirstOrDefaultAsync();
            string prompt = $@"

                Bạn là nhân viên tư vấn đồng hồ chuyên nghiệp
                của cửa hàng C&P Store.

                Nhiệm vụ:

                - tư vấn sản phẩm phù hợp
                - trả lời ngắn gọn
                - thân thiện
                - chuyên nghiệp
                - ưu tiên giới thiệu sản phẩm phù hợp giá tiền
                - có thể dùng emoji nhẹ
                - xuống dòng khi liệt kê 2 sản phẩm trở lên
                - nhớ đọc giá chuẩn và trả lời đúng theo giá 
                - Hãy trả lời tự nhiên như nhân viên tư vấn bán hàng.

                    Format mỗi sản phẩm:
                    ⌚ [tên sản phẩm]
                    🆔 Mã: [Id]
                    💰 Giá: [giá]
                    ✨ [lý do ngắn phù hợp]
                   
                Thông tin sản phẩm:

                {productInfo}

                Câu hỏi khách:
                {req.Message}

                ";



            using var client = new HttpClient();

            var requestData = new
            {
                model = "gemma3:1b",
                prompt = prompt,
                stream = false
            };

            var response = await client.PostAsJsonAsync( "http://localhost:11434/api/generate", requestData);             
            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
            return Json(new{ reply = result.Response});
                
 
        }

        // hàm chuyển giá tiền từ k,tr => số nguyên
        private decimal ConvertPrice(decimal value, string unit)
        {
            unit = unit.ToLower();

            if (unit == "k")
            {
                return value * 1000;
            }

            if (unit == "tr" || unit == "triệu")
            {
                return value * 1000000;
            }

            return value;
        }
        // HÀM ĐỌC GIÁ TIỀN

        private (decimal minPrice, decimal maxPrice, string type)
        ExtractPrice(string message)
        {
            message = message.ToLower();


            // KHOẢNG GIÁ:  1tr - 2tr, 500k đến 2tr

            var rangeMatch = Regex.Match(message,@"(\d+)\s*(tr|triệu|k)?\s*(-|đến)\s*(\d+)\s*(tr|triệu|k)?");

            if (rangeMatch.Success)
            {
                decimal min =ConvertPrice(decimal.Parse(rangeMatch.Groups[1].Value),rangeMatch.Groups[2].Value);

                decimal max =ConvertPrice(decimal.Parse(rangeMatch.Groups[4].Value),rangeMatch.Groups[5].Value);
                   
                return (min, max, "range");
            }


            // GIÁ NHỎ HƠN

            var lessMatch = Regex.Match(message, @"(dưới|<)\s*(\d+)\s*(tr|triệu|k)?" );
                
            if (lessMatch.Success)
            {
                decimal value = ConvertPrice( decimal.Parse(lessMatch.Groups[2].Value), lessMatch.Groups[3].Value );
                   
                return (0, value, "less");  // lấy giá từ 0 đến value
            }

            // GIÁ LỚN HƠN

            var greaterMatch = Regex.Match(message, @"(trên|>|hơn)\s*(\d+)\s*(tr|triệu|k)?" );
                
            if (greaterMatch.Success)
            {
                decimal value =  ConvertPrice( decimal.Parse(greaterMatch.Groups[2].Value), greaterMatch.Groups[3].Value);
                  
                return (value, 0, "greater");
            }

            // giá bằng hoặc gần gần
            var exactMatch = Regex.Match(message,@"(\d+)\s*(tr|triệu|k)");

            if (exactMatch.Success)
            {
                decimal value = ConvertPrice( decimal.Parse(exactMatch.Groups[1].Value), exactMatch.Groups[2].Value );
                   
                return (value, value, "exact");
            }

            return (0, 0, "");
        }
    }
}