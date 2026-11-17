using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Antifraude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MedicoesDrift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "medicoes_drift",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    versao_modelo = table.Column<int>(type: "integer", nullable: false),
                    medida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inicio_janela = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    amostra = table.Column<int>(type: "integer", nullable: false),
                    amostra_score = table.Column<int>(type: "integer", nullable: false),
                    situacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nivel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    psi_maximo = table.Column<double>(type: "double precision", nullable: true),
                    psis = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medicoes_drift", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_medicoes_drift_medida_em",
                table: "medicoes_drift",
                column: "medida_em");

            migrationBuilder.CreateIndex(
                name: "IX_medicoes_drift_versao_modelo_medida_em",
                table: "medicoes_drift",
                columns: new[] { "versao_modelo", "medida_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "medicoes_drift");
        }
    }
}
