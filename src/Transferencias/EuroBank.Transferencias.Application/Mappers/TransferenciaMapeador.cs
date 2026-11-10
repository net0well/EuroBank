using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.Entities;

namespace EuroBank.Transferencias.Application.Mappers;

public static class TransferenciaMapeador
{
    public static TransferenciaDto ParaDto(this Transferencia transferencia) => new(
        transferencia.Id.Valor,
        transferencia.Tipo.ToString(),
        transferencia.ContaOrigemId,
        transferencia.MoedaOrigem.ToString(),
        transferencia.ContaDestinoId,
        transferencia.IbanDestino.Valor,
        transferencia.Bic,
        transferencia.NomeBeneficiario,
        transferencia.Valor.Valor,
        transferencia.MoedaDoValor.ToString(),
        transferencia.Tarifa.Valor,
        transferencia.ValorDebitado?.Valor,
        transferencia.ValorDestino?.Valor,
        transferencia.MoedaDestino.ToString(),
        transferencia.Cotacao is { } cotacao ? new CotacaoDto(cotacao.Id, cotacao.Taxa, cotacao.ValidaAte) : null,
        transferencia.Descricao,
        transferencia.EmailNotificacao,
        transferencia.SolicitadaEm,
        transferencia.ExecutarEm,
        transferencia.ExecutarEmEfetivo,
        transferencia.Estado.ToString(),
        transferencia.Motivo?.ToString(),
        transferencia.CancelamentoSolicitado,
        transferencia.TentativasNoPasso,
        transferencia.PrazoDoPasso,
        transferencia.Versao,
        transferencia.AtualizadaEm,
        transferencia.Historico
            .OrderBy(passo => passo.Ordem)
            .Select(passo => new PassoTransferenciaDto(passo.Ordem, passo.EstadoApos.ToString(), passo.Descricao, passo.OcorridoEm))
            .ToList());

    public static TransferenciaResumoDto ParaResumo(this Transferencia transferencia) => new(
        transferencia.Id.Valor,
        transferencia.Tipo.ToString(),
        transferencia.ContaOrigemId,
        transferencia.IbanDestino.Valor,
        transferencia.NomeBeneficiario,
        transferencia.Valor.Valor,
        transferencia.MoedaDoValor.ToString(),
        transferencia.Tarifa.Valor,
        transferencia.MoedaOrigem.ToString(),
        transferencia.ValorDestino?.Valor,
        transferencia.MoedaDestino.ToString(),
        transferencia.Estado.ToString(),
        transferencia.Motivo?.ToString(),
        transferencia.ExecutarEmEfetivo,
        transferencia.AtualizadaEm);
}
