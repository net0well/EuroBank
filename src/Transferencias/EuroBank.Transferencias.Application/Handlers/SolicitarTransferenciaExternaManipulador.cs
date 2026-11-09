using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Application.Commands;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Mappers;
using EuroBank.Transferencias.Application.Services;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>
/// Registra a transferência e, se for para já, começa a saga na mesma transação (o comando de débito sai pela
/// outbox junto). Repetir a mesma Idempotency-Key com o mesmo corpo devolve a transferência já criada.
/// </summary>
public sealed class SolicitarTransferenciaExternaManipulador(
    IRepositorioTransferencias repositorio,
    ServicoInicioTransferencia inicio,
    IExecutorTransacional executor,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio,
    ILogger<SolicitarTransferenciaExternaManipulador> logger)
{
    public async Task<TransferenciaDto> Handle(SolicitarTransferenciaExterna comando, CancellationToken cancellationToken)
    {
        var chave = comando.IdempotencyKey?.Trim();
        if (string.IsNullOrEmpty(chave) || chave.Length > ExcecaoIdempotencyKeyObrigatoria.TamanhoMaximo)
        {
            throw new ExcecaoIdempotencyKeyObrigatoria();
        }

        var hash = ImpressaoDigitalPedido.Calcular(comando);
        var existente = await repositorio.ObterPorIdempotencyKeyAsync(chave, cancellationToken);
        if (existente is not null)
        {
            return existente.MesmoPedido(hash) ? existente.ParaDto() : throw new ExcecaoIdempotencyKeyReutilizada();
        }

        var agora = relogio.GetUtcNow();
        var transferencia = Transferencia.SolicitarExterna(
            comando.ContaOrigemId,
            IbanDestino.Criar(comando.IbanDestino),
            comando.Bic,
            comando.NomeBeneficiario,
            Dinheiro.De(comando.Valor),
            comando.Descricao,
            comando.EmailNotificacao,
            chave,
            hash,
            comando.ExecutarEm,
            agora);
        repositorio.Adicionar(transferencia);

        await executor.ExecutarAsync(
            async token =>
            {
                await inicio.IniciarSeNaHoraAsync(transferencia, agora, token);
                await unidadeDeTrabalho.SalvarAsync(token);
            },
            cancellationToken);

        logger.LogInformation(
            "Transferência externa {TransferenciaId} da conta {ContaId}: {Estado}",
            transferencia.Id, transferencia.ContaOrigemId, transferencia.Estado);
        return transferencia.ParaDto();
    }
}
