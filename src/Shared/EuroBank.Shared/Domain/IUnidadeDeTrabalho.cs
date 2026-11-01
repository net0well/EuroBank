namespace EuroBank.Shared.Domain;

/// <summary>
/// Confirma numa única transação todas as alterações dos agregados carregados, junto com as linhas da outbox.
/// </summary>
public interface IUnidadeDeTrabalho
{
    /// <exception cref="Exceptions.ExcecaoConcorrencia">Outra operação alterou o agregado depois que ele foi carregado.</exception>
    Task SalvarAsync(CancellationToken cancellationToken);
}
