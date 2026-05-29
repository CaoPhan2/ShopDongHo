using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDongHo.Repository;
using ShopDongHo.Models;

namespace ShopDongHo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin, Seller")]
    [Route("Admin/Order/")]
    public class OrderController : Controller
    {
        private readonly DataContext _dataContext;
        public OrderController(DataContext context)
        {
            _dataContext = context;
        }
        public async Task<IActionResult> Index()
        {
            return View(await _dataContext.Orders.OrderByDescending(o => o.Id).ToListAsync());
        }

        [Route("ViewOrder")]
        public async Task<IActionResult> ViewOrder(string ordercode)
        {
            var DetailsOrder = await _dataContext.OrderDetails.Include(o => o.Product).Where(o => o.OrderCode == ordercode).ToListAsync();

            //laays shippingCost
            var Order = _dataContext.Orders.Where(s => s.OrderCode == ordercode).First();
            ViewBag.ShippingCost = Order.ShippingCost;
            ViewBag.Status = Order.Status;
            ViewBag.Discount = Order.Discount;       
            ViewBag.GrandTotal = Order.GrandTotal;
            return View(DetailsOrder);
        }

        [HttpPost]
        [Route("UpdateOrder")]
        public async Task<IActionResult> UpdateOrder(string ordercode, int status)
        {
            var order = await _dataContext.Orders.FirstOrDefaultAsync(o => o.OrderCode == ordercode);
            if (order == null)
            {
                return NotFound();
            }
            order.Status = status;
            _dataContext.Update(order);

            if (status == 0)
            {
                var DetailsOrder = await _dataContext.OrderDetails.Include(od => od.Product).Where(od => od.OrderCode == ordercode).Select(od => new
                {
                    od.Quantity,
                    od.Product.Price,
                    od.Product.CapitalPrice
                }).ToListAsync();

                var statisticalModel = await _dataContext.Statisticals.FirstOrDefaultAsync(s => s.DateCreate.Date == order.CreateDate.Date);
                if (statisticalModel != null)
                {
                    foreach (var orderDetail in DetailsOrder)
                    {
                        statisticalModel.Quantity += 1;
                        statisticalModel.Sold += orderDetail.Quantity;
                        statisticalModel.Revenue += orderDetail.Quantity * orderDetail.Price;
                        statisticalModel.Profit += orderDetail.Price - orderDetail.CapitalPrice;

                    }
                    _dataContext.Update(statisticalModel);

                }
                else
                {
                    int new_quantity = 0;
                    int new_sold = 0;
                    decimal new_profit = 0;
                    foreach (var orderDetail in DetailsOrder)
                    {
                        new_quantity += 1;
                        new_sold += orderDetail.Quantity;
                        new_profit += orderDetail.Price - orderDetail.CapitalPrice;

                        statisticalModel = new StatisticalModel
                        {
                            Quantity = new_quantity,
                            Sold = new_sold,
                            Revenue = orderDetail.Quantity * orderDetail.Price,
                            Profit = new_profit,
                            DateCreate = order.CreateDate
                        };
                    }
                    _dataContext.Add(statisticalModel);

                }
            }

            try
            {
                await _dataContext.SaveChangesAsync();
                return Ok(new { success = true, message = "Order status updated successfully" });

            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occured while updating the order status.");
            }
        }

        [HttpGet]
        [Route("PaymentVnpayInfo")]
        public async Task<IActionResult> PaymentVnpayInfo(string orderId)
        {
            var vnpayInfo = await _dataContext.VnInfors.FirstOrDefaultAsync(v => v.PaymentId == orderId);
            if (vnpayInfo == null)
            {
                return NotFound();
            }
            return View(vnpayInfo);
        }

        [Route("Delete")]
        public async Task<IActionResult> Delete(int Id)
        {
            OrderModel order = await _dataContext.Orders.FindAsync(Id);
            _dataContext.Remove(order);
            _dataContext.SaveChanges();
            TempData["success"] = "Đã xóa thành công";
            return RedirectToAction("Index");
        }
    }
}
