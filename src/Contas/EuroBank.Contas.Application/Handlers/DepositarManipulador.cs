using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Extensions;
using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Handlers;

public sealed class DepositarManipulador(
    IRepositorioContas repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<ContaDto> Handle(Depositar comando, CancellationToken cancellationToken)
    {
        var conta = await repositorio.ObterOuFalharAsync(comando.ContaId, cancellationToken);
        var valor = Dinheiro.De(comando.Valor, conta.Moeda);

        conta.Depositar(valor, relogio.GetUtcNow());

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return conta.ParaDto();
    }
}
