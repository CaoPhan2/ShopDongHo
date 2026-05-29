using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDongHo.Models;
using ShopDongHo.Repository;

namespace ShopDongHo.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<AppUserModel> _userManager;

        public HomeController(ILogger<HomeController> logger, DataContext context, UserManager<AppUserModel> userManager)
        {
            _logger = logger;
            _dataContext = context;
            _userManager = userManager;
        }

        public IActionResult Index(string price, int pg = 1)
        {
            var products = _dataContext.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            // FILTER GIÁ

            switch (price)
            {
                case "duoi-1-trieu":
                    products = products.Where(p => p.Price < 1000000);
                    break;

                case "1-3-trieu":
                    products = products.Where(p =>
                        p.Price >= 1000000 &&
                        p.Price <= 3000000);
                    break;

                case "3-5-trieu":
                    products = products.Where(p =>
                        p.Price >= 3000000 &&
                        p.Price <= 5000000);
                    break;

                case "5-10-trieu":
                    products = products.Where(p =>
                        p.Price >= 5000000 &&
                        p.Price <= 10000000);
                    break;

                case "tren-10-trieu":
                    products = products.Where(p => p.Price > 10000000);
                    break;
            }

            // PAGINATION

            const int pageSize = 12;

            int recsCount = products.Count();

            int totalPages = (int)Math.Ceiling((double)recsCount / pageSize);

            if (pg < 1) pg = 1;
            if (pg > totalPages) pg = totalPages;

            int recSkip = (pg - 1) * pageSize;

            var data = products
                .Skip(recSkip)
                .Take(pageSize)
                .ToList();

            ViewBag.Pager = new Paginate(recsCount, pg, pageSize);

            // CATEGORY NỔI BẬT

            var categories = _dataContext.Categories.Select(c => new{Category = c,ProductCount = _dataContext.Products.Count(p => p.CategoryId == c.Id)})
                .Take(4)
                .ToList();

            ViewBag.FeaturedCategories = categories;

            // lấy rating
            var reviews = _dataContext.Ratings.Where(x => x.Star >= 4).OrderByDescending(x => x.Star).Take(10).ToList();
            
            ViewBag.Reviews = reviews;
            //Viewbag dùng dropmenu
            ViewBag.Categories = _dataContext.Categories.ToList();

            return View(data);
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public async Task<IActionResult> Contact()
        {
            var contact = await _dataContext.Contact.FirstAsync();
            return View(contact);
        }

        public async Task<IActionResult> Wishlist()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null) 
                return RedirectToAction("Login", "Account");

            var wishlist = await _dataContext.Wishlists
                .Include(x => x.Product)
                .ThenInclude(x => x.Brand)
                .Where(x => x.UserId == user.Id)
                .ToListAsync();

            return View(wishlist);
        }
        public async Task<IActionResult> AboutUs()
        {

            return View();
        }



        [HttpPost]
        public async Task<IActionResult> AddToWishlist(long Id)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Unauthorized();
            }

            var wishlistItem = await _dataContext.Wishlists
                .FirstOrDefaultAsync(w =>
                    w.ProductId == Id &&
                    w.UserId == user.Id);

            // Nếu đã có => xóa
            if (wishlistItem != null)
            {
                _dataContext.Wishlists.Remove(wishlistItem);

                await _dataContext.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    added = false,
                    message = "Đã xóa khỏi yêu thích"
                });
            }

            // Nếu chưa có => thêm
            var wishlistProduct = new WishlistModel
            {
                ProductId = Id,
                UserId = user.Id
            };

            _dataContext.Wishlists.Add(wishlistProduct);

            await _dataContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                added = true,
                message = "Đã thêm vào yêu thích"
            });
        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int statuscode)
        {
            if(statuscode == 404)
            {
                return View("NotFound");
            }
            else
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
                
        }


    }
}
