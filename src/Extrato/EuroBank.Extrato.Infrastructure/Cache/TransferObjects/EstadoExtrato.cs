using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Infrastructure.Cache.TransferObjects;

/// <summary>O resumo do extrato como fica no Redis: tudo o que o agregado precisa para aplicar o próximo evento.</summary>
public sealed record EstadoExtrato(
    Guid ContaId,
    string Iban,
    string Titular,
    string Status,
    string Moeda,
    decimal Saldo,
    int Versao,
    DateTimeOffset AtualizadoEm)
{
    public static EstadoExtrato De(ExtratoConta extrato) => new(
        extrato.ContaId,
        extrato.Iban,
        extrato.Titular,
        extrato.Status.ToString(),
        extrato.Moeda.ToString(),
        extrato.SaldoAtual.Valor,
        extrato.UltimaVersao,
        extrato.AtualizadoEm);

    public ExtratoConta ParaAgregado()
    {
        var moeda = Enum.Parse<Moeda>(Moeda);
        return ExtratoConta.Restaurar(
            ContaId, Iban, Titular, Enum.Parse<StatusConta>(Status), moeda, Shared.ValueObjects.Saldo.De(Saldo, moeda), Versao, AtualizadoEm);
    }
}
