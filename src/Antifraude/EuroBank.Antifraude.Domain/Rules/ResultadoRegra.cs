namespace EuroBank.Antifraude.Domain.Rules;

/// <summary>Se a regra disparou e por quê (o motivo vai para o detalhe da análise, também quando não dispara).</summary>
public sealed record ResultadoRegra(bool Disparou, string Motivo)
{
    public static ResultadoRegra Sim(string motivo) => new(true, motivo);

    public static ResultadoRegra Nao(string motivo) => new(false, motivo);
}
