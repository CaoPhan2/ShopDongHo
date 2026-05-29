
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ShopDongHo.Areas.Admin.Repository;
using ShopDongHo.Models;
using ShopDongHo.Models;
using ShopDongHo.Models.ViewModel;
using ShopDongHo.Repository;
using ShopDongHo.Services.VnPay;
using System.Security.Claims;

namespace ShopDongHo.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly DataContext _dataContext;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<AppUserModel> _UserManager;
        public CheckoutController(IEmailSender emailSender, DataContext context, IVnPayService vnPayService, UserManager<AppUserModel> userManager)
        {
            _dataContext = context;
            _emailSender = emailSender;
            _vnPayService = vnPayService;
            _UserManager = userManager;
        }

        public async Task<IActionResult> Index()
        {

            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (userEmail != null)
            {
                var user = await _UserManager.FindByEmailAsync(userEmail);
                ViewBag.FullName = user?.FullName;
                ViewBag.Phone = user?.PhoneNumber;
                ViewBag.Address = user?.Address;
            }

            var cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart")
                            ?? new List<CartItemModel>();

            decimal subtotal = cartItems.Sum(x => x.Total);

           

            decimal discount = 0;

            var couponCode = Request.Cookies["CouponCode"];

            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = _dataContext.Coupons
                    .FirstOrDefault(x => x.Code == couponCode);

                if (coupon != null)
                {
                    if (coupon.DiscountType == "percent")
                    {
                        discount = subtotal * coupon.DiscountPercent / 100m;
                    }
                    else
                    {
                        discount = coupon.DiscountAmount;
                    }
                }
            }

            var model = new CartItemViewModel
            {
                CartItems = cartItems,
                GrandTotal = Math.Max(0, subtotal - discount)
            };

            ViewBag.SubTotal = subtotal;
            ViewBag.Discount = discount;

            return View(model);
        }

        

        [HttpPost]
        public async Task<IActionResult> Checkout(string PaymentMethod, string PaymentId)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (userEmail == null)
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
                // Đọc discount từ cookie
                decimal discountAmount = 0;
                var discountCookie = Request.Cookies["CouponValue"];
                if (discountCookie != null)
                    discountAmount = JsonConvert.DeserializeObject<decimal>(discountCookie);

                decimal totalCartPrice = carts.Sum(c => c.Price * c.Quantity);

                
                // Lưu discount và grandtotal vào order
                orderItem.Discount = discountAmount;
                orderItem.GrandTotal = Math.Max(0, totalCartPrice + shippingPrice - discountAmount);
                _dataContext.Update(orderItem);
                await _dataContext.SaveChangesAsync();

                foreach (var cart in carts)
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

                // Discount và tổng tiền lưu ở OrderModel
                orderItem.Discount = discountAmount;
                orderItem.GrandTotal = Math.Max(0, carts.Sum(c => c.Price * c.Quantity) + shippingPrice - discountAmount);

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

                return RedirectToAction("Success", "Checkout",new { ordercode = orderItem.OrderCode });
    

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
                // Sau khi Checkout() lưu xong, lấy ordercode mới nhất của user từ DB
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                var lastOrder = await _dataContext.Orders
                    .Where(o => o.UserName == userEmail)
                    .OrderByDescending(o => o.CreateDate)
                    .FirstOrDefaultAsync();

                if (lastOrder == null)
                {
                    TempData["Error"] = "Không tìm thấy đơn hàng";
                    return RedirectToAction("Index", "Cart");
                }

                //Gán ordercode thật vào response để View dùng đúng
                response.OrderId = lastOrder.OrderCode;
            }
            else
            {
                TempData["Error"] = "Thanh toán thất bại, vui lòng thử lại";
                    return RedirectToAction("Index", "Cart");
            }
            return View(response);
        }

        [HttpGet]
        public async Task<IActionResult> Success(string ordercode)
        {
            var order = await _dataContext.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == ordercode);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }
    }
}
