namespace EuroBank.Simuladores.BancoExterno.TransferObjects;

/// <summary>Toda resposta publicada em eurobank.banco-externo.respostas tem a transferência como chave.</summary>
public interface IRespostaBancoExterno
{
    Guid TransferenciaId { get; }
}
