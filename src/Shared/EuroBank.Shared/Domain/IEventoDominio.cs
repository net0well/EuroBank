namespace EuroBank.Shared.Domain;

/// <summary>Algo relevante que aconteceu no domínio, no passado.</summary>
public interface IEventoDominio
{
    DateTimeOffset OcorridoEm { get; }
}
