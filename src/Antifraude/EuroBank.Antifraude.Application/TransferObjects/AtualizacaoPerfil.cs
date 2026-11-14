namespace EuroBank.Antifraude.Application.TransferObjects;

/// <summary>
/// O que um evento de Contas muda no perfil. Campos nulos não mudam nada; a versão sempre avança. A saída entra no
/// histórico (e o IBAN nos beneficiários conhecidos).
/// </summary>
public sealed record AtualizacaoPerfil(
    Guid ContaId,
    int Versao,
    DateTimeOffset OcorridoEm,
    decimal? SaldoApos = null,
    DateTimeOffset? AbertaEm = null,
    string? Moeda = null,
    SaidaPerfil? Saida = null);
