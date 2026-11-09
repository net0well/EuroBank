using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Transferencias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndicesVarredura : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_transferencias_agendadas",
                table: "transferencias",
                column: "executar_em_efetivo",
                filter: "estado = 'Agendada'");

            migrationBuilder.CreateIndex(
                name: "ix_transferencias_prazo_do_passo",
                table: "transferencias",
                column: "prazo_do_passo",
                filter: "prazo_do_passo IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transferencias_agendadas",
                table: "transferencias");

            migrationBuilder.DropIndex(
                name: "ix_transferencias_prazo_do_passo",
                table: "transferencias");
        }
    }
}
