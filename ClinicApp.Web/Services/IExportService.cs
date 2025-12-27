using ClinicApp.Web.Models;

namespace ClinicApp.Web.Services
{
    public interface IExportService
    {
        /// <summary>
        /// Export week schedule to Excel
        /// </summary>
        byte[] ExportWeekScheduleToExcel(DateTime weekStart, List<Appointment> appointments, string clinicName);
    }
}