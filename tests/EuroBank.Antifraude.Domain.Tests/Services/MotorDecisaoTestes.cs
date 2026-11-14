using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.ValueObjects;
using static EuroBank.Antifraude.Domain.Tests.Cenarios;

namespace EuroBank.Antifraude.Domain.Tests.Services;

/// <summary>Regras duras, regras com peso, modo sombra e limiares: abaixo de 30 aprova, até 69 revisão, 70 recusa.</summary>
public sealed class MotorDecisaoTestes
{
    private readonly MotorDecisao _motor = new();

    private ResultadoMotor Decidir(
        ContextoOperacao? operacao = null,
        PerfilConta? perfil = null,
        ListasRisco? listas = null,
        IReadOnlyList<Regra>? regras = null,
        ConfiguracaoDecisao? configuracao = null) =>
        _motor.Decidir(
            operacao ?? Operacao(),
            perfil ?? PerfilComHistorico(),
            regras ?? Regras(),
            listas ?? SemListas(),
            configuracao ?? Configuracao());

    private static SinalRegra Sinal(ResultadoMotor resultado, CodigoRegra codigo) =>
        resultado.Sinais.Single(sinal => sinal.Codigo == codigo);

    [Fact]
    public void OperacaoComum_DeContaComHistorico_Aprovada_SemNenhumaRegraDisparada()
    {
        var resultado = Decidir();

        resultado.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
        resultado.ScoreFinal.ShouldBe(0);
        resultado.Sinais.ShouldAllBe(sinal => !sinal.Disparou);
        resultado.VersaoConfiguracao.ShouldBe(1);
    }

    [Fact]
    public void IbanNaListaDeBloqueio_RecusadaPelaRegraDura()
    {
        var resultado = Decidir(listas: Listas(bloqueados: [IbanConhecido]));

        resultado.Decisao.ShouldBe(DecisaoAnalise.Recusada);
        var sinal = Sinal(resultado, CodigoRegra.ListaBloqueio);
        sinal.Disparou.ShouldBeTrue();
        sinal.Pontos.ShouldBe(0);
        sinal.Motivo.ShouldContain("bloqueio");
    }

    [Theory]
    [InlineData(600, true)]
    [InlineData(500, false)]
    public void ValorAcimaDeCincoVezesAMedia_Pontua25(decimal valor, bool dispara)
    {
        var resultado = Decidir(Operacao(valor));

        Sinal(resultado, CodigoRegra.ValorAcimaDaMedia).Disparou.ShouldBe(dispara);
        resultado.ScoreFinal.ShouldBe(dispara ? 25 : 0);
        resultado.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
    }

    [Fact]
    public void ValorAcimaDaMedia_SemTresSaidasNoHistorico_NaoDispara()
    {
        var perfil = new PerfilConta(
            Conta,
            Agora.AddDays(-90),
            5_000m,
            [new SaidaConta(Agora.AddDays(-1), 10m, IbanConhecido), new SaidaConta(Agora.AddDays(-2), 10m, IbanConhecido)],
            new HashSet<string> { IbanConhecido });

        Sinal(Decidir(Operacao(1_000m), perfil), CodigoRegra.ValorAcimaDaMedia).Disparou.ShouldBeFalse();
    }

    [Fact]
    public void TresSaidasEmDezMinutos_ContandoAAtual_PontuaVelocidade()
    {
        var perfil = PerfilComHistorico(saidasExtras:
        [
            new SaidaConta(Agora.AddMinutes(-3), 100m, IbanConhecido),
            new SaidaConta(Agora.AddMinutes(-8), 100m, IbanConhecido),
        ]);

        var resultado = Decidir(perfil: perfil);

        Sinal(resultado, CodigoRegra.Velocidade).Disparou.ShouldBeTrue();
        Sinal(resultado, CodigoRegra.Velocidade).Pontos.ShouldBe(20);
    }

    [Fact]
    public void DuasSaidasEmDezMinutos_ContandoAAtual_NaoPontuaVelocidade()
    {
        var perfil = PerfilComHistorico(saidasExtras:
        [
            new SaidaConta(Agora.AddMinutes(-3), 100m, IbanConhecido),
            new SaidaConta(Agora.AddMinutes(-11), 100m, IbanConhecido),
        ]);

        Sinal(Decidir(perfil: perfil), CodigoRegra.Velocidade).Disparou.ShouldBeFalse();
    }

    [Theory]
    [InlineData(900, true)]
    [InlineData(899.99, false)]
    public void NoventaPorCentoDoSaldo_PontuaEsvaziamento(decimal valor, bool dispara)
    {
        Sinal(Decidir(Operacao(valor), PerfilComHistorico(saldo: 1_000m)), CodigoRegra.Esvaziamento).Disparou.ShouldBe(dispara);
    }

    [Fact]
    public void IbanNuncaUsadoPelaConta_PontuaBeneficiarioNovo()
    {
        var resultado = Decidir(Operacao(iban: IbanNovo));

        Sinal(resultado, CodigoRegra.BeneficiarioNovo).Pontos.ShouldBe(10);
        resultado.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
    }

    [Fact]
    public void SaqueSemIban_NaoAvaliaBeneficiarioNovo()
    {
        var resultado = Decidir(Operacao(iban: null, tipo: TipoOperacao.Saque));

        Sinal(resultado, CodigoRegra.BeneficiarioNovo).Disparou.ShouldBeFalse();
    }

    [Theory]
    [InlineData(9.99, 3, true)]
    [InlineData(500, 3, true)]
    [InlineData(9.99, 2, true)]
    [InlineData(500, 2, false)]
    [InlineData(10, 2, false)]
    public void TresValoresAbaixoDeDezEmQuinzeMinutos_PontuaTesteDeCartao(decimal valorAtual, int pequenasAnteriores, bool dispara)
    {
        var pequenas = Enumerable.Range(1, pequenasAnteriores)
            .Select(i => new SaidaConta(Agora.AddMinutes(-4 * i), 5m, IbanNovo));

        var resultado = Decidir(Operacao(valorAtual), PerfilComHistorico(saidasExtras: pequenas));

        Sinal(resultado, CodigoRegra.TesteDeCartao).Disparou.ShouldBe(dispara);
    }

    [Theory]
    [InlineData(1_000, true)]
    [InlineData(999.99, false)]
    public void ContaComMenosDeSeteDias_TransferindoMilOuMais_PontuaContaRecemAberta(decimal valor, bool dispara)
    {
        var perfil = PerfilComHistorico(abertaEm: Agora.AddDays(-6));

        Sinal(Decidir(Operacao(valor), perfil), CodigoRegra.ContaRecemAberta).Disparou.ShouldBe(dispara);
    }

    [Fact]
    public void ContaSemPerfil_EhAvaliadaComoNova()
    {
        var resultado = Decidir(Operacao(1_000m, IbanNovo), PerfilConta.Vazio(Conta));

        Sinal(resultado, CodigoRegra.ContaRecemAberta).Disparou.ShouldBeTrue();
        Sinal(resultado, CodigoRegra.BeneficiarioNovo).Disparou.ShouldBeTrue();
        Sinal(resultado, CodigoRegra.HorarioIncomum).Disparou.ShouldBeFalse();
        Sinal(resultado, CodigoRegra.ValorAcimaDaMedia).Disparou.ShouldBeFalse();
        Sinal(resultado, CodigoRegra.Esvaziamento).Disparou.ShouldBeFalse();
        resultado.ScoreFinal.ShouldBe(30);
        resultado.Decisao.ShouldBe(DecisaoAnalise.EmRevisao);
    }

    [Fact]
    public void HoraSemSaidasNoHistorico_PontuaHorarioIncomum()
    {
        var madrugada = new DateTimeOffset(2026, 11, 14, 3, 0, 0, TimeSpan.Zero);

        Sinal(Decidir(Operacao(momento: madrugada)), CodigoRegra.HorarioIncomum).Pontos.ShouldBe(10);
    }

    [Fact]
    public void PaisDeRiscoMaisBeneficiarioNovo_VaiParaRevisao()
    {
        var resultado = Decidir(Operacao(iban: IbanNovo), listas: Listas(paises: ["FR"]));

        Sinal(resultado, CodigoRegra.PaisDeRisco).Pontos.ShouldBe(30);
        resultado.ScoreFinal.ShouldBe(40);
        resultado.Decisao.ShouldBe(DecisaoAnalise.EmRevisao);
    }

    [Fact]
    public void SetentaPontosOuMais_Recusada()
    {
        // Conta de 2 dias sem saídas: país de risco 30 + beneficiário novo 10 + esvaziamento 25 + conta recém-aberta 20 = 85.
        var perfil = new PerfilConta(Conta, Agora.AddDays(-2), 1_000m, [], new HashSet<string>());

        var resultado = Decidir(Operacao(1_000m, IbanNovo), perfil, Listas(paises: ["FR"]));

        resultado.ScoreFinal.ShouldBe(85);
        resultado.Decisao.ShouldBe(DecisaoAnalise.Recusada);
    }

    [Fact]
    public void ScoreFinal_NuncaPassaDeCem()
    {
        var regras = Regras();
        foreach (var regra in regras.Where(regra => regra.Tipo == TipoRegra.Peso))
        {
            regra.Ajustar(60, ativa: true, sombra: false, parametros: null, Agora);
        }

        var resultado = Decidir(Operacao(1_000m, IbanNovo), PerfilComHistorico(saldo: 1_000m, abertaEm: Agora.AddDays(-2)),
            Listas(paises: ["FR"]), regras);

        resultado.ScoreFinal.ShouldBe(100);
    }

    [Fact]
    public void RegraEmModoSombra_AparecenosSinais_MasNaoSomaPontos()
    {
        var regras = Regras();
        regras.Single(regra => regra.Codigo == CodigoRegra.PaisDeRisco).Ajustar(30, ativa: true, sombra: true, parametros: null, Agora);

        var resultado = Decidir(Operacao(iban: IbanNovo), listas: Listas(paises: ["FR"]), regras: regras);

        var sombra = Sinal(resultado, CodigoRegra.PaisDeRisco);
        sombra.Disparou.ShouldBeTrue();
        sombra.Sombra.ShouldBeTrue();
        sombra.Pontos.ShouldBe(30);
        resultado.ScoreFinal.ShouldBe(10);
        resultado.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
    }

    [Fact]
    public void RegraDuraEmModoSombra_NaoRecusa()
    {
        var regras = Regras();
        regras.Single(regra => regra.Codigo == CodigoRegra.ListaBloqueio).Ajustar(0, ativa: true, sombra: true, parametros: null, Agora);

        var resultado = Decidir(listas: Listas(bloqueados: [IbanConhecido]), regras: regras);

        Sinal(resultado, CodigoRegra.ListaBloqueio).Disparou.ShouldBeTrue();
        resultado.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
    }

    [Fact]
    public void RegraInativa_NemEhAvaliada()
    {
        var regras = Regras();
        regras.Single(regra => regra.Codigo == CodigoRegra.BeneficiarioNovo).Ajustar(10, ativa: false, sombra: false, parametros: null, Agora);

        var resultado = Decidir(Operacao(iban: IbanNovo), regras: regras);

        resultado.Sinais.ShouldNotContain(sinal => sinal.Codigo == CodigoRegra.BeneficiarioNovo);
        resultado.ScoreFinal.ShouldBe(0);
    }

    [Fact]
    public void NovoLimiarDeRevisao_MudaADecisaoDaProximaAvaliacao()
    {
        var configuracao = Configuracao().NovaVersao(limiarRevisao: 10, limiarRecusa: 70, pesoModelo: 0, Agora);

        var resultado = Decidir(Operacao(iban: IbanNovo), configuracao: configuracao);

        resultado.Decisao.ShouldBe(DecisaoAnalise.EmRevisao);
        resultado.VersaoConfiguracao.ShouldBe(2);
    }

    [Fact]
    public void NovoPesoDaRegra_MudaADecisaoDaProximaAvaliacao()
    {
        var regras = Regras();
        regras.Single(regra => regra.Codigo == CodigoRegra.BeneficiarioNovo).Ajustar(30, ativa: true, sombra: false, parametros: null, Agora);

        Decidir(Operacao(iban: IbanNovo), regras: regras).Decisao.ShouldBe(DecisaoAnalise.EmRevisao);
    }

    [Fact]
    public void NovoParametroDaRegra_MudaADecisaoDaProximaAvaliacao()
    {
        var regras = Regras();
        regras.Single(regra => regra.Codigo == CodigoRegra.ValorAcimaDaMedia)
            .Ajustar(25, ativa: true, sombra: false, new Dictionary<string, decimal> { ["multiplicador"] = 2m }, Agora);

        Sinal(Decidir(Operacao(300m), regras: regras), CodigoRegra.ValorAcimaDaMedia).Disparou.ShouldBeTrue();
    }
}
