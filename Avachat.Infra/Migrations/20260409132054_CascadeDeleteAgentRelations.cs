using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Avachat.Infra.Migrations
{
    /// <inheritdoc />
    public partial class CascadeDeleteAgentRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing foreign keys
            migrationBuilder.DropForeignKey(
                name: "avachat_fk_agents_knowledge_files",
                table: "avachat_knowledge_files");

            migrationBuilder.DropForeignKey(
                name: "avachat_fk_agents_chat_sessions",
                table: "avachat_chat_sessions");

            migrationBuilder.DropForeignKey(
                name: "avachat_fk_chat_sessions_chat_messages",
                table: "avachat_chat_messages");

            // Re-create with cascade delete
            migrationBuilder.AddForeignKey(
                name: "avachat_fk_agents_knowledge_files",
                table: "avachat_knowledge_files",
                column: "agent_id",
                principalTable: "avachat_agents",
                principalColumn: "agent_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "avachat_fk_agents_chat_sessions",
                table: "avachat_chat_sessions",
                column: "agent_id",
                principalTable: "avachat_agents",
                principalColumn: "agent_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "avachat_fk_chat_sessions_chat_messages",
                table: "avachat_chat_messages",
                column: "chat_session_id",
                principalTable: "avachat_chat_sessions",
                principalColumn: "chat_session_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "avachat_fk_agents_knowledge_files",
                table: "avachat_knowledge_files");

            migrationBuilder.DropForeignKey(
                name: "avachat_fk_agents_chat_sessions",
                table: "avachat_chat_sessions");

            migrationBuilder.DropForeignKey(
                name: "avachat_fk_chat_sessions_chat_messages",
                table: "avachat_chat_messages");

            migrationBuilder.AddForeignKey(
                name: "avachat_fk_agents_knowledge_files",
                table: "avachat_knowledge_files",
                column: "agent_id",
                principalTable: "avachat_agents",
                principalColumn: "agent_id");

            migrationBuilder.AddForeignKey(
                name: "avachat_fk_agents_chat_sessions",
                table: "avachat_chat_sessions",
                column: "agent_id",
                principalTable: "avachat_agents",
                principalColumn: "agent_id");

            migrationBuilder.AddForeignKey(
                name: "avachat_fk_chat_sessions_chat_messages",
                table: "avachat_chat_messages",
                column: "chat_session_id",
                principalTable: "avachat_chat_sessions",
                principalColumn: "chat_session_id");
        }
    }
}
