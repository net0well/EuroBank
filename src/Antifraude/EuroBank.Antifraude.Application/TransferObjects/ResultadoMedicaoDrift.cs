namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>O resultado de um ciclo de drift: a medição gravada, ou nula sem versão ativa.</summary>
public sealed record ResultadoMedicaoDrift(MedicaoDriftDto? Medicao);
