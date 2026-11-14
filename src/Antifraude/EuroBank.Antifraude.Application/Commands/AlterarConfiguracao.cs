namespace EuroBank.Antifraude.Application.Commands;

/// <summary>Cria uma nova versão da configuração de decisão.</summary>
public sealed record AlterarConfiguracao(int LimiarRevisao, int LimiarRecusa, int PesoModelo);
