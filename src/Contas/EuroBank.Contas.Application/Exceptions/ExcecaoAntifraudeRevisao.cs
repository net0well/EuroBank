using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Application.Exceptions;

/// <summary>
/// O antifraude não aprovou a operação: recusou ou mandou para a revisão de um analista. O cliente não sabe qual regra
/// disparou, como no e-mail da transferência recusada.
/// </summary>
public sealed class ExcecaoAntifraudeRevisao(bool emRevisao)
    : ExcecaoRegraNegocio(
        "antifraude_revisao",
        emRevisao
            ? "A operação não foi aprovada na análise de segurança e ficou para a revisão do banco."
            : "A operação não foi aprovada na análise de segurança.");
