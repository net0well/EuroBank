using EuroBank.Extrato.Application.Extensions;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Extrato.Application.Handlers;

public sealed class TransferenciaRecebidaManipulador(
    IRepositorioExtratos repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ILogger<TransferenciaRecebidaManipulador> logger)
{
    public async Task Handle(EventoTransferenciaRecebida evento, CancellationToken cancellationToken)
    {
        var extrato = await repositorio.ObterOuFalharAsync(evento.ContaId, cancellationToken);

        var resultado = extrato.RegistrarTransferenciaRecebida(
            evento.Versao,
            evento.TransferenciaId,
            evento.IbanOrigem,
            Dinheiro.De(evento.Valor, extrato.Moeda),
            Dinheiro.De(evento.SaldoApos, extrato.Moeda),
            evento.OcorridoEm);

        if (resultado != ResultadoAplicacao.IgnoradoJaAplicado)
        {
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        }

        logger.RegistrarResultado(resultado, "TransferenciaRecebida", evento.ContaId, evento.Versao);
    }
}
