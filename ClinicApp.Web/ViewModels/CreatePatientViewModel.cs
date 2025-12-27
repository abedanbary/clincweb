using System.ComponentModel.DataAnnotations;

namespace ClinicApp.Web.ViewModels
{
    public class CreatePatientViewModel
    {
        [StringLength(20, ErrorMessage = "ID number cannot exceed 20 characters")]
        [Display(Name = "ID Number")]
        public string? IdNumber { get; set; }  
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; } = DateTime.Now.AddYears(-30);

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string? Address { get; set; }

        [StringLength(500, ErrorMessage = "Medical notes cannot exceed 500 characters")]
        public string? MedicalNotes { get; set; }

        [StringLength(300, ErrorMessage = "Allergies cannot exceed 300 characters")]
        public string? Allergies { get; set; }

        [StringLength(300, ErrorMessage = "Chronic diseases cannot exceed 300 characters")]
        public string? ChronicDiseases { get; set; }
    }
}