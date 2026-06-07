using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopDongHo.Models;
using ShopDongHo.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ShopDongHo.Controllers
{
    [Route("ImageSearch")]
    public class ImageSearchController : Controller
    {
        private readonly DataContext _context;
        private readonly ShopDongHo.Services.IImageSearchService _imageSearchService;
        private readonly ILogger<ImageSearchController> _logger;

        public ImageSearchController(DataContext context, ShopDongHo.Services.IImageSearchService imageSearchService, ILogger<ImageSearchController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _imageSearchService = imageSearchService ?? throw new ArgumentNullException(nameof(imageSearchService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("Search")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Search(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return Json(new { success = false, message = "Không có ?nh ???c t?i lên" });

            // Save uploaded file to wwwroot/uploads for inspection
            string saveRelative = null;
            try
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var fileName = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_") + Path.GetRandomFileName() + Path.GetExtension(image.FileName);
                var savePath = Path.Combine(uploadsDir, fileName);
                await using (var fs = new FileStream(savePath, FileMode.Create))
                {
                    await image.CopyToAsync(fs);
                }
                saveRelative = $"/uploads/{fileName}";
                _logger.LogInformation("ImageSearch: saved uploaded image to {Path} ({Bytes} bytes)", savePath, image.Length);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ImageSearch: failed to save uploaded image");
            }

            try
            {
                var keywords = await _imageSearchService.ExtractKeywordsFromImageAsync(image);

                // THÊM DÒNG NÀY ?? DEBUG
                _logger.LogInformation("S? l??ng t? khóa trích xu?t ???c: {Count}", keywords?.Count ?? 0);
                if (keywords != null)
                {
                    _logger.LogInformation("Danh sách t? khóa: {List}", string.Join(", ", keywords));
                }

                if (keywords == null || keywords.Count == 0)
                {
                    return Json(new { success = false, message = "Không tìm ???c t? khóa t? ?nh", saved = saveRelative });
                }

                var lowered = keywords.Select(k => k.ToLower()).ToList();

                var matched = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Where(p =>
                        lowered.Any(kw =>
                            (p.Name != null && p.Name.ToLower().Contains(kw)) ||
                            (p.Brand != null && p.Brand.Name != null && p.Brand.Name.ToLower().Contains(kw)) ||
                            (p.Category != null && p.Category.Name != null && p.Category.Name.ToLower().Contains(kw))
                        )
                    )
                    .Take(30)
                    .ToListAsync();

                var results = matched.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = $"{p.Price:N0}?",
                    image = string.IsNullOrEmpty(p.Images) ? "no-image.jpg" : p.Images.Trim(),
                    brand = p.Brand?.Name,
                    url = Url.Action("Details", "Product", new { id = p.Id })
                }).ToList();

                // Log matched count
                _logger.LogInformation("ImageSearch: matched {Count} products for keywords {Keywords}", results.Count, string.Join(",", keywords));

                return Json(new { success = true, reply = $"T? khóa: {string.Join(", ", keywords)}", products = results, saved = saveRelative });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ImageSearch failed");
                return Json(new { success = false, message = "L?i h? th?ng: " + ex.Message, saved = saveRelative });
            }
        }
    }
}