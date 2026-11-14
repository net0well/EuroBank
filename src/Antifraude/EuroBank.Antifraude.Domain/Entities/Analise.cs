using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Events;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Shared.Domain;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>
/// A avaliação de uma operação (uma só por OperacaoId: avaliar de novo devolve esta). Guarda o contexto, o sinal de
/// cada regra, o score e a decisão. Aprovada ou Recusada pelo motor já é final; EmRevisao espera o caso e termina
/// pela decisão do analista, pela expiração ou pelo cancelamento do cliente. Toda decisão final publica
/// <see cref="AnaliseDecidida"/>.
/// </summary>
public sealed class Analise : RaizDeAgregado<AnaliseId>
{
    private List<SinalRegra> _sinais = [];

    private Analise()
    {
        // Usado pelo EF Core.
    }

    public Guid OperacaoId { get; private set; }

    public ContextoOperacao Contexto { get; private set; } = null!;

    public IReadOnlyList<SinalRegra> Sinais => _sinais.AsReadOnly();

    public int ScoreFinal { get; private set; }

    public DecisaoAnalise Decisao { get; private set; }

    /// <summary>Versão da configuração de decisão (limiares) usada.</summary>
    public int VersaoConfiguracao { get; private set; }

    public DateTimeOffset CriadaEm { get; private set; }

    /// <summary>Quando a decisão ficou final; nulo enquanto está em revisão.</summary>
    public DateTimeOffset? DecididaEm { get; private set; }

    public string? Analista { get; private set; }

    public string? Justificativa { get; private set; }

    public bool EstaEmRevisao => Decisao == DecisaoAnalise.EmRevisao;

    public static Analise Registrar(ContextoOperacao contexto, ResultadoMotor resultado, DateTimeOffset agora)
    {
        var analise = new Analise
        {
            Id = AnaliseId.Nova(),
            OperacaoId = contexto.OperacaoId,
            Contexto = contexto,
            _sinais = [.. resultado.Sinais],
            ScoreFinal = resultado.ScoreFinal,
            Decisao = resultado.Decisao,
            VersaoConfiguracao = resultado.VersaoConfiguracao,
            CriadaEm = agora,
        };

        if (!analise.EstaEmRevisao)
        {
            analise.Fechar(resultado.Decisao, OrigemDecisao.Motor, analista: null, justificativa: null, agora);
        }

        return analise;
    }

    public void AplicarRevisao(bool aprovar, string analista, string justificativa, DateTimeOffset agora)
    {
        if (!EstaEmRevisao)
        {
            throw new ExcecaoRevisaoNaoPendente(Decisao);
        }

        Fechar(aprovar ? DecisaoAnalise.Aprovada : DecisaoAnalise.Recusada, OrigemDecisao.Analista, analista, justificativa, agora);
    }

    /// <summary>Ninguém decidiu a tempo. Fora de revisão, não faz nada.</summary>
    public bool Expirar(DateTimeOffset agora) => FecharSemAnalista(DecisaoAnalise.Expirada, OrigemDecisao.Expiracao, agora);

    /// <summary>O cliente cancelou a operação durante a revisão. Fora de revisão, não faz nada.</summary>
    public bool Cancelar(DateTimeOffset agora) => FecharSemAnalista(DecisaoAnalise.Cancelada, OrigemDecisao.Cliente, agora);

    private bool FecharSemAnalista(DecisaoAnalise decisao, OrigemDecisao origem, DateTimeOffset agora)
    {
        if (!EstaEmRevisao)
        {
            return false;
        }

        Fechar(decisao, origem, analista: null, justificativa: null, agora);
        return true;
    }

    private void Fechar(DecisaoAnalise decisao, OrigemDecisao origem, string? analista, string? justificativa, DateTimeOffset agora)
    {
        Decisao = decisao;
        DecididaEm = agora;
        Analista = analista;
        Justificativa = justificativa;
        Registrar(new AnaliseDecidida(Id, OperacaoId, Contexto.ContaId, decisao, origem, ScoreFinal, analista, justificativa, agora));
    }
}
