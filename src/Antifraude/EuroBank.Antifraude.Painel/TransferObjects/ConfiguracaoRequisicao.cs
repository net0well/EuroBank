namespace EuroBank.Antifraude.Painel.TransferObjects;

/// <summary>Uma versão nova dos limiares e do peso do modelo.</summary>
public sealed record ConfiguracaoRequisicao(
    int LimiarRevisao,
    int LimiarRecusa,
    int PesoModelo);
