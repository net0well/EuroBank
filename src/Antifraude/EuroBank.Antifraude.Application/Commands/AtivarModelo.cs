namespace EuroBank.Antifraude.Application.Commands;

/// <summary>A versão passa a decidir; a ativa anterior é aposentada. Cada instância da API troca o modelo sem reiniciar.</summary>
public sealed record AtivarModelo(int Versao);
