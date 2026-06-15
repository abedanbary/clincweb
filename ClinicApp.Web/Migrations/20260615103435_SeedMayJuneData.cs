using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedMayJuneData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Appointments ─────────────────────────────────────────────────
            // Status: Scheduled=1, Completed=2, Cancelled=3, NoShow=4
            // Patients: Ahmed=1, Maria=2, John=3, Fatima=4, David=5, Nour=6
            // Doctors:  Sarah=2, Michael=3
            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] { "Id", "PatientId", "DoctorId", "ClinicId", "StartTime", "EndTime", "Status", "ReasonForVisit", "DoctorNotes", "Notes", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    // ── May 2026 ──────────────────────────────────────────────
                    { 33, 1, 2, 1, new DateTime(2026,5, 2, 9,0,0,DateTimeKind.Utc), new DateTime(2026,5, 2,10,0,0,DateTimeKind.Utc), 2, "Routine cleaning checkup",    "Teeth in good condition",           null,                  new DateTime(2026,5, 1,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 34, 2, 3, 1, new DateTime(2026,5, 5,11,0,0,DateTimeKind.Utc), new DateTime(2026,5, 5,12,0,0,DateTimeKind.Utc), 2, "Post root canal follow-up",   "Healing well, no infection",        null,                  new DateTime(2026,5, 4,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 35, 3, 3, 1, new DateTime(2026,5, 8,10,0,0,DateTimeKind.Utc), new DateTime(2026,5, 8,11,0,0,DateTimeKind.Utc), 2, "Crown preparation",           "Prepared tooth #46 for implant",    null,                  new DateTime(2026,5, 7,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 36, 4, 2, 1, new DateTime(2026,5,12,14,0,0,DateTimeKind.Utc), new DateTime(2026,5,12,15,0,0,DateTimeKind.Utc), 2, "Cosmetic whitening session",  "Patient satisfied with results",    null,                  new DateTime(2026,5,11,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 37, 5, 2, 1, new DateTime(2026,5,15, 9,0,0,DateTimeKind.Utc), new DateTime(2026,5,15,10,0,0,DateTimeKind.Utc), 2, "Orthodontic braces check",    "Alignment progressing well",        null,                  new DateTime(2026,5,14,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 38, 6, 3, 1, new DateTime(2026,5,19,11,0,0,DateTimeKind.Utc), new DateTime(2026,5,19,12,0,0,DateTimeKind.Utc), 2, "Extraction consultation",     "Planned extraction of tooth #17",   null,                  new DateTime(2026,5,18,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 39, 1, 2, 1, new DateTime(2026,5,22,10,0,0,DateTimeKind.Utc), new DateTime(2026,5,22,11,0,0,DateTimeKind.Utc), 2, "Follow-up cleaning",          "Applied fluoride treatment",        null,                  new DateTime(2026,5,21,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 40, 2, 3, 1, new DateTime(2026,5,26,13,0,0,DateTimeKind.Utc), new DateTime(2026,5,26,14,0,0,DateTimeKind.Utc), 2, "Scaling & polishing",         "Deep clean completed",              null,                  new DateTime(2026,5,25,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 41, 3, 2, 1, new DateTime(2026,5,28, 9,0,0,DateTimeKind.Utc), new DateTime(2026,5,28,10,0,0,DateTimeKind.Utc), 2, "Routine scaling",             "Good compliance with hygiene",      null,                  new DateTime(2026,5,27,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 42, 4, 3, 1, new DateTime(2026,5,30,14,0,0,DateTimeKind.Utc), new DateTime(2026,5,30,15,0,0,DateTimeKind.Utc), 3, "Implant consultation",        null,                                "Patient rescheduled", new DateTime(2026,5,29,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    // ── June 2026 ─────────────────────────────────────────────
                    { 43, 1, 2, 1, new DateTime(2026,6, 2, 9,0,0,DateTimeKind.Utc), new DateTime(2026,6, 2,10,0,0,DateTimeKind.Utc), 2, "Routine cleaning",            "Excellent hygiene maintained",      null,                  new DateTime(2026,6, 1,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 44, 6, 3, 1, new DateTime(2026,6, 4,11,0,0,DateTimeKind.Utc), new DateTime(2026,6, 4,12,0,0,DateTimeKind.Utc), 2, "Tooth extraction #17",        "Extraction successful, no compl.",  null,                  new DateTime(2026,6, 3,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 45, 5, 2, 1, new DateTime(2026,6, 6, 9,0,0,DateTimeKind.Utc), new DateTime(2026,6, 6,10,0,0,DateTimeKind.Utc), 2, "Braces adjustment",           "Wire tightened, next in 4 weeks",  null,                  new DateTime(2026,6, 5,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 46, 2, 3, 1, new DateTime(2026,6, 9,13,0,0,DateTimeKind.Utc), new DateTime(2026,6, 9,14,0,0,DateTimeKind.Utc), 2, "Scaling follow-up",           "Gum health improved significantly", null,                  new DateTime(2026,6, 8,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 47, 3, 2, 1, new DateTime(2026,6,11,10,0,0,DateTimeKind.Utc), new DateTime(2026,6,11,11,0,0,DateTimeKind.Utc), 2, "Implant procedure #46",       "Implant placed successfully",       null,                  new DateTime(2026,6,10,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 48, 4, 3, 1, new DateTime(2026,6,12,14,0,0,DateTimeKind.Utc), new DateTime(2026,6,12,15,0,0,DateTimeKind.Utc), 2, "Final whitening session",     "Treatment plan completed",          null,                  new DateTime(2026,6,11,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 49, 1, 2, 1, new DateTime(2026,6,16,10,0,0,DateTimeKind.Utc), new DateTime(2026,6,16,11,0,0,DateTimeKind.Utc), 1, "Preventive checkup & X-ray",  null,                                null,                  new DateTime(2026,6,14,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 50, 2, 2, 1, new DateTime(2026,6,18, 9,0,0,DateTimeKind.Utc), new DateTime(2026,6,18,10,0,0,DateTimeKind.Utc), 1, "General consultation",        null,                                null,                  new DateTime(2026,6,14,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 51, 6, 3, 1, new DateTime(2026,6,20,11,0,0,DateTimeKind.Utc), new DateTime(2026,6,20,12,0,0,DateTimeKind.Utc), 1, "Denture fitting",             null,                                null,                  new DateTime(2026,6,15,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                    { 52, 5, 2, 1, new DateTime(2026,6,23, 9,0,0,DateTimeKind.Utc), new DateTime(2026,6,23,10,0,0,DateTimeKind.Utc), 1, "Braces monthly check",        null,                                null,                  new DateTime(2026,6,15,0,0,0,DateTimeKind.Utc), (object?)null, (object?)null, (object?)null },
                });

            // ── Treatment Plans ───────────────────────────────────────────────
            // PlanStatus: Draft=1, Active=2, Completed=3, Cancelled=4
            migrationBuilder.InsertData(
                table: "TreatmentPlans",
                columns: new[] { "Id", "Title", "Description", "TotalEstimatedCost", "Status", "CreatedAt", "StartDate", "CompletedAt", "PatientId", "DoctorId", "ClinicId" },
                values: new object[,]
                {
                    { 6, "Full Mouth Restoration - Nour Abdullah",  "Scaling, extraction and complete denture", 2430m, 2, new DateTime(2026,5, 1,0,0,0,DateTimeKind.Utc), new DateTime(2026,5, 2,0,0,0,DateTimeKind.Utc), (object?)null,                                   6, 3, 1 },
                    { 7, "Extraction & Implant - John Smith",       "Scale, extract and place implant #46",     5250m, 2, new DateTime(2026,5, 5,0,0,0,DateTimeKind.Utc), new DateTime(2026,5, 8,0,0,0,DateTimeKind.Utc), (object?)null,                                   3, 3, 1 },
                    { 8, "Whitening & Cosmetic - Fatima Al-Hassan", "Full cleaning followed by whitening",       950m, 3, new DateTime(2026,5,10,0,0,0,DateTimeKind.Utc), new DateTime(2026,5,12,0,0,0,DateTimeKind.Utc), new DateTime(2026,6,12,0,0,0,DateTimeKind.Utc), 4, 2, 1 },
                    { 9, "Preventive Care Plan - Ahmed Al-Rashid",  "Routine cleaning, fluoride and X-rays",     650m, 2, new DateTime(2026,6, 1,0,0,0,DateTimeKind.Utc), new DateTime(2026,6, 2,0,0,0,DateTimeKind.Utc), (object?)null,                                   1, 2, 1 },
                });

            // ── Treatments ────────────────────────────────────────────────────
            // Type: Cleaning=1, Filling=2, RootCanal=3, Extraction=4, Crown=5, Implant=7, Whitening=8, Denture=10, Scaling=11, Other=12
            // Status: Planned=1, InProgress=2, Completed=3
            migrationBuilder.InsertData(
                table: "Treatments",
                columns: new[] { "Id", "Title", "Description", "Cost", "EstimatedCost", "TreatmentDate", "CompletedAt", "ToothNumber", "Type", "Status", "Priority", "BeforeImageUrl", "AfterImageUrl", "PatientId", "DoctorId", "ClinicId", "TreatmentPlanId" },
                values: new object[,]
                {
                    // Plan 6 — Nour Abdullah: Full Mouth Restoration
                    { 11, "Full Mouth Scaling",       null, 280m,  280m,  new DateTime(2026,5, 2,0,0,0,DateTimeKind.Utc), new DateTime(2026,5, 2,0,0,0,DateTimeKind.Utc), (object?)null, 11, 3, 2, null, null, 6, 3, 1, 6 },
                    { 12, "Tooth Extraction #17",     null, 350m,  350m,  new DateTime(2026,5,19,0,0,0,DateTimeKind.Utc), new DateTime(2026,5,19,0,0,0,DateTimeKind.Utc), 17,            4,  3, 1, null, null, 6, 3, 1, 6 },
                    { 13, "Complete Denture Fitting", null, 1800m, 1800m, new DateTime(2026,6,20,0,0,0,DateTimeKind.Utc), (object?)null,                                  (object?)null, 10, 1, 3, null, null, 6, 3, 1, 6 },
                    // Plan 7 — John Smith: Extraction & Implant
                    { 14, "Scaling & Polishing",      null, 250m,  250m,  new DateTime(2026,5, 8,0,0,0,DateTimeKind.Utc), new DateTime(2026,5, 8,0,0,0,DateTimeKind.Utc), (object?)null, 11, 3, 2, null, null, 3, 3, 1, 7 },
                    { 15, "Dental Implant #46",       null, 5000m, 5000m, new DateTime(2026,6,11,0,0,0,DateTimeKind.Utc), (object?)null,                                  46,            7,  2, 1, null, null, 3, 3, 1, 7 },
                    // Plan 8 — Fatima Al-Hassan: Whitening & Cosmetic
                    { 16, "Initial Cleaning",         null, 200m,  200m,  new DateTime(2026,5,12,0,0,0,DateTimeKind.Utc), new DateTime(2026,5,12,0,0,0,DateTimeKind.Utc), (object?)null, 1,  3, 2, null, null, 4, 2, 1, 8 },
                    { 17, "Professional Whitening",   null, 750m,  750m,  new DateTime(2026,6,12,0,0,0,DateTimeKind.Utc), new DateTime(2026,6,12,0,0,0,DateTimeKind.Utc), (object?)null, 8,  3, 1, null, null, 4, 2, 1, 8 },
                    // Plan 9 — Ahmed Al-Rashid: Preventive Care
                    { 18, "Routine Cleaning",         null, 300m,  300m,  new DateTime(2026,5,22,0,0,0,DateTimeKind.Utc), new DateTime(2026,5,22,0,0,0,DateTimeKind.Utc), (object?)null, 1,  3, 2, null, null, 1, 2, 1, 9 },
                    { 19, "Fluoride Application",     null, 150m,  150m,  new DateTime(2026,6, 2,0,0,0,DateTimeKind.Utc), new DateTime(2026,6, 2,0,0,0,DateTimeKind.Utc), (object?)null, 12, 3, 2, null, null, 1, 2, 1, 9 },
                    { 20, "Comprehensive X-Ray",      null, 200m,  200m,  new DateTime(2026,6,16,0,0,0,DateTimeKind.Utc), (object?)null,                                  (object?)null, 12, 1, 3, null, null, 1, 2, 1, 9 },
                });

            // ── Payments ──────────────────────────────────────────────────────
            // Method: Cash=1, CreditCard=2, BankTransfer=3, Insurance=4
            // Status: Paid=1, Pending=2
            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "InvoiceNumber", "Amount", "Method", "Status", "PaymentDate", "Notes", "PatientId", "DoctorId", "TreatmentId", "AppointmentId", "ClinicId" },
                values: new object[,]
                {
                    { 15, 1015,  280m, 1, 1, new DateTime(2026,5, 2,0,0,0,DateTimeKind.Utc), "Full mouth scaling",                     6, 3, 11, (object?)null, 1 },
                    { 16, 1016,  350m, 2, 1, new DateTime(2026,5,19,0,0,0,DateTimeKind.Utc), null,                                     6, 3, 12, (object?)null, 1 },
                    { 17, 1017,  250m, 1, 1, new DateTime(2026,5, 8,0,0,0,DateTimeKind.Utc), null,                                     3, 3, 14, (object?)null, 1 },
                    { 18, 1018,  200m, 1, 1, new DateTime(2026,5,12,0,0,0,DateTimeKind.Utc), null,                                     4, 2, 16, (object?)null, 1 },
                    { 19, 1019,  300m, 2, 1, new DateTime(2026,5,22,0,0,0,DateTimeKind.Utc), null,                                     1, 2, 18, (object?)null, 1 },
                    { 20, 1020,  750m, 2, 1, new DateTime(2026,6,12,0,0,0,DateTimeKind.Utc), null,                                     4, 2, 17, (object?)null, 1 },
                    { 21, 1021,  150m, 1, 1, new DateTime(2026,6, 2,0,0,0,DateTimeKind.Utc), null,                                     1, 2, 19, (object?)null, 1 },
                    { 22, 1022, 5000m, 4, 2, new DateTime(2026,6,11,0,0,0,DateTimeKind.Utc), "Insurance claim pending — implant #46", 3, 3, 15, (object?)null, 1 },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData("Payments",       "Id", new object[] { 15, 16, 17, 18, 19, 20, 21, 22 });
            migrationBuilder.DeleteData("Treatments",     "Id", new object[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });
            migrationBuilder.DeleteData("TreatmentPlans", "Id", new object[] { 6, 7, 8, 9 });
            migrationBuilder.DeleteData("Appointments",   "Id", new object[] { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52 });
        }
    }
}
