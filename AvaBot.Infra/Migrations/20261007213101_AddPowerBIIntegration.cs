using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AvaBot.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddPowerBIIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "powerbi_enabled",
                table: "avabot_agents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "avabot_agent_powerbi_configs",
                columns: table => new
                {
                    agent_powerbi_config_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    client_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    client_secret_encrypted = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    client_secret_hint = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    last_test_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    last_test_success = table.Column<bool>(type: "boolean", nullable: true),
                    last_test_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avabot_agent_powerbi_configs_pkey", x => x.agent_powerbi_config_id);
                    table.ForeignKey(
                        name: "avabot_fk_agents_agent_powerbi_configs",
                        column: x => x.agent_id,
                        principalTable: "avabot_agents",
                        principalColumn: "agent_id");
                });

            migrationBuilder.CreateTable(
                name: "avabot_powerbi_datasets",
                columns: table => new
                {
                    powerbi_dataset_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    workspace_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    dataset_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    tool_key = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    schema_json = table.Column<string>(type: "jsonb", nullable: true),
                    schema_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    schema_generated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    schema_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avabot_powerbi_datasets_pkey", x => x.powerbi_dataset_id);
                    table.ForeignKey(
                        name: "avabot_fk_agents_powerbi_datasets",
                        column: x => x.agent_id,
                        principalTable: "avabot_agents",
                        principalColumn: "agent_id");
                });

            migrationBuilder.CreateTable(
                name: "avabot_powerbi_query_logs",
                columns: table => new
                {
                    powerbi_query_log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    chat_session_id = table.Column<long>(type: "bigint", nullable: true),
                    powerbi_dataset_id = table.Column<long>(type: "bigint", nullable: true),
                    dataset_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    tool_name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    user_question = table.Column<string>(type: "text", nullable: true),
                    query = table.Column<string>(type: "text", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    row_count = table.Column<int>(type: "integer", nullable: true),
                    truncated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avabot_powerbi_query_logs_pkey", x => x.powerbi_query_log_id);
                    table.ForeignKey(
                        name: "avabot_fk_agents_powerbi_query_logs",
                        column: x => x.agent_id,
                        principalTable: "avabot_agents",
                        principalColumn: "agent_id");
                    table.ForeignKey(
                        name: "avabot_fk_chat_sessions_powerbi_query_logs",
                        column: x => x.chat_session_id,
                        principalTable: "avabot_chat_sessions",
                        principalColumn: "chat_session_id");
                    table.ForeignKey(
                        name: "avabot_fk_powerbi_datasets_powerbi_query_logs",
                        column: x => x.powerbi_dataset_id,
                        principalTable: "avabot_powerbi_datasets",
                        principalColumn: "powerbi_dataset_id");
                });

            migrationBuilder.CreateIndex(
                name: "avabot_agent_powerbi_configs_agent_id_key",
                table: "avabot_agent_powerbi_configs",
                column: "agent_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "avabot_powerbi_datasets_agent_id_dataset_id_key",
                table: "avabot_powerbi_datasets",
                columns: new[] { "agent_id", "dataset_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "avabot_powerbi_datasets_agent_id_tool_key_key",
                table: "avabot_powerbi_datasets",
                columns: new[] { "agent_id", "tool_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_avabot_powerbi_query_logs_agent_id_created_at",
                table: "avabot_powerbi_query_logs",
                columns: new[] { "agent_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_avabot_powerbi_query_logs_chat_session_id",
                table: "avabot_powerbi_query_logs",
                column: "chat_session_id");

            migrationBuilder.CreateIndex(
                name: "IX_avabot_powerbi_query_logs_powerbi_dataset_id",
                table: "avabot_powerbi_query_logs",
                column: "powerbi_dataset_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avabot_agent_powerbi_configs");

            migrationBuilder.DropTable(
                name: "avabot_powerbi_query_logs");

            migrationBuilder.DropTable(
                name: "avabot_powerbi_datasets");

            migrationBuilder.DropColumn(
                name: "powerbi_enabled",
                table: "avabot_agents");
        }
    }
}
