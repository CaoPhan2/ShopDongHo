using Microsoft.AspNetCore.Identity;

namespace ShopDongHo.Models
{
    public class AppUserModel: IdentityUser
    {
        public string Occupation { get; set; }
        public string RoleId { get; set; }
        public string Token { get; set; }
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public DateTime? BirthDay { get; set; } 
        public string? Gender { get; set; }     
        public string? Avatar { get; set; }
    }
}
