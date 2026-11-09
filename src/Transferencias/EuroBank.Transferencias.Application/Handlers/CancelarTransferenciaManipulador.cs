using EuroBank.Shared.Domain;
using EuroBank.Transferencias.Application.Commands;
using EuroBank.Transferencias.Application.Extensions;
using EuroBank.Transferencias.Application.Mappers;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Repositories;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>
/// Cancelamento pelo cliente. Se o worker alterar a mesma transferência ao mesmo tempo, a versão otimista faz
/// um dos dois perder (409 para a API); o cliente repete e o pedido é reavaliado no estado novo.
/// </summary>
public sealed class CancelarTransferenciaManipulador(
    IRepositorioTransferencias repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<TransferenciaDto> Handle(CancelarTransferencia comando, CancellationToken cancellationToken)
    {
        var transferencia = await repositorio.ObterOuFalharAsync(comando.TransferenciaId, cancellationToken);

        transferencia.SolicitarCancelamento(relogio.GetUtcNow());

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return transferencia.ParaDto();
    }
}
