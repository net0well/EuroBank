namespace EuroBank.Transferencias.Infrastructure.Enums;

/// <summary>Como o canal gRPC lida com o antifraude fora do ar (status Unavailable), dentro do deadline da chamada.</summary>
public enum EstrategiaChamadaAntifraude
{
    /// <summary>Uma tentativa só: a saga tenta de novo na próxima varredura.</summary>
    Nenhuma = 0,

    /// <summary>Repete com backoff exponencial depois de uma falha.</summary>
    Retry = 1,

    /// <summary>Manda cópias da chamada em paralelo e fica com a primeira resposta (Avaliar é idempotente).</summary>
    Hedging = 2,
}
