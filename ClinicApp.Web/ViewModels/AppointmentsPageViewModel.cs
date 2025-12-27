using ClinicApp.Web.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicApp.Web.ViewModels
{
    public class AppointmentsPageViewModel
    {
        public List<Appointment> Appointments { get; set; } = new();
        public CreateAppointmentViewModel NewAppointment { get; set; } = new();
        
        // للقوائم المنسدلة
        public List<SelectListItem> Patients { get; set; } = new();
        public List<SelectListItem> Doctors { get; set; } = new();
    }
}