using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;

namespace EuroBank.Antifraude.Domain.Policies;

/// <summary>
/// As regras que o motor conhece, com os valores iniciais. Valores em dinheiro estão na moeda da conta. Os analistas
/// ajustam peso, parâmetros, ativação e modo sombra pela API; o catálogo só define o ponto de partida.
/// </summary>
public static class CatalogoRegras
{
    private static readonly Dictionary<string, decimal> SemParametros = [];

    public static IReadOnlyList<DefinicaoRegra> Definicoes { get; } =
    [
        new(CodigoRegra.ListaBloqueio, TipoRegra.Dura, "IBAN de destino na lista de bloqueio", 0, EfeitoRegraDura.Recusar, SemParametros),
        new(
            CodigoRegra.ValorAcimaDaMedia,
            TipoRegra.Peso,
            "Valor acima de N vezes a média das transferências dos últimos 30 dias",
            25,
            null,
            new Dictionary<string, decimal> { ["multiplicador"] = 5m, ["minimoSaidas"] = 3m }),
        new(
            CodigoRegra.Velocidade,
            TipoRegra.Peso,
            "Muitas transferências em pouco tempo (contando a atual)",
            20,
            null,
            new Dictionary<string, decimal> { ["quantidade"] = 3m, ["janelaMinutos"] = 10m }),
        new(
            CodigoRegra.Esvaziamento,
            TipoRegra.Peso,
            "Quase todo o saldo de uma vez",
            25,
            null,
            new Dictionary<string, decimal> { ["percentualSaldo"] = 0.9m }),
        new(CodigoRegra.BeneficiarioNovo, TipoRegra.Peso, "IBAN de destino nunca usado pela conta", 10, null, SemParametros),
        new(
            CodigoRegra.TesteDeCartao,
            TipoRegra.Peso,
            "Vários valores pequenos seguidos (contando a atual, se for pequena)",
            20,
            null,
            new Dictionary<string, decimal> { ["quantidade"] = 3m, ["valorMaximo"] = 10m, ["janelaMinutos"] = 15m }),
        new(
            CodigoRegra.ContaRecemAberta,
            TipoRegra.Peso,
            "Conta recém-aberta transferindo alto (conta sem perfil conta como nova)",
            20,
            null,
            new Dictionary<string, decimal> { ["dias"] = 7m, ["valorMinimo"] = 1_000m }),
        new(
            CodigoRegra.HorarioIncomum,
            TipoRegra.Peso,
            "Hora do dia (UTC) sem transferências nos últimos 30 dias",
            10,
            null,
            new Dictionary<string, decimal> { ["minimoSaidas"] = 10m }),
        new(CodigoRegra.PaisDeRisco, TipoRegra.Peso, "País do IBAN de destino na lista de países de risco", 30, null, SemParametros),
        new(CodigoRegra.DispositivoNovo, TipoRegra.Peso, "Dispositivo nunca usado pela conta numa operação aprovada", 15, null, SemParametros),
        new(
            CodigoRegra.PaisDiferenteDoHabitual,
            TipoRegra.Peso,
            "País da localização diferente do habitual (o mais frequente nas operações aprovadas de 30 dias)",
            15,
            null,
            new Dictionary<string, decimal> { ["minimoOperacoes"] = 3m }),
        new(
            CodigoRegra.ViagemImpossivel,
            TipoRegra.Dura,
            "Distância da última operação aprovada sobre o tempo acima da velocidade de um avião",
            0,
            EfeitoRegraDura.Revisar,
            new Dictionary<string, decimal> { ["velocidadeMaximaKmH"] = 900m, ["distanciaMinimaKm"] = 100m }),
    ];

    public static DefinicaoRegra Definicao(CodigoRegra codigo) => Definicoes.Single(definicao => definicao.Codigo == codigo);

    public static IReadOnlyList<Regra> RegrasIniciais(DateTimeOffset agora) =>
        Definicoes.Select(definicao => Regra.Criar(definicao, agora)).ToList();
}
