using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace AvaBot.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeChunks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "avabot_knowledge_chunks",
                columns: table => new
                {
                    knowledge_chunk_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    agent_id = table.Column<long>(type: "bigint", nullable: false),
                    knowledge_file_id = table.Column<long>(type: "bigint", nullable: false),
                    chunk_index = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    embedding = table.Column<float[]>(type: "real[]", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    search_content = table.Column<string>(type: "text", nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('portuguese'::regconfig, search_content)", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("avabot_knowledge_chunks_pkey", x => x.knowledge_chunk_id);
                    table.ForeignKey(
                        name: "avabot_fk_agents_knowledge_chunks",
                        column: x => x.agent_id,
                        principalTable: "avabot_agents",
                        principalColumn: "agent_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "avabot_fk_knowledge_files_knowledge_chunks",
                        column: x => x.knowledge_file_id,
                        principalTable: "avabot_knowledge_files",
                        principalColumn: "knowledge_file_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_avabot_knowledge_chunks_agent_id",
                table: "avabot_knowledge_chunks",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "ix_avabot_knowledge_chunks_file_chunk",
                table: "avabot_knowledge_chunks",
                columns: new[] { "knowledge_file_id", "chunk_index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_avabot_knowledge_chunks_search_vector",
                table: "avabot_knowledge_chunks",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avabot_knowledge_chunks");
        }
    }
}
