using System.ComponentModel.DataAnnotations;

namespace ShopDongHo.Models.ViewModel
{
    public class ProductDetailViewModel
    {
       public ProductModel ProductDetails { get; set; }

        [Required(ErrorMessage = "Yêu Cầu Nhập Đánh giá")]
        public string Comment { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Tên")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Email")]
        public string Email { get; set; }
    }
}
