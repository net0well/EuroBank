using EuroBank.Shared.Enums;
using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.TransferObjects;
using EuroBank.Transferencias.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>Alimenta a projeção local de contas com cada conta aberta em Contas.</summary>
public sealed class ContaAbertaManipulador(IContasConhecidas contas, ILogger<ContaAbertaManipulador> logger)
{
    public async Task Handle(EventoContaAberta evento, CancellationToken cancellationToken)
    {
        var moeda = string.IsNullOrWhiteSpace(evento.Moeda) ? Moeda.EUR : Moedas.Ler(evento.Moeda);
        var nova = await contas.RegistrarAsync(
            new ContaEuroBank(evento.ContaId, evento.Iban, evento.Titular, moeda), evento.OcorridoEm, cancellationToken);

        logger.LogInformation(
            nova ? "Conta {ContaId} ({Iban}, {Moeda}) agora conhecida" : "Conta {ContaId} ({Iban}, {Moeda}) já era conhecida",
            evento.ContaId, evento.Iban, moeda);
    }
}
