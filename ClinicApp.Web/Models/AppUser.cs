using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace ClinicApp.Web.Models
{
    public enum UserRole
    {
        Manager = 1,
        Doctor = 2,
        Assistant = 3
    }

    public class AppUser
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;
        public string LastName  { get; set; } = null!;

        public string Email     { get; set; } = null!;
        public string Phone     { get; set; } = null!;

        // Nullable: Google-authenticated users have no password
        public string? PasswordHash { get; set; }

        // Google OAuth link — set on first Google sign-in
        public string? GoogleId { get; set; }

        public UserRole Role { get; set; }

        // 🔹 FK + Navigation: كل مستخدم ينتمي لعيادة واحدة
        public int ClinicId { get; set; }
        [ValidateNever]     // Foreign Key
        public Clinic Clinic { get; set; } = null!;
    }
}
