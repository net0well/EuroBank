using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Notificacoes.Worker.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notificacoes",
                columns: table => new
                {
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transferencia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_evento = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    destinatario = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    assunto = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tentativas = table.Column<int>(type: "integer", nullable: false),
                    ultimo_erro = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    enviada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notificacoes", x => x.evento_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notificacoes_transferencia_id",
                table: "notificacoes",
                column: "transferencia_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notificacoes");
        }
    }
}
