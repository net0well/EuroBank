using EuroBank.Extrato.Infrastructure.Cache.TransferObjects;
using EuroBank.Extrato.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Extrato.Infrastructure.Cache.WriteBehind;

/// <summary>
/// Grava pendências do write-behind no Postgres, numa transação: o extrato só avança com versão maior que a gravada e
/// cada lançamento entra uma vez (único por conta e versão). Repetir um lote, ou gravar fora de ordem, não estraga nada.
/// </summary>
public sealed class GravadorExtratosPostgres(ExtratoDbContext contexto)
{
    public async Task GravarAsync(IReadOnlyList<PendenciaExtrato> pendencias, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pendencias);
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);
        foreach (var pendencia in pendencias)
        {
            var estado = pendencia.Estado;
            await contexto.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO extratos (conta_id, iban, titular, status, moeda, saldo_atual, ultima_versao, atualizado_em)
                VALUES ({estado.ContaId}, {estado.Iban}, {estado.Titular}, {estado.Status}, {estado.Moeda}, {estado.Saldo},
                        {estado.Versao}, {estado.AtualizadoEm})
                ON CONFLICT (conta_id) DO UPDATE SET
                    iban = EXCLUDED.iban, titular = EXCLUDED.titular, status = EXCLUDED.status, moeda = EXCLUDED.moeda,
                    saldo_atual = EXCLUDED.saldo_atual, ultima_versao = EXCLUDED.ultima_versao,
                    atualizado_em = EXCLUDED.atualizado_em
                WHERE extratos.ultima_versao < EXCLUDED.ultima_versao
                """,
                cancellationToken);

            foreach (var lancamento in pendencia.Lancamentos)
            {
                await contexto.Database.ExecuteSqlAsync(
                    $"""
                    INSERT INTO lancamentos (id, conta_id, tipo, moeda, valor, saldo_apos, tarifa, versao, ocorrido_em,
                                             descricao, transferencia_id, moeda_cambio, valor_cambio, cotacao)
                    VALUES ({lancamento.Id}, {estado.ContaId}, {lancamento.Tipo}, {lancamento.Moeda}, {lancamento.Valor},
                            {lancamento.SaldoApos}, {lancamento.Tarifa}, {lancamento.Versao}, {lancamento.OcorridoEm},
                            {lancamento.Descricao}, {lancamento.TransferenciaId}, {lancamento.MoedaCambio},
                            {lancamento.ValorCambio}, {lancamento.Cotacao})
                    ON CONFLICT (conta_id, versao) DO NOTHING
                    """,
                    cancellationToken);
            }
        }

        await transacao.CommitAsync(cancellationToken);
    }
}
