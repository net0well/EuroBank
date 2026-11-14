namespace EuroBank.Antifraude.Api.TransferObjects;

public sealed record ConfiguracaoRequisicao(
    int LimiarRevisao,
    int LimiarRecusa,
    int PesoModelo);
