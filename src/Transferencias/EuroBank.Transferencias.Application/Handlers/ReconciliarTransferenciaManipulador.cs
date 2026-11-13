using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Application.Commands;
using EuroBank.Transferencias.Application.Exceptions;
using EuroBank.Transferencias.Application.Extensions;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.Mappers;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Repositories;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>
/// Reconciliação de uma Divergente: a saga pede a Contas o débito de novo. A conta encerrada é conferida na projeção
/// local (422); se ela fechar depois disso, Contas recusa e a transferência volta a Divergente.
/// </summary>
public sealed class ReconciliarTransferenciaManipulador(
    IRepositorioTransferencias repositorio,
    IContasConhecidas contas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    PoliticaSaga politica,
    TimeProvider relogio)
{
    public async Task<TransferenciaDto> Handle(ReconciliarTransferencia comando, CancellationToken cancellationToken)
    {
        var transferencia = await repositorio.ObterOuFalharAsync(comando.TransferenciaId, cancellationToken);

        if (transferencia.Estado == EstadoTransferencia.Divergente
            && await contas.ObterPorIdAsync(transferencia.ContaOrigemId, cancellationToken) is { Encerrada: true })
        {
            throw new ExcecaoContaOrigemEncerrada(transferencia.ContaOrigemId);
        }

        transferencia.SolicitarReconciliacao(politica, relogio.GetUtcNow());

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return transferencia.ParaDto();
    }
}
