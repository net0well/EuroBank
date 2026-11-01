namespace EuroBank.Shared.Domain;

/// <summary>
/// Visão não genérica da raiz de agregado, para a infraestrutura encontrar os eventos de domínio de qualquer
/// agregado rastreado sem conhecer o tipo do id.
/// </summary>
public interface IRaizDeAgregado
{
    IReadOnlyList<IEventoDominio> EventosDominio { get; }

    void LimparEventos();
}
