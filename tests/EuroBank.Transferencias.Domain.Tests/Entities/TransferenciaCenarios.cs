using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

/// <summary>Monta transferências já paradas em cada passo da saga, sem eventos pendentes.</summary>
internal static class TransferenciaCenarios
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);

    public static readonly PoliticaSaga Politica = PoliticaSaga.Padrao;

    public static readonly Guid ContaOrigem = Guid.CreateVersion7();

    public const string IbanComum = "DE89370400440532013000";

    public static Transferencia NovaExterna(
        decimal valor = 100m,
        DateTimeOffset? executarEm = null,
        string iban = IbanComum,
        string? bic = "DEUTDEFF",
        string? beneficiario = "Bruno") =>
        Transferencia.SolicitarExterna(
            ContaOrigem,
            IbanDestino.Criar(iban),
            bic,
            beneficiario,
            Dinheiro.De(valor),
            "Aluguel",
            null,
            "chave-1",
            executarEm,
            Agora);

    public static Transferencia AguardandoDebito()
    {
        var transferencia = NovaExterna();
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);
        transferencia.LimparEventos();
        return transferencia;
    }

    public static Transferencia AguardandoLiquidacao()
    {
        var transferencia = AguardandoDebito();
        transferencia.RegistrarDebitoRealizado(Politica, Agora);
        transferencia.LimparEventos();
        return transferencia;
    }

    public static Transferencia ConsultandoLiquidacao()
    {
        var transferencia = AguardandoLiquidacao();
        EsgotarPrazos(transferencia, Politica.PrazoLiquidacao);
        transferencia.LimparEventos();
        return transferencia;
    }

    public static Transferencia AguardandoEstorno()
    {
        var transferencia = AguardandoLiquidacao();
        transferencia.RegistrarLiquidacaoRecusada(Politica, Agora);
        transferencia.LimparEventos();
        return transferencia;
    }

    /// <summary>Faz o tempo passar prazo a prazo até o passo esgotar o envio original e os reenvios.</summary>
    public static DateTimeOffset EsgotarPrazos(Transferencia transferencia, TimeSpan prazo, DateTimeOffset? inicio = null)
    {
        var instante = inicio ?? Agora;
        for (var vez = 0; vez <= Politica.ReenviosPorPasso; vez++)
        {
            instante += prazo;
            transferencia.VerificarPrazo(Politica, instante);
        }

        return instante;
    }
}
