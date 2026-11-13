using EuroBank.Shared.Enums;
using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Shared.Tests.ValueObjects;

public sealed class SaldoTestes
{
    [Fact]
    public void Subtrair_PodeFicarNegativo()
    {
        var saldo = Saldo.De(50m, Moeda.EUR) - Dinheiro.De(102.50m);

        saldo.Valor.ShouldBe(-52.50m);
        saldo.EhNegativo.ShouldBeTrue();
        saldo.ToString().ShouldBe("-52.50");
    }

    [Fact]
    public void Somar_EConferirSeCobre()
    {
        var saldo = Saldo.De(-10m, Moeda.USD) + Dinheiro.De(30m, Moeda.USD);

        saldo.ShouldBe(Saldo.De(20m, Moeda.USD));
        saldo.Cobre(Dinheiro.De(20m, Moeda.USD)).ShouldBeTrue();
        saldo.Cobre(Dinheiro.De(20.01m, Moeda.USD)).ShouldBeFalse();
    }

    [Fact]
    public void DinheiroViraSaldo_SemPerderAMoeda()
    {
        Saldo saldo = Dinheiro.De(10m, Moeda.GBP);

        saldo.ShouldBe(Saldo.De(10m, Moeda.GBP));
        saldo.EhZero.ShouldBeFalse();
    }

    [Fact]
    public void OperacaoEntreMoedasDiferentes_LancaExcecaoMoedaDiferente()
    {
        Should.Throw<ExcecaoMoedaDiferente>(() => Saldo.De(10m, Moeda.EUR) + Dinheiro.De(1m, Moeda.USD));
    }

    [Fact]
    public void MaisDeDuasCasasDecimais_LancaExcecaoValorInvalido()
    {
        Should.Throw<ExcecaoValorInvalido>(() => Saldo.De(-1.001m, Moeda.EUR));
    }
}
