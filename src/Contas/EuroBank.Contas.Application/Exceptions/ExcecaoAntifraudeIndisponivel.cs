using EuroBank.Shared.Exceptions;

namespace EuroBank.Contas.Application.Exceptions;

/// <summary>Sem resposta do antifraude: na dúvida, o dinheiro não sai (fail-closed).</summary>
public sealed class ExcecaoAntifraudeIndisponivel(string status)
    : ExcecaoRegraNegocio("antifraude_indisponivel", $"A análise de segurança está indisponível ({status}); tente de novo em instantes.");
