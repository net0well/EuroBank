namespace EuroBank.Antifraude.Domain.Enums;

/// <summary>
/// O que a análise diz sobre a operação. Aprovada e Recusada saem do motor ou do analista; EmRevisao espera o caso;
/// Expirada (ninguém decidiu a tempo) e Cancelada (o cliente desistiu) fecham uma revisão sem decisão do analista.
/// </summary>
public enum DecisaoAnalise
{
    Aprovada = 1,
    EmRevisao = 2,
    Recusada = 3,
    Expirada = 4,
    Cancelada = 5,
}
