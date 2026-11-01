using EuroBank.Extrato.Domain.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Domain.Entities;

/// <summary>Uma linha do extrato: movimentação de dinheiro com o saldo logo depois dela.</summary>
public sealed class Lancamento
{
    private Lancamento()
    {
        // Usado pelo EF Core.
    }

    internal Lancamento(
        TipoLancamento tipo,
        Dinheiro valor,
        Dinheiro saldoApos,
        int versao,
        DateTimeOffset ocorridoEm,
        string descricao,
        Guid? transferenciaId)
    {
        Id = Guid.CreateVersion7();
        Tipo = tipo;
        Valor = valor;
        SaldoApos = saldoApos;
        Versao = versao;
        OcorridoEm = ocorridoEm;
        Descricao = descricao;
        TransferenciaId = transferenciaId;
    }

    public Guid Id { get; private set; }

    public TipoLancamento Tipo { get; private set; }

    public Dinheiro Valor { get; private set; } = Dinheiro.Zero;

    public Dinheiro SaldoApos { get; private set; } = Dinheiro.Zero;

    /// <summary>Versão da conta que gerou o lançamento.</summary>
    public int Versao { get; private set; }

    public DateTimeOffset OcorridoEm { get; private set; }

    public string Descricao { get; private set; } = string.Empty;

    public Guid? TransferenciaId { get; private set; }
}
