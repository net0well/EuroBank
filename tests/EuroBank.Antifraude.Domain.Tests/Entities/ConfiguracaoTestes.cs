using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Policies;
using static EuroBank.Antifraude.Domain.Tests.Cenarios;

namespace EuroBank.Antifraude.Domain.Tests.Entities;

/// <summary>Regras e limiares ajustáveis sem reiniciar, sempre validados.</summary>
public sealed class ConfiguracaoTestes
{
    [Fact]
    public void RegrasIniciais_TemOsParametrosDoPlano()
    {
        var regras = Regras().ToDictionary(regra => regra.Codigo);

        regras.Count.ShouldBe(9);
        regras[CodigoRegra.ListaBloqueio].Tipo.ShouldBe(TipoRegra.Dura);
        regras[CodigoRegra.ValorAcimaDaMedia].Peso.ShouldBe(25);
        regras[CodigoRegra.ValorAcimaDaMedia].Parametro("multiplicador").ShouldBe(5m);
        regras[CodigoRegra.ValorAcimaDaMedia].Parametro("minimoSaidas").ShouldBe(3m);
        regras[CodigoRegra.Velocidade].Parametro("quantidade").ShouldBe(3m);
        regras[CodigoRegra.Velocidade].Parametro("janelaMinutos").ShouldBe(10m);
        regras[CodigoRegra.TesteDeCartao].Parametro("valorMaximo").ShouldBe(10m);
        regras[CodigoRegra.TesteDeCartao].Parametro("janelaMinutos").ShouldBe(15m);
        regras[CodigoRegra.Esvaziamento].Parametro("percentualSaldo").ShouldBe(0.9m);
        regras[CodigoRegra.ContaRecemAberta].Parametro("dias").ShouldBe(7m);
        regras[CodigoRegra.ContaRecemAberta].Parametro("valorMinimo").ShouldBe(1_000m);
        regras[CodigoRegra.HorarioIncomum].Parametro("minimoSaidas").ShouldBe(10m);
        regras[CodigoRegra.PaisDeRisco].Peso.ShouldBe(30);
        regras[CodigoRegra.BeneficiarioNovo].Peso.ShouldBe(10);
        regras.Values.ShouldAllBe(regra => regra.Ativa && !regra.Sombra);
    }

    [Fact]
    public void AjustarParametro_MantemOsOutros()
    {
        var velocidade = Regras().Single(regra => regra.Codigo == CodigoRegra.Velocidade);

        velocidade.Ajustar(30, ativa: true, sombra: false, new Dictionary<string, decimal> { ["quantidade"] = 5m }, Agora.AddHours(1));

        velocidade.Peso.ShouldBe(30);
        velocidade.Parametro("quantidade").ShouldBe(5m);
        velocidade.Parametro("janelaMinutos").ShouldBe(10m);
        velocidade.AtualizadaEm.ShouldBe(Agora.AddHours(1));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void PesoForaDeZeroACem_EhInvalido(int peso)
    {
        var regra = Regras().Single(regra => regra.Codigo == CodigoRegra.Velocidade);

        Should.Throw<ExcecaoParametroRegraInvalido>(() => regra.Ajustar(peso, true, false, null, Agora));
    }

    [Fact]
    public void ParametroDesconhecido_EhInvalido()
    {
        var regra = Regras().Single(regra => regra.Codigo == CodigoRegra.Velocidade);

        Should.Throw<ExcecaoParametroRegraInvalido>(() =>
            regra.Ajustar(20, true, false, new Dictionary<string, decimal> { ["multiplicador"] = 2m }, Agora));
    }

    [Fact]
    public void ParametroZeroOuNegativo_EhInvalido()
    {
        var regra = Regras().Single(regra => regra.Codigo == CodigoRegra.Velocidade);

        Should.Throw<ExcecaoParametroRegraInvalido>(() =>
            regra.Ajustar(20, true, false, new Dictionary<string, decimal> { ["quantidade"] = 0m }, Agora));
    }

    [Fact]
    public void RegraDura_NaoTemPeso()
    {
        var bloqueio = Regras().Single(regra => regra.Codigo == CodigoRegra.ListaBloqueio);

        Should.Throw<ExcecaoParametroRegraInvalido>(() => bloqueio.Ajustar(10, true, false, null, Agora));
    }

    [Fact]
    public void ConfiguracaoInicial_Trinta_Setenta_ModeloComPeso50()
    {
        var configuracao = Configuracao();

        configuracao.Versao.ShouldBe(1);
        configuracao.LimiarRevisao.ShouldBe(30);
        configuracao.LimiarRecusa.ShouldBe(70);
        configuracao.PesoModelo.ShouldBe(50);
    }

    [Fact]
    public void NovaVersao_GuardaAAnterior()
    {
        var anterior = Configuracao();

        var nova = anterior.NovaVersao(20, 60, 40, Agora.AddDays(1));

        nova.Versao.ShouldBe(2);
        nova.LimiarRevisao.ShouldBe(20);
        nova.LimiarRecusa.ShouldBe(60);
        nova.PesoModelo.ShouldBe(40);
        nova.CriadaEm.ShouldBe(Agora.AddDays(1));
        anterior.Versao.ShouldBe(1);
    }

    [Theory]
    [InlineData(70, 70, 0)]
    [InlineData(80, 70, 0)]
    [InlineData(0, 70, 0)]
    [InlineData(30, 101, 0)]
    [InlineData(30, 70, -1)]
    [InlineData(30, 70, 101)]
    public void LimiaresOuPesoInvalidos_SaoRecusados(int revisao, int recusa, int pesoModelo)
    {
        Should.Throw<ExcecaoConfiguracaoInvalida>(() => Configuracao().NovaVersao(revisao, recusa, pesoModelo, Agora));
    }

    [Fact]
    public void Catalogo_DescreveCadaRegra()
    {
        CatalogoRegras.Definicoes.Count.ShouldBe(9);
        CatalogoRegras.Definicoes.ShouldAllBe(definicao => !string.IsNullOrWhiteSpace(definicao.Descricao));
    }
}
