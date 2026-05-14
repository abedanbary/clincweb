using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedDemoData : Migration
    {
        // Password hash for "123456" — ASP.NET Core PasswordHasher V3
        private const string Hash123456 = "AQAAAAIAAYagAAAAECV+bh6m3sW3wmjld9IDOjXOyTKGFoodjr99Qo7vi96eBDHTumcfCO8vZ5UCbjGotA==";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var now = new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc);

            // ── Clinic ───────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Clinics",
                columns: new[] { "Id", "Name", "Address" },
                values: new object[] { 1, "DentalTrack Clinic", "123 Health Street, Haifa" });

            // ── Users ─────────────────────────────────────────────────────────
            // Role: Manager=1, Doctor=2, Assistant=3
            migrationBuilder.InsertData(
                table: "AppUsers",
                columns: new[] { "Id", "FirstName", "LastName", "Email", "Phone", "PasswordHash", "Role", "ClinicId" },
                values: new object[,]
                {
                    { 1, "Admin",   "User",     "admin@clinic.com",     "0500000000", Hash123456, 1, 1 },
                    { 2, "Sarah",   "Johnson",  "sarah@clinic.com",     "0501111111", Hash123456, 2, 1 },
                    { 3, "Michael", "Chen",     "michael@clinic.com",   "0502222222", Hash123456, 2, 1 },
                    { 4, "Lisa",    "Brown",    "assistant@clinic.com", "0503333333", Hash123456, 3, 1 },
                });

            // ── Patients ──────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Patients",
                columns: new[] { "Id", "FirstName", "LastName", "Phone", "Email", "DateOfBirth", "Gender", "Address", "MedicalNotes", "Allergies", "ChronicDiseases", "IdNumber", "ClinicId" },
                values: new object[,]
                {
                    { 1, "Ahmed",  "Al-Rashid", "0511111111", "ahmed@mail.com",  new DateTime(1985,  3, 15, 0, 0, 0, DateTimeKind.Utc), "Male",   "Tel Aviv",   null,              "Penicillin", "Hypertension", null, 1 },
                    { 2, "Maria",  "Garcia",    "0512222222", "maria@mail.com",  new DateTime(1990,  7, 22, 0, 0, 0, DateTimeKind.Utc), "Female", "Haifa",      "Anxious patient", null,         null,           null, 1 },
                    { 3, "John",   "Smith",     "0513333333", "john@mail.com",   new DateTime(1978, 11,  8, 0, 0, 0, DateTimeKind.Utc), "Male",   "Jerusalem",  null,              null,         "Diabetes",     null, 1 },
                    { 4, "Fatima", "Al-Hassan", "0514444444", "fatima@mail.com", new DateTime(1995,  1, 30, 0, 0, 0, DateTimeKind.Utc), "Female", "Nazareth",   null,              null,         null,           null, 1 },
                    { 5, "David",  "Cohen",     "0515555555", "david@mail.com",  new DateTime(2003,  5, 12, 0, 0, 0, DateTimeKind.Utc), "Male",   "Beer Sheva", null,              null,         null,           null, 1 },
                    { 6, "Nour",   "Abdullah",  "0516666666", "nour@mail.com",   new DateTime(1968,  9,  5, 0, 0, 0, DateTimeKind.Utc), "Female", "Akko",       null,              null,         "Arthritis",    null, 1 },
                });

            // ── Doctor Schedules ──────────────────────────────────────────────
            // DayOfWeek: Sunday=0, Monday=1, Tuesday=2, Wednesday=3, Thursday=4, Friday=5
            migrationBuilder.InsertData(
                table: "DoctorSchedules",
                columns: new[] { "Id", "DoctorId", "ClinicId", "DayOfWeek", "StartTime", "EndTime", "IsWorkingDay" },
                values: new object[,]
                {
                    {  1, 2, 1, 0, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), true },  // Sarah – Sunday
                    {  2, 2, 1, 1, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), true },  // Sarah – Monday
                    {  3, 2, 1, 2, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), true },  // Sarah – Tuesday
                    {  4, 2, 1, 3, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), true },  // Sarah – Wednesday
                    {  5, 2, 1, 4, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0), true },  // Sarah – Thursday
                    {  6, 3, 1, 1, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true },  // Michael – Monday
                    {  7, 3, 1, 2, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true },  // Michael – Tuesday
                    {  8, 3, 1, 3, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true },  // Michael – Wednesday
                    {  9, 3, 1, 4, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true },  // Michael – Thursday
                    { 10, 3, 1, 5, new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), true },  // Michael – Friday
                });

            // ── Appointments ──────────────────────────────────────────────────
            // Status: Scheduled=1, Completed=2, Cancelled=3, NoShow=4
            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] { "Id", "PatientId", "DoctorId", "ClinicId", "StartTime", "EndTime", "Status", "ReasonForVisit", "DoctorNotes", "Notes", "CreatedAt", "CreatedByUserId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    {  1, 1, 2, 1, now.AddDays(-30).AddHours(9),  now.AddDays(-30).AddHours(10), 2, "Routine cleaning",      "Good oral hygiene",   null,                now.AddDays(-35), (object?)null, (object?)null, (object?)null },
                    {  2, 2, 2, 1, now.AddDays(-20).AddHours(11), now.AddDays(-20).AddHours(12), 2, "Tooth pain",            "Filling on tooth 36", null,                now.AddDays(-25), (object?)null, (object?)null, (object?)null },
                    {  3, 3, 3, 1, now.AddDays(-10).AddHours(10), now.AddDays(-10).AddHours(11), 2, "Crown consultation",    "Crown fitted well",   null,                now.AddDays(-15), (object?)null, (object?)null, (object?)null },
                    {  4, 4, 3, 1, now.AddDays(-5).AddHours(14),  now.AddDays(-5).AddHours(15),  3, "Whitening",             null,                  "Patient cancelled", now.AddDays(-10), (object?)null, (object?)null, (object?)null },
                    {  5, 5, 2, 1, now.AddDays(-2).AddHours(9),   now.AddDays(-2).AddHours(10),  4, "Orthodontic review",    null,                  null,                now.AddDays(-7),  (object?)null, (object?)null, (object?)null },
                    {  6, 1, 2, 1, now.AddDays(1).AddHours(10),   now.AddDays(1).AddHours(11),   1, "Follow-up cleaning",    null,                  null,                now,              (object?)null, (object?)null, (object?)null },
                    {  7, 2, 3, 1, now.AddDays(3).AddHours(13),   now.AddDays(3).AddHours(14),   1, "Root canal treatment",  null,                  null,                now,              (object?)null, (object?)null, (object?)null },
                    {  8, 6, 2, 1, now.AddDays(7).AddHours(11),   now.AddDays(7).AddHours(12),   1, "Denture fitting",       null,                  null,                now,              (object?)null, (object?)null, (object?)null },
                    {  9, 3, 3, 1, now.AddDays(10).AddHours(9),   now.AddDays(10).AddHours(10),  1, "Implant consultation",  null,                  null,                now,              (object?)null, (object?)null, (object?)null },
                    { 10, 4, 2, 1, now.AddDays(14).AddHours(15),  now.AddDays(14).AddHours(16),  1, "Teeth whitening",       null,                  null,                now,              (object?)null, (object?)null, (object?)null },
                });

            // ── Treatment Plans ───────────────────────────────────────────────
            // PlanStatus: Draft=1, Active=2, Completed=3, Cancelled=4
            migrationBuilder.InsertData(
                table: "TreatmentPlans",
                columns: new[] { "Id", "Title", "Description", "TotalEstimatedCost", "Status", "CreatedAt", "StartDate", "CompletedAt", "PatientId", "DoctorId", "ClinicId" },
                values: new object[,]
                {
                    { 1, "Comprehensive Care - Ahmed Al-Rashid", "Full dental restoration plan",   3500m, 2, now.AddDays(-60), now.AddDays(-30), (object?)null,     1, 2, 1 },
                    { 2, "Orthodontic Plan - David Cohen",       "Braces and alignment treatment", 8000m, 2, now.AddDays(-45), now.AddDays(-20), (object?)null,     5, 2, 1 },
                    { 3, "Root Canal & Crown - Maria Garcia",    "Root canal followed by crown",   2200m, 3, now.AddDays(-90), now.AddDays(-80), now.AddDays(-10), 2, 3, 1 },
                });

            // ── Treatments ────────────────────────────────────────────────────
            // TreatmentType: Cleaning=1, Filling=2, RootCanal=3, Crown=5, Implant=7, Whitening=8, Orthodontics=9, Scaling=11
            // TreatmentStatus: Planned=1, InProgress=2, Completed=3
            migrationBuilder.InsertData(
                table: "Treatments",
                columns: new[] { "Id", "Title", "Description", "Cost", "EstimatedCost", "TreatmentDate", "CompletedAt", "ToothNumber", "Type", "Status", "Priority", "BeforeImageUrl", "AfterImageUrl", "PatientId", "DoctorId", "ClinicId", "TreatmentPlanId" },
                values: new object[,]
                {
                    { 1, "Full Mouth Cleaning",       null, 300m,  300m,  now.AddDays(-30), now.AddDays(-30), (object?)null, 1,  3, 2, null, null, 1, 2, 1, 1           },
                    { 2, "Composite Filling #36",     null, 450m,  400m,  now.AddDays(-20), now.AddDays(-20), 36,            2,  3, 1, null, null, 2, 2, 1, 3           },
                    { 3, "Root Canal #36",            null, 1200m, 1200m, now.AddDays(-15), now.AddDays(-15), 36,            3,  3, 1, null, null, 2, 3, 1, 3           },
                    { 4, "Porcelain Crown #36",       null, 1800m, 1800m, now.AddDays(-10), now.AddDays(-10), 36,            5,  3, 1, null, null, 2, 3, 1, 3           },
                    { 5, "Orthodontic Braces",        null, 6000m, 8000m, now.AddDays(-20), (object?)null,    (object?)null, 9,  2, 2, null, null, 5, 2, 1, 2           },
                    { 6, "Scaling & Polishing",       null, 250m,  250m,  now.AddDays(7),   (object?)null,    (object?)null, 11, 1, 3, null, null, 6, 2, 1, (object?)null },
                    { 7, "Implant #46 Consultation",  null, 5000m, 5000m, now.AddDays(10),  (object?)null,    46,            7,  1, 1, null, null, 3, 3, 1, (object?)null },
                    { 8, "Teeth Whitening",           null, 800m,  800m,  now.AddDays(14),  (object?)null,    (object?)null, 8,  1, 3, null, null, 4, 2, 1, 1           },
                });

            // ── Payments ──────────────────────────────────────────────────────
            // PaymentMethod: Cash=1, CreditCard=2, BankTransfer=3, Insurance=4
            // PaymentStatus: Paid=1, Pending=2, Refunded=3
            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "InvoiceNumber", "Amount", "Method", "Status", "PaymentDate", "Notes", "PatientId", "DoctorId", "TreatmentId", "ClinicId" },
                values: new object[,]
                {
                    { 1, 1001, 300m,  1, 1, now.AddDays(-30), "Paid in full",                          1, 2, 1, 1 },
                    { 2, 1002, 450m,  2, 1, now.AddDays(-20), (object?)null,                           2, 2, 2, 1 },
                    { 3, 1003, 1200m, 3, 1, now.AddDays(-15), (object?)null,                           2, 3, 3, 1 },
                    { 4, 1004, 1800m, 4, 1, now.AddDays(-10), "Insurance claim #INS-2024-445",         2, 3, 4, 1 },
                    { 5, 1005, 3000m, 2, 2, now.AddDays(-20), "50% deposit for orthodontic treatment", 5, 2, 5, 1 },
                });

            // ── Materials ─────────────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "Materials",
                columns: new[] { "Id", "Name", "Description", "Quantity", "MinimumLimit", "Supplier", "Unit", "ClinicId" },
                values: new object[,]
                {
                    { 1, "Latex Gloves",         "Disposable examination gloves",  500, 100, "MedSupply Co.", "pcs",     1 },
                    { 2, "Surgical Masks",        "3-ply disposable face masks",    200, 50,  "MedSupply Co.", "pcs",     1 },
                    { 3, "Composite Resin",       "Tooth-colored filling material", 15,  5,   "DentaLab Inc.", "syringe", 1 },
                    { 4, "Anesthetic Cartridges", "Lidocaine 2% with epinephrine",  8,   10,  "PharmaCore",    "box",     1 },
                    { 5, "Dental X-Ray Film",     "Size 2 periapical film",         100, 20,  "RadioDent",     "pcs",     1 },
                    { 6, "Suture Thread 3-0",     "Absorbable sutures",             4,   5,   "SurgicalPlus",  "pack",    1 },
                });

            // ── Material Histories ────────────────────────────────────────────
            migrationBuilder.InsertData(
                table: "MaterialHistories",
                columns: new[] { "Id", "MaterialId", "QuantityChange", "NewQuantity", "Supplier", "CreatedAt", "Note" },
                values: new object[,]
                {
                    { 1, 1, 500, 500, "MedSupply Co.", now.AddDays(-90), "Initial stock" },
                    { 2, 2, 200, 200, "MedSupply Co.", now.AddDays(-90), "Initial stock" },
                    { 3, 3, 20,  20,  "DentaLab Inc.", now.AddDays(-60), "Initial stock" },
                    { 4, 3, -5,  15,  (object?)null,   now.AddDays(-10), "Used for fillings" },
                    { 5, 4, 10,  10,  "PharmaCore",    now.AddDays(-45), "Initial stock" },
                    { 6, 4, -2,  8,   (object?)null,   now.AddDays(-5),  "Used in procedures" },
                    { 7, 6, 6,   6,   "SurgicalPlus",  now.AddDays(-30), "Initial stock" },
                    { 8, 6, -2,  4,   (object?)null,   now.AddDays(-3),  "Used in extractions" },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData("MaterialHistories", "Id", new object[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            migrationBuilder.DeleteData("Payments",          "Id", new object[] { 1, 2, 3, 4, 5 });
            migrationBuilder.DeleteData("Treatments",        "Id", new object[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            migrationBuilder.DeleteData("TreatmentPlans",    "Id", new object[] { 1, 2, 3 });
            migrationBuilder.DeleteData("Appointments",      "Id", new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            migrationBuilder.DeleteData("DoctorSchedules",   "Id", new object[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            migrationBuilder.DeleteData("Patients",          "Id", new object[] { 1, 2, 3, 4, 5, 6 });
            migrationBuilder.DeleteData("AppUsers",          "Id", new object[] { 1, 2, 3, 4 });
            migrationBuilder.DeleteData("Clinics",           "Id", (object)1);
        }
    }
}
