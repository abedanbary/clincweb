using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ClinicApp.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaterialInvoiceId",
                table: "MaterialHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvoiceNumber = table.Column<string>(type: "text", nullable: false),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    ClinicId = table.Column<int>(type: "integer", nullable: false),
                    Supplier = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialInvoices_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaterialInvoices_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaterialHistories_MaterialInvoiceId",
                table: "MaterialHistories",
                column: "MaterialInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialInvoices_ClinicId",
                table: "MaterialInvoices",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialInvoices_MaterialId",
                table: "MaterialInvoices",
                column: "MaterialId");

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialHistories_MaterialInvoices_MaterialInvoiceId",
                table: "MaterialHistories",
                column: "MaterialInvoiceId",
                principalTable: "MaterialInvoices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MaterialHistories_MaterialInvoices_MaterialInvoiceId",
                table: "MaterialHistories");

            migrationBuilder.DropTable(
                name: "MaterialInvoices");

            migrationBuilder.DropIndex(
                name: "IX_MaterialHistories_MaterialInvoiceId",
                table: "MaterialHistories");

            migrationBuilder.DropColumn(
                name: "MaterialInvoiceId",
                table: "MaterialHistories");
        }
    }
}
