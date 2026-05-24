using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ShopDongHo.Models;
using ShopDongHo.Repository;

namespace ShopDongHo.Controllers
{
    public class CategoryController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IMemoryCache _cache;

        public CategoryController(DataContext context, IMemoryCache cache)
        {
            _dataContext = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index(string Slug = "", string sort_by = "")
        {
            // key cache riêng cho từng category + kiểu sort
            string cacheKey = $"CATEGORY_{Slug}_{sort_by}";

            // kiểm tra cache
            if (!_cache.TryGetValue(cacheKey, out List<ProductModel> products))
            {
                CategoryModel category = await _dataContext.Categories
                    .FirstOrDefaultAsync(e => e.Slug == Slug);

                if (category == null)
                    return RedirectToAction("Index");

                ViewBag.Slug = Slug;

                IQueryable<ProductModel> productsByCategory =
                    _dataContext.Products
                    .Where(e => e.CategoryId == category.Id);

                var count = await productsByCategory.CountAsync();

                if (count > 0)
                {
                    if (sort_by == "price_increase")
                    {
                        productsByCategory = productsByCategory.OrderBy(e => e.Price);
                    }
                    else if (sort_by == "price_decrease")
                    {
                        productsByCategory = productsByCategory.OrderByDescending(e => e.Price);
                    }
                    else if (sort_by == "price_newest")
                    {
                        productsByCategory = productsByCategory.OrderByDescending(e => e.Id);
                    }
                    else if (sort_by == "price_oldest")
                    {
                        productsByCategory = productsByCategory.OrderBy(e => e.Id);
                    }
                    else
                    {
                        productsByCategory = productsByCategory.OrderByDescending(e => e.Id);
                    }
                }

                products = await productsByCategory.ToListAsync();

                // cấu hình cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10))  // 10p mà ko có ai caching thì xóa
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30)); // 30p mà có dùng cũng xóa caching

                // lưu cache
                _cache.Set(cacheKey, products, cacheOptions);
            }

            return View(products);
        }
    }
}