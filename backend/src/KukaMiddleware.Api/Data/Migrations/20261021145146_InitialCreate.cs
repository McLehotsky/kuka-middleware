using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace KukaMiddleware.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "api_client",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    key_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_api_client", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "robot",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kuka_robot_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    battery_level = table.Column<int>(type: "integer", nullable: true),
                    current_node = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_robot", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mission",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    mission_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    external_ref = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    mission_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    robot_id = table.Column<int>(type: "integer", nullable: true),
                    created_by_client_id = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mission", x => x.id);
                    table.ForeignKey(
                        name: "fk_mission_api_client_created_by_client_id",
                        column: x => x.created_by_client_id,
                        principalTable: "api_client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_mission_robot_robot_id",
                        column: x => x.robot_id,
                        principalTable: "robot",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    api_client_id = table.Column<int>(type: "integer", nullable: false),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    http_status = table.Column<int>(type: "integer", nullable: true),
                    details = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_log_api_client_api_client_id",
                        column: x => x.api_client_id,
                        principalTable: "api_client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_log_mission_mission_id",
                        column: x => x.mission_id,
                        principalTable: "mission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "mission_status_history",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mission_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_mission_status_history_mission_mission_id",
                        column: x => x.mission_id,
                        principalTable: "mission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "mission_step",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    node_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mission_step", x => x.id);
                    table.ForeignKey(
                        name: "fk_mission_step_mission_mission_id",
                        column: x => x.mission_id,
                        principalTable: "mission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_api_client_key_hash",
                table: "api_client",
                column: "key_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_api_client_id",
                table: "audit_log",
                column: "api_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_mission_id",
                table: "audit_log",
                column: "mission_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_timestamp",
                table: "audit_log",
                column: "timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_mission_created_at",
                table: "mission",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_mission_created_by_client_id",
                table: "mission",
                column: "created_by_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_mission_mission_code",
                table: "mission",
                column: "mission_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mission_robot_id",
                table: "mission",
                column: "robot_id");

            migrationBuilder.CreateIndex(
                name: "ix_mission_status",
                table: "mission",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_mission_status_history_mission_id_changed_at",
                table: "mission_status_history",
                columns: new[] { "mission_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_mission_step_mission_id_sequence",
                table: "mission_step",
                columns: new[] { "mission_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_robot_kuka_robot_id",
                table: "robot",
                column: "kuka_robot_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "mission_status_history");

            migrationBuilder.DropTable(
                name: "mission_step");

            migrationBuilder.DropTable(
                name: "mission");

            migrationBuilder.DropTable(
                name: "api_client");

            migrationBuilder.DropTable(
                name: "robot");
        }
    }
}
