using EuroBank.Antifraude.Application.Extensions;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>Transferência que saiu da conta: entra no histórico (mesmo se depois for estornada) e muda o saldo.</summary>
public sealed class SaidaContaManipulador(IPerfisContas perfis, ILogger<SaidaContaManipulador> logger)
{
    public async Task Handle(EventoSaidaConta evento, CancellationToken cancellationToken)
    {
        var aplicado = await perfis.AplicarAsync(
            new AtualizacaoPerfil(
                evento.ContaId,
                evento.Versao,
                evento.OcorridoEm,
                SaldoApos: evento.SaldoApos,
                Saida: new SaidaPerfil(evento.TransferenciaId, evento.Valor, evento.IbanDestino)),
            cancellationToken);
        logger.RegistrarPerfil(aplicado, "Saida", evento.ContaId, evento.Versao);
    }
}
