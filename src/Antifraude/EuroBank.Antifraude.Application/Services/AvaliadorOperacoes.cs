using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Application.Services;

/// <summary>
/// Junta o que o motor precisa (perfil no Redis, regras, listas e configuração no Postgres) e decide. Usado pela
/// avaliação de verdade e pela simulação, para as duas serem idênticas.
/// </summary>
public sealed class AvaliadorOperacoes(
    IPerfisContas perfis,
    IRepositorioRegras regras,
    IRepositorioListas listas,
    IRepositorioConfiguracao configuracoes,
    MotorDecisao motor)
{
    public async Task<ResultadoMotor> DecidirAsync(ContextoOperacao operacao, CancellationToken cancellationToken)
    {
        var configuracao = await configuracoes.AtualAsync(cancellationToken)
            ?? throw new InvalidOperationException("Configuração de decisão ausente: os dados iniciais não foram gravados.");
        var perfil = await perfis.ObterAsync(operacao.ContaId, operacao.Momento, cancellationToken);
        var regrasAtuais = await regras.ListarAsync(cancellationToken);
        var listasAtuais = await listas.ObterAsync(cancellationToken);
        return motor.Decidir(operacao, perfil, regrasAtuais, listasAtuais, configuracao);
    }
}
