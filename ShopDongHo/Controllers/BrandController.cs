using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShopDongHo.Models;
using ShopDongHo.Repository;

namespace ShopDongHo.Controllers
{
    public class BrandController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IMemoryCache _cache;

        public BrandController(DataContext context, IMemoryCache cache)
        {
            _dataContext = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index(string Slug = "")
        {
            string cacheKey = $"brand_{Slug}";

            // kiểm tra cache
            if (!_cache.TryGetValue(cacheKey, out List<ProductModel> products))
            {
                ViewBag.CacheStatus = "Load từ Database";
                // chưa có cache => query db
                BrandModel brand = await _dataContext.Brands
                    .FirstOrDefaultAsync(e => e.Slug == Slug);

                if (brand == null)
                    return RedirectToAction("Index");

                products = await _dataContext.Products
                    .Where(e => e.BrandId == brand.Id)
                    .OrderByDescending(e => e.Id)
                    .ToListAsync();

                // thiết lập thời gian cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

                // lưu cache
                _cache.Set(cacheKey, products, cacheOptions);
            }
            else
            {
                ViewBag.CacheStatus = "Load từ Cache";
            }

            return View(products);
        }
    }
}