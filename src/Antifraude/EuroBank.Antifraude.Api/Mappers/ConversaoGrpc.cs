using EuroBank.Antifraude.Application.Exceptions;
using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using Google.Protobuf.WellKnownTypes;
using ContribuicaoFeatureGrpc = EuroBank.Antifraude.Grpc.ContribuicaoFeature;
using DecisaoGrpc = EuroBank.Antifraude.Grpc.Decisao;
using LocalizacaoGrpc = EuroBank.Antifraude.Grpc.Localizacao;
using PontuacaoModeloGrpc = EuroBank.Antifraude.Grpc.PontuacaoModelo;
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

        if (pedido.DispositivoId.Length > 100)
        {
            throw new ExcecaoPedidoAvaliacaoInvalido("dispositivo_id com no máximo 100 caracteres");
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
            pedido.Momento?.ToDateTimeOffset() ?? agora,
            string.IsNullOrWhiteSpace(pedido.DispositivoId) ? null : pedido.DispositivoId.Trim(),
            ParaLocalizacao(pedido.Localizacao));
    }

    /// <summary>Fora dos limites (latitude, longitude, país de duas letras): InvalidArgument, pela exceção de validação.</summary>
    private static Localizacao? ParaLocalizacao(LocalizacaoGrpc? localizacao) =>
        localizacao is null ? null : new Localizacao(localizacao.Latitude, localizacao.Longitude, localizacao.Pais);

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
            Modelo = resultado.Modelo?.ParaGrpc(),
            Challenger = resultado.Challenger?.ParaGrpc(),
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

    private static PontuacaoModeloGrpc ParaGrpc(this PontuacaoModelo pontuacao)
    {
        var dto = pontuacao.ParaDto();
        var mensagem = new PontuacaoModeloGrpc { Versao = dto.Versao, Probabilidade = dto.Probabilidade, Pontos = dto.Pontos };
        mensagem.Contribuicoes.AddRange(dto.Contribuicoes.Select(contribuicao =>
            new ContribuicaoFeatureGrpc { Feature = contribuicao.Feature, Valor = contribuicao.Valor }));
        return mensagem;
    }
}
