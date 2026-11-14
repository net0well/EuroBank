using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>A análise já tem decisão: não há revisão a aplicar.</summary>
public sealed class ExcecaoRevisaoNaoPendente(DecisaoAnalise decisao)
    : ExcecaoConflito("revisao_nao_pendente", $"A análise não está em revisão (decisão atual: {decisao}).");
