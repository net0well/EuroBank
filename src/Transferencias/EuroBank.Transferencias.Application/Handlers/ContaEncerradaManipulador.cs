using EuroBank.Transferencias.Application.Interfaces;
using EuroBank.Transferencias.Application.TransferObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Transferencias.Application.Handlers;

/// <summary>Marca na projeção local as contas encerradas em Contas (a reconciliação de uma delas é recusada).</summary>
public sealed class ContaEncerradaManipulador(IContasConhecidas contas, ILogger<ContaEncerradaManipulador> logger)
{
    public async Task Handle(EventoContaEncerrada evento, CancellationToken cancellationToken)
    {
        var marcada = await contas.RegistrarEncerramentoAsync(evento.ContaId, evento.OcorridoEm, cancellationToken);

        logger.LogInformation(
            marcada ? "Conta {ContaId} encerrada na projeção" : "Encerramento da conta {ContaId} já registrado ou conta desconhecida",
            evento.ContaId);
    }
}
