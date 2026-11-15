using System.Diagnostics;
using System.Text.Json;
using EuroBank.Simuladores.Cambio.Persistencia;
using EuroBank.Simuladores.Cambio.Regras;
using EuroBank.Simuladores.Cambio.TransferObjects;
using EuroBank.Simuladores.Comum;
using Microsoft.Extensions.Options;

namespace EuroBank.Simuladores.Cambio;

/// <summary>
/// Recebe os pedidos de cotação e agenda a resposta (na mesma transação da inbox). Um pedido repetido da mesma
/// transferência (reenvio da saga) recebe a cotação que ainda vale, guardada no Redis (ou na tabela, com o Redis fora);
/// sem cotação válida, emite uma nova.
/// </summary>
public sealed class ProcessadorComandosCambio(
    RepositorioCambio repositorio,
    CotacoesValidas cotacoes,
    TabelaCambio tabela,
    IOptions<OpcoesCambio> opcoes,
    TimeProvider relogio,
    ILogger<ProcessadorComandosCambio> logger)
{
    public async Task ProcessarAsync(MensagemRecebida mensagem, CancellationToken cancellationToken)
    {
        if (mensagem.Id is null || mensagem.Tipo != nameof(CotarCambio))
        {
            logger.LogWarning("Mensagem {Id} do tipo {Tipo} descartada: sem id ou tipo desconhecido", mensagem.Id, mensagem.Tipo);
            return;
        }

        using var atividade = Rastreamento.Iniciar(Telemetria.Fonte, $"cambio {mensagem.Tipo}", ActivityKind.Consumer, mensagem.Traceparent);
        var traceparent = atividade?.Id ?? mensagem.Traceparent;
        var agora = relogio.GetUtcNow();

        await using var sessao = await repositorio.IniciarSessaoAsync(cancellationToken);
        if (!await sessao.RegistrarMensagemAsync(mensagem.Id, agora, cancellationToken))
        {
            logger.LogInformation("Mensagem {Id} ({Tipo}) já processada; ignorada", mensagem.Id, mensagem.Tipo);
            return;
        }

        var pedido = JsonSerializer.Deserialize<CotarCambio>(mensagem.Payload, JsonSerializerOptions.Web)
            ?? throw new JsonException($"Payload vazio na mensagem {mensagem.Id}.");
        var responderEm = agora.AddMilliseconds(opcoes.Value.LatenciaMs);
        if (await cotacoes.ObterAsync(pedido, agora, cancellationToken) is { } valida)
        {
            await AgendarAsync(sessao, valida, traceparent, responderEm, cancellationToken);
            await sessao.ConfirmarAsync(cancellationToken);
            logger.LogInformation("Cotação {CotacaoId} reaproveitada para a transferência {TransferenciaId}: vale até {ValidaAte}",
                valida.CotacaoId, valida.TransferenciaId, valida.ValidaAte);
            return;
        }

        var resultado = tabela.Cotar(pedido.MoedaOrigem, pedido.MoedaDestino, pedido.Valor, pedido.MoedaDoValor);
        CotacaoEmitida? emitida = null;

        if (resultado.Disponivel)
        {
            var cotacao = new CotacaoEmitida(
                pedido.TransferenciaId,
                Guid.CreateVersion7(),
                pedido.MoedaOrigem,
                pedido.MoedaDestino,
                resultado.Taxa,
                resultado.ValorOrigem,
                resultado.ValorDestino,
                agora.AddMinutes(opcoes.Value.ValidadeMinutos));
            await sessao.RegistrarCotacaoAsync(cotacao, agora, cancellationToken);
            emitida = cotacao;
            await AgendarAsync(sessao, cotacao, traceparent, responderEm, cancellationToken);
            logger.LogInformation("Cotação {CotacaoId} {Origem}/{Destino} a {Taxa} para a transferência {TransferenciaId}",
                cotacao.CotacaoId, cotacao.MoedaOrigem, cotacao.MoedaDestino, cotacao.Taxa, cotacao.TransferenciaId);
        }
        else
        {
            await AgendarAsync(
                sessao,
                new CotacaoIndisponivel(pedido.TransferenciaId, pedido.MoedaOrigem, pedido.MoedaDestino, resultado.Motivo!),
                traceparent,
                responderEm,
                cancellationToken);
            logger.LogInformation("Cotação {Origem}/{Destino} indisponível para a transferência {TransferenciaId}: {Motivo}",
                pedido.MoedaOrigem, pedido.MoedaDestino, pedido.TransferenciaId, resultado.Motivo);
        }

        await sessao.ConfirmarAsync(cancellationToken);
        if (emitida is not null)
        {
            await cotacoes.GuardarAsync(emitida, agora);
        }
    }

    private static Task AgendarAsync<TResposta>(
        SessaoCambio sessao,
        TResposta resposta,
        string? traceparent,
        DateTimeOffset responderEm,
        CancellationToken cancellationToken)
        where TResposta : IRespostaCambio =>
        sessao.AgendarRespostaAsync(
            resposta.TransferenciaId,
            typeof(TResposta).Name,
            JsonSerializer.Serialize(resposta, JsonSerializerOptions.Web),
            traceparent,
            responderEm,
            cancellationToken);
}
