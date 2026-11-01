namespace EuroBank.Contas.Dominio.Contas;

/// <summary>Reserva o próximo número sequencial de conta e devolve o IBAN correspondente.</summary>
public interface IGeradorIban
{
    Task<Iban> GerarAsync(CancellationToken cancellationToken);
}
