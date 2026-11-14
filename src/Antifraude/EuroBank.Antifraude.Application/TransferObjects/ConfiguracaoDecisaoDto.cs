namespace EuroBank.Antifraude.Application.TransferObjects;

public sealed record ConfiguracaoDecisaoDto(
    int Versao,
    int LimiarRevisao,
    int LimiarRecusa,
    int PesoModelo,
    DateTimeOffset CriadaEm);
