using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Contas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ContasMultimoeda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_transferencias_conta",
                table: "transferencias_conta");

            migrationBuilder.DropIndex(
                name: "IX_transferencias_conta_conta_id",
                table: "transferencias_conta");

            migrationBuilder.AddColumn<string>(
                name: "moeda",
                table: "transferencias_conta",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "moeda",
                table: "contas",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transferencias_conta",
                table: "transferencias_conta",
                columns: new[] { "conta_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_transferencias_conta",
                table: "transferencias_conta");

            migrationBuilder.DropColumn(
                name: "moeda",
                table: "transferencias_conta");

            migrationBuilder.DropColumn(
                name: "moeda",
                table: "contas");

            migrationBuilder.AddPrimaryKey(
                name: "PK_transferencias_conta",
                table: "transferencias_conta",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_conta_conta_id",
                table: "transferencias_conta",
                column: "conta_id");
        }
    }
}
