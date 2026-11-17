namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>Uma medição de drift; Nivel e PsiMaximo nulos com amostra insuficiente. Psis do maior para o menor.</summary>
public sealed record MedicaoDriftDto(
    int VersaoModelo,
    DateTimeOffset MedidaEm,
    DateTimeOffset InicioJanela,
    int Amostra,
    int AmostraScore,
    string Situacao,
    string? Nivel,
    double? PsiMaximo,
    IReadOnlyList<PsiFeatureDto> Psis);
