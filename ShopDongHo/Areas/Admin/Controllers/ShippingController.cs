using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDongHo.Models;
using ShopDongHo.Repository;

namespace ShopDongHo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/Shipping")]
    [Authorize(Roles = "Admin, Seller")]
    public class ShippingController : Controller
    {
        private readonly DataContext _dataContext;
        public ShippingController(DataContext context)
        {
            _dataContext = context;
        }
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var shippingList = await _dataContext.Shippings.ToListAsync();
            ViewBag.Shippings = shippingList;
            return View();
        }

        [HttpPost]
        [Route("StoreShipping")]

        public async Task<IActionResult> StoreShipping(ShippingModel shippingModel, string tinh, string phuong, string quan, decimal price)
        {
            shippingModel.City = tinh;
            shippingModel.Ward = phuong;
            shippingModel.District = quan;
            shippingModel.Price = price;

            try
            {
                var existingShipping = await _dataContext.Shippings.AnyAsync(s => s.City == tinh && s.District == quan && s.Ward == phuong);
                if (existingShipping)
                {
                    return Ok(new { duplicate = true, message = "Dữ liệu trùng lặp" });
                }
                _dataContext.Shippings.Add(shippingModel);
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true, message = "Thêm thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "có lỗi khi thêm shipping");
            }
        }

        public async Task<IActionResult> Delete(int Id)
        {
            ShippingModel shipping = await _dataContext.Shippings.FindAsync(Id);
            _dataContext.Shippings.Remove(shipping);
            await _dataContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }


}
