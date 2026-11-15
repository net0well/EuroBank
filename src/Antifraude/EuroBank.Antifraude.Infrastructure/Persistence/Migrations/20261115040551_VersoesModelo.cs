using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EuroBank.Antifraude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class VersoesModelo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "versoes_modelo",
                columns: table => new
                {
                    versao = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    arquivo = table.Column<byte[]>(type: "bytea", nullable: false),
                    metricas = table.Column<string>(type: "jsonb", nullable: false),
                    limiar_recomendado = table.Column<double>(type: "double precision", nullable: false),
                    dados_treino = table.Column<string>(type: "jsonb", nullable: false),
                    treinada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ativada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    aposentada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    distribuicao = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_versoes_modelo", x => x.versao);
                });

            migrationBuilder.CreateIndex(
                name: "ux_versoes_modelo_ativa",
                table: "versoes_modelo",
                column: "status",
                unique: true,
                filter: "status = 'Ativa'");

            migrationBuilder.CreateIndex(
                name: "ux_versoes_modelo_challenger",
                table: "versoes_modelo",
                column: "status",
                unique: true,
                filter: "status = 'Challenger'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "versoes_modelo");
        }
    }
}
