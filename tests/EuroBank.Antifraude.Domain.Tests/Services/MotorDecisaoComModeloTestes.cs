using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.Tests.Fakes;
using EuroBank.Antifraude.Domain.ValueObjects;
using static EuroBank.Antifraude.Domain.Tests.Cenarios;

namespace EuroBank.Antifraude.Domain.Tests.Services;

/// <summary>
/// O modelo no motor: a probabilidade da versão ativa vezes o peso entra como um sinal; o challenger é registrado,
/// mas não pontua nem decide.
/// </summary>
public sealed class MotorDecisaoComModeloTestes
{
    private static ResultadoMotor Decidir(IPontuadorFraude pontuador, ConfiguracaoDecisao? configuracao = null) =>
        new MotorDecisao(pontuador).Decidir(Operacao(), PerfilComHistorico(), Regras(), SemListas(), configuracao ?? Configuracao());

    [Fact]
    public void VersaoAtiva_ProbabilidadeVezesOPeso_EntraNoScoreComoSinal()
    {
        var resultado = Decidir(new PontuadorFixo(ativa: 0.9f));

        var sinal = resultado.Sinais.Single(sinal => sinal.Codigo == CodigoRegra.ModeloFraude);
        sinal.Tipo.ShouldBe(TipoRegra.Peso);
        sinal.Disparou.ShouldBeTrue();
        sinal.Pontos.ShouldBe(45);
        sinal.Sombra.ShouldBeFalse();
        sinal.Motivo.ShouldBe("Probabilidade de fraude 0.900 na versão 3 do modelo");
        resultado.ScoreFinal.ShouldBe(45);
        resultado.Decisao.ShouldBe(DecisaoAnalise.EmRevisao);
        resultado.Modelo.ShouldNotBeNull().Pontos.ShouldBe(45);
        resultado.Modelo.Versao.ShouldBe(PontuadorFixo.VersaoAtiva);
        resultado.Modelo.Contribuicoes.Count.ShouldBe(2);
        resultado.Challenger.ShouldBeNull();
    }

    [Theory]
    [InlineData(0.25f, 10, 3)]
    [InlineData(0.009f, 50, 0)]
    [InlineData(1f, 50, 50)]
    [InlineData(0.37f, 100, 37)]
    public void Pontos_ProbabilidadeVezesOPeso_Arredondado(float probabilidade, int peso, int pontos)
    {
        var configuracao = Configuracao().NovaVersao(30, 70, peso, Agora);

        var resultado = Decidir(new PontuadorFixo(probabilidade), configuracao);

        resultado.Sinais.Single(sinal => sinal.Codigo == CodigoRegra.ModeloFraude).Pontos.ShouldBe(pontos);
        resultado.ScoreFinal.ShouldBe(pontos);
    }

    [Fact]
    public void PesoZero_ModeloAparece_MasNaoPontua()
    {
        var configuracao = Configuracao().NovaVersao(30, 70, 0, Agora);

        var resultado = Decidir(new PontuadorFixo(ativa: 0.99f), configuracao);

        var sinal = resultado.Sinais.Single(sinal => sinal.Codigo == CodigoRegra.ModeloFraude);
        sinal.Disparou.ShouldBeFalse();
        sinal.Pontos.ShouldBe(0);
        resultado.ScoreFinal.ShouldBe(0);
        resultado.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
    }

    [Fact]
    public void Challenger_PontuaEmSombra_SemMudarScoreNemDecisao()
    {
        var resultado = Decidir(new PontuadorFixo(ativa: 0.1f, challenger: 0.9f));

        resultado.ScoreFinal.ShouldBe(5);
        resultado.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
        resultado.Sinais.Count(sinal => sinal.Codigo == CodigoRegra.ModeloFraude).ShouldBe(1);
        var challenger = resultado.Challenger.ShouldNotBeNull();
        challenger.Versao.ShouldBe(PontuadorFixo.VersaoChallenger);
        challenger.Pontos.ShouldBe(45);
    }

    [Fact]
    public void SoChallenger_SemSinalDoModelo()
    {
        var resultado = Decidir(new PontuadorFixo(ativa: null, challenger: 0.95f));

        resultado.Sinais.ShouldNotContain(sinal => sinal.Codigo == CodigoRegra.ModeloFraude);
        resultado.ScoreFinal.ShouldBe(0);
        resultado.Modelo.ShouldBeNull();
        resultado.Challenger.ShouldNotBeNull();
    }

    [Fact]
    public void SemModelo_SoAsRegras()
    {
        var resultado = Decidir(PontuadorSemModelo.Instancia);

        resultado.Sinais.ShouldNotContain(sinal => sinal.Codigo == CodigoRegra.ModeloFraude);
        resultado.Modelo.ShouldBeNull();
        resultado.Challenger.ShouldBeNull();
    }

    [Fact]
    public void Pontuador_RecebeAsFeaturesDaAnalise()
    {
        var pontuador = new PontuadorFixo(ativa: 0.2f);

        var resultado = Decidir(pontuador);

        pontuador.Recebidas.ShouldNotBeNull().ShouldBe(resultado.Features);
    }

    [Fact]
    public void Analise_GuardaAsPontuacoesDoModeloEDoChallenger()
    {
        var resultado = Decidir(new PontuadorFixo(ativa: 0.6f, challenger: 0.2f));

        var analise = Analise.Registrar(Operacao(), resultado, Agora);

        analise.Modelo.ShouldBe(resultado.Modelo);
        analise.Challenger.ShouldBe(resultado.Challenger);
        analise.ScoreFinal.ShouldBe(30);
        analise.Decisao.ShouldBe(DecisaoAnalise.EmRevisao);
    }
}
