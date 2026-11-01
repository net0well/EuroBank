namespace EuroBank.Shared.Exceptions;

/// <summary>O recurso referenciado não existe.</summary>
public abstract class ExcecaoNaoEncontrado(string codigo, string mensagem) : ExcecaoDominio(codigo, mensagem);
