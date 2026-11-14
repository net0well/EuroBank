using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.Exceptions;
using EuroBank.Transferencias.Domain.Policies;
using EuroBank.Transferencias.Domain.ValueObjects;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

/// <summary>
/// O passo de risco logo antes do débito: AnalisandoRisco (a varredura chama o antifraude) e, se for para revisão,
/// EmAnaliseManual até a decisão do analista. Antifraude fora do ar até o prazo do passo: Recusada (fail-closed).
/// </summary>
public sealed class TransferenciaRiscoTestes
{
    private static readonly PoliticaSaga ComRisco = Politica with
    {
        AnaliseRiscoAtiva = true,
        PrazoAnaliseRisco = TimeSpan.FromSeconds(30),
        IntervaloAnaliseRisco = TimeSpan.FromSeconds(2),
        IntervaloConsultaRevisao = TimeSpan.FromMinutes(2),
    };

    private static readonly Guid Analise = Guid.CreateVersion7();

    private static readonly Guid Caso = Guid.CreateVersion7();

    private static ResultadoRisco Resultado(DecisaoRisco decisao, Guid? caso = null) => new(decisao, Analise, caso);

    private static Transferencia AnalisandoRisco()
    {
        var transferencia = NovaExterna();
        transferencia.Iniciar(Dinheiro.Zero, ComRisco, Agora);
        transferencia.LimparEventos();
        return transferencia;
    }

    private static Transferencia EmAnaliseManual()
    {
        var transferencia = AnalisandoRisco();
        transferencia.RegistrarResultadoRisco(Resultado(DecisaoRisco.EmRevisao, Caso), ComRisco, Agora);
        transferencia.LimparEventos();
        return transferencia;
    }

    [Fact]
    public void Iniciar_ComAnaliseDeRisco_EsperaOAntifraude_SemPedirODebito()
    {
        var transferencia = AnalisandoRisco();

        transferencia.Estado.ShouldBe(EstadoTransferencia.AnalisandoRisco);
        transferencia.PrazoDoPasso.ShouldBe(Agora);
        transferencia.TentativasNoPasso.ShouldBe(0);
        transferencia.Historico.Last().Descricao.ShouldContain("antifraude");
    }

    [Fact]
    public void Iniciar_SemAnaliseDeRisco_PedeODebitoComoAntes()
    {
        var transferencia = NovaExterna();

        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        transferencia.EventosDominio.OfType<DebitoSolicitado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Aprovada_PedeODebito()
    {
        var transferencia = AnalisandoRisco();

        transferencia.RegistrarResultadoRisco(Resultado(DecisaoRisco.Aprovada), ComRisco, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        transferencia.AnaliseRiscoId.ShouldBe(Analise);
        transferencia.CasoRevisaoId.ShouldBeNull();
        transferencia.EventosDominio.OfType<DebitoSolicitado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void Recusada_TerminaSemDebito()
    {
        var transferencia = AnalisandoRisco();

        transferencia.RegistrarResultadoRisco(Resultado(DecisaoRisco.Recusada), ComRisco, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.RecusadaPeloAntifraude);
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaRecusada>();
    }

    [Fact]
    public void EmRevisao_EsperaOAnalista_EConsultaDeNovoSeOEventoNaoChegar()
    {
        var transferencia = AnalisandoRisco();

        transferencia.RegistrarResultadoRisco(Resultado(DecisaoRisco.EmRevisao, Caso), ComRisco, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.EmAnaliseManual);
        transferencia.CasoRevisaoId.ShouldBe(Caso);
        transferencia.PrazoDoPasso.ShouldBe(Agora.AddMinutes(2));
        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void EmAnaliseManual_AindaEmRevisao_ConsultaDepoisDeNovo()
    {
        var transferencia = EmAnaliseManual();

        transferencia.RegistrarResultadoRisco(Resultado(DecisaoRisco.EmRevisao, Caso), ComRisco, Agora.AddMinutes(2));

        transferencia.Estado.ShouldBe(EstadoTransferencia.EmAnaliseManual);
        transferencia.PrazoDoPasso.ShouldBe(Agora.AddMinutes(4));
    }

    [Theory]
    [InlineData(DecisaoRisco.Recusada, MotivoTransferencia.RecusadaPeloAntifraude)]
    [InlineData(DecisaoRisco.Expirada, MotivoTransferencia.RevisaoExpirada)]
    public void DecisaoDoAnalistaOuExpiracao_TerminaRecusada(DecisaoRisco decisao, MotivoTransferencia motivo)
    {
        var transferencia = EmAnaliseManual();

        transferencia.RegistrarResultadoRisco(Resultado(decisao, Caso), ComRisco, Agora.AddMinutes(5));

        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(motivo);
        transferencia.PrazoDoPasso.ShouldBeNull();
    }

    [Fact]
    public void AnalistaAprova_PedeODebito()
    {
        var transferencia = EmAnaliseManual();

        transferencia.RegistrarResultadoRisco(Resultado(DecisaoRisco.Aprovada, Caso), ComRisco, Agora.AddMinutes(5));

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        transferencia.EventosDominio.OfType<DebitoSolicitado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void AntifraudeForaDoAr_TentaDeNovoAteOPrazo_DepoisRecusa()
    {
        var transferencia = AnalisandoRisco();

        transferencia.RegistrarAntifraudeIndisponivel("Unavailable", ComRisco, Agora.AddSeconds(10)).ShouldBe(ResultadoPasso.Aplicado);
        transferencia.Estado.ShouldBe(EstadoTransferencia.AnalisandoRisco);
        transferencia.TentativasNoPasso.ShouldBe(1);
        transferencia.PrazoDoPasso.ShouldBe(Agora.AddSeconds(12));

        transferencia.RegistrarAntifraudeIndisponivel("DeadlineExceeded", ComRisco, Agora.AddSeconds(30));
        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.AntifraudeIndisponivel);
        transferencia.EventosDominio.OfType<TransferenciaRecusada>().ShouldHaveSingleItem();
    }

    [Fact]
    public void AntifraudeForaDoAr_DuranteARevisao_SoConsultaDepois()
    {
        var transferencia = EmAnaliseManual();

        transferencia.RegistrarAntifraudeIndisponivel("Unavailable", ComRisco, Agora.AddHours(2));

        transferencia.Estado.ShouldBe(EstadoTransferencia.EmAnaliseManual);
        transferencia.PrazoDoPasso.ShouldBe(Agora.AddHours(2).AddMinutes(2));
    }

    [Fact]
    public void Cambio_AnaliseDepoisDaCotacao_ComOValorNaMoedaDaConta()
    {
        var transferencia = NovoCambioInterno();
        transferencia.Iniciar(Dinheiro.Zero, ComRisco, Agora);
        transferencia.Estado.ShouldBe(EstadoTransferencia.Cotando);

        CotarCambio(transferencia, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AnalisandoRisco);
        transferencia.ValorDebitado.ShouldBe(Dinheiro.De(93.53m, Moeda.EUR));
    }

    [Fact]
    public void Cambio_CotacaoVenceuDuranteARevisao_RecotaESegueSemReanalisar()
    {
        var transferencia = NovoCambioInterno();
        transferencia.Iniciar(Dinheiro.Zero, ComRisco, Agora);
        CotarCambio(transferencia, Agora);
        transferencia.RegistrarResultadoRisco(Resultado(DecisaoRisco.EmRevisao, Caso), ComRisco, Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarResultadoRisco(Resultado(DecisaoRisco.Aprovada, Caso), ComRisco, Agora.AddMinutes(15));

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cotando);
        transferencia.EventosDominio.OfType<CotacaoSolicitada>().ShouldHaveSingleItem();

        CotarCambio(transferencia, Agora.AddMinutes(16));
        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelamentoDuranteAAnalise_TerminaCanceladaSemDebito(bool emRevisao)
    {
        var transferencia = emRevisao ? EmAnaliseManual() : AnalisandoRisco();

        transferencia.SolicitarCancelamento(Agora.AddMinutes(1));

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.CanceladaPeloCliente);
        transferencia.PrazoDoPasso.ShouldBeNull();
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaCancelada>();
    }

    [Fact]
    public void DecisaoTardia_DepoisDoFim_EhIgnorada()
    {
        var transferencia = EmAnaliseManual();
        transferencia.SolicitarCancelamento(Agora);
        transferencia.LimparEventos();

        transferencia.RegistrarResultadoRisco(Resultado(DecisaoRisco.Aprovada, Caso), ComRisco, Agora.AddMinutes(1))
            .ShouldBe(ResultadoPasso.Ignorado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void AnaliseEmAndamento_OcupaOLimiteDiario()
    {
        TabelaTransferencias.EstadosQueConsomemLimite.ShouldContain(EstadoTransferencia.AnalisandoRisco);
        TabelaTransferencias.EstadosQueConsomemLimite.ShouldContain(EstadoTransferencia.EmAnaliseManual);
    }

    [Fact]
    public void LimiteExcedido_RecusaAntesDaAnalise()
    {
        var transferencia = NovaExterna(valor: 4_950m);

        transferencia.Iniciar(Dinheiro.De(100m), ComRisco, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.LimiteDiarioExcedido);
    }

    private static void CotarCambio(Transferencia transferencia, DateTimeOffset agora) =>
        transferencia.RegistrarCotacaoEmitida(
            new CotacaoTravada(Guid.CreateVersion7(), 1.0692m, agora.AddMinutes(10)),
            Dinheiro.De(93.53m, Moeda.EUR),
            Dinheiro.De(100m, Moeda.USD),
            Dinheiro.ZeroEm(Moeda.EUR),
            ComRisco,
            agora);
}
