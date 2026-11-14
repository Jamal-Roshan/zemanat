using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zemanat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPrintFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FatherName",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GenderPrefix",
                table: "Documents",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LoanOwnerFatherName",
                table: "Documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoanOwnerName",
                table: "Documents",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PrintTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Html = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrintTemplates_Kind",
                table: "PrintTemplates",
                column: "Kind",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrintTemplates");

            migrationBuilder.DropColumn(
                name: "FatherName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "GenderPrefix",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "LoanOwnerFatherName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "LoanOwnerName",
                table: "Documents");
        }
    }
}
