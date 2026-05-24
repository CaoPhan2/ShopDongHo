using ShopDongHo.Repository.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopDongHo.Models
{
    public class ContactModel
    {
        [Key]
        [Required(ErrorMessage = "Yêu Cầu Nhập tiêu đề website")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Bản đồ")]
        public string Map { get; set; }
        
        [Required(ErrorMessage = "Yêu Cầu Nhập Email")]
        public string Email { get; set; }
        [Required(ErrorMessage = "Yêu Cầu Nhập Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Yêu Cầu Nhập thông tin liên hệ")]
        public string Description { get; set; }

        public string LogoImg { get; set; }

        [NotMapped]
        [FileExtention]
        public IFormFile ImageUpload { get; set; }

    }
}
