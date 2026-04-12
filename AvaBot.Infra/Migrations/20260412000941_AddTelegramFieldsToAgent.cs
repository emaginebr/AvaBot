using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvaBot.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramFieldsToAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "telegram_bot_name",
                table: "avabot_agents",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telegram_bot_token",
                table: "avabot_agents",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telegram_webhook_secret",
                table: "avabot_agents",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_avabot_agents_telegram_bot_token",
                table: "avabot_agents",
                column: "telegram_bot_token",
                unique: true,
                filter: "telegram_bot_token IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_avabot_agents_telegram_bot_token",
                table: "avabot_agents");

            migrationBuilder.DropColumn(
                name: "telegram_bot_name",
                table: "avabot_agents");

            migrationBuilder.DropColumn(
                name: "telegram_bot_token",
                table: "avabot_agents");

            migrationBuilder.DropColumn(
                name: "telegram_webhook_secret",
                table: "avabot_agents");
        }
    }
}
