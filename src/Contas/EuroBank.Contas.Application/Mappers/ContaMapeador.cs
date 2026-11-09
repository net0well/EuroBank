using EuroBank.Contas.Application.TransferObjects;
using EuroBank.Contas.Domain.Entities;

namespace EuroBank.Contas.Application.Mappers;

public static class ContaMapeador
{
    public static ContaDto ParaDto(this Conta conta) => new(
        conta.Id.Valor,
        conta.Iban.Valor,
        conta.Titular.Nome,
        conta.Saldo.Valor,
        conta.Moeda.ToString(),
        conta.Status.ToString(),
        conta.Versao,
        conta.AbertaEm);
}
