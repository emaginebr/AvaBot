using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AvaBot.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndAgentOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "owner_user_id",
                table: "avabot_agents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "avabot_users",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    email = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avabot_users_pkey", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avabot_agents_owner_user_id",
                table: "avabot_agents",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "avabot_users_email_key",
                table: "avabot_users",
                column: "email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "avabot_fk_users_agents",
                table: "avabot_agents",
                column: "owner_user_id",
                principalTable: "avabot_users",
                principalColumn: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "avabot_fk_users_agents",
                table: "avabot_agents");

            migrationBuilder.DropTable(
                name: "avabot_users");

            migrationBuilder.DropIndex(
                name: "ix_avabot_agents_owner_user_id",
                table: "avabot_agents");

            migrationBuilder.DropColumn(
                name: "owner_user_id",
                table: "avabot_agents");
        }
    }
}
