using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Infrastructure.Messaging;

namespace EuroBank.Antifraude.Worker.Tests.Messaging;

/// <summary>O perfil lê os eventos de Contas por forma: abertura, saídas, entradas, demais movimentos de saldo e status.</summary>
public sealed class TiposEventosContasTestes
{
    [Theory]
    [InlineData("ContaAberta", typeof(EventoContaAberta))]
    [InlineData("TransferenciaEnviada", typeof(EventoSaidaConta))]
    [InlineData("TransferenciaExternaEnviada", typeof(EventoSaidaConta))]
    [InlineData("TransferenciaCambioEnviada", typeof(EventoSaidaConta))]
    [InlineData("DepositoRealizado", typeof(EventoEntradaConta))]
    [InlineData("SaqueRealizado", typeof(EventoMovimentoConta))]
    [InlineData("TransferenciaRecebida", typeof(EventoEntradaConta))]
    [InlineData("TransferenciaCambioRecebida", typeof(EventoEntradaConta))]
    [InlineData("EstornoTransferencia", typeof(EventoMovimentoConta))]
    [InlineData("DevolucaoTransferenciaExterna", typeof(EventoMovimentoConta))]
    [InlineData("DebitoReconciliacao", typeof(EventoMovimentoConta))]
    [InlineData("ContaBloqueada", typeof(EventoStatusConta))]
    [InlineData("ContaDesbloqueada", typeof(EventoStatusConta))]
    [InlineData("ContaEncerrada", typeof(EventoStatusConta))]
    public void CadaEventoDeContas_TemUmaForma(string tipo, Type esperado)
    {
        TiposEventosContas.Mapa[tipo].ShouldBe(esperado);
    }

    [Fact]
    public void Topico_EhOsEventosDeContas()
    {
        TiposEventosContas.Topico.ShouldBe("eurobank.contas.eventos");
        TiposEventosContas.Mapa.Count.ShouldBe(14);
    }
}
