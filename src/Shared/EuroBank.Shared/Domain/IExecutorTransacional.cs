namespace EuroBank.Shared.Domain;

/// <summary>
/// Abre uma transação em volta de uma operação que precisa ler e gravar com a mesma visão do banco (por exemplo,
/// travar a conta, somar o que ela já transferiu no dia e iniciar a saga). A unidade de trabalho chamada dentro
/// da operação participa dessa transação.
/// </summary>
public interface IExecutorTransacional
{
    Task ExecutarAsync(Func<CancellationToken, Task> operacao, CancellationToken cancellationToken);
}
