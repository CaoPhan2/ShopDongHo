using System.ComponentModel.DataAnnotations;
namespace ShopDongHo.Models
{
    public class CategoryModel
    {
        [Key]
        public int Id { get; set; }
        [Required (ErrorMessage ="Yêu Cầu Nhập Tên Danh Mục")]
        public string Name { get; set; }
        [Required (ErrorMessage = "Yêu Cầu Nhập Mô Tả")]
        public string Description { get; set; }
        public string Slug { get; set; }
        public int Status { get; set; }
    }
}
