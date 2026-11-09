namespace EuroBank.Simuladores.Cambio.TransferObjects;

/// <summary>Toda resposta publicada em eurobank.cambio.respostas tem a transferência como chave.</summary>
public interface IRespostaCambio
{
    Guid TransferenciaId { get; }
}
