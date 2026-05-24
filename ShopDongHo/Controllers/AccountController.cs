using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client.Kerberos;
using ShopDongHo.Areas.Admin.Repository;
using ShopDongHo.Models;
using ShopDongHo.Models.ViewModel;
using ShopDongHo.Repository;
using System.Security.Claims;


namespace ShopDongHo.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<AppUserModel> _userManage;
        private SignInManager<AppUserModel> _signInManager;

        private readonly IEmailSender _emailSender;
        private readonly DataContext _dataContext;
        
        public AccountController(IEmailSender emailSender, SignInManager<AppUserModel> signInManager, UserManager<AppUserModel> userManage, DataContext context)
        {
            _signInManager = signInManager;
            _userManage = userManage;
            _dataContext = context;
            _emailSender = emailSender;
        }


        public async Task<IActionResult> NewPass(AppUserModel user, string token)
        { 
            var checkuser = await _userManage.Users.Where(u => u.Email == user.Email).Where(u=> u.Token == user.Token).FirstOrDefaultAsync();
            if(checkuser != null)
            {
                ViewBag.Email = checkuser.Email;
                ViewBag.Token = token;
            }
            else
            {
                TempData["error"] = "Token không hợp lệ hoặc đã hết hạn";
                return RedirectToAction("ForgetPass", "Account");
            }

                return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateNewPassword(AppUserModel user, string token)
        { 
            var checkuser = await _userManage.Users.Where(u => u.Email == user.Email).Where(u=> u.Token == user.Token).FirstOrDefaultAsync();
            if(checkuser != null)
            {
                string newtoken = Guid.NewGuid().ToString();
                var passwordHasher = new PasswordHasher<AppUserModel>();
                var passwordHash = passwordHasher.HashPassword(checkuser, user.PasswordHash);

                checkuser.PasswordHash = passwordHash;
                checkuser.Token = newtoken;

                await _userManage.UpdateAsync(checkuser);
                TempData["success"] = "Đặt lại mật khẩu thành công, vui lòng đăng nhập lại";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                TempData["error"] = "Token không hợp lệ hoặc đã hết hạn";
                return RedirectToAction("ForgetPass", "Account");
            }

            return View();
        }
        public async Task<IActionResult> ForgetPass(string returnUrl)
        { 
            return View();
        }
        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl});
        }
       
        public async Task<IActionResult> UpdateAccount()
        {
            // check logined
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            
            //get user by email
            var user = await _userManage.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
            if(user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateInfoAccount(AppUserModel user)
        {
          
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            //get user by email
            var userById = await _userManage.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if(userById == null)
            {
                return NotFound();
            }
            else
            {
                var passwordHasher = new PasswordHasher<AppUserModel>();
                var passwordHash = passwordHasher.HashPassword(userById, user.PasswordHash);

                userById.PasswordHash = passwordHash;
                _dataContext.Update(userById);
                await _dataContext.SaveChangesAsync();
                TempData["success"] = "Cập nhật thông tin tài khoản thành công";
            }
            return RedirectToAction("UpdateAccount", "Account");
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (ModelState.IsValid)
            { 
                Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(loginVM.UserName, loginVM.Password, false,false);
                if (result.Succeeded)
                {
                    return Redirect(loginVM.ReturnUrl ?? "/");
                }
                ModelState.AddModelError("", "userName hoặc Password bị sai");
            }
            return View(loginVM);
        }


        public IActionResult Create()
        {
            return View();
        }
        public async Task<IActionResult> History()
        {
            if(!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");

            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            var Orders = await _dataContext.Orders.Where(od=>od.UserName==userEmail).OrderByDescending(od=>od.Id).ToListAsync();

            ViewBag.UserEmail = userEmail;
            return View(Orders);
        }

        public async Task<IActionResult> ViewOrder(string ordercode)
        {
            var DetailsOrder = await _dataContext.OrderDetails.Include(o => o.Product).Where(o => o.OrderCode == ordercode).ToListAsync();

            //laays shippingCost
            var Order = _dataContext.Orders.Where(s => s.OrderCode == ordercode).First();
            ViewBag.ShippingCost = Order.ShippingCost;
            ViewBag.Status = Order.Status;

            return View(DetailsOrder);
        }
        public async Task<IActionResult> CancelOrder(string ordercode)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            try
            {
                var order = await _dataContext.Orders.Where(od => od.OrderCode == ordercode).FirstAsync();
                order.Status = 3;
                _dataContext.Update(order);
                await _dataContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest("An Error Occurred: " + ex.Message);
            }
            return RedirectToAction("History", "Account");
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserModel user)
        {
            if (ModelState.IsValid)
            {
                AppUserModel newUser = new AppUserModel { UserName = user.UserName, Email = user.Email };
                IdentityResult result = await _userManage.CreateAsync(newUser, user.Password);
                if (result.Succeeded)
                {
                    TempData["success"] = "Tạo User thành công";
                    return Redirect("/account/login");
                }
                foreach(IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(user);
        }

        public async Task<IActionResult> Logout(string returnUrl="/")
        {
            await HttpContext.SignOutAsync();
            await _signInManager.SignOutAsync();
            return Redirect(returnUrl);
        }

        [HttpPost]
        public async Task<IActionResult> SendMailForgotPass(AppUserModel user)
        {
            var checkMail = await _userManage.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (checkMail == null)
            {
                TempData["error"] = "Email chưa đăng kí tài khoản";
                return RedirectToAction("ForgetPass", "Account");

            }
            // Tạo token reset password
            else
            {
                string token = Guid.NewGuid().ToString();
                //update token vào database
                checkMail.Token = token;
                _dataContext.Update(checkMail);
                await _dataContext.SaveChangesAsync();
                // Gửi email chứa link reset password
                var receiver = checkMail.Email;
                var subject = "Yêu cầu đặt lại mật khẩu cho " + checkMail.Email;
                var message = "Nhấn vào link sau để đặt lại mật khẩu: " + "<a href='" + $"{Request.Scheme}://{Request.Host}/Account/NewPass?email=" + checkMail.Email + "&token=" + token + "'>";

                await _emailSender.SendEmailAsync(receiver, subject, message);
            }

            TempData["success"] = "Đã gửi email đặt lại mật khẩu, vui lòng kiểm tra hộp thư của bạn";
            return RedirectToAction("ForgetPass", "Account");
        }

        public async Task LoginByGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse")
            });
        }
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme); //xác thực , lấy info

            if (!result.Succeeded) // kt ĐN
            {
                return RedirectToAction("Login");
            }

            // lấy info
            var claims = result.Principal.Identities.FirstOrDefault()?.Claims.Select(claim => new
            {
                claim.Type,
                claim.Value
            });

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email)) //kt lấy đc email ko
            {
                TempData["error"] = "Không lấy được email từ Google.";
                return RedirectToAction("Login");
            }

            string UserName = email.Split('@')[0]; // cắt lấy name từ email

            var existingUser = await _userManage.FindByEmailAsync(email); //kt user tồn tại chưa

            if (existingUser == null) // chưa 
            {
                var passwordHasher = new PasswordHasher<AppUserModel>();
                var hashedPassword = passwordHasher.HashPassword(null, "123456");

                var newUser = new AppUserModel // tạo mới 
                {
                    UserName = UserName,
                    Email = email,
                    PasswordHash = hashedPassword
                };

                var createUserResult = await _userManage.CreateAsync(newUser);

                if (!createUserResult.Succeeded) //kt tạo user thành công ko 
                {
                    TempData["error"] = "Đăng ký thất bại";
                    return RedirectToAction("Login");
                }

                await _signInManager.SignInAsync(newUser, false);

                return RedirectToAction("Index", "Home");
            }
            else // user đã tồn t
            {
                await _signInManager.SignInAsync(existingUser, false);

                return RedirectToAction("Index", "Home");
            }
        }


    }


}
