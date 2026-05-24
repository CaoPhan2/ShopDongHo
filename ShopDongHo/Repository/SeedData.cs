using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopDongHo.Models;

namespace ShopDongHo.Repository
{
    public class SeedData
    {
        public static void SeedingData(DataContext _context)
        {
            _context.Database.Migrate();

            if (!_context.Products.Any())
            {
                // Categories
                CategoryModel dongHoNam = new CategoryModel
                {
                    Name = "Đồng Hồ Nam",
                    Slug = "dong-ho-nam",
                    Description = "Các mẫu đồng hồ thời trang dành cho nam",
                    Status = 1
                };

                CategoryModel dongHoNu = new CategoryModel
                {
                    Name = "Đồng Hồ Nữ",
                    Slug = "dong-ho-nu",
                    Description = "Các mẫu đồng hồ thời trang dành cho nữ",
                    Status = 1
                };

                // Brands
                BrandModel casio = new BrandModel
                {
                    Name = "Casio",
                    Slug = "casio",
                    Description = "Thương hiệu đồng hồ Nhật Bản nổi tiếng",
                    Status = 1
                };

                BrandModel rolex = new BrandModel
                {
                    Name = "Rolex",
                    Slug = "rolex",
                    Description = "Thương hiệu đồng hồ cao cấp Thụy Sĩ",
                    Status = 1
                };

                // Products
                _context.Products.AddRange(
                    new ProductModel
                    {
                        Name = "Casio MTP-V002 Nam",
                        Slug = "casio-mtp-v002-nam",
                        Description = "Đồng hồ nam thiết kế đơn giản, lịch lãm",
                        Images = "casio-nam.jpg",
                        Category = dongHoNam,
                        Brand = casio,
                        Price = 1200000,
                        CapitalPrice = 800000,
                        Quantity = 100
                    },

                    new ProductModel
                    {
                        Name = "Rolex Datejust Nữ",
                        Slug = "rolex-datejust-nu",
                        Description = "Đồng hồ nữ cao cấp, sang trọng",
                        Images = "rolex-nu.jpg",
                        Category = dongHoNu,
                        Brand = rolex,
                        Price = 15000000,
                        CapitalPrice = 10000000,
                        Quantity = 20
                    }
                );

                _context.SaveChanges();
            }

            // Contact
            if (!_context.Contact.Any())
            {
                ContactModel contact = new ContactModel
                {
                    Name = "Shop Đồng Hồ Thời Trang",
                    Description = "Chuyên cung cấp đồng hồ thời trang nam nữ chính hãng, giá tốt",
                    Phone = "0901234567",
                    Email = "dongho@gmail.com",
                    Map = "Google Map iframe here",
                    LogoImg = "logo-dongho.jpg"
                };

                _context.Contact.Add(contact);
                _context.SaveChanges();
            }

            // Roles
            if (!_context.Roles.Any())
            {
                var roles = new List<IdentityRole>
                {
                    new IdentityRole { Name = "Admin", NormalizedName = "ADMIN" },
                    new IdentityRole { Name = "Staff", NormalizedName = "STAFF" },
                    new IdentityRole { Name = "Customer", NormalizedName = "CUSTOMER" }
                };

                _context.Roles.AddRange(roles);
                _context.SaveChanges();
            }

            // Admin User
            if (!_context.Users.Any())
            {
                var user = new AppUserModel
                {
                    UserName = "admin",
                    Email = "admin@dongho.com",
                    EmailConfirmed = true,
                    NormalizedUserName = "ADMIN",
                    NormalizedEmail = "ADMIN@DONGHO.COM",
                    PasswordHash = new PasswordHasher<AppUserModel>().HashPassword(null, "123456"),
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };

                _context.Users.Add(user);

                var role = _context.Roles.FirstOrDefault(r => r.Name == "Admin");

                var userRole = new IdentityUserRole<string>
                {
                    UserId = user.Id,
                    RoleId = role.Id
                };

                _context.UserRoles.Add(userRole);
                _context.SaveChanges();
            }
        }
    }
}