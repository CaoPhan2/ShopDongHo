using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDongHo.Repository;

namespace ShopDongHo.ViewComponents
{
    public class BestsellerViewComponent : ViewComponent
    {
        private readonly DataContext _context;

        public BestsellerViewComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var products = await _context.Products
                .OrderByDescending(x => x.Sold)
                .Take(3)
                .ToListAsync();

            return View(products);
        }
    }
}