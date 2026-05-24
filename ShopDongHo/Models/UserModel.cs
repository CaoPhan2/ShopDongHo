using System.ComponentModel.DataAnnotations;

namespace ShopDongHo.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập UserName")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập Email"), EmailAddress]
        public string Email { get; set; }
        [DataType(DataType.Password), Required(ErrorMessage = "Vui lòng nhập Password")]
        public string Password { get; set; }

    }
}
