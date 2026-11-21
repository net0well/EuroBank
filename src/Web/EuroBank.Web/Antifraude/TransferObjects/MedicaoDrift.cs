namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>Uma medição de drift; os PSIs vêm do maior para o menor.</summary>
public sealed record MedicaoDrift(
    int VersaoModelo,
    DateTimeOffset MedidaEm,
    DateTimeOffset InicioJanela,
    int Amostra,
    int AmostraScore,
    string Situacao,
    string? Nivel,
    double? PsiMaximo,
    IReadOnlyList<PsiFeature> Psis);
