using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Antifraude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FraudesConfirmadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fraudes_confirmadas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    analise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    analista = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    observacao = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    confirmada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fraudes_confirmadas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fraudes_confirmadas_operacao_id",
                table: "fraudes_confirmadas",
                column: "operacao_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fraudes_confirmadas");
        }
    }
}
