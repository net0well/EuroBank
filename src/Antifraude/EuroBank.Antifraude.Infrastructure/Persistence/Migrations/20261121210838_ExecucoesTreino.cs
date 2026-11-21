using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Antifraude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExecucoesTreino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "execucoes_treino",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    parametros = table.Column<string>(type: "jsonb", nullable: false),
                    solicitante = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    progresso = table.Column<int>(type: "integer", nullable: false),
                    relatorio = table.Column<string>(type: "jsonb", nullable: true),
                    versao = table.Column<int>(type: "integer", nullable: true),
                    erro = table.Column<string>(type: "text", nullable: true),
                    cancelamento_pedido = table.Column<bool>(type: "boolean", nullable: false),
                    solicitada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    iniciada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    terminada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    atualizada_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    etapas = table.Column<string>(type: "jsonb", nullable: false),
                    registros = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_execucoes_treino", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_execucoes_treino_solicitada_em",
                table: "execucoes_treino",
                column: "solicitada_em",
                descending: new bool[0]);

            // Um treino ativo por vez: o índice é sobre uma constante, então duas linhas ativas colidem sempre.
            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX ux_execucoes_treino_ativa ON execucoes_treino ((true))
                WHERE status IN ('Solicitada', 'EmAndamento');
                """);

            // Uma execução solicitada avisa o treinador (LISTEN antifraude_treinos) quando a transação confirma.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION notificar_treino_solicitado() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    PERFORM pg_notify('antifraude_treinos', NEW.id::text);
                    RETURN NULL;
                END;
                $$;

                CREATE TRIGGER execucoes_treino_notifica_solicitacao
                AFTER INSERT ON execucoes_treino
                FOR EACH ROW WHEN (NEW.status = 'Solicitada') EXECUTE FUNCTION notificar_treino_solicitado();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION notificar_treino_solicitado() CASCADE;");

            migrationBuilder.DropTable(
                name: "execucoes_treino");
        }
    }
}
