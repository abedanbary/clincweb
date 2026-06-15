using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class SeedCalendarAppointments : Migration
    {
        // ── Doctor work schedules ────────────────────────────────────────────
        // Sarah  (Id=2): Sunday–Thursday  08:00–17:00
        // Michael(Id=3): Monday–Friday    09:00–18:00
        //
        // Patients: Ahmed=1, Maria=2, John=3, Fatima=4, David=5, Nour=6
        // Status:   Scheduled=1, Completed=2, Cancelled=3, NoShow=4

        private static DateTime A(int y, int m, int d, int h) =>
            new DateTime(y, m, d, h, 0, 0, DateTimeKind.Utc);

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Appointments",
                columns: new[] {
                    "Id","PatientId","DoctorId","ClinicId",
                    "StartTime","EndTime","Status",
                    "ReasonForVisit","DoctorNotes","Notes",
                    "CreatedAt","CreatedByUserId","UpdatedAt","UpdatedByUserId"
                },
                values: new object[,]
                {
                    // ═══════════════════════════ MAY 2026 ════════════════════════════

                    // May 1 (Fri) — Michael only (Mon–Fri 09–18)
                    {  53, 4, 3, 1, A(2026,5, 1, 9), A(2026,5, 1,10), 2, "Consultation",          "Discussed treatment options",  null, A(2026,4,30, 8), (object?)null,(object?)null,(object?)null },
                    {  54, 3, 3, 1, A(2026,5, 1,14), A(2026,5, 1,15), 2, "Follow-up check",       "Patient recovering well",      null, A(2026,4,30, 8), (object?)null,(object?)null,(object?)null },

                    // May 3 (Sun) — Sarah only (Sun–Thu 08–17)
                    {  55, 2, 2, 1, A(2026,5, 3, 9), A(2026,5, 3,10), 2, "Routine cleaning",      "Good oral hygiene",            null, A(2026,5, 2, 8), (object?)null,(object?)null,(object?)null },
                    {  56, 6, 2, 1, A(2026,5, 3,14), A(2026,5, 3,15), 2, "Scaling",               "Deep clean performed",         null, A(2026,5, 2, 8), (object?)null,(object?)null,(object?)null },

                    // May 4 (Mon) — Both (Michael has 10–11 already)
                    {  57, 4, 2, 1, A(2026,5, 4, 9), A(2026,5, 4,10), 2, "Whitening consultation","Shade assessment done",        null, A(2026,5, 3, 8), (object?)null,(object?)null,(object?)null },
                    {  58, 5, 2, 1, A(2026,5, 4,13), A(2026,5, 4,14), 2, "Orthodontic check",     "Wire tension adjusted",        null, A(2026,5, 3, 8), (object?)null,(object?)null,(object?)null },
                    {  59, 1, 3, 1, A(2026,5, 4,13), A(2026,5, 4,14), 2, "Implant follow-up",     "Osseointegration progressing", null, A(2026,5, 3, 8), (object?)null,(object?)null,(object?)null },

                    // May 6 (Wed) — Both
                    {  60, 2, 2, 1, A(2026,5, 6, 8), A(2026,5, 6, 9), 2, "Early cleaning",        "Plaque removed thoroughly",    null, A(2026,5, 5, 8), (object?)null,(object?)null,(object?)null },
                    {  61, 3, 2, 1, A(2026,5, 6,10), A(2026,5, 6,11), 2, "Crown check",           "Crown fit confirmed",          null, A(2026,5, 5, 8), (object?)null,(object?)null,(object?)null },
                    {  62, 6, 3, 1, A(2026,5, 6, 9), A(2026,5, 6,10), 2, "Extraction prep",       "Pre-op instructions given",    null, A(2026,5, 5, 8), (object?)null,(object?)null,(object?)null },
                    {  63, 5, 3, 1, A(2026,5, 6,14), A(2026,5, 6,15), 2, "Braces tightening",     "Patient tolerating well",      null, A(2026,5, 5, 8), (object?)null,(object?)null,(object?)null },

                    // May 7 (Thu) — Both
                    {  64, 1, 2, 1, A(2026,5, 7, 8), A(2026,5, 7, 9), 2, "Routine cleaning",      "Fluoride applied",             null, A(2026,5, 6, 8), (object?)null,(object?)null,(object?)null },
                    {  65, 4, 2, 1, A(2026,5, 7,11), A(2026,5, 7,12), 2, "Whitening follow-up",   "Result excellent",             null, A(2026,5, 6, 8), (object?)null,(object?)null,(object?)null },
                    {  66, 2, 3, 1, A(2026,5, 7,10), A(2026,5, 7,11), 2, "Scaling",               "Gum health improved",          null, A(2026,5, 6, 8), (object?)null,(object?)null,(object?)null },

                    // May 11 (Sun) — Sarah only
                    {  67, 3, 2, 1, A(2026,5,11, 9), A(2026,5,11,10), 2, "Consultation",          "X-ray reviewed",               null, A(2026,5,10, 8), (object?)null,(object?)null,(object?)null },
                    {  68, 5, 2, 1, A(2026,5,11,14), A(2026,5,11,15), 2, "Braces check",          "Alignment improving",          null, A(2026,5,10, 8), (object?)null,(object?)null,(object?)null },

                    // May 13 (Tue) — Both
                    {  69, 6, 2, 1, A(2026,5,13, 8), A(2026,5,13, 9), 2, "Post-extraction check", "Healing as expected",          null, A(2026,5,12, 8), (object?)null,(object?)null,(object?)null },
                    {  70, 1, 2, 1, A(2026,5,13,10), A(2026,5,13,11), 2, "Routine cleaning",      "Teeth in good condition",      null, A(2026,5,12, 8), (object?)null,(object?)null,(object?)null },
                    {  71, 4, 3, 1, A(2026,5,13, 9), A(2026,5,13,10), 2, "Composite filling",     "Filling placed, bite checked", null, A(2026,5,12, 8), (object?)null,(object?)null,(object?)null },
                    {  72, 2, 3, 1, A(2026,5,13,13), A(2026,5,13,14), 2, "Root canal review",     "No signs of re-infection",     null, A(2026,5,12, 8), (object?)null,(object?)null,(object?)null },

                    // May 16 (Fri) — Michael only
                    {  73, 3, 3, 1, A(2026,5,16,10), A(2026,5,16,11), 2, "Implant review",        "Tissue integration good",      null, A(2026,5,15, 8), (object?)null,(object?)null,(object?)null },
                    {  74, 5, 3, 1, A(2026,5,16,14), A(2026,5,16,15), 2, "Orthodontic check",     "Next phase started",           null, A(2026,5,15, 8), (object?)null,(object?)null,(object?)null },

                    // May 18 (Sun) — Sarah only
                    {  75, 6, 2, 1, A(2026,5,18, 9), A(2026,5,18,10), 2, "Follow-up",             "Gum healing well",             null, A(2026,5,17, 8), (object?)null,(object?)null,(object?)null },
                    {  76, 1, 2, 1, A(2026,5,18,11), A(2026,5,18,12), 2, "Fluoride treatment",    "Applied protective coating",   null, A(2026,5,17, 8), (object?)null,(object?)null,(object?)null },

                    // May 20 (Tue) — Both
                    {  77, 2, 2, 1, A(2026,5,20, 8), A(2026,5,20, 9), 2, "Check-up",              "No new caries found",          null, A(2026,5,19, 8), (object?)null,(object?)null,(object?)null },
                    {  78, 4, 2, 1, A(2026,5,20,11), A(2026,5,20,12), 2, "Whitening session",     "Two shades lighter",           null, A(2026,5,19, 8), (object?)null,(object?)null,(object?)null },
                    {  79, 3, 3, 1, A(2026,5,20, 9), A(2026,5,20,10), 2, "Crown preparation",     "Temporary crown fitted",       null, A(2026,5,19, 8), (object?)null,(object?)null,(object?)null },
                    {  80, 5, 3, 1, A(2026,5,20,14), A(2026,5,20,15), 2, "Braces adjustment",     "Wire changed",                 null, A(2026,5,19, 8), (object?)null,(object?)null,(object?)null },

                    // May 23 (Fri) — Michael only
                    {  81, 6, 3, 1, A(2026,5,23,10), A(2026,5,23,11), 2, "Denture review",        "Good fit confirmed",           null, A(2026,5,22, 8), (object?)null,(object?)null,(object?)null },
                    {  82, 1, 3, 1, A(2026,5,23,13), A(2026,5,23,14), 2, "X-ray & check",         "No issues detected",           null, A(2026,5,22, 8), (object?)null,(object?)null,(object?)null },

                    // May 25 (Sun) — Sarah only
                    {  83, 2, 2, 1, A(2026,5,25, 9), A(2026,5,25,10), 2, "Post root canal",       "Sensitivity resolved",         null, A(2026,5,24, 8), (object?)null,(object?)null,(object?)null },
                    {  84, 4, 2, 1, A(2026,5,25,14), A(2026,5,25,15), 2, "Cosmetic consultation", "Treatment plan discussed",     null, A(2026,5,24, 8), (object?)null,(object?)null,(object?)null },

                    // May 27 (Tue) — Both
                    {  85, 3, 2, 1, A(2026,5,27, 8), A(2026,5,27, 9), 2, "Implant post-op",       "Sutures removed",              null, A(2026,5,26, 8), (object?)null,(object?)null,(object?)null },
                    {  86, 6, 2, 1, A(2026,5,27,11), A(2026,5,27,12), 2, "Denture follow-up",     "Minor adjustment made",        null, A(2026,5,26, 8), (object?)null,(object?)null,(object?)null },
                    {  87, 5, 3, 1, A(2026,5,27, 9), A(2026,5,27,10), 2, "Orthodontic wire",      "Arch wire replaced",           null, A(2026,5,26, 8), (object?)null,(object?)null,(object?)null },
                    {  88, 1, 3, 1, A(2026,5,27,14), A(2026,5,27,15), 2, "Routine cleaning",      "Good hygiene maintained",      null, A(2026,5,26, 8), (object?)null,(object?)null,(object?)null },

                    // May 29 (Thu) — Both
                    {  89, 2, 2, 1, A(2026,5,29, 8), A(2026,5,29, 9), 2, "Scaling",               "Full mouth scale complete",     null, A(2026,5,28, 8), (object?)null,(object?)null,(object?)null },
                    {  90, 4, 2, 1, A(2026,5,29,10), A(2026,5,29,11), 2, "Whitening final",       "Treatment completed",          null, A(2026,5,28, 8), (object?)null,(object?)null,(object?)null },
                    {  91, 3, 3, 1, A(2026,5,29, 9), A(2026,5,29,10), 2, "Implant check",         "Healing on track",             null, A(2026,5,28, 8), (object?)null,(object?)null,(object?)null },
                    {  92, 5, 3, 1, A(2026,5,29,15), A(2026,5,29,16), 2, "Braces tightening",     "Patient managing discomfort",  null, A(2026,5,28, 8), (object?)null,(object?)null,(object?)null },

                    // ══════════════════════════ JUNE 2026 ════════════════════════════

                    // Jun 1 (Mon) — Both
                    {  93, 6, 2, 1, A(2026,6, 1, 9), A(2026,6, 1,10), 2, "Cleaning",              "Excellent hygiene",            null, A(2026,5,31, 8), (object?)null,(object?)null,(object?)null },
                    {  94, 1, 2, 1, A(2026,6, 1,11), A(2026,6, 1,12), 2, "Check-up",              "No new issues",                null, A(2026,5,31, 8), (object?)null,(object?)null,(object?)null },
                    {  95, 2, 3, 1, A(2026,6, 1,10), A(2026,6, 1,11), 2, "Filling",               "Composite filling placed",     null, A(2026,5,31, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 3 (Wed) — Both
                    {  96, 4, 2, 1, A(2026,6, 3, 8), A(2026,6, 3, 9), 2, "Consultation",          "New plan discussed",           null, A(2026,6, 2, 8), (object?)null,(object?)null,(object?)null },
                    {  97, 3, 2, 1, A(2026,6, 3,14), A(2026,6, 3,15), 2, "Crown fitting",         "Permanent crown cemented",     null, A(2026,6, 2, 8), (object?)null,(object?)null,(object?)null },
                    {  98, 5, 3, 1, A(2026,6, 3, 9), A(2026,6, 3,10), 2, "Braces check",          "Good progress",                null, A(2026,6, 2, 8), (object?)null,(object?)null,(object?)null },
                    {  99, 6, 3, 1, A(2026,6, 3,13), A(2026,6, 3,14), 2, "Denture fitting",       "Final denture delivered",      null, A(2026,6, 2, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 5 (Fri) — Michael only
                    { 100, 1, 3, 1, A(2026,6, 5,10), A(2026,6, 5,11), 2, "Preventive care",       "Sealants applied",             null, A(2026,6, 4, 8), (object?)null,(object?)null,(object?)null },
                    { 101, 2, 3, 1, A(2026,6, 5,14), A(2026,6, 5,15), 2, "Follow-up",             "Gum condition stable",         null, A(2026,6, 4, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 7 (Sun) — Sarah only
                    { 102, 4, 2, 1, A(2026,6, 7, 9), A(2026,6, 7,10), 2, "Cleaning",              "Polishing done",               null, A(2026,6, 6, 8), (object?)null,(object?)null,(object?)null },
                    { 103, 3, 2, 1, A(2026,6, 7,14), A(2026,6, 7,15), 2, "Post-implant check",    "No complications",             null, A(2026,6, 6, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 8 (Mon) — Both
                    { 104, 5, 2, 1, A(2026,6, 8, 8), A(2026,6, 8, 9), 2, "Braces",                "Wire shortened",               null, A(2026,6, 7, 8), (object?)null,(object?)null,(object?)null },
                    { 105, 6, 2, 1, A(2026,6, 8,11), A(2026,6, 8,12), 2, "Denture check",         "Fit comfortable",              null, A(2026,6, 7, 8), (object?)null,(object?)null,(object?)null },
                    { 106, 1, 3, 1, A(2026,6, 8, 9), A(2026,6, 8,10), 2, "Routine cleaning",      "Good condition",               null, A(2026,6, 7, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 10 (Wed) — Both
                    { 107, 2, 2, 1, A(2026,6,10, 9), A(2026,6,10,10), 2, "Consultation",          "Scaling recommended",          null, A(2026,6, 9, 8), (object?)null,(object?)null,(object?)null },
                    { 108, 4, 2, 1, A(2026,6,10,11), A(2026,6,10,12), 2, "Whitening review",      "Shade held well",              null, A(2026,6, 9, 8), (object?)null,(object?)null,(object?)null },
                    { 109, 3, 3, 1, A(2026,6,10,10), A(2026,6,10,11), 2, "Implant follow-up",     "Bone integration confirmed",   null, A(2026,6, 9, 8), (object?)null,(object?)null,(object?)null },
                    { 110, 5, 3, 1, A(2026,6,10,14), A(2026,6,10,15), 2, "Braces tightening",     "Phase 2 started",              null, A(2026,6, 9, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 15 (Mon — today) — Both
                    { 111, 6, 2, 1, A(2026,6,15, 9), A(2026,6,15,10), 1, "Consultation",          null, null, A(2026,6,14, 8), (object?)null,(object?)null,(object?)null },
                    { 112, 1, 2, 1, A(2026,6,15,14), A(2026,6,15,15), 1, "Cleaning",              null, null, A(2026,6,14, 8), (object?)null,(object?)null,(object?)null },
                    { 113, 2, 3, 1, A(2026,6,15,10), A(2026,6,15,11), 1, "Filling follow-up",     null, null, A(2026,6,14, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 17 (Wed) — Both
                    { 114, 4, 2, 1, A(2026,6,17, 9), A(2026,6,17,10), 1, "Whitening",             null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 115, 3, 2, 1, A(2026,6,17,11), A(2026,6,17,12), 1, "Crown review",          null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 116, 5, 3, 1, A(2026,6,17,10), A(2026,6,17,11), 1, "Braces check",          null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 21 (Sun) — Sarah only
                    { 117, 6, 2, 1, A(2026,6,21, 9), A(2026,6,21,10), 1, "Denture review",        null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 118, 1, 2, 1, A(2026,6,21,14), A(2026,6,21,15), 1, "Preventive care",       null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 22 (Mon) — Both
                    { 119, 2, 2, 1, A(2026,6,22, 9), A(2026,6,22,10), 1, "Check-up",              null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 120, 4, 2, 1, A(2026,6,22,11), A(2026,6,22,12), 1, "Consultation",          null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 121, 3, 3, 1, A(2026,6,22,10), A(2026,6,22,11), 1, "Implant osseo check",   null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 24 (Wed) — Both
                    { 122, 5, 2, 1, A(2026,6,24, 8), A(2026,6,24, 9), 1, "Braces",                null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 123, 6, 2, 1, A(2026,6,24,10), A(2026,6,24,11), 1, "Denture adjustment",    null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 124, 1, 3, 1, A(2026,6,24, 9), A(2026,6,24,10), 1, "Routine cleaning",      null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 125, 2, 3, 1, A(2026,6,24,14), A(2026,6,24,15), 1, "Scaling",               null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 25 (Thu) — Both
                    { 126, 4, 2, 1, A(2026,6,25, 9), A(2026,6,25,10), 1, "Whitening maintenance", null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 127, 3, 2, 1, A(2026,6,25,11), A(2026,6,25,12), 1, "Follow-up",             null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 128, 5, 3, 1, A(2026,6,25,10), A(2026,6,25,11), 1, "Orthodontic review",    null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 26 (Fri) — Michael only
                    { 129, 6, 3, 1, A(2026,6,26, 9), A(2026,6,26,10), 1, "Denture final check",   null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 130, 1, 3, 1, A(2026,6,26,13), A(2026,6,26,14), 1, "Consultation",          null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 28 (Sun) — Sarah only
                    { 131, 2, 2, 1, A(2026,6,28,10), A(2026,6,28,11), 1, "Cleaning",              null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 132, 4, 2, 1, A(2026,6,28,14), A(2026,6,28,15), 1, "Cosmetic review",       null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 29 (Mon) — Both
                    { 133, 3, 2, 1, A(2026,6,29, 8), A(2026,6,29, 9), 1, "Implant review",        null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 134, 5, 2, 1, A(2026,6,29,11), A(2026,6,29,12), 1, "Braces",                null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 135, 6, 3, 1, A(2026,6,29,10), A(2026,6,29,11), 1, "Denture check",         null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jun 30 (Tue) — Both
                    { 136, 1, 2, 1, A(2026,6,30, 9), A(2026,6,30,10), 1, "Preventive care",       null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 137, 2, 3, 1, A(2026,6,30,11), A(2026,6,30,12), 1, "Root canal review",     null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 138, 4, 3, 1, A(2026,6,30,14), A(2026,6,30,15), 1, "Consultation",          null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // ══════════════════════════ JULY 2026 ════════════════════════════
                    // Jul 1 (Wed) — Both
                    { 139, 3, 2, 1, A(2026,7, 1, 9), A(2026,7, 1,10), 1, "Routine check",         null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 140, 5, 3, 1, A(2026,7, 1,10), A(2026,7, 1,11), 1, "Braces adjustment",     null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jul 2 (Thu) — Both
                    { 141, 6, 2, 1, A(2026,7, 2, 9), A(2026,7, 2,10), 1, "Denture check",         null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 142, 1, 3, 1, A(2026,7, 2,11), A(2026,7, 2,12), 1, "Cleaning",              null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jul 5 (Sun) — Sarah only
                    { 143, 2, 2, 1, A(2026,7, 5,10), A(2026,7, 5,11), 1, "Check-up",              null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jul 6 (Mon) — Both
                    { 144, 4, 2, 1, A(2026,7, 6, 9), A(2026,7, 6,10), 1, "Consultation",          null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 145, 3, 3, 1, A(2026,7, 6,10), A(2026,7, 6,11), 1, "Implant osseo",         null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jul 7 (Tue) — Both
                    { 146, 5, 2, 1, A(2026,7, 7, 9), A(2026,7, 7,10), 1, "Braces check",          null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 147, 6, 3, 1, A(2026,7, 7,11), A(2026,7, 7,12), 1, "Denture follow-up",     null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jul 8 (Wed) — Both
                    { 148, 1, 2, 1, A(2026,7, 8, 8), A(2026,7, 8, 9), 1, "Cleaning",              null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 149, 2, 3, 1, A(2026,7, 8,10), A(2026,7, 8,11), 1, "Scaling",               null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },

                    // Jul 9 (Thu) — Both
                    { 150, 4, 2, 1, A(2026,7, 9, 9), A(2026,7, 9,10), 1, "Whitening touch-up",    null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                    { 151, 3, 3, 1, A(2026,7, 9,11), A(2026,7, 9,12), 1, "Crown review",          null, null, A(2026,6,15, 8), (object?)null,(object?)null,(object?)null },
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                "Appointments",
                "Id",
                new object[]
                {
                    53,54,55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,
                    71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,
                    89,90,91,92,93,94,95,96,97,98,99,100,101,102,103,104,
                    105,106,107,108,109,110,111,112,113,114,115,116,117,118,
                    119,120,121,122,123,124,125,126,127,128,129,130,131,132,
                    133,134,135,136,137,138,139,140,141,142,143,144,145,146,
                    147,148,149,150,151
                });
        }
    }
}
