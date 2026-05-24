using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ShopDongHo.Repository.Conponents
{
    public class FooterViewComponent : ViewComponent
    {
        private readonly DataContext _datacontext;
        public FooterViewComponent(DataContext context)
        {
            _datacontext = context;
        }
        public async Task<IViewComponentResult> InvokeAsync() => View(await _datacontext.Contact.FirstOrDefaultAsync());

    }
}
