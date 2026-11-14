using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zemanat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInquiryAndClose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClosedByUserId",
                table: "Documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClosedNote",
                table: "Documents",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedUtc",
                table: "Documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BankInquiries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    BankLetterNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BankLetterDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BorrowerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BorrowerFatherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    SequenceYear = table.Column<int>(type: "int", nullable: false),
                    NumberText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IssuedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankInquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankInquiries_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankInquiries_DocumentId",
                table: "BankInquiries",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BankInquiries_SequenceYear_Sequence",
                table: "BankInquiries",
                columns: new[] { "SequenceYear", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankInquiries");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ClosedNote",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ClosedUtc",
                table: "Documents");
        }
    }
}
