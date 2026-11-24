using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AvaBot.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddWhatsappAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "whatsapp_access_mode",
                table: "avabot_agents",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp_confirmation_message",
                table: "avabot_agents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp_declined_message",
                table: "avabot_agents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp_offer_message",
                table: "avabot_agents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp_waiting_message",
                table: "avabot_agents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "avabot_whatsapp_contacts",
                columns: table => new
                {
                    whatsapp_contact_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    phone_normalized = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    phone_match_key = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    whatsapp_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    source = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avabot_whatsapp_contacts_pkey", x => x.whatsapp_contact_id);
                    table.ForeignKey(
                        name: "avabot_fk_agents_whatsapp_contacts",
                        column: x => x.agent_id,
                        principalTable: "avabot_agents",
                        principalColumn: "agent_id");
                });

            migrationBuilder.CreateTable(
                name: "avabot_whatsapp_sender_states",
                columns: table => new
                {
                    whatsapp_sender_state_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    whatsapp_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    last_auto_reply_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    offer_sent_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avabot_whatsapp_sender_states_pkey", x => x.whatsapp_sender_state_id);
                    table.ForeignKey(
                        name: "avabot_fk_agents_whatsapp_sender_states",
                        column: x => x.agent_id,
                        principalTable: "avabot_agents",
                        principalColumn: "agent_id");
                });

            migrationBuilder.CreateTable(
                name: "avabot_whatsapp_access_requests",
                columns: table => new
                {
                    whatsapp_access_request_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    whatsapp_id = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    phone_normalized = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    phone_match_key = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    profile_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    requested_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    decided_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    decided_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    whatsapp_contact_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avabot_whatsapp_access_requests_pkey", x => x.whatsapp_access_request_id);
                    table.ForeignKey(
                        name: "avabot_fk_agents_whatsapp_access_requests",
                        column: x => x.agent_id,
                        principalTable: "avabot_agents",
                        principalColumn: "agent_id");
                    table.ForeignKey(
                        name: "avabot_fk_users_whatsapp_access_requests",
                        column: x => x.decided_by_user_id,
                        principalTable: "avabot_users",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "avabot_fk_whatsapp_contacts_access_requests",
                        column: x => x.whatsapp_contact_id,
                        principalTable: "avabot_whatsapp_contacts",
                        principalColumn: "whatsapp_contact_id");
                });

            migrationBuilder.CreateIndex(
                name: "avabot_whatsapp_access_requests_pending_key",
                table: "avabot_whatsapp_access_requests",
                columns: new[] { "agent_id", "whatsapp_id" },
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_avabot_whatsapp_access_requests_agent_status",
                table: "avabot_whatsapp_access_requests",
                columns: new[] { "agent_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_avabot_whatsapp_access_requests_decided_by_user_id",
                table: "avabot_whatsapp_access_requests",
                column: "decided_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_avabot_whatsapp_access_requests_whatsapp_contact_id",
                table: "avabot_whatsapp_access_requests",
                column: "whatsapp_contact_id");

            migrationBuilder.CreateIndex(
                name: "avabot_whatsapp_contacts_agent_phone_key",
                table: "avabot_whatsapp_contacts",
                columns: new[] { "agent_id", "phone_match_key" },
                unique: true,
                filter: "phone_match_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "avabot_whatsapp_contacts_agent_wid_key",
                table: "avabot_whatsapp_contacts",
                columns: new[] { "agent_id", "whatsapp_id" },
                unique: true,
                filter: "whatsapp_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_avabot_whatsapp_contacts_agent_id",
                table: "avabot_whatsapp_contacts",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "avabot_whatsapp_sender_states_agent_wid_key",
                table: "avabot_whatsapp_sender_states",
                columns: new[] { "agent_id", "whatsapp_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avabot_whatsapp_access_requests");

            migrationBuilder.DropTable(
                name: "avabot_whatsapp_sender_states");

            migrationBuilder.DropTable(
                name: "avabot_whatsapp_contacts");

            migrationBuilder.DropColumn(
                name: "whatsapp_access_mode",
                table: "avabot_agents");

            migrationBuilder.DropColumn(
                name: "whatsapp_confirmation_message",
                table: "avabot_agents");

            migrationBuilder.DropColumn(
                name: "whatsapp_declined_message",
                table: "avabot_agents");

            migrationBuilder.DropColumn(
                name: "whatsapp_offer_message",
                table: "avabot_agents");

            migrationBuilder.DropColumn(
                name: "whatsapp_waiting_message",
                table: "avabot_agents");
        }
    }
}
