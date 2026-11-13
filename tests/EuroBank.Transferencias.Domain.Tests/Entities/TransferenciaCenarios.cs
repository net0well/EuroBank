using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using EuroBank.Transferencias.Domain.Entities;
using EuroBank.Transferencias.Domain.Enums;
using EuroBank.Transferencias.Domain.ValueObjects;

namespace EuroBank.Transferencias.Domain.Tests.Entities;

/// <summary>Monta transferências já paradas em cada passo da saga, sem eventos pendentes.</summary>
internal static class TransferenciaCenarios
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 9, 10, 0, 0, TimeSpan.Zero);

    public static readonly PoliticaSaga Politica = PoliticaSaga.Padrao;

    public static readonly Guid ContaOrigem = Guid.CreateVersion7();

    public static readonly ContaEuroBank Origem = new(ContaOrigem, "PT50EURO0000000000001", "Ana", Moeda.EUR);

    public static readonly ContaEuroBank DestinoEmDolar = new(Guid.CreateVersion7(), "PT50EURO0000000000002", "Bruno", Moeda.USD);

    public const string IbanComum = "DE89370400440532013000";

    public static Transferencia NovaExterna(
        decimal valor = 100m,
        DateTimeOffset? executarEm = null,
        string iban = IbanComum,
        string? bic = "DEUTDEFF",
        string? beneficiario = "Bruno",
        Moeda? moedaDestino = null,
        Moeda? moedaDoValor = null,
        ContaEuroBank? origem = null)
    {
        var conta = origem ?? Origem;
        return Transferencia.SolicitarExterna(
            conta,
            IbanDestino.Criar(iban),
            bic,
            beneficiario,
            Dinheiro.De(valor, moedaDoValor ?? conta.Moeda),
            moedaDestino ?? conta.Moeda,
            "Aluguel",
            null,
            "chave-1",
            "hash-1",
            executarEm,
            Agora);
    }

    public static Transferencia NovoCambioInterno(decimal valor = 100m, Moeda moedaDoValor = Moeda.USD) =>
        Transferencia.SolicitarCambioInterno(
            Origem, DestinoEmDolar, Dinheiro.De(valor, moedaDoValor), null, null, "chave-2", "hash-2", null, Agora);

    /// <summary>Cotação de 100 USD saindo de EUR a 1,0692 (93,53 EUR), válida por 10 minutos.</summary>
    public static Transferencia CotarCem(Transferencia transferencia, DateTimeOffset? agora = null, decimal jaTransferido = 0m) =>
        Registrar(transferencia, t => t.RegistrarCotacaoEmitida(
            new CotacaoTravada(Guid.CreateVersion7(), 1.0692m, (agora ?? Agora).AddMinutes(10)),
            Dinheiro.De(93.53m, Moeda.EUR),
            Dinheiro.De(100m, Moeda.USD),
            Dinheiro.De(jaTransferido, Moeda.EUR),
            Politica,
            agora ?? Agora));

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

    /// <summary>Externa estornada porque a consulta disse NaoLiquidada (o caminho do final 95).</summary>
    public static Transferencia CanceladaSemRespostaDoBanco()
    {
        var transferencia = ConsultandoLiquidacao();
        transferencia.RegistrarStatusLiquidacao(StatusLiquidacaoExterna.NaoLiquidada, Politica, Agora);
        transferencia.RegistrarEstornoRealizado(Agora);
        transferencia.LimparEventos();
        return transferencia;
    }

    /// <summary>Confirmação do banco depois do estorno.</summary>
    public static Transferencia Divergente()
    {
        var transferencia = CanceladaSemRespostaDoBanco();
        transferencia.RegistrarLiquidacaoConfirmada(Agora);
        transferencia.LimparEventos();
        return transferencia;
    }

    public static Transferencia Concluida()
    {
        var transferencia = AguardandoLiquidacao();
        transferencia.RegistrarLiquidacaoConfirmada(Agora);
        transferencia.LimparEventos();
        return transferencia;
    }

    /// <summary>Câmbio interno de 100 USD já cotado, debitado e com o crédito pedido.</summary>
    public static Transferencia AguardandoCredito()
    {
        var transferencia = NovoCambioInterno();
        transferencia.Iniciar(Dinheiro.Zero, Politica, Agora);
        CotarCem(transferencia);
        transferencia.RegistrarDebitoRealizado(Politica, Agora);
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

    private static Transferencia Registrar(Transferencia transferencia, Action<Transferencia> acao)
    {
        acao(transferencia);
        return transferencia;
    }
}
