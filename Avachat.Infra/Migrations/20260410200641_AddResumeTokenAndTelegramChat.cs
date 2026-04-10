using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avachat.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddResumeTokenAndTelegramChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "resume_token",
                table: "avachat_chat_sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "avachat_telegram_chats",
                columns: table => new
                {
                    telegram_chat_id = table.Column<long>(type: "bigint", nullable: false),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    chat_session_id = table.Column<long>(type: "bigint", nullable: false),
                    telegram_username = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    telegram_first_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avachat_telegram_chats_pkey", x => x.telegram_chat_id);
                    table.ForeignKey(
                        name: "avachat_fk_telegram_chat_agent",
                        column: x => x.agent_id,
                        principalTable: "avachat_agents",
                        principalColumn: "agent_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "avachat_fk_telegram_chat_session",
                        column: x => x.chat_session_id,
                        principalTable: "avachat_chat_sessions",
                        principalColumn: "chat_session_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avachat_chat_sessions_resume_token",
                table: "avachat_chat_sessions",
                column: "resume_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_avachat_telegram_chats_agent_id",
                table: "avachat_telegram_chats",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_avachat_telegram_chats_chat_session_id",
                table: "avachat_telegram_chats",
                column: "chat_session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avachat_telegram_chats");

            migrationBuilder.DropIndex(
                name: "ix_avachat_chat_sessions_resume_token",
                table: "avachat_chat_sessions");

            migrationBuilder.DropColumn(
                name: "resume_token",
                table: "avachat_chat_sessions");
        }
    }
}
