using ClinicApp.Web.Data;
using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace ClinicApp.Web.Controllers
{
     [Authorize(Roles = "Manager")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentClinicId()
        {
            var clinicClaim = User.FindFirst("ClinicId");
            if (clinicClaim == null)
                throw new InvalidOperationException("ClinicId claim is missing.");

            return int.Parse(clinicClaim.Value);
        }

        // 🟦 GET: /Users
        public async Task<IActionResult> Index()
        {
            var clinicId = GetCurrentClinicId();
            var users = await _context.AppUsers
                .Where(m => m.ClinicId == clinicId)
                .OrderBy(m => m.LastName)
                .ToListAsync();

            var vm = new UsersPageViewModel
            {
                AppUsers = users,
                NewUser = new CreateUserViewModel()
            };

            ViewData["Title"] = "Users";
            return View(vm);
        }

        // 🟩 POST: /Users/Add
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(UsersPageViewModel vm)
        {
            var clinicId = GetCurrentClinicId();

            if (!ModelState.IsValid)
            {
                vm.AppUsers = await _context.AppUsers
                    .Where(u => u.ClinicId == clinicId)
                    .OrderBy(u => u.LastName)
                    .ToListAsync();

                return View("Index", vm);
            }

            // Invite model: no password — user authenticates via Google OAuth
            var newUser = new AppUser
            {
                FirstName = vm.NewUser.FirstName,
                LastName = vm.NewUser.LastName,
                Email = vm.NewUser.Email,
                Phone = vm.NewUser.Phone,
                Role = vm.NewUser.Role,
                ClinicId = clinicId,
                PasswordHash = null,
                GoogleId = null
            };

            _context.AppUsers.Add(newUser);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🟡 POST: /Users/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string firstName, string lastName, string email, string phone, int role)
        {
            var clinicId = GetCurrentClinicId();

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.ClinicId == clinicId);

            if (user == null)
                return NotFound();

            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.Phone = phone;
            user.Role = (UserRole)role;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // 🔴 POST: /Users/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var clinicId = GetCurrentClinicId();

            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == id && u.ClinicId == clinicId);

            if (user == null)
                return NotFound();

            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}