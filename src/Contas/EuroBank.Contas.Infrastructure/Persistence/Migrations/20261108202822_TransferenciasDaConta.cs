using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Contas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TransferenciasDaConta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "transferencias_conta",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tarifa = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    nome_beneficiario = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    debitada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estornada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias_conta", x => x.id);
                    table.ForeignKey(
                        name: "FK_transferencias_conta_contas_conta_id",
                        column: x => x.conta_id,
                        principalTable: "contas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_conta_conta_id",
                table: "transferencias_conta",
                column: "conta_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "transferencias_conta");
        }
    }
}
