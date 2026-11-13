using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zemanat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOrgUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrgUnit",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrgUnit",
                table: "AspNetUsers");
        }
    }
}
