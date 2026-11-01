namespace EuroBank.Contas.Dominio.Comum;

/// <summary>
/// Base das exceções de domínio. Cada categoria abaixo vira um status HTTP diferente na API
/// (validação 400, não encontrado 404, concorrência 409, regra de negócio 422).
/// </summary>
public abstract class ExcecaoDominio(string codigo, string mensagem) : Exception(mensagem)
{
    /// <summary>Código estável, legível por máquina (vai no ProblemDetails).</summary>
    public string Codigo { get; } = codigo;
}

/// <summary>Entrada inválida: o pedido nunca poderia ser atendido como veio.</summary>
public abstract class ExcecaoValidacao(string codigo, string mensagem) : ExcecaoDominio(codigo, mensagem);

/// <summary>O recurso referenciado não existe.</summary>
public abstract class ExcecaoNaoEncontrado(string codigo, string mensagem) : ExcecaoDominio(codigo, mensagem);

/// <summary>O pedido é válido, mas o estado atual do agregado não permite a operação.</summary>
public abstract class ExcecaoRegraNegocio(string codigo, string mensagem) : ExcecaoDominio(codigo, mensagem);

/// <summary>Outra operação alterou o agregado entre a leitura e a gravação (concorrência otimista).</summary>
public sealed class ExcecaoConcorrencia()
    : ExcecaoDominio("conflito_de_concorrencia", "A conta foi alterada por outra operação. Tente novamente.");

public sealed class ExcecaoValorInvalido(string motivo)
    : ExcecaoValidacao("valor_invalido", $"Valor inválido: {motivo}.");
