using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Transferencias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrigemDoCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dispositivo_id",
                table: "transferencias",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "transferencias",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "transferencias",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pais_localizacao",
                table: "transferencias",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dispositivo_id",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "transferencias");

            migrationBuilder.DropColumn(
                name: "pais_localizacao",
                table: "transferencias");
        }
    }
}
