using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EuroBank.Antifraude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriacaoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analises",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_final = table.Column<int>(type: "integer", nullable: false),
                    decisao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    versao_configuracao = table.Column<int>(type: "integer", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decidida_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    analista = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    justificativa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    iban_destino = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    moeda = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    momento = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sinais = table.Column<string>(type: "jsonb", nullable: false),
                    tipo_operacao = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analises", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "casos_revisao",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analise_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operacao_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score_final = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    analista = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    aberto_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expira_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assumido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    decidido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    justificativa = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    bloquear_iban = table.Column<bool>(type: "boolean", nullable: false),
                    versao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_casos_revisao", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configuracoes_decisao",
                columns: table => new
                {
                    versao = table.Column<int>(type: "integer", nullable: false),
                    limiar_revisao = table.Column<int>(type: "integer", nullable: false),
                    limiar_recusa = table.Column<int>(type: "integer", nullable: false),
                    peso_modelo = table.Column<int>(type: "integer", nullable: false),
                    criada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_configuracoes_decisao", x => x.versao);
                });

            migrationBuilder.CreateTable(
                name: "ibans_bloqueados",
                columns: table => new
                {
                    iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bloqueado_por = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    bloqueado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibans_bloqueados", x => x.iban);
                });

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
                name: "paises_risco",
                columns: table => new
                {
                    codigo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    incluido_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paises_risco", x => x.codigo);
                });

            migrationBuilder.CreateTable(
                name: "regras",
                columns: table => new
                {
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    tipo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    descricao = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    peso = table.Column<int>(type: "integer", nullable: false),
                    efeito = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ativa = table.Column<bool>(type: "boolean", nullable: false),
                    sombra = table.Column<bool>(type: "boolean", nullable: false),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    parametros = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_regras", x => x.codigo);
                });

            migrationBuilder.CreateTable(
                name: "casos_revisao_auditoria",
                columns: table => new
                {
                    ordem = table.Column<int>(type: "integer", nullable: false),
                    caso_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quando = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    acao = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    detalhe = table.Column<string>(type: "character varying(1100)", maxLength: 1100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_casos_revisao_auditoria", x => new { x.caso_id, x.ordem });
                    table.ForeignKey(
                        name: "FK_casos_revisao_auditoria_casos_revisao_caso_id",
                        column: x => x.caso_id,
                        principalTable: "casos_revisao",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analises_conta_id",
                table: "analises",
                column: "conta_id");

            migrationBuilder.CreateIndex(
                name: "IX_analises_operacao_id",
                table: "analises",
                column: "operacao_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_casos_revisao_analise_id",
                table: "casos_revisao",
                column: "analise_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_casos_revisao_operacao_id",
                table: "casos_revisao",
                column: "operacao_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_casos_revisao_pendentes",
                table: "casos_revisao",
                column: "expira_em",
                filter: "estado IN ('Aberto', 'EmAnalise')");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_sequencia",
                table: "outbox",
                column: "sequencia",
                unique: true);

            // Listas iniciais, gravadas uma vez só (removidas pelo analista, não voltam numa nova subida): os países
            // da lista de "ação" do GAFI e um IBAN de exemplo (final 66, sem comportamento especial no banco externo).
            var inicio = new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero);
            migrationBuilder.InsertData(
                table: "paises_risco",
                columns: ["codigo", "incluido_em"],
                values: new object[,] { { "IR", inicio }, { "KP", inicio }, { "MM", inicio } });
            migrationBuilder.InsertData(
                table: "ibans_bloqueados",
                columns: ["iban", "motivo", "bloqueado_por", "bloqueado_em"],
                values: ["DE89370400440532013066", "IBAN de exemplo do laboratório", "sistema", inicio]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analises");

            migrationBuilder.DropTable(
                name: "casos_revisao_auditoria");

            migrationBuilder.DropTable(
                name: "configuracoes_decisao");

            migrationBuilder.DropTable(
                name: "ibans_bloqueados");

            migrationBuilder.DropTable(
                name: "outbox");

            migrationBuilder.DropTable(
                name: "paises_risco");

            migrationBuilder.DropTable(
                name: "regras");

            migrationBuilder.DropTable(
                name: "casos_revisao");
        }
    }
}
