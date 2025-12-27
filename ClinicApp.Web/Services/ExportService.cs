using ClinicApp.Web.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace ClinicApp.Web.Services
{
    public class ExportService : IExportService
    {
        public byte[] ExportWeekScheduleToExcel(DateTime weekStart, List<Appointment> appointments, string clinicName)
        {
            // Set EPPlus License (NonCommercial or Commercial)
           // Set EPPlus License (NonCommercial)
           

            var weekEnd = weekStart.AddDays(6);

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Week Schedule");

            // ===== HEADER =====
            worksheet.Cells["A1:G1"].Merge = true;
            worksheet.Cells["A1"].Value = clinicName;
            worksheet.Cells["A1"].Style.Font.Size = 18;
            worksheet.Cells["A1"].Style.Font.Bold = true;
            worksheet.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(16, 185, 129)); // Green
            worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A2:G2"].Merge = true;
            worksheet.Cells["A2"].Value = "Weekly Appointments Schedule";
            worksheet.Cells["A2"].Style.Font.Size = 14;
            worksheet.Cells["A2"].Style.Font.Bold = true;
            worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            worksheet.Cells["A3:G3"].Merge = true;
            worksheet.Cells["A3"].Value = $"{weekStart:MMMM dd} - {weekEnd:MMMM dd, yyyy}";
            worksheet.Cells["A3"].Style.Font.Size = 11;
            worksheet.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells["A3"].Style.Font.Color.SetColor(Color.Gray);

            // ===== COLUMN HEADERS =====
            int currentRow = 5;
            
            var headers = new[] { "Date", "Time", "Patient", "Phone", "Doctor", "Reason", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[currentRow, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(16, 185, 129)); // Green
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            currentRow++;

            // ===== DATA ROWS =====
            if (appointments.Any())
            {
                // Group by day
                var appointmentsByDay = appointments
                    .OrderBy(a => a.StartTime)
                    .GroupBy(a => a.StartTime.Date)
                    .OrderBy(g => g.Key);

                foreach (var dayGroup in appointmentsByDay)
                {
                    bool isFirstInDay = true;

                    foreach (var apt in dayGroup)
                    {
                        // Date (only show once per day)
                        if (isFirstInDay)
                        {
                            worksheet.Cells[currentRow, 1].Value = apt.StartTime.ToLocalTime().ToString("dddd, MMM dd");
                            worksheet.Cells[currentRow, 1].Style.Font.Bold = true;
                            isFirstInDay = false;
                        }
                        else
                        {
                            worksheet.Cells[currentRow, 1].Value = ""; // Empty for same day
                        }

                        // Time
                        worksheet.Cells[currentRow, 2].Value = 
                            $"{apt.StartTime.ToLocalTime():HH:mm} - {apt.EndTime.ToLocalTime():HH:mm}";

                        // Patient
                        worksheet.Cells[currentRow, 3].Value = 
                            $"{apt.Patient.FirstName} {apt.Patient.LastName}";

                        // Phone
                        worksheet.Cells[currentRow, 4].Value = apt.Patient.Phone;

                        // Doctor
                        worksheet.Cells[currentRow, 5].Value = 
                            $"Dr. {apt.Doctor.FirstName} {apt.Doctor.LastName}";

                        // Reason
                        worksheet.Cells[currentRow, 6].Value = apt.ReasonForVisit ?? "";

                        // Status
                        var statusCell = worksheet.Cells[currentRow, 7];
                        statusCell.Value = apt.Status.ToString();
                        statusCell.Style.Font.Bold = true;
                        
                        // Status color
                        statusCell.Style.Font.Color.SetColor(apt.Status switch
                        {
                            AppointmentStatus.Scheduled => Color.Blue,
                            AppointmentStatus.Completed => Color.Green,
                            AppointmentStatus.Cancelled => Color.Red,
                            AppointmentStatus.NoShow => Color.Orange,
                            _ => Color.Gray
                        });

                        // Row styling - alternating colors
                        if (currentRow % 2 == 0)
                        {
                            for (int col = 1; col <= 7; col++)
                            {
                                worksheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                                worksheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 250, 252));
                            }
                        }

                        currentRow++;
                    }

                    // Add space between days
                    currentRow++;
                }
            }
            else
            {
                worksheet.Cells[currentRow, 1, currentRow, 7].Merge = true;
                worksheet.Cells[currentRow, 1].Value = "No appointments scheduled for this week";
                worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[currentRow, 1].Style.Font.Italic = true;
                worksheet.Cells[currentRow, 1].Style.Font.Color.SetColor(Color.Gray);
                currentRow++;
            }

            // ===== FOOTER =====
            currentRow++;
            worksheet.Cells[currentRow, 1, currentRow, 7].Merge = true;
            worksheet.Cells[currentRow, 1].Value = $"Generated on: {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}";
            worksheet.Cells[currentRow, 1].Style.Font.Size = 9;
            worksheet.Cells[currentRow, 1].Style.Font.Color.SetColor(Color.Gray);
            worksheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // ===== FORMATTING =====
            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            
            // Set minimum column widths
            worksheet.Column(1).Width = 20; // Date
            worksheet.Column(2).Width = 15; // Time
            worksheet.Column(3).Width = 20; // Patient
            worksheet.Column(4).Width = 15; // Phone
            worksheet.Column(5).Width = 20; // Doctor
            worksheet.Column(6).Width = 30; // Reason
            worksheet.Column(7).Width = 12; // Status

            // Add borders to data
            var dataRange = worksheet.Cells[5, 1, currentRow - 2, 7];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

            return package.GetAsByteArray();
        }
    }
}