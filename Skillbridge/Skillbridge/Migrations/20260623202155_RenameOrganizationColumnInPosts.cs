using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Skillbridge.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrganizationColumnInPosts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Organization",
                table: "Posts",
                newName: "OrganizationId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OrganizationId",
                table: "Posts",
                newName: "Organization");
        }
    }
}
