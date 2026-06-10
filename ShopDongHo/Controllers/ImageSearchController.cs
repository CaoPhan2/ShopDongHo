using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopDongHo.Repository;
using ShopDongHo.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShopDongHo.Controllers
{
    [Route("ImageSearch")]
    public class ImageSearchController : Controller
    {
        private readonly DataContext _context;
        private readonly IImageSearchService _imageSearchService;
        private readonly ILogger<ImageSearchController> _logger;

        public ImageSearchController(DataContext context, IImageSearchService imageSearchService, ILogger<ImageSearchController> logger)
        {
            _context = context;
            _imageSearchService = imageSearchService;
            _logger = logger;
        }
        [HttpPost("Search")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Search(IFormFile image, string keyWord)
        {
            _logger.LogInformation("[CONTROLLER-LOG 1] Hệ thống nhận được yêu cầu tìm kiếm bằng ảnh từ Client.");
            _logger.LogInformation("[CONTROLLER-LOG 1.1] Từ khóa chữ đi kèm (nếu có): '{KeyWord}'", keyWord);

            if (image == null || image.Length == 0)
            {
                _logger.LogWarning("[CONTROLLER-LOG 1.2] Không tìm thấy file ảnh đính kèm. Chuyển hướng sang tìm kiếm chữ thông thường.");
                return RedirectToAction("Search", "Product", new { keyWord = keyWord });
            }

            try
            {
                _logger.LogInformation("[CONTROLLER-LOG 2] Đang chuyển tiếp ảnh qua ImageSearchService...");
                var aiKeywords = await _imageSearchService.ExtractKeywordsFromImageAsync(image);

                _logger.LogInformation("[CONTROLLER-LOG 3] Service phản hồi kết quả về cho Controller.");

                if (aiKeywords == null || aiKeywords.Count == 0)
                {
                    _logger.LogWarning("[CONTROLLER-LOG 3.1] Mảng từ khóa trả về rỗng (0 phần tử). Kích hoạt TempData lỗi.");
                    TempData["error"] = "AI không nhận diện được hình ảnh.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("[CONTROLLER-LOG 4] Bắt đầu gộp từ khóa AI và từ khóa của người dùng để quét Database.");
                var searchTerms = aiKeywords.Select(k => k.ToLower()).ToList();

                if (!string.IsNullOrEmpty(keyWord))
                {
                    var userTerms = keyWord.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    searchTerms.AddRange(userTerms);
                }

                searchTerms = searchTerms.Distinct().Where(t => t.Length > 1).ToList();
                _logger.LogInformation("[CONTROLLER-LOG 4.1] Các từ khóa cuối cùng dùng để quét SQL: [{Terms}]", string.Join(" | ", searchTerms));

                // Tiến hành Query Database
                var query = _context.Products.AsNoTracking().Include(p => p.Brand).Include(p => p.Category).AsQueryable();

                query = query.Where(p => searchTerms.Any(term =>
                    (p.Name != null && p.Name.ToLower().Contains(term)) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)) ||
                    (p.Brand != null && p.Brand.Name != null && p.Brand.Name.ToLower().Contains(term)) ||
                    (p.Category != null && p.Category.Name != null && p.Category.Name.ToLower().Contains(term))
                ));

                var products = await query.Take(30).ToListAsync();
                _logger.LogInformation("[CONTROLLER-LOG 5] Quét DB hoàn tất. Tìm thấy {Count} sản phẩm khớp.", products.Count);

                ViewBag.keyword = string.Join(", ", aiKeywords) + (!string.IsNullOrEmpty(keyWord) ? $" + {keyWord}" : "");

                _logger.LogInformation("[CONTROLLER-LOG 6] Đang render dữ liệu ra giao diện View Product/Search.cshtml");
                return View("~/Views/Product/Search.cshtml", products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CONTROLLER-LOG EXCEPTION] Lỗi hệ thống tại ImageSearchController.");
                TempData["error"] = "Hệ thống tìm kiếm bằng ảnh gặp sự cố.";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}