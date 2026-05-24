using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopDongHo.Repository;

namespace ShopDongHo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class DashboardController : Controller
    {
        private readonly DataContext _dataContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public DashboardController(DataContext context, IWebHostEnvironment webHostEnvironment)
        {
            _dataContext = context;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            var count_products = _dataContext.Products.Count();
            var count_orders = _dataContext.Orders.Count();
            var count_users = _dataContext.Users.Count();
            var count_categories = _dataContext.Categories.Count();
            ViewBag.countProducts = count_products;
            ViewBag.countOrders = count_orders;
            ViewBag.countUsers = count_users;
            ViewBag.countCategories = count_categories;
            
            return View();
        }

        [HttpPost]
        [Route("GetChartData")]
        public async Task<IActionResult> GetChartData()
        {
            var data = _dataContext.Statisticals.Select(s => new
            {
                date = s.DateCreate.ToString("yyyy-MM-dd"),
                sold = s.Sold,
                profit = s.Profit,
                quantity = s.Quantity,
                revenue = s.Revenue

            }).ToList();
            return Json(data);
        }
        [HttpPost]
        [Route("GetChartDataBySelect")]
        public async Task<IActionResult> GetChartDataBySelect(DateTime startDate, DateTime endDate)
        {
            var data = _dataContext.Statisticals.Where(s=>s.DateCreate >= startDate && s.DateCreate <= endDate).Select(s => new
            {
                date = s.DateCreate.ToString("yyyy-MM-dd"),
                sold = s.Sold,
                profit = s.Profit,
                quantity = s.Quantity,
                revenue = s.Revenue

            }).ToList();
            return Json(data);
        }

        [HttpPost]
        [Route("FilterData")]
        public async Task<IActionResult> FilterData(DateTime? fromDate, DateTime? toDate)
        {
            var query = _dataContext.Statisticals.AsQueryable();
            if (fromDate.HasValue)
            {
                query = query.Where(s => s.DateCreate >= fromDate.Value);
            }
            if (toDate.HasValue)
            {
                query = query.Where(s => s.DateCreate <= toDate);
            }

            var data = query.Select(s => new
            {
                date = s.DateCreate.ToString("yyyy-MM-dd"),
                sold = s.Sold,
                profit = s.Profit,
                quantity = s.Quantity,
                revenue = s.Revenue
            }).ToList();
            return Json(data);
        }
        }
}
