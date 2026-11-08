using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Extensions;
using EuroBank.Contas.Application.Interfaces;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Shared.Domain;

namespace EuroBank.Contas.Application.Handlers;

public sealed class EstornarTransferenciaManipulador(
    IRepositorioContas repositorio,
    IRespostasSaga respostas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task Handle(EstornarTransferencia comando, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterOuFalharAsync(comando.ContaId, cancellationToken);

        conta.EstornarTransferencia(comando.TransferenciaId, relogio.GetUtcNow());

        var transferencia = conta.Transferencias.First(t => t.Id == comando.TransferenciaId);
        respostas.EstornoRealizado(comando.TransferenciaId, comando.ContaId, transferencia.Valor + transferencia.Tarifa);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
    }
}
