using EuroBank.Extrato.Domain.Enums;
using EuroBank.Extrato.Domain.ValueObjects;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Domain.Entities;

/// <summary>
/// Uma linha do extrato: movimentação de dinheiro com o saldo logo depois dela, na moeda da conta. Os valores
/// são persistidos só como números, ao lado da moeda.
/// </summary>
public sealed class Lancamento
{
    private readonly decimal _valor;
    private readonly decimal _saldoApos;
    private readonly decimal? _tarifa;
    private readonly Moeda? _moedaCambio;
    private readonly decimal? _valorCambio;
    private readonly decimal? _cotacao;

    private Lancamento()
    {
        // Usado pelo EF Core.
    }

    internal Lancamento(
        TipoLancamento tipo,
        Dinheiro valor,
        Saldo saldoApos,
        int versao,
        DateTimeOffset ocorridoEm,
        string descricao,
        Guid? transferenciaId,
        Dinheiro? tarifa = null,
        CambioDoLancamento? cambio = null)
    {
        Id = Guid.CreateVersion7();
        Tipo = tipo;
        Moeda = valor.Moeda;
        _valor = valor.Valor;
        _saldoApos = saldoApos.Valor;
        _tarifa = tarifa?.Valor;
        _moedaCambio = cambio?.Moeda;
        _valorCambio = cambio?.Valor;
        _cotacao = cambio?.Cotacao;
        Versao = versao;
        OcorridoEm = ocorridoEm;
        Descricao = descricao;
        TransferenciaId = transferenciaId;
    }

    public Guid Id { get; private set; }

    public TipoLancamento Tipo { get; private set; }

    public Moeda Moeda { get; private set; }

    public Dinheiro Valor => Dinheiro.De(_valor, Moeda);

    public Saldo SaldoApos => Saldo.De(_saldoApos, Moeda);

    /// <summary>Tarifa cobrada junto com o lançamento (só nas transferências das sagas).</summary>
    public Dinheiro? Tarifa => _tarifa is { } tarifa ? Dinheiro.De(tarifa, Moeda) : null;

    /// <summary>Só nos lançamentos com câmbio.</summary>
    public CambioDoLancamento? Cambio =>
        _moedaCambio is { } moeda ? new CambioDoLancamento(moeda, _valorCambio!.Value, _cotacao!.Value) : null;

    /// <summary>Versão da conta que gerou o lançamento.</summary>
    public int Versao { get; private set; }

    public DateTimeOffset OcorridoEm { get; private set; }

    public string Descricao { get; private set; } = string.Empty;

    public Guid? TransferenciaId { get; private set; }
}
