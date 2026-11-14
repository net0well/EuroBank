using EuroBank.Notificacoes.Worker.TransferObjects;

namespace EuroBank.Notificacoes.Worker.Tests.Fixtures;

/// <summary>Eventos finais de transferência como chegam de eurobank.transferencias.eventos.</summary>
internal static class Eventos
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 13, 17, 5, 0, TimeSpan.Zero);

    public static ResumoTransferencia Resumo(
        string? email = "ana@exemplo.com",
        string beneficiario = "Bruno Souza",
        string? descricao = "Aluguel de novembro") =>
        new(
            "Externa",
            "Ana Lima",
            beneficiario,
            "DE89370400440532013000",
            1_234.50m,
            "EUR",
            2.50m,
            "EUR",
            1_234.50m,
            1_234.50m,
            "EUR",
            null,
            descricao,
            email);

    /// <summary>Externa de 100 USD saindo de uma conta EUR (93,53 EUR a 1,0692).</summary>
    public static ResumoTransferencia ResumoComCambio(string? email = "ana@exemplo.com") =>
        new(
            "Externa",
            "Ana Lima",
            "John Smith",
            "US12345678901234567890",
            100m,
            "USD",
            2.50m,
            "EUR",
            93.53m,
            100m,
            "USD",
            1.0692m,
            null,
            email);

    public static EventoTransferenciaConcluida Concluida(ResumoTransferencia? resumo = null, string? motivo = null)
    {
        var r = resumo ?? Resumo();
        return new(Guid.CreateVersion7(), Guid.CreateVersion7(), r.ValorDebitado ?? 0m, r.Tarifa, r.MoedaOrigem, motivo, r, Agora);
    }

    public static EventoTransferenciaCancelada Cancelada(string motivo = "RecusadaPeloBanco", ResumoTransferencia? resumo = null) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), motivo, resumo ?? Resumo(), Agora);

    public static EventoTransferenciaRecusada Recusada(string motivo = "SaldoInsuficiente", ResumoTransferencia? resumo = null) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), motivo, resumo ?? Resumo(), Agora);

    public static EventoTransferenciaDivergente Divergente(string motivo = "ConfirmadaAposEstorno", ResumoTransferencia? resumo = null) =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), motivo, resumo ?? Resumo(), Agora);

    public static EventoTransferenciaDevolvida Devolvida(ResumoTransferencia? resumo = null)
    {
        var r = resumo ?? Resumo();
        return new(Guid.CreateVersion7(), Guid.CreateVersion7(), r.ValorDebitado ?? 0m, r.MoedaOrigem, r, Agora);
    }

    /// <summary>Destinatário único por teste: os testes rodam em paralelo contra o mesmo enviador falso.</summary>
    public static string EmailUnico() => $"cliente-{Guid.NewGuid():N}@exemplo.com";
}
