using System.Text.Json;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.Events;
using EuroBank.Transferencias.Domain.ValueObjects;
using EuroBank.Transferencias.Infrastructure.Outbox;

namespace EuroBank.Transferencias.Worker.Tests.Outbox;

/// <summary>Os cinco eventos finais saem em eurobank.transferencias.eventos com o resumo da transferência.</summary>
public sealed class MapeadorEventosFinaisTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);

    private static readonly TransferenciaId Id = TransferenciaId.Nova();

    private static readonly Guid Conta = Guid.CreateVersion7();

    private static readonly ResumoTransferencia ComCambio = new(
        TipoTransferencia.Externa,
        "Ana",
        "Bruno",
        "US12345678901234567890",
        Dinheiro.De(100m, Moeda.USD),
        Dinheiro.De(2.50m),
        Dinheiro.De(93.53m),
        Dinheiro.De(100m, Moeda.USD),
        Moeda.USD,
        1.0692m,
        "Aluguel",
        "ana@exemplo.com");

    private static readonly ResumoTransferencia SemCotacaoNemEmail = new(
        TipoTransferencia.CambioInterno,
        "Ana",
        "Carla",
        "PT50EURO0000000000002",
        Dinheiro.De(100m, Moeda.GBP),
        Dinheiro.De(15m, Moeda.BRL),
        null,
        null,
        Moeda.GBP,
        null,
        null,
        null);

    private readonly MapeadorMensagensOutbox _mapeador = new();

    [Fact]
    public void Concluida_LevaOResumoComValoresEmTexto()
    {
        var evento = new TransferenciaConcluida(Id, Conta, Dinheiro.De(93.53m), Dinheiro.De(2.50m), null, ComCambio, Agora);

        var mensagem = _mapeador.Mapear(evento, null);

        mensagem.TipoAgregado.ShouldBe("transferencias.eventos");
        mensagem.TipoEvento.ShouldBe("TransferenciaConcluida");
        mensagem.IdAgregado.ShouldBe(Id.ToString());
        using var payload = JsonDocument.Parse(mensagem.Payload);
        var raiz = payload.RootElement;
        raiz.GetProperty("valor").GetString().ShouldBe("93.53");
        raiz.GetProperty("tarifa").GetString().ShouldBe("2.50");
        raiz.GetProperty("moeda").GetString().ShouldBe("EUR");
        raiz.GetProperty("motivo").ValueKind.ShouldBe(JsonValueKind.Null);
        var resumo = raiz.GetProperty("resumo");
        resumo.GetProperty("tipo").GetString().ShouldBe("Externa");
        resumo.GetProperty("nomeTitular").GetString().ShouldBe("Ana");
        resumo.GetProperty("nomeBeneficiario").GetString().ShouldBe("Bruno");
        resumo.GetProperty("ibanDestino").GetString().ShouldBe("US12345678901234567890");
        resumo.GetProperty("valor").GetString().ShouldBe("100.00");
        resumo.GetProperty("moedaDoValor").GetString().ShouldBe("USD");
        resumo.GetProperty("tarifa").GetString().ShouldBe("2.50");
        resumo.GetProperty("moedaOrigem").GetString().ShouldBe("EUR");
        resumo.GetProperty("valorDebitado").GetString().ShouldBe("93.53");
        resumo.GetProperty("valorDestino").GetString().ShouldBe("100.00");
        resumo.GetProperty("moedaDestino").GetString().ShouldBe("USD");
        resumo.GetProperty("taxa").GetDecimal().ShouldBe(1.0692m);
        resumo.GetProperty("descricao").GetString().ShouldBe("Aluguel");
        resumo.GetProperty("emailNotificacao").GetString().ShouldBe("ana@exemplo.com");
    }

    [Fact]
    public void ConcluidaPorReconciliacao_LevaOMotivo()
    {
        var evento = new TransferenciaConcluida(
            Id, Conta, Dinheiro.De(93.53m), Dinheiro.De(2.50m), MotivoTransferencia.Reconciliada, ComCambio, Agora);

        using var payload = JsonDocument.Parse(_mapeador.Mapear(evento, null).Payload);

        payload.RootElement.GetProperty("motivo").GetString().ShouldBe("Reconciliada");
    }

    [Fact]
    public void Recusada_SemCotacaoNemEmail_LevaOsCamposNulos()
    {
        var evento = new TransferenciaRecusada(Id, Conta, MotivoTransferencia.CambioIndisponivel, SemCotacaoNemEmail, Agora);

        var mensagem = _mapeador.Mapear(evento, null);

        mensagem.TipoEvento.ShouldBe("TransferenciaRecusada");
        using var payload = JsonDocument.Parse(mensagem.Payload);
        payload.RootElement.GetProperty("motivo").GetString().ShouldBe("CambioIndisponivel");
        var resumo = payload.RootElement.GetProperty("resumo");
        resumo.GetProperty("tipo").GetString().ShouldBe("CambioInterno");
        resumo.GetProperty("valor").GetString().ShouldBe("100.00");
        resumo.GetProperty("moedaDoValor").GetString().ShouldBe("GBP");
        resumo.GetProperty("moedaOrigem").GetString().ShouldBe("BRL");
        resumo.GetProperty("moedaDestino").GetString().ShouldBe("GBP");
        resumo.GetProperty("valorDebitado").ValueKind.ShouldBe(JsonValueKind.Null);
        resumo.GetProperty("valorDestino").ValueKind.ShouldBe(JsonValueKind.Null);
        resumo.GetProperty("taxa").ValueKind.ShouldBe(JsonValueKind.Null);
        resumo.GetProperty("emailNotificacao").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public void CanceladaEDivergente_LevamOResumo()
    {
        var cancelada = _mapeador.Mapear(
            new TransferenciaCancelada(Id, Conta, MotivoTransferencia.RecusadaPeloBanco, ComCambio, Agora), null);
        var divergente = _mapeador.Mapear(
            new TransferenciaDivergente(Id, Conta, MotivoTransferencia.ConfirmadaAposEstorno, ComCambio, Agora), null);

        cancelada.TipoEvento.ShouldBe("TransferenciaCancelada");
        divergente.TipoEvento.ShouldBe("TransferenciaDivergente");
        foreach (var mensagem in new[] { cancelada, divergente })
        {
            using var payload = JsonDocument.Parse(mensagem.Payload);
            payload.RootElement.GetProperty("resumo").GetProperty("emailNotificacao").GetString().ShouldBe("ana@exemplo.com");
        }
    }

    [Fact]
    public void Devolvida_LevaOResumo()
    {
        var evento = new TransferenciaDevolvida(Id, Conta, Dinheiro.De(93.53m), ComCambio, Agora);

        using var payload = JsonDocument.Parse(_mapeador.Mapear(evento, null).Payload);

        payload.RootElement.GetProperty("valorDevolvido").GetString().ShouldBe("93.53");
        payload.RootElement.GetProperty("moeda").GetString().ShouldBe("EUR");
        payload.RootElement.GetProperty("resumo").GetProperty("nomeBeneficiario").GetString().ShouldBe("Bruno");
    }
}
