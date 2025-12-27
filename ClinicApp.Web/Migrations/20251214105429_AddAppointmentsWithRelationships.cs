using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentsWithRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AppUsers_DoctorId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Patients_PatientId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoctorNotes",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonForVisit",
                table: "Appointments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Appointments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Appointments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_CreatedByUserId",
                table: "Appointments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_StartTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_StartTime",
                table: "Appointments",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status",
                table: "Appointments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_UpdatedByUserId",
                table: "Appointments",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AppUsers_CreatedByUserId",
                table: "Appointments",
                column: "CreatedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AppUsers_DoctorId",
                table: "Appointments",
                column: "DoctorId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AppUsers_UpdatedByUserId",
                table: "Appointments",
                column: "UpdatedByUserId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Patients_PatientId",
                table: "Appointments",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AppUsers_CreatedByUserId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AppUsers_DoctorId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AppUsers_UpdatedByUserId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Patients_PatientId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_CreatedByUserId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_StartTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_Status",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_UpdatedByUserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "DoctorNotes",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ReasonForVisit",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AppUsers_DoctorId",
                table: "Appointments",
                column: "DoctorId",
                principalTable: "AppUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Patients_PatientId",
                table: "Appointments",
                column: "PatientId",
                principalTable: "Patients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
