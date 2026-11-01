namespace EuroBank.Contas.Dominio.Contas;

/// <summary>Persiste o agregado Conta como uma unidade.</summary>
public interface IRepositorioContas
{
    Task<Conta?> ObterPorIdAsync(ContaId id, CancellationToken cancellationToken);

    Task<Conta?> ObterPorIbanAsync(Iban iban, CancellationToken cancellationToken);

    Task<bool> ExisteAlgumaAsync(CancellationToken cancellationToken);

    void Adicionar(Conta conta);
}
