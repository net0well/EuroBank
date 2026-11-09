using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Extrato.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TarifaLancamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "tarifa",
                table: "lancamentos",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tarifa",
                table: "lancamentos");
        }
    }
}
