using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ShopDongHo.Repository.Conponents
{
    public class CategoriesViewComponent : ViewComponent
    {
        private readonly DataContext _datacontext;
        public CategoriesViewComponent(DataContext context)
        {
            _datacontext = context;
        }
        public async Task<IViewComponentResult> InvokeAsync(string viewName = "Default")
        {
            var categories = await _datacontext.Categories.ToListAsync();
            return View(viewName, categories);
        }
    }
}
