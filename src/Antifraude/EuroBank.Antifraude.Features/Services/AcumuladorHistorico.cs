using EuroBank.Antifraude.Features.Policies;
using EuroBank.Antifraude.Features.ValueObjects;

namespace EuroBank.Antifraude.Features.Services;

/// <summary>
/// O caminho do treino: reproduz em memória o que o perfil no Redis guarda, evento a evento (a mesma regra de versão,
/// a mesma retenção). O teste de paridade compara o histórico daqui com o do Redis para os mesmos eventos.
/// </summary>
public sealed class AcumuladorHistorico
{
    private readonly List<MovimentoHistorico> _saidas = [];
    private readonly List<MovimentoHistorico> _entradas = [];
    private readonly HashSet<string> _beneficiarios = new(StringComparer.Ordinal);
    private int _versao;
    private DateTimeOffset? _abertaEm;
    private decimal? _saldo;

    public bool Abrir(int versao, DateTimeOffset momento) => Aplicar(versao, () => _abertaEm = momento);

    /// <summary>Depósito ou transferência recebida.</summary>
    public bool Entrada(int versao, DateTimeOffset momento, decimal valor, decimal saldoApos) =>
        Aplicar(versao, () =>
        {
            _saldo = saldoApos;
            _entradas.Add(new MovimentoHistorico(momento, valor));
            _entradas.RemoveAll(entrada => entrada.Momento < momento - JanelasFeatures.Retencao);
        });

    /// <summary>Transferência que saiu: entra no histórico e o IBAN vira beneficiário conhecido.</summary>
    public bool Saida(int versao, DateTimeOffset momento, decimal valor, string iban, decimal saldoApos) =>
        Aplicar(versao, () =>
        {
            _saldo = saldoApos;
            _saidas.Add(new MovimentoHistorico(momento, valor, iban));
            _saidas.RemoveAll(saida => saida.Momento < momento - JanelasFeatures.Retencao);
            _beneficiarios.Add(iban);
        });

    /// <summary>Outro movimento que só muda o saldo (saque, estorno, devolução, reconciliação).</summary>
    public bool Movimento(int versao, decimal saldoApos) => Aplicar(versao, () => _saldo = saldoApos);

    /// <summary>O histórico como a avaliação o leria em <paramref name="agora"/>.</summary>
    public HistoricoConta Retrato(DateTimeOffset agora) => new(
        _abertaEm,
        _saldo,
        _saidas.Where(saida => saida.Momento >= agora - JanelasFeatures.Retencao).ToList(),
        _entradas.Where(entrada => entrada.Momento >= agora - JanelasFeatures.Retencao).ToList(),
        new HashSet<string>(_beneficiarios, StringComparer.Ordinal));

    private bool Aplicar(int versao, Action mudanca)
    {
        if (versao <= _versao)
        {
            return false;
        }

        _versao = versao;
        mudanca();
        return true;
    }
}
