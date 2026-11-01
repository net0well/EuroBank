using EuroBank.Contas.Dominio.Comum;

namespace EuroBank.Contas.Dominio.Contas.Eventos;

/// <summary>
/// Evento de domínio de uma conta. <see cref="Versao"/> é a versão da conta depois do evento: cada operação
/// incrementa a versão em 1, o que permite ao Extrato descartar eventos repetidos.
/// </summary>
public interface IEventoDeConta : IEventoDominio
{
    ContaId ContaId { get; }

    int Versao { get; }
}

public sealed record ContaAberta(ContaId ContaId, Iban Iban, Titular Titular, int Versao, DateTimeOffset OcorridoEm)
    : IEventoDeConta;

public sealed record DepositoRealizado(
    ContaId ContaId, Dinheiro Valor, Dinheiro SaldoApos, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;

public sealed record SaqueRealizado(
    ContaId ContaId, Dinheiro Valor, Dinheiro SaldoApos, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;

public sealed record TransferenciaEnviada(
    ContaId ContaId,
    Guid TransferenciaId,
    Iban IbanDestino,
    Dinheiro Valor,
    Dinheiro SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;

public sealed record TransferenciaRecebida(
    ContaId ContaId,
    Guid TransferenciaId,
    Iban IbanOrigem,
    Dinheiro Valor,
    Dinheiro SaldoApos,
    int Versao,
    DateTimeOffset OcorridoEm) : IEventoDeConta;

public sealed record ContaBloqueada(ContaId ContaId, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;

public sealed record ContaDesbloqueada(ContaId ContaId, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;

public sealed record ContaEncerrada(ContaId ContaId, int Versao, DateTimeOffset OcorridoEm) : IEventoDeConta;
