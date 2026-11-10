using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Transferencias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TransferenciasCambio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "bic",
                table: "transferencias",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11);

            migrationBuilder.AddColumn<Guid>(
                name: "conta_destino_id",
                table: "transferencias",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "cotacao_id",
                table: "transferencias",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cotacao_taxa",
                table: "transferencias",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "cotacao_valida_ate",
                table: "transferencias",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "iban_origem",
                table: "transferencias",
                type: "character varying(34)",
                maxLength: 34,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "moeda_destino",
                table: "transferencias",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "moeda_do_valor",
                table: "transferencias",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "moeda_origem",
                table: "transferencias",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "nome_origem",
                table: "transferencias",
                type: "character varying(140)",
                maxLength: 140,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "valor_debitado",
                table: "transferencias",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_destino",
                table: "transferencias",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            // As transferências que já existiam eram todas em euro e sem câmbio: o que sai da conta é o que chega.
            migrationBuilder.Sql("UPDATE transferencias SET valor_debitado = valor, valor_destino = valor WHERE valor_debitado IS NULL");

            migrationBuilder.CreateTable(
                name: "contas_conhecidas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    titular = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    moeda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    aberta_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas_conhecidas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contas_conhecidas_iban",
                table: "contas_conhecidas",
                column: "iban",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contas_conhecidas");

            migrationBuilder.DropColumn(
                name: "conta_destino_id",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "cotacao_id",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "cotacao_taxa",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "cotacao_valida_ate",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "iban_origem",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "moeda_destino",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "moeda_do_valor",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "moeda_origem",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "nome_origem",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "valor_debitado",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "valor_destino",
                table: "transferencias");

            migrationBuilder.AlterColumn<string>(
                name: "bic",
                table: "transferencias",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(11)",
                oldMaxLength: 11,
                oldNullable: true);
        }
    }
}
