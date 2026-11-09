using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.Exceptions;
using EuroBank.Transferencias.Domain.ValueObjects;
using static EuroBank.Transferencias.Domain.Tests.Entities.TransferenciaCenarios;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

public sealed class TransferenciaSolicitacaoTestes
{
    [Fact]
    public void SolicitarExterna_SemData_FicaAgendadaParaAgoraComATarifaDaTabela()
    {
        var transferencia = NovaExterna();

        transferencia.Id.Valor.Version.ShouldBe(7);
        transferencia.Tipo.ShouldBe(TipoTransferencia.Externa);
        transferencia.Estado.ShouldBe(EstadoTransferencia.Agendada);
        transferencia.Valor.ShouldBe(Dinheiro.De(100m));
        transferencia.Tarifa.ShouldBe(Dinheiro.De(2.50m));
        transferencia.ExecutarEm.ShouldBeNull();
        transferencia.ExecutarEmEfetivo.ShouldBe(Agora);
        transferencia.Motivo.ShouldBeNull();
        transferencia.Versao.ShouldBe(1);
        transferencia.Historico.ShouldHaveSingleItem().EstadoApos.ShouldBe(EstadoTransferencia.Agendada);
        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void SolicitarExterna_ComDataFutura_SoFicaNaHoraNaData()
    {
        var amanha = Agora.AddDays(1);

        var transferencia = NovaExterna(executarEm: amanha);

        transferencia.ExecutarEm.ShouldBe(amanha);
        transferencia.ExecutarEmEfetivo.ShouldBe(amanha);
        transferencia.EstaNaHora(Agora).ShouldBeFalse();
        transferencia.EstaNaHora(amanha).ShouldBeTrue();
    }

    [Fact]
    public void SolicitarExterna_ComDataPassada_ExecutaAgora()
    {
        var transferencia = NovaExterna(executarEm: Agora.AddHours(-1));

        transferencia.ExecutarEmEfetivo.ShouldBe(Agora);
    }

    [Fact]
    public void SolicitarExterna_ParaIbanEuroBank_Recusa()
    {
        Should.Throw<ExcecaoDestinoEuroBank>(() => NovaExterna(iban: "PT50EURO0000000000001"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void SolicitarExterna_SemBic_Recusa(string? bic)
    {
        Should.Throw<ExcecaoBicObrigatorio>(() => NovaExterna(bic: bic));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SolicitarExterna_SemBeneficiario_Recusa(string? beneficiario)
    {
        Should.Throw<ExcecaoBeneficiarioObrigatorio>(() => NovaExterna(beneficiario: beneficiario));
    }

    [Fact]
    public void SolicitarExterna_ValorZero_Recusa()
    {
        Should.Throw<ExcecaoValorInvalido>(() => NovaExterna(valor: 0m));
    }

    [Fact]
    public void Iniciar_NaHora_PedeODebitoDoValorComATarifa()
    {
        var transferencia = NovaExterna();

        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora).ShouldBe(ResultadoPasso.Aplicado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
        transferencia.TentativasNoPasso.ShouldBe(1);
        transferencia.PrazoDoPasso.ShouldBe(Agora + Politica.PrazoDebito);
        var debito = transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<DebitoSolicitado>();
        debito.TransferenciaId.ShouldBe(transferencia.Id);
        debito.ContaOrigemId.ShouldBe(ContaOrigem);
        debito.Valor.ShouldBe(Dinheiro.De(100m));
        debito.Tarifa.ShouldBe(Dinheiro.De(2.50m));
        debito.IbanDestino.ShouldBe(IbanComum);
        debito.NomeBeneficiario.ShouldBe("Bruno");
        debito.Tentativa.ShouldBe(1);
    }

    [Fact]
    public void Iniciar_AntesDaHora_EIgnorado()
    {
        var transferencia = NovaExterna(executarEm: Agora.AddHours(1));

        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Agendada);
        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void Iniciar_DuasVezes_PedeODebitoUmaVezSo()
    {
        var transferencia = AguardandoDebito();

        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora).ShouldBe(ResultadoPasso.Ignorado);

        transferencia.EventosDominio.ShouldBeEmpty();
    }

    [Fact]
    public void Iniciar_PassandoDoLimiteDiario_RecusaSemPedirDebito()
    {
        var transferencia = NovaExterna(valor: 100m);

        transferencia.Iniciar(Dinheiro.De(4_950m), Politica, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.Recusada);
        transferencia.Motivo.ShouldBe(MotivoTransferencia.LimiteDiarioExcedido);
        transferencia.PrazoDoPasso.ShouldBeNull();
        transferencia.EventosDominio.ShouldHaveSingleItem().ShouldBeOfType<TransferenciaRecusada>()
            .Motivo.ShouldBe(MotivoTransferencia.LimiteDiarioExcedido);
    }

    [Fact]
    public void Iniciar_ChegandoExatoNoLimite_PedeODebito_PorqueATarifaNaoConta()
    {
        var transferencia = NovaExterna(valor: 100m);

        transferencia.Iniciar(Dinheiro.De(4_900m), Politica, Agora);

        transferencia.Estado.ShouldBe(EstadoTransferencia.AguardandoDebito);
    }
}
