using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatApp.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentRoomToUserStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentRoom",
                table: "UserStatuses",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentRoom",
                table: "UserStatuses");
        }
    }
}
