namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>As métricas da versão ativa na hora do treino, para comparar com a nova.</summary>
public sealed record ComparacaoTreino(int Versao, double AucPr, double Precisao, double Recall, double F1, double LimiarRecomendado);
