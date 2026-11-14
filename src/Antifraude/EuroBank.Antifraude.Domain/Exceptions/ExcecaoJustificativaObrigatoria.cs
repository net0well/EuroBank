using EuroBank.Shared.Exceptions;

namespace EuroBank.Antifraude.Domain.Exceptions;

/// <summary>Toda decisão de caso precisa de justificativa (auditoria).</summary>
public sealed class ExcecaoJustificativaObrigatoria()
    : ExcecaoValidacao("justificativa_obrigatoria", "A decisão do caso exige justificativa.");
