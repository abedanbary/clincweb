using ClinicApp.Web.Models;

namespace ClinicApp.Web.Services
{
    public interface IExportService
    {
        byte[] ExportWeekScheduleToExcel(DateTime weekStart, List<Appointment> appointments, string clinicName);
        byte[] ExportInventoryToExcel(List<Material> materials, string clinicName, List<MaterialHistory> allHistory);
    }
}