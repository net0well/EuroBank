using EuroBank.Extrato.Domain.Entities;

namespace EuroBank.Extrato.Infrastructure.Cache.WriteBehind;

/// <summary>
/// Os extratos que uma mensagem carregou do Redis (ou criou), com a versão de quando foram carregados: o que avançou
/// vai para o Redis e para as pendências no fim. Também lembra se esta mensagem caiu para o Postgres.
/// </summary>
public sealed class SessaoWriteBehind
{
    private readonly Dictionary<Guid, (ExtratoConta Extrato, int VersaoCarregada)> _rastreados = [];

    /// <summary>Nulo até a primeira operação; verdadeiro com o cache suspeito ou o Redis fora.</summary>
    public bool? UsaPostgres { get; set; }

    public void Rastrear(ExtratoConta extrato, int versaoCarregada) => _rastreados[extrato.ContaId] = (extrato, versaoCarregada);

    /// <summary>Os extratos que mudaram desde que foram carregados (os criados nesta mensagem entram com versão 0).</summary>
    public IReadOnlyList<ExtratoConta> Alterados() =>
        _rastreados.Values.Where(item => item.Extrato.UltimaVersao > item.VersaoCarregada)
            .Select(item => item.Extrato)
            .ToList();

    /// <summary>Depois de gravar: uma segunda gravação na mesma mensagem não repete o que já foi.</summary>
    public void Confirmar(ExtratoConta extrato) => _rastreados[extrato.ContaId] = (extrato, int.MaxValue);
}
