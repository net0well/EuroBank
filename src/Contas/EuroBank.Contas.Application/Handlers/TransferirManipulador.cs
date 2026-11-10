using EuroBank.Contas.Application.Commands;
using EuroBank.Contas.Application.Extensions;
using EuroBank.Contas.Application.Mappers;
using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Domain.Exceptions;
using EuroBank.Contas.Domain.Repositories;
using EuroBank.Contas.Domain.Services;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Handlers;

public sealed class TransferirManipulador(
    IRepositorioContas repositorio,
    ServicoTransferencia servicoTransferencia,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    /// <returns>A conta de origem depois da transferência.</returns>
    public async Task<ContaDto> Handle(Transferir comando, CancellationToken cancellationToken)
    {
        var ibanDestino = Iban.Criar(comando.IbanDestino);

        var origem = await repositorio.ObterOuFalharAsync(comando.ContaOrigemId, cancellationToken);
        var destino = await repositorio.ObterPorIbanAsync(ibanDestino, cancellationToken)
            ?? throw new ExcecaoIbanNaoEncontrado(ibanDestino);

        // O valor é na moeda da conta de origem; entre moedas diferentes o serviço recusa (vai para o câmbio).
        var valor = Dinheiro.De(comando.Valor, origem.Moeda);

        servicoTransferencia.Transferir(origem, destino, valor, relogio.GetUtcNow());

        // As duas contas e os dois eventos da outbox vão no mesmo SaveChanges, numa transação só.
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return origem.ParaDto();
    }
}
