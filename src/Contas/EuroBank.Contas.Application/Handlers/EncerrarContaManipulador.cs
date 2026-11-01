using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Extensions;
using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Shared.Domain;

namespace EuroBank.Contas.Application.Handlers;

public sealed class EncerrarContaManipulador(
    IRepositorioContas repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<ContaDto> Handle(EncerrarConta comando, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterOuFalharAsync(comando.ContaId, cancellationToken);

        conta.Encerrar(relogio.GetUtcNow());

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return conta.ParaDto();
    }
}
