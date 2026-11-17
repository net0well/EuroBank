using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Antifraude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OrigemDaAnalise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "dispositivo_id",
                table: "analises",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "analises",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "analises",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pais_localizacao",
                table: "analises",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dispositivo_id",
                table: "analises");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "analises");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "analises");

            migrationBuilder.DropColumn(
                name: "pais_localizacao",
                table: "analises");
        }
    }
}
