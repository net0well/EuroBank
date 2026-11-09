namespace EuroBank.Shared.Exceptions;

/// <summary>O pedido chegou tarde: o recurso já passou do ponto em que a operação era possível (409).</summary>
public abstract class ExcecaoConflito(string codigo, string mensagem) : ExcecaoDominio(codigo, mensagem);
