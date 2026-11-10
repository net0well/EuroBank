using EuroBank.Simuladores.Comum;

namespace EuroBank.Simuladores.Cambio.Servicos;

/// <summary>Lê eurobank.cambio.comandos e repassa cada mensagem ao processador.</summary>
public sealed class ConsumidorComandosCambio(
    OpcoesKafkaSimulador opcoes,
    ProcessadorComandosCambio processador,
    ILogger<ConsumidorComandosCambio> logger)
    : ConsumidorKafka(opcoes, Topicos.Comandos, logger)
{
    protected override Task ProcessarAsync(MensagemRecebida mensagem, CancellationToken cancellationToken) =>
        processador.ProcessarAsync(mensagem, cancellationToken);
}
