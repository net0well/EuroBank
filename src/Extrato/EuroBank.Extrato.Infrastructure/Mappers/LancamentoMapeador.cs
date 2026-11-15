using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Entities;
using EuroBank.Extrato.Domain.Enums;

namespace EuroBank.Extrato.Infrastructure.Mappers;

/// <summary>O mesmo lançamento na resposta da API, venha ele do Postgres ou do Redis.</summary>
public static class LancamentoMapeador
{
    /// <summary>No envio, o outro lado do câmbio é o destino; no recebimento, a origem.</summary>
    public static LancamentoDto ParaDto(this Lancamento lancamento)
    {
        var recebimento = lancamento.Tipo == TipoLancamento.TransferenciaCambioRecebida;
        var cambio = lancamento.Cambio;
        return new LancamentoDto(
            lancamento.Tipo.ToString(),
            lancamento.Valor.Valor,
            lancamento.Tarifa?.Valor,
            lancamento.SaldoApos.Valor,
            lancamento.Versao,
            lancamento.OcorridoEm,
            lancamento.Descricao,
            MoedaDestino: recebimento ? null : cambio?.Moeda.ToString(),
            ValorDestino: recebimento ? null : cambio?.Valor,
            MoedaOrigem: recebimento ? cambio?.Moeda.ToString() : null,
            ValorOrigem: recebimento ? cambio?.Valor : null,
            Cotacao: cambio?.Cotacao);
    }
}
