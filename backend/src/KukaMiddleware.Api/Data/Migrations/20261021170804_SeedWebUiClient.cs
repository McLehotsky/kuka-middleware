using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KukaMiddleware.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedWebUiClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "mission_code_seq");

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                table: "mission",
                type: "integer",
                nullable: false,
                defaultValue: 2,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.InsertData(
                table: "api_client",
                columns: new[] { "id", "created_at", "is_active", "key_hash", "name", "role" },
                values: new object[] { 1, new DateTimeOffset(new DateTime(2026, 10, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "!disabled-seed-web-ui", "web-ui", "Operator" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "api_client",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DropSequence(
                name: "mission_code_seq");

            migrationBuilder.AlterColumn<int>(
                name: "priority",
                table: "mission",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 2);
        }
    }
}
