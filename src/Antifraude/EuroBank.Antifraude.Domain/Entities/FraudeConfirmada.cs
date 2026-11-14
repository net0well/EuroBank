using EuroBank.Antifraude.Domain.Exceptions;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>
/// Uma operação marcada como fraude depois do fato (reclamação do cliente, investigação). Uma marcação por operação;
/// vira rótulo positivo no retreino do modelo.
/// </summary>
public sealed class FraudeConfirmada
{
    private FraudeConfirmada()
    {
        // Usado pelo EF Core.
    }

    public Guid Id { get; private set; }

    public Guid OperacaoId { get; private set; }

    public Guid AnaliseId { get; private set; }

    public string Analista { get; private set; } = string.Empty;

    public string? Observacao { get; private set; }

    public DateTimeOffset ConfirmadaEm { get; private set; }

    public static FraudeConfirmada Registrar(Analise analise, string analista, string? observacao, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(analista))
        {
            throw new ExcecaoAnalistaObrigatorio();
        }

        return new FraudeConfirmada
        {
            Id = Guid.CreateVersion7(),
            OperacaoId = analise.OperacaoId,
            AnaliseId = analise.Id.Valor,
            Analista = analista.Trim(),
            Observacao = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim(),
            ConfirmadaEm = agora,
        };
    }
}
