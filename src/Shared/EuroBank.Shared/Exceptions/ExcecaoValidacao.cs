namespace EuroBank.Shared.Exceptions;

/// <summary>Entrada inválida: o pedido nunca poderia ser atendido como veio.</summary>
public abstract class ExcecaoValidacao(string codigo, string mensagem) : ExcecaoDominio(codigo, mensagem);
