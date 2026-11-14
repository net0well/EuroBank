using EuroBank.Notificacoes.Worker.Messaging;
using EuroBank.Notificacoes.Worker.TransferObjects;

namespace EuroBank.Notificacoes.Worker.Tests.Messaging;

public sealed class TiposEventosTransferenciasTestes
{
    [Theory]
    [InlineData("TransferenciaConcluida", typeof(EventoTransferenciaConcluida))]
    [InlineData("TransferenciaCancelada", typeof(EventoTransferenciaCancelada))]
    [InlineData("TransferenciaRecusada", typeof(EventoTransferenciaRecusada))]
    [InlineData("TransferenciaDivergente", typeof(EventoTransferenciaDivergente))]
    [InlineData("TransferenciaDevolvida", typeof(EventoTransferenciaDevolvida))]
    public void Tabela_TemOsCincoEventosFinais(string tipo, Type esperado)
    {
        TiposEventosTransferencias.Mapa[tipo].ShouldBe(esperado);
    }

    [Fact]
    public void Topico_EhOsEventosDeTransferencias()
    {
        TiposEventosTransferencias.Topico.ShouldBe("eurobank.transferencias.eventos");
        TiposEventosTransferencias.Mapa.Count.ShouldBe(5);
    }
}
