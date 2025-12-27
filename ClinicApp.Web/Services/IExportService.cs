using ClinicApp.Web.Models;

namespace ClinicApp.Web.Services
{
    public interface IExportService
    {
        /// <summary>
        /// Export week schedule to Excel
        /// </summary>
        byte[] ExportWeekScheduleToExcel(DateTime weekStart, List<Appointment> appointments, string clinicName);
        byte[] ExportInventoryToExcel(List<Material> materials, string clinicName, List<MaterialHistory> allHistory);

    }
}