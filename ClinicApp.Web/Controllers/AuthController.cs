using System.Security.Claims;
using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<AppUser> _passwordHasher;

        public AuthController(ApplicationDbContext context, IPasswordHasher<AppUser> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        // 🟦 1) GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // 🟩 2) POST: /Auth/Login
       [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model){
            if (!ModelState.IsValid)
              return View(model);

           var user = await _context.AppUsers
           .FirstOrDefaultAsync(u => u.Email == model.Email);

           if (user == null)
            {
             ModelState.AddModelError("", "Invalid email or password");
              return View(model);
            }

            // ✅ تحقق آمن: مقارنة كلمة المرور المدخلة مع الـ Hash المخزّن
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

              if (result == PasswordVerificationResult.Failed)
               {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
               }

            // 🔐 لو الوصول هنا → كلمة المرور صحيحة
               var claims = new List<Claim>
               {
                 new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),  // ✅ هذا السطر جديد
                  new Claim("UserId", user.Id.ToString()),   
                  new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                  new Claim(ClaimTypes.Email, user.Email),
                  new Claim(ClaimTypes.Role, user.Role.ToString()),
                  new Claim("ClinicId", user.ClinicId.ToString())
               };

               var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
               var principal = new ClaimsPrincipal(identity);

               await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

             // 🔁 توجيه حسب الدور
               if (user.Role == UserRole.Manager)
                 return RedirectToAction("Index", "Manager");

             if (user.Role == UserRole.Doctor)
                 return RedirectToAction("Index", "Doctor");

            if (user.Role == UserRole.Assistant)
                 return RedirectToAction("Index", "Assistant");

             return RedirectToAction("Index", "Home");
        }

        // 🟥 تسجيل الخروج
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        // صفحة لو ما عنده صلاحية
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
