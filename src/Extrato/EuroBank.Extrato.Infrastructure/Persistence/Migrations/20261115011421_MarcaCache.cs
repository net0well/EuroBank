using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Extrato.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MarcaCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cache_estado",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    suspeito_desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cache_estado", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "cache_estado",
                columns: new[] { "id", "suspeito_desde" },
                values: new object[] { 1, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cache_estado");
        }
    }
}
