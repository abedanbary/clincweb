using ClinicApp.Web.Models;

namespace ClinicApp.Web.ViewModels
{
    public class PatientsPageViewModel
    {
        public List<Patient> Patients { get; set; } = new();
        public CreatePatientViewModel NewPatient { get; set; } = new();
    }
}