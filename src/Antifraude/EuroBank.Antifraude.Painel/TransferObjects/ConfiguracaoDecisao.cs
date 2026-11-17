namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Limiares e peso do modelo (a versão atual).</summary>
public sealed record ConfiguracaoDecisao(
    int Versao,
    int LimiarRevisao,
    int LimiarRecusa,
    int PesoModelo,
    DateTimeOffset CriadaEm);
