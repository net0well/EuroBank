using EuroBank.Shared.Exceptions;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Contas.Domain.ValueObjects;

/// <summary>
/// O outro lado de uma transferência com câmbio: quanto, e em que moeda, a contraparte recebeu (no envio) ou
/// pagou (no recebimento), e a taxa usada (unidades da moeda de destino por 1 da moeda de origem, já com spread).
/// Contas não converte nada: só registra a conversão que a saga cotou.
/// </summary>
public sealed record Conversao
{
    private Conversao(Dinheiro contraparte, decimal taxa)
    {
        Contraparte = contraparte;
        Taxa = taxa;
    }

    public Dinheiro Contraparte { get; }

    public decimal Taxa { get; }

    public static Conversao Criar(Dinheiro contraparte, decimal taxa)
    {
        ArgumentNullException.ThrowIfNull(contraparte);
        if (taxa <= 0m)
        {
            throw new ExcecaoValorInvalido("a taxa de câmbio deve ser maior que zero");
        }

        return new Conversao(contraparte, taxa);
    }
}
