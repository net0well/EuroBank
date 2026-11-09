using EuroBank.Simuladores.Comum;

namespace EuroBank.Simuladores.BancoExterno.Servicos;

/// <summary>Lê eurobank.banco-externo.comandos e repassa cada mensagem ao processador.</summary>
public sealed class ConsumidorComandosBanco(
    OpcoesKafkaSimulador opcoes,
    ProcessadorComandos processador,
    ILogger<ConsumidorComandosBanco> logger)
    : ConsumidorKafka(opcoes, Topicos.Comandos, logger)
{
    protected override Task ProcessarAsync(MensagemRecebida mensagem, CancellationToken cancellationToken) =>
        processador.ProcessarAsync(mensagem, cancellationToken);
}
