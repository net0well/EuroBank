namespace EuroBank.Antifraude.Domain.ValueObjects;

/// <summary>Uma faixa de probabilidade no teste, com quantas fraudes e quantas operações normais caíram nela.</summary>
public sealed record FaixaScore(double De, double Ate, int Fraudes, int Normais);
