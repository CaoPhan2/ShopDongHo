using System.ComponentModel.DataAnnotations;

namespace ShopDongHo.Models.ViewModel
{
    public class LoginViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập UserName")]
        public string UserName { get; set; }

        [DataType(DataType.Password), Required(ErrorMessage ="Vui lòng nhập password")]
        public string Password { get; set; }
        public string ReturnUrl { get; set; }
    }
}
