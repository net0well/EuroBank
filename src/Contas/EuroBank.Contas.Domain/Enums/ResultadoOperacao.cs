namespace EuroBank.Contas.Domain.Enums;

/// <summary>Resultado de um comando idempotente vindo de uma saga.</summary>
public enum ResultadoOperacao
{
    Aplicada = 1,

    /// <summary>O mesmo comando já tinha sido aplicado antes (reenvio): nada mudou.</summary>
    JaProcessada = 2,
}
