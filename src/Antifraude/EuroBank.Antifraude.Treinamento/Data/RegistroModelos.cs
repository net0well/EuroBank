using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Infrastructure.Persistence;
using EuroBank.Antifraude.Treinamento.Training;

namespace EuroBank.Antifraude.Treinamento.Data;

/// <summary>Grava o resultado de um treino como uma nova versão Registrada (ativar ou pôr em sombra é com o analista).</summary>
public static class RegistroModelos
{
    public static async Task<VersaoModelo> RegistrarAsync(
        AntifraudeDbContext contexto,
        ResultadoTreino resultado,
        DateTimeOffset agora,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        ArgumentNullException.ThrowIfNull(resultado);
        var versao = VersaoModelo.Registrar(
            resultado.Arquivo, resultado.Metricas, resultado.LimiarRecomendado, resultado.Dados, resultado.Distribuicao, agora);
        contexto.VersoesModelo.Add(versao);
        await contexto.SaveChangesAsync(cancellationToken);
        return versao;
    }
}
