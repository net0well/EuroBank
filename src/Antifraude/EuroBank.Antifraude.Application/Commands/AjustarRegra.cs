using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Application.Commands;

/// <summary>Parâmetros nulos ou ausentes não mudam; os informados substituem os atuais.</summary>
public sealed record AjustarRegra(CodigoRegra Codigo, int Peso, bool Ativa, bool Sombra, IReadOnlyDictionary<string, decimal>? Parametros);
