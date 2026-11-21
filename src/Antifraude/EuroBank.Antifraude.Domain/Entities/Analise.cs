using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Events;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;

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

    // O contexto é guardado campo a campo (o valor só como número; a moeda numa coluna própria).
    private TipoOperacao _tipoOperacao;
    private decimal _valor;
    private Moeda _moeda;
    private string? _ibanDestino;
    private DateTimeOffset _momento;
    private string? _dispositivoId;
    private double? _latitude;
    private double? _longitude;
    private string? _paisLocalizacao;

    private Analise()
    {
        // Usado pelo EF Core.
    }

    public Guid OperacaoId { get; private set; }

    public Guid ContaId { get; private set; }

    public ContextoOperacao Contexto =>
        new(OperacaoId, _tipoOperacao, ContaId, Dinheiro.De(_valor, _moeda), _ibanDestino, _momento, _dispositivoId, Localizacao);

    private Localizacao? Localizacao =>
        _latitude is { } latitude && _longitude is { } longitude && _paisLocalizacao is { } pais ? new Localizacao(latitude, longitude, pais) : null;

    public IReadOnlyList<SinalRegra> Sinais => _sinais.AsReadOnly();

    public int ScoreFinal { get; private set; }

    public DecisaoAnalise Decisao { get; private set; }

    /// <summary>
    /// As features da operação no momento da avaliação: alimentam o modelo, o drift e, com a decisão do analista, o
    /// retreino. Nulas nas análises anteriores às features.
    /// </summary>
    public VetorFeatures? Features { get; private set; }

    /// <summary>A pontuação da versão ativa do modelo (também entre os sinais), com a explicação; nula sem modelo.</summary>
    public PontuacaoModelo? Modelo { get; private set; }

    /// <summary>A pontuação do challenger, em sombra: registrada para comparar, nunca decide.</summary>
    public PontuacaoModelo? Challenger { get; private set; }

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
            ContaId = contexto.ContaId,
            _tipoOperacao = contexto.Tipo,
            _valor = contexto.Valor.Valor,
            _moeda = contexto.Valor.Moeda,
            _ibanDestino = contexto.IbanDestino,
            _momento = contexto.Momento,
            _dispositivoId = contexto.DispositivoId,
            _latitude = contexto.Localizacao?.Latitude,
            _longitude = contexto.Localizacao?.Longitude,
            _paisLocalizacao = contexto.Localizacao?.Pais,
            _sinais = [.. resultado.Sinais],
            ScoreFinal = resultado.ScoreFinal,
            Decisao = resultado.Decisao,
            VersaoConfiguracao = resultado.VersaoConfiguracao,
            Features = resultado.Features,
            Modelo = resultado.Modelo,
            Challenger = resultado.Challenger,
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
        Registrar(new AnaliseDecidida(
            Id, OperacaoId, ContaId, decisao, origem, ScoreFinal, analista, justificativa, agora, _momento, _dispositivoId, Localizacao));
    }
}
