using ClinicApp.Web.Models;

namespace ClinicApp.Web.Services
{
    public interface IPrintService
    {
        /// <summary>
        /// Generate PDF for week schedule
        /// </summary>
        /// <param name="weekStart">Start date of the week</param>
        /// <param name="appointments">List of appointments</param>
        /// <param name="clinicName">Clinic name for header</param>
        /// <returns>PDF as byte array</returns>
        byte[] GenerateWeekSchedulePdf(DateTime weekStart, List<Appointment> appointments, string clinicName);
    }
}