
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ShopDongHo.Areas.Admin.Repository;
using ShopDongHo.Models;
using ShopDongHo.Repository;
using ShopDongHo.Services.VnPay;
using ShopDongHo.Models;
using System.Security.Claims;

namespace ShopDongHo.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly DataContext _dataContext;
        private readonly IEmailSender _emailSender;
        public CheckoutController(IEmailSender emailSender, DataContext context, IVnPayService vnPayService)
        {
            _dataContext = context;
            _emailSender = emailSender;
            _vnPayService = vnPayService;
        }
        
        public async Task<IActionResult> Checkout(string PaymentMethod, string PaymentId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if(userEmail == null)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                var ordercode = Guid.NewGuid().ToString();
                var orderItem = new OrderModel();
                orderItem.OrderCode = ordercode;
                //Nhận shipping giá từ cookie
                var shippingPriceCookie = Request.Cookies["ShippingPrice"];
                decimal shippingPrice = 0;
                //Nhận Coupon code từ cookie
                var coupon_code = Request.Cookies["CouponTitle"];

                if (shippingPriceCookie != null)
                {

                    var shippingPriceJson = shippingPriceCookie;
                    shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
                }
                else
                {
                    shippingPrice = 0;
                }


                orderItem.ShippingCost = shippingPrice;
                orderItem.CouponCode = coupon_code;
                orderItem.UserName = userEmail;
                orderItem.PaymentMethod = PaymentMethod + " " + PaymentId;

                orderItem.Status = 1;
                orderItem.CreateDate = DateTime.Now;
                _dataContext.Add(orderItem);
                _dataContext.SaveChanges();


                //taoj chi tieets donw hangf
                List<CartItemModel> carts = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
                foreach(var cart in carts)
                {
                    var orderDetails = new OrderDetails();
                    orderDetails.UserName = userEmail;
                    orderDetails.OrderCode = ordercode;
                    orderDetails.productId = cart.ProductId;
                    orderDetails.Price = cart.Price;
                    orderDetails.Quantity = cart.Quantity;

                    //update product quantity
                    var product = await _dataContext.Products.Where(p => p.Id == cart.ProductId).FirstAsync();
                    product.Quantity -= cart.Quantity;
                    product.Sold += cart.Quantity;
                    _dataContext.Update(product);

                    _dataContext.Add(orderDetails);
                    _dataContext.SaveChanges();
                }

                // ===== THỐNG KÊ =====

                var statistical = await _dataContext.Statisticals
                    .FirstOrDefaultAsync(s => s.DateCreate.Date == DateTime.Now.Date);

                int quantity = 0;
                decimal revenue = 0;
                decimal profit = 0;

                foreach (var cart in carts)
                {
                    quantity += cart.Quantity;

                    var product = await _dataContext.Products
                        .FirstOrDefaultAsync(p => p.Id == cart.ProductId);

                    if (product != null)  // 
                    {
                        decimal sellPrice = product.Price;
                        decimal capitalPrice = product.CapitalPrice;

                        revenue += sellPrice * cart.Quantity;    // doanh thu

                        profit += (sellPrice - capitalPrice) * cart.Quantity;  //lợi nhuận = (giá bán - giá vốn) * số lượng
                    }
                }

                if (statistical == null)
                {
                    statistical = new StatisticalModel
                    {
                        DateCreate = DateTime.Now,
                        Quantity = quantity,
                        Sold = 1,
                        Revenue = revenue,
                        Profit = profit
                    };

                    _dataContext.Statisticals.Add(statistical);
                }
                else
                {
                    statistical.Quantity += quantity;
                    statistical.Sold += 1;
                    statistical.Revenue += revenue;
                    statistical.Profit += profit;

                    _dataContext.Statisticals.Update(statistical);
                }

                await _dataContext.SaveChangesAsync();
                HttpContext.Session.Remove("Cart");
                Response.Cookies.Delete("CouponCode");
                Response.Cookies.Delete("CouponTitle");
                Response.Cookies.Delete("ShippingPrice");
                // send email orders when success
                var receiver = userEmail;
                var subject = "Đặt hàng thành công";
                var message = "Cảm ơn bạn đã đặt hàng. Mã đơn hàng của bạn là: " + ordercode;
                await _emailSender.SendEmailAsync(receiver, subject, message);
                TempData["Success"] = "Đặt hàng thành công, vui lòng chờ duyệt đơn hàng";
                return RedirectToAction("History", "Account");

            }
                return View();
        }

      

        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if(response.VnPayResponseCode == "00")
            {
                var newVnpayInsert = new VnPayModel
                {
                    OrderId = response.OrderId,
                    PaymentMethod = response.PaymentMethod,
                    OrderDescription = response.OrderDescription,
                    TransactionId = response.TransactionId,
                    PaymentId = response.PaymentId,
                    DateCreated = DateTime.Now

                };
               _dataContext.Add(newVnpayInsert);
               await _dataContext.SaveChangesAsync();
                var PaymentMethod = response.PaymentMethod;
                var PaymentId = response.PaymentId;
                await Checkout(PaymentMethod, PaymentId);
            }
            else
            {
                TempData["Error"] = "Thanh toán thất bại, vui lòng thử lại";
                    return RedirectToAction("Index", "Cart");
            }
            return View(response);
        }
    }
}
