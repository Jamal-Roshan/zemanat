using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zemanat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AmountRial = table.Column<long>(type: "bigint", nullable: false),
                    Beneficiary = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSurplus = table.Column<bool>(type: "bit", nullable: false),
                    ExcessAmountRial = table.Column<long>(type: "bigint", nullable: false),
                    SurplusCommitmentAccepted = table.Column<bool>(type: "bit", nullable: false),
                    PersonnelCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PersonName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NationalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OrgUnit = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    NetPayRial = table.Column<long>(type: "bigint", nullable: false),
                    CapRial = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: true),
                    SequenceYear = table.Column<int>(type: "int", nullable: true),
                    NumberText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrackingCode = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    IssuedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecidedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_AspNetUsers_PersonId",
                        column: x => x.PersonId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecretariatCounters",
                columns: table => new
                {
                    Year = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastSequence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretariatCounters", x => x.Year);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PersonId_Status",
                table: "Documents",
                columns: new[] { "PersonId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_SequenceYear_Sequence",
                table: "Documents",
                columns: new[] { "SequenceYear", "Sequence" },
                unique: true,
                filter: "[SequenceYear] IS NOT NULL AND [Sequence] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Status",
                table: "Documents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TrackingCode",
                table: "Documents",
                column: "TrackingCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "SecretariatCounters");
        }
    }
}
