using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EuroBank.Antifraude.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ModeloNaAnalise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "challenger",
                table: "analises",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modelo",
                table: "analises",
                type: "jsonb",
                nullable: true);

            // Cada mudança de status em versoes_modelo avisa as instâncias da API (LISTEN antifraude_modelos). O
            // aviso só sai quando a transação confirma, então quem recarrega já lê o estado novo.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION notificar_troca_modelo() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    IF TG_OP = 'UPDATE' AND OLD.status = NEW.status THEN
                        RETURN NULL;
                    END IF;
                    PERFORM pg_notify('antifraude_modelos', (CASE WHEN TG_OP = 'DELETE' THEN OLD.versao ELSE NEW.versao END)::text);
                    RETURN NULL;
                END;
                $$;

                CREATE TRIGGER versoes_modelo_notifica_troca
                AFTER INSERT OR DELETE OR UPDATE OF status ON versoes_modelo
                FOR EACH ROW EXECUTE FUNCTION notificar_troca_modelo();
                """);

            // O peso padrão do modelo passa a 50. Num banco que já existe e ainda está com peso 0, uma versão nova da
            // configuração com os mesmos limiares (a antiga fica para auditoria). Banco novo: a tabela está vazia e o
            // semeador grava a configuração inicial, já com 50.
            migrationBuilder.Sql(
                """
                INSERT INTO configuracoes_decisao (versao, limiar_revisao, limiar_recusa, peso_modelo, criada_em)
                SELECT versao + 1, limiar_revisao, limiar_recusa, 50, now()
                FROM configuracoes_decisao
                WHERE versao = (SELECT max(versao) FROM configuracoes_decisao) AND peso_modelo = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // A versão da configuração criada acima fica: apagá-la quebraria as análises que a usaram.
            migrationBuilder.Sql(
                """
                DROP TRIGGER versoes_modelo_notifica_troca ON versoes_modelo;
                DROP FUNCTION notificar_troca_modelo();
                """);

            migrationBuilder.DropColumn(
                name: "challenger",
                table: "analises");

            migrationBuilder.DropColumn(
                name: "modelo",
                table: "analises");
        }
    }
}
