using EuroBank.Extrato.Domain.Enums;
using EuroBank.Extrato.Domain.ValueObjects;
using EuroBank.Shared.Enums;
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

    /// <summary>A moeda da conta, vinda do ContaAberta; todos os valores do extrato estão nela.</summary>
    public Moeda Moeda { get; private set; }

    /// <summary>Saldo depois do último evento aplicado (vem do saldoApos do evento, não é recalculado).</summary>
    public Saldo SaldoAtual
    {
        get => Saldo.De(_saldoAtual, Moeda);
        private set => _saldoAtual = value.Valor;
    }

    private decimal _saldoAtual;

    public int UltimaVersao { get; private set; }

    public DateTimeOffset AtualizadoEm { get; private set; }

    public IReadOnlyList<Lancamento> Lancamentos => _lancamentos.AsReadOnly();

    public static ExtratoConta Abrir(
        Guid contaId,
        string iban,
        string titular,
        int versao,
        DateTimeOffset ocorridoEm,
        Moeda moeda = Moeda.EUR) =>
        new()
        {
            ContaId = contaId,
            Iban = iban,
            Titular = titular,
            Moeda = moeda,
            Status = StatusConta.Ativa,
            SaldoAtual = Saldo.ZeroEm(moeda),
            UltimaVersao = versao,
            AtualizadoEm = ocorridoEm,
        };

    public ResultadoAplicacao RegistrarDeposito(int versao, Dinheiro valor, Saldo saldoApos, DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(TipoLancamento.Deposito, versao, valor, saldoApos, ocorridoEm, "Depósito", transferenciaId: null);

    public ResultadoAplicacao RegistrarSaque(int versao, Dinheiro valor, Saldo saldoApos, DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(TipoLancamento.Saque, versao, valor, saldoApos, ocorridoEm, "Saque", transferenciaId: null);

    public ResultadoAplicacao RegistrarTransferenciaEnviada(
        int versao,
        Guid transferenciaId,
        string ibanDestino,
        Dinheiro valor,
        Saldo saldoApos,
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
        Saldo saldoApos,
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
        Saldo saldoApos,
        DateTimeOffset ocorridoEm,
        CambioDoLancamento? cambio = null) =>
        RegistrarLancamento(
            TipoLancamento.TransferenciaExternaEnviada,
            versao,
            valor,
            saldoApos,
            ocorridoEm,
            ComDescricao($"Transferência para {nomeBeneficiario} ({ibanDestino})", descricao),
            transferenciaId,
            tarifa,
            cambio);

    /// <summary>Débito de um câmbio para outra conta EuroBank; <paramref name="destino"/> é o que ela recebeu.</summary>
    public ResultadoAplicacao RegistrarTransferenciaCambioEnviada(
        int versao,
        Guid transferenciaId,
        string ibanDestino,
        string nomeDestino,
        string? descricao,
        Dinheiro valor,
        Dinheiro tarifa,
        Saldo saldoApos,
        CambioDoLancamento destino,
        DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(
            TipoLancamento.TransferenciaCambioEnviada,
            versao,
            valor,
            saldoApos,
            ocorridoEm,
            ComDescricao($"Câmbio para {nomeDestino} ({ibanDestino}): {destino.Valor:0.00} {destino.Moeda}", descricao),
            transferenciaId,
            tarifa,
            destino);

    /// <summary>Crédito de um câmbio vindo de outra conta EuroBank; <paramref name="origem"/> é o que ela pagou.</summary>
    public ResultadoAplicacao RegistrarTransferenciaCambioRecebida(
        int versao,
        Guid transferenciaId,
        string ibanOrigem,
        string nomeOrigem,
        string? descricao,
        Dinheiro valor,
        Saldo saldoApos,
        CambioDoLancamento origem,
        DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(
            TipoLancamento.TransferenciaCambioRecebida,
            versao,
            valor,
            saldoApos,
            ocorridoEm,
            ComDescricao($"Câmbio de {nomeOrigem} ({ibanOrigem}): {origem.Valor:0.00} {origem.Moeda}", descricao),
            transferenciaId,
            tarifa: null,
            origem);

    /// <summary>O banco externo devolveu a transferência: o valor voltou para a conta (a tarifa, não).</summary>
    public ResultadoAplicacao RegistrarDevolucaoTransferenciaExterna(
        int versao,
        Guid transferenciaId,
        string nomeBeneficiario,
        Dinheiro valor,
        Saldo saldoApos,
        DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(
            TipoLancamento.DevolucaoTransferenciaExterna,
            versao,
            valor,
            saldoApos,
            ocorridoEm,
            $"Devolução da transferência para {nomeBeneficiario}",
            transferenciaId);

    /// <summary>Débito de reconciliação: o banco liquidou depois do estorno; o saldo pode ficar negativo.</summary>
    public ResultadoAplicacao RegistrarDebitoReconciliacao(
        int versao,
        Guid transferenciaId,
        string nomeBeneficiario,
        Dinheiro valor,
        Saldo saldoApos,
        DateTimeOffset ocorridoEm) =>
        RegistrarLancamento(
            TipoLancamento.DebitoReconciliacao,
            versao,
            valor,
            saldoApos,
            ocorridoEm,
            $"Reconciliação da transferência para {nomeBeneficiario}",
            transferenciaId);

    private static string ComDescricao(string texto, string? descricao) =>
        string.IsNullOrWhiteSpace(descricao) ? texto : $"{texto}: {descricao}";

    public ResultadoAplicacao RegistrarEstornoTransferencia(
        int versao,
        Guid transferenciaId,
        string nomeBeneficiario,
        Dinheiro valor,
        Saldo saldoApos,
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
        Saldo saldoApos,
        DateTimeOffset ocorridoEm,
        string descricao,
        Guid? transferenciaId,
        Dinheiro? tarifa = null,
        CambioDoLancamento? cambio = null) =>
        Aplicar(versao, ocorridoEm, () =>
        {
            _lancamentos.Add(new Lancamento(tipo, valor, saldoApos, versao, ocorridoEm, descricao, transferenciaId, tarifa, cambio));
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
