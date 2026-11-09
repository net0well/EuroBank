namespace EuroBank.Transferencias.Domain.Enums;

/// <summary>
/// Se uma resposta ou um prazo mudou a saga. Respostas duplicadas, atrasadas ou fora de ordem são ignoradas
/// (e anotadas no histórico), nunca tratadas como erro.
/// </summary>
public enum ResultadoPasso
{
    Aplicado = 1,
    Ignorado = 2,
}
