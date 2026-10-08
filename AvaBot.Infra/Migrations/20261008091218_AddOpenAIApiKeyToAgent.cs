using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AvaBot.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenAIApiKeyToAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "openai_api_key_encrypted",
                table: "avabot_agents",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "openai_api_key_encrypted",
                table: "avabot_agents");
        }
    }
}
