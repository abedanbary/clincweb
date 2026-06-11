using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpgradePatientFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "PatientFiles",
                newName: "Notes");

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "PatientFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "PatientFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Extension",
                table: "PatientFiles",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "PatientFiles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "PatientFiles");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "PatientFiles");

            migrationBuilder.DropColumn(
                name: "Extension",
                table: "PatientFiles");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "PatientFiles");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "PatientFiles",
                newName: "Description");
        }
    }
}
