using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Domain.ValueObjects;

/// <summary>
/// Retrato da transferência na hora em que ela termina. Vai dentro dos eventos finais para que quem os consome
/// (as notificações) não precise consultar Transferencias. Valor é o que o cliente pediu, na moeda em que pediu;
/// ValorDebitado e Tarifa estão na moeda da conta de origem; ValorDestino, na moeda de destino. Com câmbio,
/// ValorDebitado, ValorDestino e Taxa só existem depois da cotação.
/// </summary>
public sealed record ResumoTransferencia(
    TipoTransferencia Tipo,
    string NomeTitular,
    string NomeBeneficiario,
    string IbanDestino,
    Dinheiro Valor,
    Dinheiro Tarifa,
    Dinheiro? ValorDebitado,
    Dinheiro? ValorDestino,
    Moeda MoedaDestino,
    decimal? Taxa,
    string? Descricao,
    string? EmailNotificacao);
