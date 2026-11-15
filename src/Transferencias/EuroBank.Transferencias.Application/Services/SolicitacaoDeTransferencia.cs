using System.Diagnostics;
using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Application.Enums;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.Mappers;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Services;

/// <summary>
/// Caminho comum dos pedidos de transferência: Idempotency-Key, registro e, se for para já, o começo da saga na
/// mesma transação (o primeiro comando sai pela outbox junto). Repetir a mesma key com o mesmo corpo devolve a
/// transferência já criada; com outro corpo, 422. A key é reservada no Redis antes: um pedido repetido enquanto o
/// primeiro ainda grava espera até 2 s por ele e, se não der tempo, recebe 409 com Retry-After. A constraint única
/// do Postgres continua sendo a garantia final (e a única com o Redis fora).
/// </summary>
public sealed class SolicitacaoDeTransferencia(
    IRepositorioTransferencias repositorio,
    ServicoInicioTransferencia inicio,
    IReservasIdempotencia reservas,
    IExecutorTransacional executor,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<SolicitacaoDeTransferencia> logger)
{
    private static readonly TimeSpan EsperaPeloOutroPedido = TimeSpan.FromSeconds(2);

    private static readonly TimeSpan IntervaloDaEspera = TimeSpan.FromMilliseconds(100);

    public async Task<TransferenciaDto> RegistrarAsync(
        string? idempotencyKey,
        string hashDoPedido,
        Func<string, DateTimeOffset, Task<Transferencia>> criar,
        CancellationToken cancellationToken)
    {
        var chave = idempotencyKey?.Trim();
        if (string.IsNullOrEmpty(chave) || chave.Length > ExcecaoIdempotencyKeyObrigatoria.TamanhoMaximo)
        {
            throw new ExcecaoIdempotencyKeyObrigatoria();
        }

        var reserva = await ReservarOuEsperarAsync(chave, hashDoPedido, cancellationToken);
        var reservou = reserva.Situacao == SituacaoReserva.Reservada;
        try
        {
            var transferencia = await RegistrarNoBancoAsync(chave, hashDoPedido, criar, cancellationToken);
            if (reservou)
            {
                await reservas.ConcluirAsync(chave, hashDoPedido, transferencia.Id, cancellationToken);
            }

            return transferencia;
        }
        catch when (reservou)
        {
            await reservas.LiberarAsync(chave, CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Reserva a key ou, se outro pedido igual a tem, espera ele terminar. Devolve a reserva com que este pedido segue
    /// para o banco: reservada, concluída pelo outro (o banco devolve a transferência dele) ou indisponível.
    /// </summary>
    private async Task<ReservaIdempotencia> ReservarOuEsperarAsync(string chave, string hashDoPedido, CancellationToken cancellationToken)
    {
        var inicioDaEspera = Stopwatch.GetTimestamp();
        var reserva = await reservas.ReservarAsync(chave, hashDoPedido, cancellationToken);
        while (true)
        {
            if (reserva.DeOutroPedido(hashDoPedido))
            {
                throw new ExcecaoIdempotencyKeyReutilizada();
            }

            if (reserva.Situacao != SituacaoReserva.EmAndamento)
            {
                return reserva;
            }

            if (Stopwatch.GetElapsedTime(inicioDaEspera) >= EsperaPeloOutroPedido)
            {
                logger.LogInformation("Idempotency-Key {Chave} ainda em andamento depois de {Espera}: 409", chave, EsperaPeloOutroPedido);
                throw new ExcecaoIdempotencyKeyEmAndamento();
            }

            // Espera de relógio de parede: é o outro pedido, não o domínio, que precisa de tempo.
            await Task.Delay(IntervaloDaEspera, cancellationToken);
            reserva = await reservas.ConsultarAsync(chave, cancellationToken);
            if (reserva.Situacao == SituacaoReserva.Livre)
            {
                // O outro pedido falhou e soltou a key: este tenta reservar de novo.
                reserva = await reservas.ReservarAsync(chave, hashDoPedido, cancellationToken);
            }
        }
    }

    private async Task<TransferenciaDto> RegistrarNoBancoAsync(
        string chave,
        string hashDoPedido,
        Func<string, DateTimeOffset, Task<Transferencia>> criar,
        CancellationToken cancellationToken)
    {
        var existente = await repositorio.ObterPorIdempotencyKeyAsync(chave, cancellationToken);
        if (existente is not null)
        {
            return existente.MesmoPedido(hashDoPedido) ? existente.ParaDto() : throw new ExcecaoIdempotencyKeyReutilizada();
        }

        var agora = relogio.GetUtcNow();
        var transferencia = await criar(chave, agora);
        repositorio.Adicionar(transferencia);

        await executor.ExecutarAsync(
            async token =>
            {
                await inicio.IniciarSeNaHoraAsync(transferencia, agora, token);
                await unidadeDeTrabalho.SalvarAsync(token);
            },
            cancellationToken);

        logger.LogInformation(
            "Transferência {Tipo} {TransferenciaId} da conta {ContaId}: {Estado}",
            transferencia.Tipo, transferencia.Id, transferencia.ContaOrigemId, transferencia.Estado);
        return transferencia.ParaDto();
    }
}
