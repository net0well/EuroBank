using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Extensions;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Shared.Domain;

namespace EuroBank.Contas.Application.Handlers;

public sealed class LiberarTravaTransferenciaManipulador(IRepositorioContas repositorio, IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task Handle(LiberarTravaTransferencia comando, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterOuFalharAsync(comando.ContaId, cancellationToken);

        conta.LiberarTrava(comando.TransferenciaId);

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
    }
}
