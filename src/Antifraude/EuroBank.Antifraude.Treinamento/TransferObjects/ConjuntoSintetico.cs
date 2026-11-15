namespace EuroBank.Antifraude.Treinamento.TransferObjects;

/// <summary>O conteúdo do CSV sintético: a semente que o gerou (vai para o registro da versão) e as transferências.</summary>
public sealed record ConjuntoSintetico(int Semente, IReadOnlyList<TransferenciaSintetica> Transferencias);
