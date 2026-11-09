using EuroBank.Extrato.Domain.Enums;
using EuroBank.Shared.ValueObjects;

namespace EuroBank.Extrato.Domain.Entities;

/// <summary>
/// Projeção do extrato de uma conta, montada a partir dos eventos de integração do contexto Contas.
/// Regra central de idempotência: cada evento traz a versão da conta; versão menor ou igual à última aplicada é
/// ignorada, então reprocessar mensagens (duplicatas, reset de offset) nunca altera o saldo.
/// </summary>
public sealed class ExtratoConta
{
    private readonly List<Lancamento> _lancamentos = [];

    private ExtratoConta()
    {
        // Usado pelo EF Core.
    }

    public Guid ContaId { get; private set; }

    public string Iban { get; private set; } = string.Empty;

    public string Titular { get; private set; } = string.Empty;

    public StatusConta Status { get; private set; }

    /// <summary>Saldo depois do último evento aplicado (vem do saldoApos do evento, não é recalculado).</summary>
    public Dinheiro SaldoAtual { get; private set; } = Dinheiro.Zero;

    public int UltimaVersao { get; private set; }

    public DateTimeOffset AtualizadoEm { get; private set; }

    public IReadOnlyList<Lancamento> Lancamentos => _lancamentos.AsReadOnly();

    public static ExtratoConta Abrir(Guid contaId, string iban, string titular, int versao, DateTimeOffset ocorridoEm) =>
        new()
        {
            ContaId = contaId,
            Iban = iban,
            Titular = titular,
            Status = StatusConta.Ativa,
            SaldoAtual = Dinheiro.Zero,
            UltimaVersao = versao,
            AtualizadoEm = ocorridoEm,
        };

    public ResultadoAplicacao RegistrarDeposito(int versao, Dinheiro valor, Dinheiro saldoApos, DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(TipoLancamento.Deposito, versao, valor, saldoApos, ocorridoEm, "Depósito", transferenciaId: null);

    public ResultadoAplicacao RegistrarSaque(int versao, Dinheiro valor, Dinheiro saldoApos, DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(TipoLancamento.Saque, versao, valor, saldoApos, ocorridoEm, "Saque", transferenciaId: null);

    public ResultadoAplicacao RegistrarTransferenciaEnviada(
        int versao,
        Guid transferenciaId,
        string ibanDestino,
        Dinheiro valor,
        Dinheiro saldoApos,
        DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(
            TipoLancamento.TransferenciaEnviada,
            versao,
            valor,
            saldoApos,
            ocorridoEm,
            $"Transferência para {ibanDestino}",
            transferenciaId);

    public ResultadoAplicacao RegistrarTransferenciaRecebida(
        int versao,
        Guid transferenciaId,
        string ibanOrigem,
        Dinheiro valor,
        Dinheiro saldoApos,
        DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(
            TipoLancamento.TransferenciaRecebida,
            versao,
            valor,
            saldoApos,
            ocorridoEm,
            $"Transferência de {ibanOrigem}",
            transferenciaId);

    public ResultadoAplicacao AlterarStatus(StatusConta status, int versao, DateTimeOffset ocorridoEm) =>
        Aplicar(versao, ocorridoEm, () => Status = status);

    public ResultadoAplicacao RegistrarTransferenciaExternaEnviada(
        int versao,
        Guid transferenciaId,
        string ibanDestino,
        string nomeBeneficiario,
        string? descricao,
        Dinheiro valor,
        Dinheiro tarifa,
        Dinheiro saldoApos,
        DateTimeOffset ocorridoEm)
    {
        var texto = $"Transferência para {nomeBeneficiario} ({ibanDestino})";
        if (!string.IsNullOrWhiteSpace(descricao))
        {
            texto += $": {descricao}";
        }

        return RegistrarLancamento(
            TipoLancamento.TransferenciaExternaEnviada, versao, valor, saldoApos, ocorridoEm, texto, transferenciaId, tarifa);
    }

    public ResultadoAplicacao RegistrarEstornoTransferencia(
        int versao,
        Guid transferenciaId,
        string nomeBeneficiario,
        Dinheiro valor,
        Dinheiro saldoApos,
        DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(
            TipoLancamento.EstornoTransferencia,
            versao,
            valor,
            saldoApos,
            ocorridoEm,
            $"Estorno da transferência para {nomeBeneficiario}",
            transferenciaId);

    private ResultadoAplicacao RegistrarLancamento(
        TipoLancamento tipo,
        int versao,
        Dinheiro valor,
        Dinheiro saldoApos,
        DateTimeOffset ocorridoEm,
        string descricao,
        Guid? transferenciaId,
        Dinheiro? tarifa = null) =>
        Aplicar(versao, ocorridoEm, () =>
        {
            _lancamentos.Add(new Lancamento(tipo, valor, saldoApos, versao, ocorridoEm, descricao, transferenciaId, tarifa));
            SaldoAtual = saldoApos;
        });

    private ResultadoAplicacao Aplicar(int versao, DateTimeOffset ocorridoEm, Action aplicar)
    {
        if (versao <= UltimaVersao)
        {
            return ResultadoAplicacao.IgnoradoJaAplicado;
        }

        var houveLacuna = versao > UltimaVersao + 1;
        aplicar();
        UltimaVersao = versao;
        AtualizadoEm = ocorridoEm;
        return houveLacuna ? ResultadoAplicacao.AplicadoComLacuna : ResultadoAplicacao.Aplicado;
    }
}
