namespace EuroBank.Contas.Application.Enums;

/// <summary>O que o antifraude disse: só Aprovada deixa a operação seguir.</summary>
public enum DecisaoRisco
{
    Aprovada = 1,
    EmRevisao = 2,
    Recusada = 3,
}
