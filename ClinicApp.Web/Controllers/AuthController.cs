using System.Security.Claims;
using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Auth/Login — show Google sign-in page
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectByRole();

            return View();
        }

        // GET /Auth/ExternalLogin?provider=Google — kick off Google OAuth challenge
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider = "Google")
        {
            var redirectUrl = Url.Action(nameof(ExternalCallback), "Auth");
            var properties  = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, provider);
        }

        // GET /Auth/ExternalCallback — Google redirects here after sign-in
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalCallback()
        {
            // Read the temporary ExternalCookie set by the Google middleware
            var result = await HttpContext.AuthenticateAsync("ExternalCookie");
            if (!result.Succeeded)
            {
                TempData["Error"] = "Google sign-in failed or was cancelled.";
                return RedirectToAction(nameof(Login));
            }

            // Extract identity from Google
            var principal  = result.Principal;
            var googleId   = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var email      = principal.FindFirstValue(ClaimTypes.Email);
            var firstName  = principal.FindFirstValue(ClaimTypes.GivenName)
                             ?? principal.FindFirstValue(ClaimTypes.Name)?.Split(' ').FirstOrDefault()
                             ?? "User";
            var lastName   = principal.FindFirstValue(ClaimTypes.Surname)
                             ?? (principal.FindFirstValue(ClaimTypes.Name)?.Contains(' ') == true
                                 ? principal.FindFirstValue(ClaimTypes.Name)!.Substring(principal.FindFirstValue(ClaimTypes.Name)!.IndexOf(' ') + 1)
                                 : "");

            // Discard the temporary cookie — we no longer need it
            await HttpContext.SignOutAsync("ExternalCookie");

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(googleId))
            {
                TempData["Error"] = "Could not retrieve your email from Google. Please try again.";
                return RedirectToAction(nameof(Login));
            }

            // Find existing user: first by GoogleId, then fall back to email match (for existing accounts)
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.GoogleId == googleId)
                ?? await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                // Unknown Google account — show pending approval page
                ViewBag.Email     = email;
                ViewBag.FirstName = firstName;
                ViewBag.LastName  = lastName;
                return View("PendingApproval");
            }

            // Link the GoogleId if this is the first Google sign-in for a legacy account
            if (user.GoogleId == null)
            {
                user.GoogleId = googleId;
                await _context.SaveChangesAsync();
            }

            // Sign in with our app cookie
            await SignInUserAsync(user);
            return RedirectByRole(user.Role);
        }

        // POST /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // GET /Auth/AccessDenied
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task SignInUserAsync(AppUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("UserId",        user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("ClinicId",   user.ClinicId.ToString()),
                new Claim("FirstName",  user.FirstName),   // used by layouts
                new Claim("LastName",   user.LastName)     // used by layouts
            };

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = true });
        }

        private IActionResult RedirectByRole(UserRole? role = null)
        {
            return role switch
            {
                UserRole.Manager   => RedirectToAction("Index", "Manager"),
                UserRole.Doctor    => RedirectToAction("Index", "Doctor"),
                UserRole.Assistant => RedirectToAction("Index", "Assistant"),
                _                  => RedirectToAction("Index", "Home")
            };
        }
    }
}
