using ClinicApp.Web.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicApp.Web.Services
{
    public class PrintService : IPrintService
    {
        public byte[] GenerateWeekSchedulePdf(DateTime weekStart, List<Appointment> appointments, string clinicName)
        {
            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;

            var weekEnd = weekStart.AddDays(6);

            // Group by day
            var appointmentsByDay = appointments
                .GroupBy(a => a.StartTime.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(a => a.StartTime).ToList());

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text(clinicName)
                            .FontSize(20).Bold().FontColor(Colors.Green.Medium);
                        
                        column.Item().PaddingTop(5).AlignCenter()
                            .Text("Weekly Appointments Schedule")
                            .FontSize(14).SemiBold();
                        
                        column.Item().PaddingTop(5).AlignCenter()
                            .Text($"{weekStart:MMM dd} - {weekEnd:MMM dd, yyyy}")
                            .FontSize(11).FontColor(Colors.Grey.Darken1);
                        
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                    });

                    page.Content().PaddingTop(20).Column(column =>
                    {
                        if (!appointmentsByDay.Any())
                        {
                            column.Item().AlignCenter().PaddingVertical(50)
                                .Text("No appointments this week")
                                .FontSize(12).Italic().FontColor(Colors.Grey.Medium);
                            return;
                        }

                        foreach (var day in appointmentsByDay)
                        {
                            // Day Header
                            column.Item().PaddingBottom(10).Background(Colors.Green.Lighten2)
                                .Padding(10).Text(day.Key.ToString("dddd, MMMM dd"))
                                .FontSize(12).Bold().FontColor(Colors.White);

                            // Appointments
                            foreach (var apt in day.Value)
                            {
                                column.Item().PaddingBottom(8).Border(1)
                                    .BorderColor(Colors.Grey.Lighten2).Padding(10)
                                    .Column(aptColumn =>
                                    {
                                        // Time and Patient
                                        aptColumn.Item().Row(row =>
                                        {
                                            row.RelativeItem().Text(text =>
                                            {
                                                text.Span($"{apt.StartTime.ToLocalTime():HH:mm} - {apt.EndTime.ToLocalTime():HH:mm}")
                                                    .Bold().FontColor(Colors.Green.Medium);
                                                text.Span(" | ");
                                                text.Span($"{apt.Patient.FirstName} {apt.Patient.LastName}")
                                                    .Bold();
                                            });
                                        });

                                        // Details
                                        aptColumn.Item().PaddingTop(5).Text(text =>
                                        {
                                            text.Span("Dr. ").FontSize(9);
                                            text.Span($"{apt.Doctor.FirstName} {apt.Doctor.LastName}")
                                                .FontSize(9);
                                            text.Span($" • {apt.Patient.Phone}")
                                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                                            
                                            if (!string.IsNullOrEmpty(apt.ReasonForVisit))
                                            {
                                                text.Span($" • {apt.ReasonForVisit}")
                                                    .FontSize(9).FontColor(Colors.Grey.Darken1);
                                            }
                                        });

                                        // Status
                                        aptColumn.Item().PaddingTop(3).Text(apt.Status.ToString())
                                            .FontSize(8).Bold()
                                            .FontColor(apt.Status switch
                                            {
                                                AppointmentStatus.Scheduled => Colors.Blue.Medium,
                                                AppointmentStatus.Completed => Colors.Green.Medium,
                                                AppointmentStatus.Cancelled => Colors.Red.Medium,
                                                AppointmentStatus.NoShow => Colors.Orange.Medium,
                                                _ => Colors.Grey.Medium
                                            });
                                    });
                            }

                            column.Item().PaddingBottom(15); // Space between days
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated: ").FontSize(8);
                        text.Span(DateTime.Now.ToString("MMM dd, yyyy HH:mm")).FontSize(8);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}