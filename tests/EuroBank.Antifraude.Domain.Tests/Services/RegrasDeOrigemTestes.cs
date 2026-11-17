using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Events;
using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Shared.ValueObjects;
using static EuroBank.Antifraude.Domain.Tests.Cenarios;

namespace EuroBank.Antifraude.Domain.Tests.Services;

/// <summary>
/// Dispositivo novo, país diferente do habitual e viagem impossível, contra o que a conta fez em operações aprovadas.
/// </summary>
public sealed class RegrasDeOrigemTestes
{
    private static readonly Localizacao Paris = new(48.8566, 2.3522, "FR");
    private static readonly Localizacao Lyon = new(45.7640, 4.8357, "FR");
    private static readonly Localizacao Londres = new(51.5074, -0.1278, "GB");
    private static readonly Localizacao SaoPaulo = new(-23.5505, -46.6333, "BR");

    private readonly MotorDecisao _motor = new();

    private static PerfilConta Perfil(IEnumerable<string>? dispositivos = null, params LocalizacaoConta[] localizacoes)
    {
        var historico = PerfilComHistorico();
        return new PerfilConta(
            historico.ContaId,
            historico.AbertaEm,
            historico.Saldo,
            historico.Saidas,
            historico.BeneficiariosConhecidos,
            historico.Entradas,
            new HashSet<string>(dispositivos ?? []),
            localizacoes);
    }

    private static LocalizacaoConta Em(Localizacao localizacao, TimeSpan antes) => new(Agora - antes, localizacao);

    private ResultadoMotor Decidir(PerfilConta perfil, string? dispositivo = null, Localizacao? localizacao = null) =>
        _motor.Decidir(Operacao() with { DispositivoId = dispositivo, Localizacao = localizacao }, perfil, Regras(), SemListas(), Configuracao());

    private static SinalRegra Sinal(ResultadoMotor resultado, CodigoRegra codigo) => resultado.Sinais.Single(sinal => sinal.Codigo == codigo);

    [Fact]
    public void DispositivoNuncaUsado_Pontua15()
    {
        var resultado = Decidir(Perfil(["celular-da-ana"]), dispositivo: "notebook-desconhecido");

        var sinal = Sinal(resultado, CodigoRegra.DispositivoNovo);
        sinal.Disparou.ShouldBeTrue();
        sinal.Pontos.ShouldBe(15);
        sinal.Motivo.ShouldContain("notebook-desconhecido");
        resultado.ScoreFinal.ShouldBe(15);
    }

    [Theory]
    [InlineData("celular-da-ana")]
    [InlineData(null)]
    public void DispositivoConhecidoOuNaoInformado_NaoDispara(string? dispositivo)
    {
        Sinal(Decidir(Perfil(["celular-da-ana"]), dispositivo), CodigoRegra.DispositivoNovo).Disparou.ShouldBeFalse();
    }

    [Fact]
    public void ContaSemDispositivoConhecido_NaoHaReferencia()
    {
        var sinal = Sinal(Decidir(Perfil(), dispositivo: "qualquer"), CodigoRegra.DispositivoNovo);

        sinal.Disparou.ShouldBeFalse();
        sinal.Motivo.ShouldContain("ainda não há referência");
    }

    [Fact]
    public void ForaDoPaisHabitual_Pontua15()
    {
        var perfil = Perfil(null, Em(Paris, TimeSpan.FromDays(3)), Em(Lyon, TimeSpan.FromDays(2)), Em(Paris, TimeSpan.FromDays(1)));

        var sinal = Sinal(Decidir(perfil, localizacao: Londres), CodigoRegra.PaisDiferenteDoHabitual);

        sinal.Disparou.ShouldBeTrue();
        sinal.Motivo.ShouldBe("Operação em GB, país habitual FR");
    }

    [Fact]
    public void PoucasOperacoesOuAntigas_SemHabito()
    {
        var poucas = Perfil(null, Em(Paris, TimeSpan.FromDays(2)), Em(Paris, TimeSpan.FromDays(1)));
        var antigas = Perfil(null, Em(Paris, TimeSpan.FromDays(40)), Em(Paris, TimeSpan.FromDays(35)), Em(Paris, TimeSpan.FromDays(31)));

        Sinal(Decidir(poucas, localizacao: Londres), CodigoRegra.PaisDiferenteDoHabitual).Disparou.ShouldBeFalse();
        Sinal(Decidir(antigas, localizacao: Londres), CodigoRegra.PaisDiferenteDoHabitual).Disparou.ShouldBeFalse();
    }

    [Fact]
    public void ParisESaoPauloEmUmaHora_ViagemImpossivel_Revisao()
    {
        var resultado = Decidir(Perfil(null, Em(Paris, TimeSpan.FromHours(1))), localizacao: SaoPaulo);

        var sinal = Sinal(resultado, CodigoRegra.ViagemImpossivel);
        sinal.Disparou.ShouldBeTrue();
        sinal.Tipo.ShouldBe(TipoRegra.Dura);
        sinal.Motivo.ShouldContain("de FR a BR em 60 min");
        resultado.ScoreFinal.ShouldBe(0);
        resultado.Decisao.ShouldBe(DecisaoAnalise.EmRevisao);
    }

    [Fact]
    public void ParisELondresEmUmaHora_Possivel()
    {
        var resultado = Decidir(Perfil(null, Em(Paris, TimeSpan.FromHours(1))), localizacao: Londres);

        Sinal(resultado, CodigoRegra.ViagemImpossivel).Disparou.ShouldBeFalse();
        resultado.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
    }

    [Fact]
    public void PoucosQuilometrosLogoDepois_VariacaoDoGps_NaoDispara()
    {
        var aoLado = new Localizacao(48.90, 2.40, "FR");

        Sinal(Decidir(Perfil(null, Em(Paris, TimeSpan.FromSeconds(30))), localizacao: aoLado), CodigoRegra.ViagemImpossivel)
            .Disparou.ShouldBeFalse();
    }

    [Fact]
    public void SemLocalizacaoAnterior_NaoDispara()
    {
        Sinal(Decidir(Perfil(), localizacao: SaoPaulo), CodigoRegra.ViagemImpossivel).Motivo.ShouldBe("Sem localização anterior da conta");
    }

    [Fact]
    public void PaisHabitual_EmpateFicaOMaisRecente()
    {
        var perfil = Perfil(null, Em(Paris, TimeSpan.FromDays(4)), Em(Londres, TimeSpan.FromDays(3)), Em(Paris, TimeSpan.FromDays(2)), Em(Londres, TimeSpan.FromDays(1)));

        perfil.PaisHabitual(Agora, TimeSpan.FromDays(30), 3).ShouldBe("GB");
    }

    [Fact]
    public void Analise_GuardaAOrigem_EODecididoLevaParaOPerfil()
    {
        var operacao = Operacao() with { DispositivoId = "celular-da-ana", Localizacao = Paris };

        var analise = Analise.Registrar(operacao, _motor.Decidir(operacao, PerfilComHistorico(), Regras(), SemListas(), Configuracao()), Agora);

        analise.Contexto.ShouldBe(operacao);
        var evento = analise.EventosDominio.OfType<AnaliseDecidida>().ShouldHaveSingleItem();
        evento.DispositivoId.ShouldBe("celular-da-ana");
        evento.Localizacao.ShouldBe(Paris);
        evento.MomentoOperacao.ShouldBe(operacao.Momento);
    }
}
