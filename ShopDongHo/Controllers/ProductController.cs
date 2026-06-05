using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopDongHo.Models;
using ShopDongHo.Repository;
using ShopDongHo.Models.ViewModel;
using System.Threading.Tasks;

namespace ShopDongHo.Controllers
{

    public class ProductController : Controller
    {
        private readonly DataContext _dataContext;
        public ProductController(DataContext context)
        {
            _dataContext = context;
        }
        
        public IActionResult Index()
        {
            
            return View();
        }

        public async Task<IActionResult> Details(int Id)
        {
            if (Id == null) return RedirectToAction("Index");       
            var productById = _dataContext.Products.Include(p=>p.Ratings).Where(p => p.Id == Id).FirstOrDefault();
            // san pham lien quan
            var relatedProducts = await _dataContext.Products.Where(p=>p.CategoryId == productById.CategoryId && p.Id != productById.Id).Take(4).ToListAsync();
            ViewBag.RelatedProducts = relatedProducts;

            //rating
            var viewModel = new ProductDetailViewModel
            {
                ProductDetails = productById,
                
            };
            return View(viewModel);
        }

        public async Task<IActionResult> Search(string keyWord)
        {
            var products = await _dataContext.Products.Where(p => p.Name.Contains(keyWord) || p.Description.Contains(keyWord)).ToListAsync();
            ViewBag.keyword = keyWord;
            return View(products);
        }

        public async Task<IActionResult> CommentProduct(RatingModel rating)
        {
            if (ModelState.IsValid)
            {
                rating.CreatedAt = DateTime.Now;
                _dataContext.Ratings.Add(rating);
                await _dataContext.SaveChangesAsync();

                TempData["success"] = "Thêm đánh giá thành công";
                return Redirect(Request.Headers["Referer"]);
            }

            List<string> errors = new List<string>();
            foreach (var value in ModelState.Values)
            {
                foreach (var error in value.Errors)
                {
                    errors.Add(error.ErrorMessage);
                }
            }

            TempData["error"] = "Thêm đánh giá thất bại";

            return RedirectToAction("Details", new { id = rating.ProductId });
        }

    }
}
