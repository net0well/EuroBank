using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

public sealed class TransferenciaPrazosTestes
{
    [Fact]
    public void PrazoAindaNaoVencido_NaoFazNada()
    {
        var transferencia = AguardandoLiquidacao();

        transferencia.VerificarPrazo(Politica, Agora + Politica.PrazoLiquidacao - TimeSpan.FromSeconds(1)).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.EventosDominio.ShouldBeEmpty();
        transferencia.TentativasNoPasso.ShouldBe(1);
    }

    [Fact]
    public void PrazoDaLiquidacao_ReenviaTresVezesEDepoisConsultaOBanco()
    {
        var transferencia = AguardandoLiquidacao();

        var fim = EsgotarPrazos(transferencia, Politica.PrazoLiquidacao);

        transferencia.EventosDominio.OfType<LiquidacaoSolicitada>().Select(evento => evento.Tentativa).ShouldBe([2, 3, 4]);
        transferencia.Estado.ShouldBe(EstadoTransferencia.ConsultandoLiquidacao);
        transferencia.TentativasNoPasso.ShouldBe(1);
        transferencia.PrazoDoPasso.ShouldBe(fim + Politica.PrazoConsulta);
        transferencia.EventosDominio.OfType<ConsultaLiquidacaoSolicitada>().ShouldHaveSingleItem().Tentativa.ShouldBe(1);
    }

    [Fact]
    public void ConsultasEsgotadas_PedemOEstornoSemRespostaDoBanco()
    {
        var transferencia = ConsultandoLiquidacao();
        var inicio = transferencia.PrazoDoPasso!.Value - Politica.PrazoConsulta;

        EsgotarPrazos(transferencia, Politica.PrazoConsulta, inicio);

        transferencia.EventosDominio.OfType<ConsultaLiquidacaoSolicitada>().Select(evento => evento.Tentativa).ShouldBe([2, 3, 4]);
        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoEstorno);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.SemRespostaDoBanco);
        transferencia.EventosDominio.OfType<EstornoSolicitado>().ShouldHaveSingleItem();
    }

    [Fact]
    public void PrazoDoEstorno_ReenviaSempre_PorqueACompensacaoPrecisaAcontecer()
    {
        var transferencia = AguardandoEstorno();
        var instante = Agora;

        for (var vez = 0; vez < 10; vez++)
        {
            instante += Politica.PrazoEstorno;
            transferencia.VerificarPrazo(Politica, instante).ShouldBe(ResultadoPasso.Aplicado);
        }

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoEstorno);
        transferencia.EventosDominio.OfType<EstornoSolicitado>().Count().ShouldBe(10);
        transferencia.TentativasNoPasso.ShouldBe(11);
    }

    [Fact]
    public void PrazoDoDebito_ReenviaTresVezesEDepoisRecusaSemRespostaDeContas()
    {
        var transferencia = AguardandoDebito();

        EsgotarPrazos(transferencia, Politica.PrazoDebito);

        transferencia.EventosDominio.OfType<DebitoSolicitado>().Select(evento => evento.Tentativa).ShouldBe([2, 3, 4]);
        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.SemRespostaDeContas);
        transferencia.DebitoSemResposta.ShouldBeTrue();
        transferencia.EventosDominio.OfType<TransferenciaRecusada>().ShouldHaveSingleItem();
    }

    [Fact]
    public void DebitoQueChegaDepoisDeDesistir_EEstornado()
    {
        var transferencia = AguardandoDebito();
        EsgotarPrazos(transferencia, Politica.PrazoDebito);
        transferencia.LimparEventos();

        transferencia.RegistrarDebitoRealizado(Politica, Agora.AddMinutes(10)).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoEstorno);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.SemRespostaDeContas);
        transferencia.DebitoSemResposta.ShouldBeFalse();
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<EstornoSolicitado>();

        transferencia.RegistrarEstornoRealizado(Agora.AddMinutes(11));

        transferencia.Estado.ShouldBe(EstadoTransferencia.Cancelada);
    }

    [Fact]
    public void RecusaQueChegaDepoisDeDesistir_SoConfirmaQueNadaFoiDebitado()
    {
        var transferencia = AguardandoDebito();
        EsgotarPrazos(transferencia, Politica.PrazoDebito);
        transferencia.LimparEventos();

        transferencia.RegistrarDebitoRecusado(MotivoTransferencia.SaldoInsuficiente, Agora.AddMinutes(10)).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.SemRespostaDeContas);
        transferencia.DebitoSemResposta.ShouldBeFalse();
        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void DebitoDuplicadoDepoisDoEstornoTardio_EIgnorado()
    {
        var transferencia = AguardandoDebito();
        EsgotarPrazos(transferencia, Politica.PrazoDebito);
        transferencia.RegistrarDebitoRealizado(Politica, Agora.AddMinutes(10));
        transferencia.LimparEventos();

        transferencia.RegistrarDebitoRealizado(Politica, Agora.AddMinutes(10)).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void EstadoFinalSemPrazo_NaoFazNada()
    {
        var transferencia = AguardandoLiquidacao();
        transferencia.RegistrarLiquidacaoConfirmada(Agora);
        transferencia.LimparEventos();

        transferencia.VerificarPrazo(Politica, Agora.AddDays(1)).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.EventosDominio.ShouldBeEmpty();
    }
}
