using EuroBank.Antifraude.Application.Extensions;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>Depósito ou transferência recebida: entra no histórico de entradas e muda o saldo.</summary>
public sealed class EntradaContaManipulador(IPerfisContas perfis, ILogger<EntradaContaManipulador> logger)
{
    public async Task Handle(EventoEntradaConta evento, CancellationToken cancellationToken)
    {
        var aplicado = await perfis.AplicarAsync(
            new AtualizacaoPerfil(
                evento.ContaId,
                evento.Versao,
                evento.OcorridoEm,
                SaldoApos: evento.SaldoApos,
                Entrada: new EntradaPerfil(evento.Valor)),
            cancellationToken);
        logger.RegistrarPerfil(aplicado, "Entrada", evento.ContaId, evento.Versao);
    }
}
