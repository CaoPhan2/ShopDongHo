using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ShopDongHo.Models;
using ShopDongHo.Models.ViewModel;
using ShopDongHo.Repository;

namespace ShopDongHo.Controllers
{
    public class CartController : Controller
    {
        private readonly DataContext _dataContext;
        public CartController(DataContext _context)
        {
            _dataContext = _context;
        }
        public IActionResult Index()
        {
            List<CartItemModel> cartItems = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();

            //Nhận shipping giá từ cookie
            var shippingPriceCookie = Request.Cookies["ShippingPrice"];
            decimal shippingPrice = 0;
            if (shippingPriceCookie != null)
            {

                var shippingPriceJson = shippingPriceCookie;
                shippingPrice = JsonConvert.DeserializeObject<decimal>(shippingPriceJson);
            }

            //Nhận Coupon code từ cookie
            var coupon_code = Request.Cookies["CouponCode"];
            var subtotal = cartItems.Sum(x => x.Quantity * x.Price);

            decimal discount = 0;

            if (!string.IsNullOrEmpty(coupon_code))
            {
                var coupon = _dataContext.Coupons
                    .FirstOrDefault(x => x.Code == coupon_code);  // kt xem code user nhập có giống code nào trong db ko

                if (coupon != null)
                {
                    if (coupon.DiscountType == "percent")
                    {
                        discount = subtotal * coupon.DiscountPercent / 100m;
                    }
                    else if (coupon.DiscountType == "money")
                    {
                        discount = coupon.DiscountAmount;
                    }
                }
            }
            Response.Cookies.Append("CouponValue",
              JsonConvert.SerializeObject(discount),
              new CookieOptions
              {
                  HttpOnly = true,
                  Secure = true,
                  Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                  SameSite = SameSiteMode.Lax
              });
            ViewBag.SubTotal = subtotal;
            ViewBag.Discount = discount;
            // Tính tổng tiền đơn hàng
            CartItemViewModel cartVM = new()
            {
                CartItems = cartItems,
                ShippingCost = shippingPrice,
                CouponCode = coupon_code,
                GrandTotal = Math.Max(0, subtotal + shippingPrice - discount) // tổng ko đc < 0
            };
            ViewBag.GrandTotal = cartVM.GrandTotal;
            return View(cartVM);
        }


        public async Task<IActionResult> AddToCart(long Id)
        {
            ProductModel product = await _dataContext.Products.FindAsync(Id);
            if (product == null) return NotFound(); // Thêm kiểm tra phòng trường hợp ID bậy

            List<CartItemModel> carts = HttpContext.Session.GetJson<List<CartItemModel>>("Cart") ?? new List<CartItemModel>();
            CartItemModel cartItem = carts.Where(c => c.ProductId == Id).FirstOrDefault();

            if (cartItem == null)
            {
                carts.Add(new CartItemModel(product));
            }
            else
            {
                cartItem.Quantity += 1;
            }

            HttpContext.Session.SetJson("Cart", carts);
            TempData["success"] = "Thêm vào giỏ hàng thành công";

            // === ĐOẠN CẦN SỬA ĐỂ PHÙ HỢP CHO CẢ CHAT VÀ TRANG CHỦ ===
            // Kiểm tra xem đây có phải là request AJAX/Fetch gửi lên không
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                return Json(new { success = true }); // Trả về JSON cho JavaScript đọc
            }

            return Redirect(Request.Headers["Referer"].ToString()); // Giữ nguyên cho trang chủ tĩnh
        }

        public async Task<IActionResult> Decrease(int Id)
        {
            List<CartItemModel> carts = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            CartItemModel cartItem = carts.Where(c => c.ProductId == Id).FirstOrDefault();
            if (cartItem.Quantity > 1)
            {
                --cartItem.Quantity;
            }
            else
            {
                carts.RemoveAll(c => c.ProductId == Id);
            }

            if (carts.Count == 0)
            {
                HttpContext.Session.Remove("Cart");

            }
            else
            {
                HttpContext.Session.SetJson("Cart", carts); // lưu dữ liệu carts lên session
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Increase(int Id)
        {
            ProductModel product = await _dataContext.Products.Where(p => p.Id == Id).FirstOrDefaultAsync();
            List<CartItemModel> carts = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            CartItemModel cartItem = carts.Where(c => c.ProductId == Id).FirstOrDefault();
            if (cartItem.Quantity >= 1 && product.Quantity > cartItem.Quantity)
            {
                ++cartItem.Quantity;
            }
            else
            {
                cartItem.Quantity = product.Quantity;
                TempData["success"] = "Đã vượt quá số lượng trong kho";
            }
            if (carts.Count == 0)
            {
                HttpContext.Session.Remove("Cart");
            }
            else
            {
                HttpContext.Session.SetJson("Cart", carts);
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Remove(int Id)
        {
            List<CartItemModel> carts = HttpContext.Session.GetJson<List<CartItemModel>>("Cart");
            carts.RemoveAll(p => p.ProductId == Id);
            if (carts.Count == 0)
            {
                HttpContext.Session.Remove("Cart");
            }
            else
            {
                HttpContext.Session.SetJson("Cart", carts);
            }
            TempData["success"] = "Removed item successfully";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Clear()
        {
            HttpContext.Session.Remove("Cart");
            TempData["success"] = "Cleared all item";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("Cart/GetShipping")]
        public async Task<IActionResult> GetShipping(ShippingModel shippingModel, string tinh, string quan, string phuong)
        {
            var existingShipping = await _dataContext.Shippings.FirstOrDefaultAsync(x => x.City == tinh && x.District == quan && x.Ward == phuong);
            decimal shippingPrice = 0;

            if (existingShipping != null)
            {
                shippingPrice = existingShipping.Price;
            }
            else
            {
                shippingPrice = 70000; // Giá mặc định nếu không tìm thấy thông tin vận chuyển
            }

            var shippingPriceJson = JsonConvert.SerializeObject(shippingPrice);
            try
            {
                // setcookie
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),  // thời gian hết hạn
                    HttpOnly = true,  
                    Secure = true  //using https

                };
                Response.Cookies.Append("ShippingPrice", shippingPriceJson, cookieOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting cookie: {ex.Message}");

            }
            return Json(new { shippingPrice });
        }


        [HttpGet]
        [Route("Cart/DeleteShipping")]
        public IActionResult DeleteShipping()
        {
            Response.Cookies.Delete("ShippingPrice");
            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        [Route("Cart/GetCoupon")]
        public async Task<IActionResult> GetCoupon(string coupon_value)
        {
            var validCoupon = await _dataContext.Coupons
                .FirstOrDefaultAsync(x => x.Code == coupon_value);

            if (validCoupon == null)
            {
                return Ok(new { success = false, message = "Mã không hợp lệ" });
            }

            TimeSpan timeRemaining = validCoupon.DateExpired - DateTime.Now;

            if (timeRemaining.Days < 0)
            {
                return Ok(new { success = false, message = "Mã giảm giá đã hết hạn" });
            }

            string couponTitle = validCoupon.Code + " | " + validCoupon.Description;

            try
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    Expires = DateTimeOffset.UtcNow.AddMinutes(30),
                    SameSite = SameSiteMode.Lax
                };
                Response.Cookies.Append("CouponCode", validCoupon.Code, cookieOptions);
                Response.Cookies.Append("CouponTitle", couponTitle, cookieOptions);

                return Ok(new { success = true, message = "Áp dụng mã thành công" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting cookie: {ex.Message}");
                return Ok(new { success = false, message = "Áp dụng mã thất bại" });
            }
        }

    }
}