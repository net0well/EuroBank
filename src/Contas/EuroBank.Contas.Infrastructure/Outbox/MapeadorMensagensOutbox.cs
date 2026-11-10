using System.Globalization;
using System.Text.Json;
using EuroBank.Contas.Domain.Events;
using EuroBank.Contas.Domain.ValueObjects;
using EuroBank.Contas.Infrastructure.Outbox.TransferObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Persistence.Outbox;

namespace EuroBank.Contas.Infrastructure.Outbox;

/// <summary>
/// Converte eventos de domínio em linhas da outbox. O mapeamento é explícito: o payload publicado é um contrato
/// de integração, independente da forma interna dos eventos de domínio.
/// </summary>
public sealed class MapeadorMensagensOutbox : IMapeadorMensagensOutbox
{
    /// <summary>Valor de tipo_agregado dos eventos de conta; o Debezium roteia para eurobank.contas.eventos.</summary>
    public const string TipoAgregado = "contas.eventos";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public MensagemOutbox Mapear(IEventoDominio evento, string? traceparent) => evento switch
    {
        ContaAberta e => Criar(e, traceparent, new ContaAbertaMensagem(
            e.ContaId.Valor, e.Iban.Valor, e.Titular.Nome, e.Moeda.ToString(), e.Versao, e.OcorridoEm)),
        DepositoRealizado e => Criar(e, traceparent, new MovimentacaoMensagem(
            e.ContaId.Valor, e.Valor.ToString(), e.SaldoApos.ToString(), e.Versao, e.OcorridoEm)),
        SaqueRealizado e => Criar(e, traceparent, new MovimentacaoMensagem(
            e.ContaId.Valor, e.Valor.ToString(), e.SaldoApos.ToString(), e.Versao, e.OcorridoEm)),
        TransferenciaEnviada e => Criar(e, traceparent, new TransferenciaEnviadaMensagem(
            e.ContaId.Valor,
            e.TransferenciaId,
            e.IbanDestino.Valor,
            e.Valor.ToString(),
            e.SaldoApos.ToString(),
            e.Versao,
            e.OcorridoEm)),
        TransferenciaRecebida e => Criar(e, traceparent, new TransferenciaRecebidaMensagem(
            e.ContaId.Valor,
            e.TransferenciaId,
            e.IbanOrigem.Valor,
            e.Valor.ToString(),
            e.SaldoApos.ToString(),
            e.Versao,
            e.OcorridoEm)),
        ContaBloqueada e => Criar(e, traceparent, new AlteracaoStatusMensagem(e.ContaId.Valor, e.Versao, e.OcorridoEm)),
        ContaDesbloqueada e => Criar(e, traceparent, new AlteracaoStatusMensagem(e.ContaId.Valor, e.Versao, e.OcorridoEm)),
        ContaEncerrada e => Criar(e, traceparent, new AlteracaoStatusMensagem(e.ContaId.Valor, e.Versao, e.OcorridoEm)),
        TransferenciaExternaEnviada e => Criar(e, traceparent, new TransferenciaExternaEnviadaMensagem(
            e.ContaId.Valor,
            e.TransferenciaId,
            e.IbanDestino,
            e.NomeBeneficiario,
            e.Descricao,
            e.Valor.ToString(),
            e.Tarifa.ToString(),
            e.SaldoApos.ToString(),
            e.Versao,
            e.OcorridoEm,
            e.Conversao?.Contraparte.Moeda.ToString(),
            e.Conversao?.Contraparte.ToString(),
            Taxa(e.Conversao))),
        TransferenciaCambioEnviada e => Criar(e, traceparent, new TransferenciaCambioEnviadaMensagem(
            e.ContaId.Valor,
            e.TransferenciaId,
            e.IbanDestino,
            e.NomeDestino,
            e.Descricao,
            e.Valor.ToString(),
            e.Tarifa.ToString(),
            e.Conversao.Contraparte.Moeda.ToString(),
            e.Conversao.Contraparte.ToString(),
            Taxa(e.Conversao)!,
            e.SaldoApos.ToString(),
            e.Versao,
            e.OcorridoEm)),
        TransferenciaCambioRecebida e => Criar(e, traceparent, new TransferenciaCambioRecebidaMensagem(
            e.ContaId.Valor,
            e.TransferenciaId,
            e.IbanOrigem,
            e.NomeOrigem,
            e.Descricao,
            e.Valor.ToString(),
            e.Conversao.Contraparte.Moeda.ToString(),
            e.Conversao.Contraparte.ToString(),
            Taxa(e.Conversao)!,
            e.SaldoApos.ToString(),
            e.Versao,
            e.OcorridoEm)),
        EstornoTransferencia e => Criar(e, traceparent, new EstornoTransferenciaMensagem(
            e.ContaId.Valor,
            e.TransferenciaId,
            e.NomeBeneficiario,
            e.Valor.ToString(),
            e.SaldoApos.ToString(),
            e.Versao,
            e.OcorridoEm)),
        _ => throw new InvalidOperationException($"O evento {evento.GetType().Name} não tem mapeamento para a outbox."),
    };

    /// <summary>A taxa vai como texto, como os valores, para não perder casas decimais no caminho.</summary>
    private static string? Taxa(Conversao? conversao) =>
        conversao?.Taxa.ToString(CultureInfo.InvariantCulture);

    private static MensagemOutbox Criar<TPayload>(IEventoDeConta evento, string? traceparent, TPayload payload) =>
        new(
            Guid.CreateVersion7(),
            TipoAgregado,
            evento.ContaId.ToString(),
            evento.GetType().Name,
            JsonSerializer.Serialize(payload, Json),
            evento.OcorridoEm,
            traceparent);
}
