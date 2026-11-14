using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Policies;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>IBAN na lista de bloqueio: qualquer saída para ele é recusada pela regra dura.</summary>
public sealed class IbanBloqueado
{
    private IbanBloqueado()
    {
        // Usado pelo EF Core.
    }

    /// <summary>Normalizado: sem espaços e em maiúsculas.</summary>
    public string Iban { get; private set; } = string.Empty;

    public string? Motivo { get; private set; }

    /// <summary>Quem bloqueou (o analista, ou "sistema" nos dados iniciais).</summary>
    public string BloqueadoPor { get; private set; } = string.Empty;

    public DateTimeOffset BloqueadoEm { get; private set; }

    public static IbanBloqueado Criar(string iban, string? motivo, string bloqueadoPor, DateTimeOffset agora)
    {
        var normalizado = NormalizacaoIban.Normalizar(iban);
        if (normalizado is not { Length: >= 15 and <= 34 } || NormalizacaoIban.Pais(normalizado) is null)
        {
            throw new ExcecaoIbanInvalido(iban);
        }

        return new IbanBloqueado
        {
            Iban = normalizado,
            Motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
            BloqueadoPor = bloqueadoPor,
            BloqueadoEm = agora,
        };
    }
}
