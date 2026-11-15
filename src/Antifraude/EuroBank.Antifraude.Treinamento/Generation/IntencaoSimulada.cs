using EuroBank.Antifraude.Treinamento.Enums;

namespace EuroBank.Antifraude.Treinamento.Generation;

/// <summary>Um item da agenda de uma conta simulada, processado em ordem de tempo.</summary>
public sealed record IntencaoSimulada(DateTimeOffset Momento, TipoIntencao Tipo, PadraoFraude Padrao, decimal Valor = 0, string? Iban = null);
