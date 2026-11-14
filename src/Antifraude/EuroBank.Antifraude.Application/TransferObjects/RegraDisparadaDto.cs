namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>Quantas vezes uma regra disparou (sem contar o modo sombra).</summary>
public sealed record RegraDisparadaDto(
    string Codigo,
    int Vezes);
