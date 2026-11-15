using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Tests.Entities;

public sealed class VersaoModeloTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 15, 10, 0, 0, TimeSpan.Zero);

    private static VersaoModelo Registrada() => VersaoModelo.Registrar(
        [1, 2, 3],
        new MetricasModelo(0.8, 0.5, 0.9, 0.64, new MatrizConfusao(90, 90, 9_800, 10), [new PontoCurva(0.5, 0.5, 0.9, 0.018)]),
        0.5,
        new DadosTreino(42, 10_000, 150, 0, 0, Agora.AddDays(-90), Agora.AddDays(-20), Agora, 2_000, 0.9, 0.05, MetaAtingida: true),
        [new DistribuicaoFeature("LogValor", [1, 2], [0.3, 0.5, 0.2])],
        Agora);

    [Fact]
    public void Registrar_NasceRegistrada()
    {
        var versao = Registrada();

        versao.Status.ShouldBe(StatusModelo.Registrada);
        versao.TreinadaEm.ShouldBe(Agora);
        versao.Distribuicao.ShouldHaveSingleItem().Faixa(1.5).ShouldBe(1);
    }

    [Fact]
    public void Registrar_SemArquivo_Falha()
    {
        Should.Throw<ArgumentException>(() => VersaoModelo.Registrar(
            [], Registrada().Metricas, 0.5, Registrada().Dados, [], Agora));
    }

    [Fact]
    public void SombraDepoisAtivaDepoisAposentada()
    {
        var versao = Registrada();

        versao.ColocarEmSombra();
        versao.Status.ShouldBe(StatusModelo.Challenger);

        versao.Ativar(Agora.AddHours(1));
        versao.Status.ShouldBe(StatusModelo.Ativa);
        versao.AtivadaEm.ShouldBe(Agora.AddHours(1));

        versao.Aposentar(Agora.AddHours(2));
        versao.Status.ShouldBe(StatusModelo.Aposentada);
        versao.AposentadaEm.ShouldBe(Agora.AddHours(2));
    }

    [Fact]
    public void Ativa_NaoVaiParaASombra()
    {
        var versao = Registrada();
        versao.Ativar(Agora);

        Should.Throw<ExcecaoTransicaoModeloInvalida>(versao.ColocarEmSombra).Codigo.ShouldBe("transicao_modelo_invalida");
    }

    [Theory]
    [InlineData(0.05, 0)]
    [InlineData(1, 1)]
    [InlineData(1.99, 1)]
    [InlineData(2, 2)]
    [InlineData(50, 2)]
    public void Faixa_DaDistribuicao(double valor, int faixa)
    {
        new DistribuicaoFeature("LogValor", [1, 2], [0.3, 0.5, 0.2]).Faixa(valor).ShouldBe(faixa);
    }
}
