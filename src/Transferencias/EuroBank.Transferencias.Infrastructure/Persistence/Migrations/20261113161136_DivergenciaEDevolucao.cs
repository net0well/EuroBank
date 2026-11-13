using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Transferencias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DivergenciaEDevolucao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "liquidacao_confirmada_no_estorno",
                table: "transferencias",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "encerrada_em",
                table: "contas_conhecidas",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "liquidacao_confirmada_no_estorno",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "encerrada_em",
                table: "contas_conhecidas");
        }
    }
}
