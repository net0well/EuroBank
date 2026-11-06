using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Infrastructure.Messaging;
using Wolverine;
using Wolverine.Util;

namespace EuroBank.Extrato.Worker.Tests.Messaging;

public sealed class MapeadorEnvelopeDebeziumTestes
{
    private readonly MapeadorEnvelopeDebezium _mapeador = new();

    private static Message<string, byte[]> Mensagem(string? tipo, string? id = null, string? traceparent = null)
    {
        var headers = new Headers();
        if (id is not null)
        {
            headers.Add("id", Encoding.UTF8.GetBytes(id));
        }

        if (tipo is not null)
        {
            headers.Add("tipo", Encoding.UTF8.GetBytes(tipo));
        }

        if (traceparent is not null)
        {
            headers.Add("traceparent", Encoding.UTF8.GetBytes(traceparent));
        }

        return new Message<string, byte[]>
        {
            Key = "01a195bf-84ca-779f-aefd-d2ff7931f2e4",
            Value = Encoding.UTF8.GetBytes("""{"contaId":"01a195bf-84ca-779f-aefd-d2ff7931f2e4","valor":"10.00"}"""),
            Headers = headers,
        };
    }

    [Fact]
    public void TipoConhecido_DefineTipoDaMensagemIdDoEventoTraceECorpo()
    {
        var eventoId = Guid.CreateVersion7();
        const string traceparent = "00-4ec0e9a4e59373e59c1b456265157e28-f6186d492757de83-01";
        var mensagem = Mensagem("DepositoRealizado", eventoId.ToString(), traceparent);
        var envelope = new Envelope();

        _mapeador.MapIncomingToEnvelope(envelope, mensagem);

        envelope.MessageType.ShouldBe(typeof(EventoDepositoRealizado).ToMessageTypeName());
        envelope.Id.ShouldBe(eventoId);
        envelope.ParentId.ShouldBe(traceparent);
        envelope.ContentType.ShouldBe("application/json");
        envelope.Data.ShouldBe(mensagem.Value);
    }

    [Theory]
    [InlineData("ContaAberta", typeof(EventoContaAberta))]
    [InlineData("DepositoRealizado", typeof(EventoDepositoRealizado))]
    [InlineData("SaqueRealizado", typeof(EventoSaqueRealizado))]
    [InlineData("TransferenciaEnviada", typeof(EventoTransferenciaEnviada))]
    [InlineData("TransferenciaRecebida", typeof(EventoTransferenciaRecebida))]
    [InlineData("ContaBloqueada", typeof(EventoContaBloqueada))]
    [InlineData("ContaDesbloqueada", typeof(EventoContaDesbloqueada))]
    [InlineData("ContaEncerrada", typeof(EventoContaEncerrada))]
    public void CadaTipoPublicadoPorContas_TemUmTipoDeMensagem(string tipo, Type esperado)
    {
        var envelope = new Envelope();

        _mapeador.MapIncomingToEnvelope(envelope, Mensagem(tipo, Guid.CreateVersion7().ToString()));

        envelope.MessageType.ShouldBe(esperado.ToMessageTypeName());
    }

    [Fact]
    public void TipoDesconhecido_ViraEventoDesconhecidoComONomeDoTipo()
    {
        var envelope = new Envelope();

        _mapeador.MapIncomingToEnvelope(envelope, Mensagem("ContaPromovidaParaVip", Guid.CreateVersion7().ToString()));

        envelope.MessageType.ShouldBe(typeof(EventoDesconhecido).ToMessageTypeName());
        JsonSerializer.Deserialize<EventoDesconhecido>(envelope.Data!, JsonSerializerOptions.Web)!.Tipo
            .ShouldBe("ContaPromovidaParaVip");
    }

    [Fact]
    public void SemHeaderTipo_ViraMensagemInvalida()
    {
        var envelope = new Envelope();

        _mapeador.MapIncomingToEnvelope(envelope, Mensagem(tipo: null, Guid.CreateVersion7().ToString()));

        envelope.MessageType.ShouldBe(typeof(MensagemInvalida).ToMessageTypeName());
    }

    [Fact]
    public void HeaderIdInvalido_MantemOIdQueOWolverineAtribuiu()
    {
        var envelope = new Envelope();
        var idOriginal = envelope.Id;

        _mapeador.MapIncomingToEnvelope(envelope, Mensagem("DepositoRealizado", id: "nao-e-um-guid"));

        envelope.Id.ShouldBe(idOriginal);
    }
}
