using EuroBank.Transferencias.Infrastructure.Enums;
using EuroBank.Transferencias.Infrastructure.Options;
using Grpc.Core;
using Grpc.Net.Client.Configuration;

namespace EuroBank.Transferencias.Infrastructure.Clients;

/// <summary>
/// Service config do canal: a política de retry ou de hedging vale para todos os métodos e só age em Unavailable
/// (antifraude fora do ar). As tentativas cabem no deadline da chamada; o que passar dele é DeadlineExceeded.
/// </summary>
public static class ConfiguracaoCanalAntifraude
{
    public static ServiceConfig Criar(OpcoesAntifraude opcoes)
    {
        var metodos = new MethodConfig { Names = { MethodName.Default } };
        var tentativas = Math.Clamp(opcoes.MaxTentativas, 1, 5);

        if (tentativas > 1)
        {
            switch (opcoes.Estrategia)
            {
                case EstrategiaChamadaAntifraude.Retry:
                    metodos.RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = tentativas,
                        InitialBackoff = TimeSpan.FromMilliseconds(100),
                        MaxBackoff = TimeSpan.FromSeconds(1),
                        BackoffMultiplier = 2,
                        RetryableStatusCodes = { StatusCode.Unavailable },
                    };
                    break;
                case EstrategiaChamadaAntifraude.Hedging:
                    metodos.HedgingPolicy = new HedgingPolicy
                    {
                        MaxAttempts = tentativas,
                        HedgingDelay = TimeSpan.FromMilliseconds(300),
                        NonFatalStatusCodes = { StatusCode.Unavailable },
                    };
                    break;
                case EstrategiaChamadaAntifraude.Nenhuma:
                    break;
                default:
                    throw new InvalidOperationException($"Estratégia {opcoes.Estrategia} sem configuração.");
            }
        }

        return new ServiceConfig { MethodConfigs = { metodos } };
    }
}
