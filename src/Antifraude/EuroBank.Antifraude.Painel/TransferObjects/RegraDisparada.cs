namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Quantas vezes uma regra disparou de verdade nas últimas 24 horas.</summary>
public sealed record RegraDisparada(
    string Codigo,
    int Vezes);
