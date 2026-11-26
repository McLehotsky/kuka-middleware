using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KukaMiddleware.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMissionRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "mission",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "mission");
        }
    }
}
