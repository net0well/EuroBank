using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Domain.Entities;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Application.Mappers;

public static class ContaMapeador
{
    public static ContaDto ParaDto(this Conta conta) => new(
        conta.Id.Valor,
        conta.Iban.Valor,
        conta.Titular.Nome,
        conta.Saldo.Valor,
        Dinheiro.Moeda,
        conta.Status.ToString(),
        conta.Versao,
        conta.AbertaEm);
}
