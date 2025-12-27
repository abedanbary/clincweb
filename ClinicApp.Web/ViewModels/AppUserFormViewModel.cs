using System.ComponentModel.DataAnnotations;
using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class AppUserFormViewModel
    {
        public int? Id { get; set; } // null في حالة Create

        [Required]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [Required]
        [Display(Name = "Role")]
        public UserRole Role { get; set; }

        // كلمة السر فقط للفورم، مش للـ DB
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }
    }
}
