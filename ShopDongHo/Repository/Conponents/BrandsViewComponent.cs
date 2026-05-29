using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ShopDongHo.Repository.Conponents
{
    public class BrandsViewComponent : ViewComponent
    {
        private readonly DataContext _datacontext;
        public BrandsViewComponent(DataContext context)
        {
            _datacontext = context;
        }
        public async Task<IViewComponentResult> InvokeAsync(string viewName = "Default")
        {
            var brands = await _datacontext.Brands.ToListAsync();
            return View(viewName, brands);
        }
    }
}
