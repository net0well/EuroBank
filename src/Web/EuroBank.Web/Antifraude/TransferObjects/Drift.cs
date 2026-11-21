namespace EuroBank.Web.Antifraude.TransferObjects;

/// <summary>A situação do drift (SemModeloAtivo, AguardandoMedicao, AmostraInsuficiente ou Medido).</summary>
public sealed record Drift(
    string Situacao,
    int? VersaoModelo,
    MedicaoDrift? UltimaMedicao);
