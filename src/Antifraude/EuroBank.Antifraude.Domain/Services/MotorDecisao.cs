using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Rules;
using EuroBank.Antifraude.Domain.ValueObjects;

namespace EuroBank.Antifraude.Domain.Services;

/// <summary>
/// Avalia as regras ativas e decide. Regras duras decidem sozinhas (recusa ou revisão); as com peso somam pontos
/// num score de 0 a 100, comparado com os limiares da configuração. Em caso de conflito, vale a decisão mais severa.
/// Regras em modo sombra são avaliadas e registradas, mas não pontuam nem decidem.
/// </summary>
public sealed class MotorDecisao
{
    private readonly Dictionary<CodigoRegra, IAvaliadorRegra> _avaliadores;

    public MotorDecisao()
        : this(
        [
            new AvaliadorListaBloqueio(),
            new AvaliadorValorAcimaDaMedia(),
            new AvaliadorVelocidade(),
            new AvaliadorEsvaziamento(),
            new AvaliadorBeneficiarioNovo(),
            new AvaliadorTesteDeCartao(),
            new AvaliadorContaRecemAberta(),
            new AvaliadorHorarioIncomum(),
            new AvaliadorPaisDeRisco(),
        ])
    {
    }

    public MotorDecisao(IEnumerable<IAvaliadorRegra> avaliadores) =>
        _avaliadores = avaliadores.ToDictionary(avaliador => avaliador.Codigo);

    public ResultadoMotor Decidir(
        ContextoOperacao operacao,
        PerfilConta perfil,
        IReadOnlyList<Regra> regras,
        ListasRisco listas,
        ConfiguracaoDecisao configuracao)
    {
        var sinais = new List<SinalRegra>();
        foreach (var regra in regras.Where(regra => regra.Ativa).OrderBy(regra => regra.Codigo))
        {
            if (!_avaliadores.TryGetValue(regra.Codigo, out var avaliador))
            {
                throw new InvalidOperationException($"Nenhum avaliador para a regra {regra.Codigo}.");
            }

            var resultado = avaliador.Avaliar(operacao, perfil, listas, regra);
            var pontos = resultado.Disparou && regra.Tipo == TipoRegra.Peso ? regra.Peso : 0;
            sinais.Add(new SinalRegra(regra.Codigo, regra.Tipo, resultado.Disparou, pontos, regra.Sombra, resultado.Motivo, regra.Efeito));
        }

        var decisivos = sinais.Where(sinal => sinal.Disparou && !sinal.Sombra).ToList();
        var score = Math.Min(100, decisivos.Sum(sinal => sinal.Pontos));
        var recusaDura = decisivos.Any(sinal => sinal.Tipo == TipoRegra.Dura && sinal.Efeito == EfeitoRegraDura.Recusar);
        var revisaoDura = decisivos.Any(sinal => sinal.Tipo == TipoRegra.Dura && sinal.Efeito == EfeitoRegraDura.Revisar);

        var decisao = recusaDura || score >= configuracao.LimiarRecusa
            ? DecisaoAnalise.Recusada
            : revisaoDura || score >= configuracao.LimiarRevisao
                ? DecisaoAnalise.EmRevisao
                : DecisaoAnalise.Aprovada;

        return new ResultadoMotor(decisao, score, sinais, configuracao.Versao);
    }
}
