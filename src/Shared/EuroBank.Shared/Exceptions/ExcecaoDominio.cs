namespace EuroBank.Shared.Exceptions;

/// <summary>
/// Base das exceções de domínio. Cada categoria derivada vira um status HTTP diferente na API
/// (validação 400, não encontrado 404, concorrência 409, regra de negócio 422).
/// </summary>
public abstract class ExcecaoDominio(string codigo, string mensagem) : Exception(mensagem)
{
    /// <summary>Código estável, legível por máquina (vai no ProblemDetails).</summary>
    public string Codigo { get; } = codigo;
}
