using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class MakePaymentTreatmentIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Treatments_TreatmentId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "TreatmentId",
                table: "Payments",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Treatments_TreatmentId",
                table: "Payments",
                column: "TreatmentId",
                principalTable: "Treatments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Treatments_TreatmentId",
                table: "Payments");

            migrationBuilder.AlterColumn<int>(
                name: "TreatmentId",
                table: "Payments",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Treatments_TreatmentId",
                table: "Payments",
                column: "TreatmentId",
                principalTable: "Treatments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
