namespace EuroBank.Extrato.Domain.Enums;

/// <summary>Status da conta como o Extrato o conhece (cópia local, sem depender do contexto Contas).</summary>
public enum StatusConta
{
    Ativa = 1,
    Bloqueada = 2,
    Encerrada = 3,
}
