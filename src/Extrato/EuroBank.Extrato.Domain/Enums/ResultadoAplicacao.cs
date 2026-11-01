namespace EuroBank.Extrato.Domain.Enums;

/// <summary>O que aconteceu ao aplicar um evento na projeção.</summary>
public enum ResultadoAplicacao
{
    /// <summary>Evento aplicado na sequência esperada (versão = última + 1).</summary>
    Aplicado = 1,

    /// <summary>Versão menor ou igual à última aplicada: duplicata ou reprocessamento, nada muda.</summary>
    IgnoradoJaAplicado = 2,

    /// <summary>Aplicado, mas faltaram versões no meio (vale um log de alerta).</summary>
    AplicadoComLacuna = 3,
}
