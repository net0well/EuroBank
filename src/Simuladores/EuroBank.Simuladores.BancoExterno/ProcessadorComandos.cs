using System.Diagnostics;
using System.Text.Json;
using EuroBank.Simuladores.BancoExterno.Persistencia;
using EuroBank.Simuladores.BancoExterno.Regras;
using EuroBank.Simuladores.BancoExterno.TransferObjects;
using EuroBank.Simuladores.Comum;

namespace EuroBank.Simuladores.BancoExterno;

/// <summary>
/// Recebe os comandos do EuroBank e decide o que o banco externo responde e quando. Nada é publicado aqui: as
/// respostas ficam agendadas no banco (na mesma transação da inbox) e o EntregadorRespostas publica na hora certa.
/// </summary>
public sealed class ProcessadorComandos(
    RepositorioBancoExterno repositorio,
    RegrasLiquidacao regras,
    TimeProvider relogio,
    ILogger<ProcessadorComandos> logger)
{
    private const string Liquidada = "Liquidada";
    private const string Recusada = "Recusada";
    private const string NaoLiquidada = "NaoLiquidada";

    public async Task ProcessarAsync(MensagemRecebida mensagem, CancellationToken cancellationToken)
    {
        if (mensagem.Id is null || mensagem.Tipo is not (nameof(LiquidarTransferencia) or nameof(ConsultarLiquidacao)))
        {
            logger.LogWarning("Mensagem {Id} do tipo {Tipo} descartada: sem id ou tipo desconhecido", mensagem.Id, mensagem.Tipo);
            return;
        }

        using var atividade = Rastreamento.Iniciar(
            Telemetria.Fonte, $"banco-externo {mensagem.Tipo}", ActivityKind.Consumer, mensagem.Traceparent);
        // As respostas continuam o trace de quem enviou o comando.
        var traceparent = atividade?.Id ?? mensagem.Traceparent;
        var agora = relogio.GetUtcNow();

        await using var sessao = await repositorio.IniciarSessaoAsync(cancellationToken);
        if (!await sessao.RegistrarMensagemAsync(mensagem.Id, agora, cancellationToken))
        {
            logger.LogInformation("Mensagem {Id} ({Tipo}) já processada; ignorada", mensagem.Id, mensagem.Tipo);
            return;
        }

        if (mensagem.Tipo == nameof(LiquidarTransferencia))
        {
            await LiquidarAsync(sessao, Ler<LiquidarTransferencia>(mensagem), traceparent, agora, cancellationToken);
        }
        else
        {
            await ConsultarAsync(sessao, Ler<ConsultarLiquidacao>(mensagem), traceparent, agora, cancellationToken);
        }

        await sessao.ConfirmarAsync(cancellationToken);
    }

    /// <summary>
    /// Cada LiquidarTransferencia recebido gera uma resposta, inclusive os reenvios da saga: é assim que o final 97
    /// acaba respondendo duas vezes (a saga precisa tolerar a resposta duplicada).
    /// </summary>
    private async Task LiquidarAsync(
        SessaoBancoExterno sessao,
        LiquidarTransferencia comando,
        string? traceparent,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        var decisao = regras.Decidir(comando.IbanDestino);
        var silenciosa = decisao.Comportamento == ComportamentoLiquidacao.Silenciar;
        var status = decisao.Comportamento switch
        {
            ComportamentoLiquidacao.Recusar => Recusada,
            ComportamentoLiquidacao.Silenciar => NaoLiquidada,
            _ => Liquidada,
        };

        await sessao.RegistrarLiquidacaoAsync(
            comando.TransferenciaId, comando.IbanDestino, comando.Valor, comando.Moeda, status, silenciosa, agora, cancellationToken);

        logger.LogInformation("Transferência {TransferenciaId} para {Iban}: {Comportamento}",
            comando.TransferenciaId, comando.IbanDestino, decisao.Comportamento);

        var responderEm = agora + decisao.Atraso;
        switch (decisao.Comportamento)
        {
            case ComportamentoLiquidacao.Confirmar:
                await AgendarAsync(sessao, new LiquidacaoConfirmada(comando.TransferenciaId, responderEm), traceparent, responderEm, cancellationToken);
                break;
            case ComportamentoLiquidacao.Recusar:
                await AgendarAsync(sessao, new LiquidacaoRecusada(comando.TransferenciaId, "Conta de destino inexistente ou bloqueada"), traceparent, responderEm, cancellationToken);
                break;
            case ComportamentoLiquidacao.Silenciar:
            case ComportamentoLiquidacao.LiquidarSemResponder:
                // Nenhuma resposta: a saga vai esgotar o prazo e consultar.
                break;
            default:
                throw new UnreachableException($"Comportamento {decisao.Comportamento} sem tratamento.");
        }
    }

    private async Task ConsultarAsync(
        SessaoBancoExterno sessao,
        ConsultarLiquidacao consulta,
        string? traceparent,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        var liquidacao = await sessao.ObterLiquidacaoAsync(consulta.TransferenciaId, cancellationToken);
        if (liquidacao is { Silenciosa: true })
        {
            logger.LogInformation("Consulta de {TransferenciaId} ignorada: banco fora do ar para essa transferência", consulta.TransferenciaId);
            return;
        }

        var status = liquidacao?.Status ?? NaoLiquidada;
        var responderEm = agora + regras.Latencia;
        await AgendarAsync(sessao, new StatusLiquidacao(consulta.TransferenciaId, status), traceparent, responderEm, cancellationToken);
    }

    private static Task AgendarAsync<TResposta>(
        SessaoBancoExterno sessao,
        TResposta resposta,
        string? traceparent,
        DateTimeOffset responderEm,
        CancellationToken cancellationToken)
        where TResposta : IRespostaBancoExterno =>
        sessao.AgendarRespostaAsync(
            resposta.TransferenciaId,
            typeof(TResposta).Name,
            JsonSerializer.Serialize(resposta, JsonSerializerOptions.Web),
            traceparent,
            responderEm,
            cancellationToken);

    private static T Ler<T>(MensagemRecebida mensagem) =>
        JsonSerializer.Deserialize<T>(mensagem.Payload, JsonSerializerOptions.Web)
        ?? throw new JsonException($"Payload vazio na mensagem {mensagem.Id} ({mensagem.Tipo}).");
}
