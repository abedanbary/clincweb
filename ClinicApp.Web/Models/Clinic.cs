namespace ClinicApp.Web.Models
{
    public class Clinic
    {
        public int Id { get; set; }  // Primary Key

        public string Name { get; set; } // Clinic name

        public string Address { get; set; } // Clinic address
        public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    }
}
