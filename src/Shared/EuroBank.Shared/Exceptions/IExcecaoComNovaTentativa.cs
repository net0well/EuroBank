namespace EuroBank.Shared.Exceptions;

/// <summary>
/// Uma exceção de domínio passageira: o mesmo pedido pode dar certo daqui a pouco. As APIs respondem com o header
/// Retry-After, em segundos.
/// </summary>
public interface IExcecaoComNovaTentativa
{
    TimeSpan TentarDeNovoEm { get; }
}
