using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Events;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.ValueObjects;
using static EuroBank.Antifraude.Domain.Tests.Cenarios;

namespace EuroBank.Antifraude.Domain.Tests.Entities;

/// <summary>A análise guarda a decisão do motor e, quando vai para revisão, a decisão final.</summary>
public sealed class AnaliseTestes
{
    private static Analise Registrar(ContextoOperacao operacao, ListasRisco? listas = null)
    {
        var resultado = new MotorDecisao().Decidir(operacao, PerfilComHistorico(), Regras(), listas ?? SemListas(), Configuracao());
        return Analise.Registrar(operacao, resultado, Agora);
    }

    private static Analise EmRevisao()
    {
        var analise = Registrar(Operacao(iban: IbanNovo), Listas(paises: ["FR"]));
        analise.Decisao.ShouldBe(DecisaoAnalise.EmRevisao);
        return analise;
    }

    [Fact]
    public void Aprovada_PeloMotor_PublicaAnaliseDecidida()
    {
        var operacao = Operacao();

        var analise = Registrar(operacao);

        analise.OperacaoId.ShouldBe(operacao.OperacaoId);
        analise.Contexto.ShouldBe(operacao);
        analise.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
        analise.DecididaEm.ShouldBe(Agora);
        var evento = analise.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<AnaliseDecidida>();
        evento.AnaliseId.ShouldBe(analise.Id);
        evento.OperacaoId.ShouldBe(operacao.OperacaoId);
        evento.ContaId.ShouldBe(Conta);
        evento.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
        evento.Origem.ShouldBe(OrigemDecisao.Motor);
        evento.Analista.ShouldBeNull();
    }

    [Fact]
    public void Recusada_PeloMotor_GuardaOsSinaisEPublica()
    {
        var analise = Registrar(Operacao(), Listas(bloqueados: [IbanConhecido]));

        analise.Decisao.ShouldBe(DecisaoAnalise.Recusada);
        analise.Sinais.ShouldContain(sinal => sinal.Codigo == CodigoRegra.ListaBloqueio && sinal.Disparou);
        analise.VersaoConfiguracao.ShouldBe(1);
        analise.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<AnaliseDecidida>().Decisao.ShouldBe(DecisaoAnalise.Recusada);
    }

    [Fact]
    public void EmRevisao_NaoPublicaDecisaoAinda()
    {
        var analise = EmRevisao();

        analise.ScoreFinal.ShouldBe(40);
        analise.DecididaEm.ShouldBeNull();
        analise.EventosDominio.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true, DecisaoAnalise.Aprovada)]
    [InlineData(false, DecisaoAnalise.Recusada)]
    public void DecisaoDoAnalista_FechaAAnalise(bool aprovar, DecisaoAnalise esperada)
    {
        var analise = EmRevisao();

        analise.AplicarRevisao(aprovar, "Marta", "Cliente confirmou por telefone", Agora.AddMinutes(5));

        analise.Decisao.ShouldBe(esperada);
        analise.DecididaEm.ShouldBe(Agora.AddMinutes(5));
        var evento = analise.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<AnaliseDecidida>();
        evento.Decisao.ShouldBe(esperada);
        evento.Origem.ShouldBe(OrigemDecisao.Analista);
        evento.Analista.ShouldBe("Marta");
        evento.Justificativa.ShouldBe("Cliente confirmou por telefone");
    }

    [Fact]
    public void Revisao_DeAnaliseQueNaoEstaEmRevisao_EhConflito()
    {
        var analise = Registrar(Operacao());

        Should.Throw<ExcecaoRevisaoNaoPendente>(() => analise.AplicarRevisao(true, "Marta", "ok", Agora));
    }

    [Fact]
    public void Expiracao_FechaComoExpirada()
    {
        var analise = EmRevisao();

        analise.Expirar(Agora.AddMinutes(30)).ShouldBeTrue();

        analise.Decisao.ShouldBe(DecisaoAnalise.Expirada);
        analise.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<AnaliseDecidida>().Origem.ShouldBe(OrigemDecisao.Expiracao);
    }

    [Fact]
    public void CancelamentoPeloCliente_FechaComoCancelada()
    {
        var analise = EmRevisao();

        analise.Cancelar(Agora.AddMinutes(2)).ShouldBeTrue();

        analise.Decisao.ShouldBe(DecisaoAnalise.Cancelada);
        analise.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<AnaliseDecidida>().Origem.ShouldBe(OrigemDecisao.Cliente);
    }

    [Fact]
    public void ExpirarOuCancelar_DepoisDeDecidida_NaoMudaNada()
    {
        var analise = EmRevisao();
        analise.AplicarRevisao(true, "Marta", "ok", Agora);
        analise.LimparEventos();

        analise.Expirar(Agora.AddHours(1)).ShouldBeFalse();
        analise.Cancelar(Agora.AddHours(1)).ShouldBeFalse();

        analise.Decisao.ShouldBe(DecisaoAnalise.Aprovada);
        analise.EventosDominio.ShouldBeEmpty();
    }
}
