using EuroBank.Extrato.Domain.Entities;

namespace EuroBank.Extrato.Infrastructure.Cache.TransferObjects;

/// <summary>Um lançamento como vai para a tabela lancamentos, com o id que nasceu no domínio.</summary>
public sealed record LancamentoGravado(
    Guid Id,
    string Tipo,
    string Moeda,
    decimal Valor,
    decimal SaldoApos,
    decimal? Tarifa,
    int Versao,
    DateTimeOffset OcorridoEm,
    string Descricao,
    Guid? TransferenciaId,
    string? MoedaCambio,
    decimal? ValorCambio,
    decimal? Cotacao)
{
    public static LancamentoGravado De(Lancamento lancamento) => new(
        lancamento.Id,
        lancamento.Tipo.ToString(),
        lancamento.Moeda.ToString(),
        lancamento.Valor.Valor,
        lancamento.SaldoApos.Valor,
        lancamento.Tarifa?.Valor,
        lancamento.Versao,
        lancamento.OcorridoEm,
        lancamento.Descricao,
        lancamento.TransferenciaId,
        lancamento.Cambio?.Moeda.ToString(),
        lancamento.Cambio?.Valor,
        lancamento.Cambio?.Cotacao);
}
