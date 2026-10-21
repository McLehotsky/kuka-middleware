using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KukaMiddleware.Api.Data.Migrations
{
    /// <summary>
    /// The seeded web-ui client (SeedWebUiClient) is inserted with an explicit id, which does not
    /// advance the identity sequence. Move it past the highest id so the next client gets id 2.
    /// </summary>
    public partial class AdvanceApiClientIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "SELECT setval(pg_get_serial_sequence('api_client', 'id'), (SELECT MAX(id) FROM api_client));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: rewinding an identity sequence could hand out ids that are already in use.
        }
    }
}
