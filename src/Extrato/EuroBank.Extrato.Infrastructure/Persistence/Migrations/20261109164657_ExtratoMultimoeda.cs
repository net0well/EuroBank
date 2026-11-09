using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Extrato.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtratoMultimoeda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cotacao",
                table: "lancamentos",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "moeda",
                table: "lancamentos",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "moeda_cambio",
                table: "lancamentos",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "valor_cambio",
                table: "lancamentos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "moeda",
                table: "extratos",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cotacao",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "moeda",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "moeda_cambio",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "valor_cambio",
                table: "lancamentos");

            migrationBuilder.DropColumn(
                name: "moeda",
                table: "extratos");
        }
    }
}
