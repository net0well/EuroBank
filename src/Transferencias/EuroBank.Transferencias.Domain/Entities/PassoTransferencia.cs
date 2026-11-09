using EuroBank.Transferencias.Domain.Enums;

namespace EuroBank.Transferencias.Domain.Entities;

/// <summary>
/// Uma linha do histórico da saga: cada comando enviado, cada resposta recebida (inclusive as ignoradas) e cada
/// prazo esgotado. É o que permite contar, depois, o que aconteceu com a transferência e quando.
/// </summary>
public sealed class PassoTransferencia
{
    private PassoTransferencia()
    {
        // Usado pelo EF Core.
    }

    internal PassoTransferencia(int ordem, EstadoTransferencia estadoApos, string descricao, DateTimeOffset ocorridoEm)
    {
        Ordem = ordem;
        EstadoApos = estadoApos;
        Descricao = descricao;
        OcorridoEm = ocorridoEm;
    }

    /// <summary>Posição no histórico, começando em 1 (igual à versão da transferência depois do passo).</summary>
    public int Ordem { get; private set; }

    public EstadoTransferencia EstadoApos { get; private set; }

    public string Descricao { get; private set; } = string.Empty;

    public DateTimeOffset OcorridoEm { get; private set; }
}
