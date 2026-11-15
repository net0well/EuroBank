using EuroBank.Antifraude.Domain.Services;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Infrastructure.MachineLearning;

/// <summary>
/// Os modelos em uso nesta instância, pontuando cada avaliação. Fail-open: erro na inferência vira pontuação nula
/// (a avaliação segue com as regras), conta uma falha e vai para o log. Quem troca os modelos é o carregador.
/// </summary>
public sealed class PontuadorFraude(TelemetriaModelo telemetria, ILogger<PontuadorFraude> logger) : IPontuadorFraude
{
    public const string PapelAtiva = "ativa";
    public const string PapelChallenger = "challenger";

    private volatile ModelosEmUso _emUso = ModelosEmUso.Nenhum;

    public ModelosEmUso EmUso => _emUso;

    public void Trocar(ModelosEmUso modelos) => _emUso = modelos ?? throw new ArgumentNullException(nameof(modelos));

    public PontuacoesModelo Pontuar(VetorFeatures features)
    {
        var emUso = _emUso;
        return emUso.Ativo is null && emUso.Challenger is null
            ? PontuacoesModelo.Nenhuma
            : new PontuacoesModelo(Pontuar(emUso.Ativo, PapelAtiva, features), Pontuar(emUso.Challenger, PapelChallenger, features));
    }

    private PontuacaoModelo? Pontuar(ModeloCarregado? modelo, string papel, VetorFeatures features)
    {
        if (modelo is null)
        {
            return null;
        }

        try
        {
            var pontuacao = modelo.Pontuar(features);
            telemetria.Pontuacao(papel, modelo.Versao);
            return pontuacao;
        }
        catch (Exception erro) when (erro is not OutOfMemoryException)
        {
            telemetria.Falha(papel, "pontuacao");
            logger.LogWarning(erro, "A versão {Versao} do modelo ({Papel}) falhou ao pontuar; a avaliação segue sem ela", modelo.Versao, papel);
            return null;
        }
    }
}
