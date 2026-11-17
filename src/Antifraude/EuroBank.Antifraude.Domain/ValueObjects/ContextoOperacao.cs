using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Policies;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>
/// A operação a avaliar, como o chamador a descreve. OperacaoId é o id da transferência (ou do saque) e torna a
/// avaliação idempotente. Valor vem na moeda da conta, que é a moeda das regras (médias, saldo, limites).
/// </summary>
public sealed record ContextoOperacao(
    Guid OperacaoId,
    TipoOperacao Tipo,
    Guid ContaId,
    Dinheiro Valor,
    string? IbanDestino,
    DateTimeOffset Momento)
{
    /// <summary>Código do país do IBAN de destino (as duas primeiras letras), quando há destino.</summary>
    public string? PaisDestino => NormalizacaoIban.Pais(IbanDestino);

    /// <summary>A operação no modelo de dados das features, com o IBAN normalizado e o país conferido nas listas.</summary>
    public OperacaoAvaliada ParaFeatures(ListasRisco listas)
    {
        ArgumentNullException.ThrowIfNull(listas);
        return new OperacaoAvaliada(Momento, Valor.Valor, NormalizacaoIban.Normalizar(IbanDestino), listas.EhPaisDeRisco(PaisDestino));
    }
}
