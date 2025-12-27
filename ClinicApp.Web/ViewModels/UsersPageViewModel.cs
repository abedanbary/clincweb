using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class UsersPageViewModel
    {
        public List<AppUser> AppUsers { get; set; } = new();
        public CreateUserViewModel NewUser { get; set; } = new();
    }
}