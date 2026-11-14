using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Transferencias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AnaliseRisco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "analise_risco_ate",
                table: "transferencias",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "analise_risco_id",
                table: "transferencias",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "caso_revisao_id",
                table: "transferencias",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "risco_aprovado",
                table: "transferencias",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "analise_risco_ate",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "analise_risco_id",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "caso_revisao_id",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "risco_aprovado",
                table: "transferencias");
        }
    }
}
