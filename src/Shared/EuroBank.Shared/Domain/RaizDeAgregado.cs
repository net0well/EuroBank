namespace EuroBank.Shared.Domain;

/// <summary>
/// Raiz de agregado: único ponto de entrada do agregado e responsável por acumular os eventos de domínio
/// gerados pelas operações. A infraestrutura lê esses eventos no SaveChanges e os grava na outbox.
/// </summary>
public abstract class RaizDeAgregado<TId>
    where TId : struct
{
    private readonly List<IEventoDominio> _eventosDominio = [];

    public TId Id { get; protected set; }

    public IReadOnlyList<IEventoDominio> EventosDominio => _eventosDominio.AsReadOnly();

    protected void Registrar(IEventoDominio evento) => _eventosDominio.Add(evento);

    public void LimparEventos() => _eventosDominio.Clear();
}
