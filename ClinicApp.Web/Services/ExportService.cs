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
public byte[] ExportInventoryToExcel(List<Material> materials, string clinicName, List<MaterialHistory> allHistory)
{
    using var package = new ExcelPackage();
    
    // ===== SHEET 1: Inventory Overview =====
    var overviewSheet = package.Workbook.Worksheets.Add("Inventory Overview");

    // HEADER
    overviewSheet.Cells["A1:H1"].Merge = true;
    overviewSheet.Cells["A1"].Value = clinicName;
    overviewSheet.Cells["A1"].Style.Font.Size = 18;
    overviewSheet.Cells["A1"].Style.Font.Bold = true;
    overviewSheet.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(16, 185, 129));
    overviewSheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

    overviewSheet.Cells["A2:H2"].Merge = true;
    overviewSheet.Cells["A2"].Value = "Inventory Report";
    overviewSheet.Cells["A2"].Style.Font.Size = 14;
    overviewSheet.Cells["A2"].Style.Font.Bold = true;
    overviewSheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

    overviewSheet.Cells["A3:H3"].Merge = true;
    overviewSheet.Cells["A3"].Value = $"Generated on: {DateTime.Now:MMMM dd, yyyy 'at' HH:mm}";
    overviewSheet.Cells["A3"].Style.Font.Size = 11;
    overviewSheet.Cells["A3"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    overviewSheet.Cells["A3"].Style.Font.Color.SetColor(Color.Gray);

    // COLUMN HEADERS
    int currentRow = 5;
    var headers = new[] { "Material Name", "Description", "Quantity", "Unit", "Min. Limit", "Supplier", "Last Updated", "Status" };
    
    for (int i = 0; i < headers.Length; i++)
    {
        var cell = overviewSheet.Cells[currentRow, i + 1];
        cell.Value = headers[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(16, 185, 129));
        cell.Style.Font.Color.SetColor(Color.White);
        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
    }

    currentRow++;

    // DATA ROWS
    if (materials.Any())
    {
        foreach (var material in materials.OrderBy(m => m.Name))
        {
            // Get last history entry for this material
            var lastHistory = allHistory
                .Where(h => h.MaterialId == material.Id)
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefault();

            overviewSheet.Cells[currentRow, 1].Value = material.Name;
            overviewSheet.Cells[currentRow, 2].Value = material.Description ?? "-";
            overviewSheet.Cells[currentRow, 3].Value = material.Quantity;
            overviewSheet.Cells[currentRow, 4].Value = material.Unit;
            overviewSheet.Cells[currentRow, 5].Value = material.MinimumLimit;
            overviewSheet.Cells[currentRow, 6].Value = material.Supplier ?? "-";
            overviewSheet.Cells[currentRow, 7].Value = lastHistory != null 
                ? lastHistory.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                : "-";

            // Status
            var statusCell = overviewSheet.Cells[currentRow, 8];
            var isLow = material.Quantity <= material.MinimumLimit;
            statusCell.Value = isLow ? "Low Stock" : "OK";
            statusCell.Style.Font.Bold = true;
            statusCell.Style.Font.Color.SetColor(isLow ? Color.Red : Color.Green);

            // Alternating row colors
            if (currentRow % 2 == 0)
            {
                for (int col = 1; col <= 8; col++)
                {
                    overviewSheet.Cells[currentRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    overviewSheet.Cells[currentRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 250, 252));
                }
            }

            currentRow++;
        }
    }
    else
    {
        overviewSheet.Cells[currentRow, 1, currentRow, 8].Merge = true;
        overviewSheet.Cells[currentRow, 1].Value = "No materials in inventory";
        overviewSheet.Cells[currentRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        overviewSheet.Cells[currentRow, 1].Style.Font.Italic = true;
        overviewSheet.Cells[currentRow, 1].Style.Font.Color.SetColor(Color.Gray);
    }

    // SUMMARY
    currentRow += 2;
    overviewSheet.Cells[currentRow, 1].Value = "Summary:";
    overviewSheet.Cells[currentRow, 1].Style.Font.Bold = true;
    currentRow++;

    var totalItems = materials.Count;
    var lowStockItems = materials.Count(m => m.Quantity <= m.MinimumLimit);
    var okItems = totalItems - lowStockItems;

    overviewSheet.Cells[currentRow, 1].Value = $"Total Materials: {totalItems}";
    currentRow++;
    overviewSheet.Cells[currentRow, 1].Value = $"Low Stock: {lowStockItems}";
    overviewSheet.Cells[currentRow, 1].Style.Font.Color.SetColor(Color.Red);
    currentRow++;
    overviewSheet.Cells[currentRow, 1].Value = $"OK Stock: {okItems}";
    overviewSheet.Cells[currentRow, 1].Style.Font.Color.SetColor(Color.Green);

    // FORMATTING
    overviewSheet.Cells[overviewSheet.Dimension.Address].AutoFitColumns();
    overviewSheet.Column(1).Width = 25; // Name
    overviewSheet.Column(2).Width = 35; // Description
    overviewSheet.Column(3).Width = 12; // Quantity
    overviewSheet.Column(4).Width = 10; // Unit
    overviewSheet.Column(5).Width = 12; // Min Limit
    overviewSheet.Column(6).Width = 20; // Supplier
    overviewSheet.Column(7).Width = 18; // Last Updated
    overviewSheet.Column(8).Width = 12; // Status

    // Borders
    var dataRange = overviewSheet.Cells[5, 1, currentRow - 4, 8];
    dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
    dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
    dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

    // ===== SHEET 2: Full History =====
    var historySheet = package.Workbook.Worksheets.Add("Full History");

    // HEADER
    historySheet.Cells["A1:F1"].Merge = true;
    historySheet.Cells["A1"].Value = "Material History - All Transactions";
    historySheet.Cells["A1"].Style.Font.Size = 16;
    historySheet.Cells["A1"].Style.Font.Bold = true;
    historySheet.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(16, 185, 129));
    historySheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

    // COLUMN HEADERS
    var historyHeaders = new[] { "Material Name", "Date & Time", "Change", "New Quantity", "Supplier", "Note" };
    int historyRow = 3;
    
    for (int i = 0; i < historyHeaders.Length; i++)
    {
        var cell = historySheet.Cells[historyRow, i + 1];
        cell.Value = historyHeaders[i];
        cell.Style.Font.Bold = true;
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(16, 185, 129));
        cell.Style.Font.Color.SetColor(Color.White);
        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
    }

    historyRow++;

    // DATA ROWS
    if (allHistory.Any())
    {
        foreach (var history in allHistory.OrderByDescending(h => h.CreatedAt))
        {
            var material = materials.FirstOrDefault(m => m.Id == history.MaterialId);
            
            historySheet.Cells[historyRow, 1].Value = material?.Name ?? "Unknown";
            historySheet.Cells[historyRow, 2].Value = history.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            
            // Change (with color)
            var changeCell = historySheet.Cells[historyRow, 3];
            changeCell.Value = history.QuantityChange > 0 ? $"+{history.QuantityChange}" : history.QuantityChange.ToString();
            changeCell.Style.Font.Bold = true;
            changeCell.Style.Font.Color.SetColor(history.QuantityChange > 0 ? Color.Green : Color.Red);
            
            historySheet.Cells[historyRow, 4].Value = history.NewQuantity;
            historySheet.Cells[historyRow, 5].Value = history.Supplier ?? "-";
            historySheet.Cells[historyRow, 6].Value = history.Note ?? "-";

            // Alternating colors
            if (historyRow % 2 == 0)
            {
                for (int col = 1; col <= 6; col++)
                {
                    historySheet.Cells[historyRow, col].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    historySheet.Cells[historyRow, col].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(248, 250, 252));
                }
            }

            historyRow++;
        }
    }
    else
    {
        historySheet.Cells[historyRow, 1, historyRow, 6].Merge = true;
        historySheet.Cells[historyRow, 1].Value = "No history available";
        historySheet.Cells[historyRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        historySheet.Cells[historyRow, 1].Style.Font.Italic = true;
    }

    // FORMATTING
    historySheet.Cells[historySheet.Dimension.Address].AutoFitColumns();
    historySheet.Column(1).Width = 25;
    historySheet.Column(2).Width = 20;
    historySheet.Column(3).Width = 12;
    historySheet.Column(4).Width = 15;
    historySheet.Column(5).Width = 20;
    historySheet.Column(6).Width = 40;

    // Borders
    var historyDataRange = historySheet.Cells[3, 1, historyRow - 1, 6];
    historyDataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
    historyDataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
    historyDataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
    historyDataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

    return package.GetAsByteArray();
}

       
    }
}