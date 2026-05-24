using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopDongHo.Models
{
    public class RatingModel
    {
        [Key]
        public int Id { get; set; }
        public long ProductId { get; set; }
        public string Comment { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Tên")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Email")]
        public string Email { get; set; }
        public int Star { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("ProductId")]
        public ProductModel Product { get; set; }


    }
}
