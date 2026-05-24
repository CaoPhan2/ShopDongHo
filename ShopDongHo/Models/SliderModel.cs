using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopDongHo.Models
{
    public class SliderModel
    {
        public int Id { get; set; }
        [Required, MinLength(4, ErrorMessage = "Không được bỏ trống Tên")]
        public string Name { get; set; }
        [Required, MinLength(4, ErrorMessage = "Không được bỏ trống mô tả")]
        public string Description { get; set; }
        public string Image { get; set; }
        [NotMapped]
        //[FileExtensions(Extensions = "jpg,jpeg,png,gif")]
        public IFormFile ImageUpload { get; set; }

        public int? Status { get; set; }
    }
}
