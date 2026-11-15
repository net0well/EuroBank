namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// O drift na visão geral. Situacao: SemModeloAtivo, AguardandoMedicao (a versão ativa ainda não foi medida),
/// AmostraInsuficiente ou Medido; UltimaMedicao é a mais recente da versão ativa.
/// </summary>
public sealed record DriftDto(string Situacao, int? VersaoModelo, MedicaoDriftDto? UltimaMedicao)
{
    public const string SemModeloAtivo = nameof(SemModeloAtivo);

    public const string AguardandoMedicao = nameof(AguardandoMedicao);
}
