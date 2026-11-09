using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EuroBank.Transferencias.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_agregado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    id_agregado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tipo_evento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    ocorrido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    traceparent = table.Column<string>(type: "character varying(55)", maxLength: 55, nullable: true),
                    sequencia = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transferencias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    conta_origem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    iban_destino = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    bic = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    nome_beneficiario = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tarifa = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descricao = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: true),
                    email_notificacao = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hash_do_pedido = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    solicitada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    executar_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    executar_em_efetivo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    motivo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    cancelamento_solicitado = table.Column<bool>(type: "boolean", nullable: false),
                    debito_sem_resposta = table.Column<bool>(type: "boolean", nullable: false),
                    tentativas_no_passo = table.Column<int>(type: "integer", nullable: false),
                    prazo_do_passo = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    versao = table.Column<int>(type: "integer", nullable: false),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "passos_transferencia",
                columns: table => new
                {
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    transferencia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado_apos = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ocorrido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passos_transferencia", x => new { x.transferencia_id, x.ordem });
                    table.ForeignKey(
                        name: "FK_passos_transferencia_transferencias_transferencia_id",
                        column: x => x.transferencia_id,
                        principalTable: "transferencias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_outbox_sequencia",
                table: "outbox",
                column: "sequencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_conta_origem_id_executar_em_efetivo",
                table: "transferencias",
                columns: new[] { "conta_origem_id", "executar_em_efetivo" });

            migrationBuilder.CreateIndex(
                name: "IX_transferencias_idempotency_key",
                table: "transferencias",
                column: "idempotency_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox");

            migrationBuilder.DropTable(
                name: "passos_transferencia");

            migrationBuilder.DropTable(
                name: "transferencias");
        }
    }
}
