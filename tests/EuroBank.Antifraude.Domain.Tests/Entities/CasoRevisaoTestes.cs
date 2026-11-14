using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Events;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.ValueObjects;
using static EuroBank.Antifraude.Domain.Tests.Cenarios;

namespace EuroBank.Antifraude.Domain.Tests.Entities;

/// <summary>O caso de revisão: o analista assume, decide com justificativa, e o que ninguém decide expira.</summary>
public sealed class CasoRevisaoTestes
{
    private static readonly TimeSpan Prazo = TimeSpan.FromMinutes(30);

    private static CasoRevisao Aberto()
    {
        var caso = CasoRevisao.Abrir(AnaliseId.Nova(), Guid.CreateVersion7(), Conta, 40, Prazo, Agora);
        caso.LimparEventos();
        return caso;
    }

    private static CasoRevisao AssumidoPorMarta()
    {
        var caso = Aberto();
        caso.Assumir("Marta", Agora.AddMinutes(1));
        return caso;
    }

    [Fact]
    public void Abrir_ComPrazoDeExpiracao_PublicaCasoAberto()
    {
        var analiseId = AnaliseId.Nova();
        var operacaoId = Guid.CreateVersion7();

        var caso = CasoRevisao.Abrir(analiseId, operacaoId, Conta, 40, Prazo, Agora);

        caso.Estado.ShouldBe(EstadoCaso.Aberto);
        caso.ExpiraEm.ShouldBe(Agora.AddMinutes(30));
        caso.Analista.ShouldBeNull();
        caso.Auditoria.ShouldHaveSingleItem().Acao.ShouldBe("Caso aberto");
        var evento = caso.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<CasoAberto>();
        evento.CasoId.ShouldBe(caso.Id);
        evento.AnaliseId.ShouldBe(analiseId);
        evento.OperacaoId.ShouldBe(operacaoId);
        evento.ScoreFinal.ShouldBe(40);
        evento.ExpiraEm.ShouldBe(caso.ExpiraEm);
    }

    [Fact]
    public void Assumir_ColocaEmAnaliseComONomeDoAnalista()
    {
        var caso = AssumidoPorMarta();

        caso.Estado.ShouldBe(EstadoCaso.EmAnalise);
        caso.Analista.ShouldBe("Marta");
        caso.AssumidoEm.ShouldBe(Agora.AddMinutes(1));
        caso.Auditoria.Last().Quem.ShouldBe("Marta");
    }

    [Fact]
    public void Assumir_DeNovoPeloMesmoAnalista_NaoMudaNada()
    {
        var caso = AssumidoPorMarta();

        caso.Assumir("Marta", Agora.AddMinutes(2));

        caso.AssumidoEm.ShouldBe(Agora.AddMinutes(1));
        caso.Auditoria.Count.ShouldBe(2);
    }

    [Fact]
    public void Assumir_CasoDeOutroAnalista_EhConflito()
    {
        var caso = AssumidoPorMarta();

        Should.Throw<ExcecaoCasoDeOutroAnalista>(() => caso.Assumir("Paulo", Agora.AddMinutes(2)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Assumir_SemNome_EhInvalido(string analista)
    {
        Should.Throw<ExcecaoAnalistaObrigatorio>(() => Aberto().Assumir(analista, Agora));
    }

    [Theory]
    [InlineData(true, EstadoCaso.Aprovado)]
    [InlineData(false, EstadoCaso.Recusado)]
    public void Decidir_ComJustificativa_EncerraOCaso(bool aprovar, EstadoCaso esperado)
    {
        var caso = AssumidoPorMarta();

        caso.Decidir("Marta", aprovar, "Conferido com o cliente", bloquearIban: false, Agora.AddMinutes(5));

        caso.Estado.ShouldBe(esperado);
        caso.Justificativa.ShouldBe("Conferido com o cliente");
        caso.DecididoEm.ShouldBe(Agora.AddMinutes(5));
        caso.BloquearIban.ShouldBeFalse();
        caso.Auditoria.Last().Detalhe!.ShouldContain("Conferido com o cliente");
    }

    [Fact]
    public void Recusar_PodeBloquearOIban()
    {
        var caso = AssumidoPorMarta();

        caso.Decidir("Marta", aprovar: false, "Mula conhecida", bloquearIban: true, Agora.AddMinutes(5));

        caso.BloquearIban.ShouldBeTrue();
    }

    [Fact]
    public void Aprovar_ComBloqueioDeIban_EhInvalido()
    {
        Should.Throw<ExcecaoBloqueioSemRecusa>(() =>
            AssumidoPorMarta().Decidir("Marta", aprovar: true, "ok", bloquearIban: true, Agora.AddMinutes(5)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Decidir_SemJustificativa_EhInvalido(string justificativa)
    {
        Should.Throw<ExcecaoJustificativaObrigatoria>(() =>
            AssumidoPorMarta().Decidir("Marta", aprovar: true, justificativa, bloquearIban: false, Agora.AddMinutes(5)));
    }

    [Fact]
    public void Decidir_SemAssumirAntes_NaoEhPermitido()
    {
        Should.Throw<ExcecaoCasoNaoAssumido>(() =>
            Aberto().Decidir("Marta", aprovar: true, "ok", bloquearIban: false, Agora.AddMinutes(5)));
    }

    [Fact]
    public void Decidir_CasoDeOutroAnalista_EhConflito()
    {
        Should.Throw<ExcecaoCasoDeOutroAnalista>(() =>
            AssumidoPorMarta().Decidir("Paulo", aprovar: true, "ok", bloquearIban: false, Agora.AddMinutes(5)));
    }

    [Fact]
    public void Decidir_DepoisDoPrazo_EhConflito_MesmoSemOWorkerTerExpirado()
    {
        Should.Throw<ExcecaoCasoExpirado>(() =>
            AssumidoPorMarta().Decidir("Marta", aprovar: true, "ok", bloquearIban: false, Agora.AddMinutes(30)));
    }

    [Fact]
    public void Decidir_CasoJaEncerrado_EhConflito()
    {
        var caso = AssumidoPorMarta();
        caso.Decidir("Marta", aprovar: true, "ok", bloquearIban: false, Agora.AddMinutes(5));

        Should.Throw<ExcecaoCasoEncerrado>(() =>
            caso.Decidir("Marta", aprovar: false, "mudei de ideia", bloquearIban: false, Agora.AddMinutes(6)));
        Should.Throw<ExcecaoCasoEncerrado>(() => caso.Assumir("Marta", Agora.AddMinutes(6)));
    }

    [Fact]
    public void Expirar_AntesDoPrazo_NaoMudaNada()
    {
        var caso = AssumidoPorMarta();

        caso.Expirar(Agora.AddMinutes(29)).ShouldBeFalse();

        caso.Estado.ShouldBe(EstadoCaso.EmAnalise);
    }

    [Fact]
    public void Expirar_NoPrazo_FechaComoExpirado()
    {
        var caso = AssumidoPorMarta();

        caso.Expirar(Agora.AddMinutes(30)).ShouldBeTrue();

        caso.Estado.ShouldBe(EstadoCaso.Expirado);
        caso.Auditoria.Last().Quem.ShouldBe("sistema");
    }

    [Fact]
    public void CancelamentoPeloCliente_TiraDaFila()
    {
        var caso = AssumidoPorMarta();

        caso.CancelarPeloCliente(Agora.AddMinutes(3)).ShouldBeTrue();

        caso.Estado.ShouldBe(EstadoCaso.CanceladoPeloCliente);
        caso.EstaPendente.ShouldBeFalse();
    }

    [Fact]
    public void ExpirarOuCancelar_CasoJaDecidido_NaoMudaNada()
    {
        var caso = AssumidoPorMarta();
        caso.Decidir("Marta", aprovar: true, "ok", bloquearIban: false, Agora.AddMinutes(5));

        caso.Expirar(Agora.AddHours(1)).ShouldBeFalse();
        caso.CancelarPeloCliente(Agora.AddHours(1)).ShouldBeFalse();

        caso.Estado.ShouldBe(EstadoCaso.Aprovado);
    }

    [Fact]
    public void Auditoria_RegistraQuemFezOQueEQuando()
    {
        var caso = AssumidoPorMarta();
        caso.Decidir("Marta", aprovar: false, "Padrão de mula", bloquearIban: true, Agora.AddMinutes(5));

        caso.Auditoria.Select(registro => (registro.Quem, registro.Acao)).ShouldBe(
        [
            ("sistema", "Caso aberto"),
            ("Marta", "Caso assumido"),
            ("Marta", "Recusado"),
        ]);
        caso.Auditoria.Select(registro => registro.Quando).ShouldBe([Agora, Agora.AddMinutes(1), Agora.AddMinutes(5)]);
    }
}
