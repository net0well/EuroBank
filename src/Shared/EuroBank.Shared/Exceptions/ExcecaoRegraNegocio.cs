namespace EuroBank.Shared.Exceptions;

/// <summary>O pedido é válido, mas o estado atual do agregado não permite a operação.</summary>
public abstract class ExcecaoRegraNegocio(string codigo, string mensagem) : ExcecaoDominio(codigo, mensagem);
