using ClinicApp.Web.Models;
using ClinicApp.Web.ViewModels;

namespace ClinicApp.Web.Services
{
    public interface IExportService
    {
        byte[] ExportWeekScheduleToExcel(DateTime weekStart, List<Appointment> appointments, string clinicName);
        byte[] ExportInventoryToExcel(List<Material> materials, string clinicName, List<MaterialHistory> allHistory);
        byte[] ExportPatientPaymentReport(Patient patient, List<Treatment> treatments, List<Payment> payments, string clinicName);
        byte[] ExportOutstandingBalances(List<PatientBalanceSummary> balances, string clinicName);
    }
}