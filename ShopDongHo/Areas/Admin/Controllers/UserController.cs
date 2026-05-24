using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using ShopDongHo.Models;
using ShopDongHo.Repository;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShopDongHo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _dataContext;
        public UserController(DataContext context, UserManager<AppUserModel> userManager, RoleManager<IdentityRole> roleManager)
        {
            _dataContext = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var usersWithRoles = await (from u in _dataContext.Users
                                        join ur in _dataContext.UserRoles on u.Id equals ur.UserId
                                        join r in _dataContext.Roles on ur.RoleId equals r.Id
                                        select new { User = u, RoleName = r.Name }
                                        ).ToListAsync();
            var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.loggedInUserId = loggedInUserId;
            return View(usersWithRoles);
        }
        [HttpGet]
        [Route("Create")]
        
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(new AppUserModel());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserModel user)
        {
            if (ModelState.IsValid)
            {
                AppUserModel newUser = new AppUserModel { UserName = user.UserName, Email = user.Email };
                IdentityResult result = await _userManager.CreateAsync(newUser, user.Password);
                if (result.Succeeded)
                {
                    // Gán user cho người dùng mới 
                    var roleAssignResult = await _userManager.AddToRoleAsync(newUser, "User");

                    if (!roleAssignResult.Succeeded)
                    {
                        // If role assignment fails, add errors to the ModelState
                        foreach (IdentityError error in roleAssignResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return View(user);
                    }

                    TempData["success"] = "Tạo khách hàng thành công và đã gán quyền khách hàng.";

                    return Redirect("/Account/Login");
                }
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(user);
        }

        private void AddIdentityErrors(IdentityResult identityResult)
        {
            foreach(var error in identityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        [HttpGet]
        [Route("Edit")]

        public async Task<IActionResult> Edit(string Id)
        {
            if (string.IsNullOrEmpty(Id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(Id);
            if(user == null)
            {
                return NotFound();
            }
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(user);
        }
        [HttpPost]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string Id, AppUserModel user)
        {
            var existingUser = await _userManager.FindByIdAsync(Id);

            if (ModelState.IsValid)
            {
                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;

                // UPDATE USER
                var resultUpdate = await _userManager.UpdateAsync(existingUser);

                if (resultUpdate.Succeeded)
                {
                    // ROLE HIỆN TẠI
                    var currentRoles = await _userManager.GetRolesAsync(existingUser);

                    // XÓA ROLE CŨ
                    await _userManager.RemoveFromRolesAsync(existingUser, currentRoles);

                    // TÌM ROLE MỚI
                    var role = await _roleManager.FindByIdAsync(user.RoleId);

                    if (role != null)
                    {
                        // ADD ROLE MỚI
                        await _userManager.AddToRoleAsync(existingUser, role.Name);
                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    AddIdentityErrors(resultUpdate);
                    return View(existingUser);
                }
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");

            return View(existingUser);
        }

        [Route("Delete")]

        public async Task<IActionResult> Delete(string Id)
        {
            if (string.IsNullOrEmpty(Id))
            {
                return NotFound();
            }
            else
            {
                var user = await _userManager.FindByIdAsync(Id);
                if(user == null)
                {
                    return NotFound();
                }
                var deleteResult = await _userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    return View("Error");
                }
                TempData["success"] = "Xóa user thành công";
                return RedirectToAction("Index");
            }
        }
    }
}
