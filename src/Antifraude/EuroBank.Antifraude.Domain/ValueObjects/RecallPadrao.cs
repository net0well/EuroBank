namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Um padrão de fraude sintético no teste: quantas havia e quantas o limiar recomendado pegou.</summary>
public sealed record RecallPadrao(string Padrao, int Fraudes, int Detectadas, double Recall);
