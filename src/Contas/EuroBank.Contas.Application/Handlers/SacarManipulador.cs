using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Extensions;
using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Handlers;

public sealed class SacarManipulador(
    IRepositorioContas repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<ContaDto> Handle(Sacar comando, CancellationToken cancellationToken)
    {
        var valor = Dinheiro.De(comando.Valor);
        var conta = await repositorio.ObterOuFalharAsync(comando.ContaId, cancellationToken);

        conta.Sacar(valor, relogio.GetUtcNow());

        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return conta.ParaDto();
    }
}
