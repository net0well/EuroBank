using EuroBank.Antifraude.Application.Exceptions;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using DecisaoGrpc = EuroBank.Antifraude.Grpc.Decisao;
using ResultadoAvaliacao = EuroBank.Antifraude.Grpc.ResultadoAvaliacao;
using SinalRegraGrpc = EuroBank.Antifraude.Grpc.SinalRegra;
using SolicitacaoAvaliacao = EuroBank.Antifraude.Grpc.SolicitacaoAvaliacao;
using TipoOperacaoGrpc = EuroBank.Antifraude.Grpc.TipoOperacao;

namespace EuroBank.Antifraude.Api.Mappers;

/// <summary>Entre as mensagens do contrato gRPC e o domínio. Validação de formato aqui; regras de negócio no domínio.</summary>
public static class ConversaoGrpc
{
    public static ContextoOperacao ParaContexto(this SolicitacaoAvaliacao pedido, DateTimeOffset agora)
    {
        var operacaoId = Guid.TryParse(pedido.OperacaoId, out var operacao)
            ? operacao
            : throw new ExcecaoPedidoAvaliacaoInvalido("operacao_id deve ser um Guid");
        var contaId = Guid.TryParse(pedido.ContaId, out var conta)
            ? conta
            : throw new ExcecaoPedidoAvaliacaoInvalido("conta_id deve ser um Guid");
        var tipo = pedido.TipoOperacao switch
        {
            TipoOperacaoGrpc.Externa => TipoOperacao.Externa,
            TipoOperacaoGrpc.Cambio => TipoOperacao.Cambio,
            TipoOperacaoGrpc.Saque => TipoOperacao.Saque,
            TipoOperacaoGrpc.TransferenciaInterna => TipoOperacao.TransferenciaInterna,
            _ => throw new ExcecaoPedidoAvaliacaoInvalido("informe o tipo_operacao"),
        };
        var moeda = System.Enum.TryParse<Moeda>(pedido.Moeda, ignoreCase: false, out var codigo) && System.Enum.IsDefined(codigo)
            ? codigo
            : throw new ExcecaoPedidoAvaliacaoInvalido($"moeda '{pedido.Moeda}' desconhecida");
        if (pedido.Valor is null)
        {
            throw new ExcecaoPedidoAvaliacaoInvalido("informe o valor");
        }

        decimal valor = pedido.Valor;
        if (valor <= 0m)
        {
            throw new ExcecaoPedidoAvaliacaoInvalido("o valor deve ser maior que zero");
        }

        return new ContextoOperacao(
            operacaoId,
            tipo,
            contaId,
            Dinheiro.De(valor, moeda),
            string.IsNullOrWhiteSpace(pedido.IbanDestino) ? null : pedido.IbanDestino,
            pedido.Momento?.ToDateTimeOffset() ?? agora);
    }

    public static Guid ParaAnaliseId(string analiseId) =>
        Guid.TryParse(analiseId, out var id) ? id : throw new ExcecaoPedidoAvaliacaoInvalido("analise_id deve ser um Guid");

    public static ResultadoAvaliacao ParaGrpc(this ResultadoAvaliacaoDto resultado)
    {
        var mensagem = new ResultadoAvaliacao
        {
            AnaliseId = resultado.AnaliseId?.ToString() ?? string.Empty,
            OperacaoId = resultado.OperacaoId.ToString(),
            Decisao = resultado.Decisao switch
            {
                DecisaoAnalise.Aprovada => DecisaoGrpc.Aprovada,
                DecisaoAnalise.EmRevisao => DecisaoGrpc.EmRevisao,
                DecisaoAnalise.Recusada => DecisaoGrpc.Recusada,
                DecisaoAnalise.Expirada => DecisaoGrpc.Expirada,
                DecisaoAnalise.Cancelada => DecisaoGrpc.Cancelada,
                _ => DecisaoGrpc.NaoInformada,
            },
            ScoreFinal = resultado.ScoreFinal,
            CasoId = resultado.CasoId?.ToString() ?? string.Empty,
            VersaoConfiguracao = resultado.VersaoConfiguracao,
            AvaliadaEm = Timestamp.FromDateTimeOffset(resultado.AvaliadaEm),
            DecididaEm = resultado.DecididaEm is { } decididaEm ? Timestamp.FromDateTimeOffset(decididaEm) : null,
            Simulacao = resultado.Simulacao,
        };
        mensagem.Sinais.AddRange(resultado.Sinais.Select(sinal => new SinalRegraGrpc
        {
            Codigo = sinal.Codigo.ToString(),
            Tipo = sinal.Tipo == TipoRegra.Dura ? "DURA" : "PESO",
            Disparou = sinal.Disparou,
            Pontos = sinal.Pontos,
            Sombra = sinal.Sombra,
            Motivo = sinal.Motivo,
        }));
        return mensagem;
    }
}
