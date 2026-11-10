using EuroBank.Extrato.Application.Extensions;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Shared.Domain;
using EuroBank.Shared.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Extrato.Application.Handlers;

public sealed class DebitoReconciliacaoManipulador(
    IRepositorioExtratos repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ILogger<DebitoReconciliacaoManipulador> logger)
{
    public async Task Handle(EventoDebitoReconciliacao evento, CancellationToken cancellationToken)
    {
        var extrato = await repositorio.ObterOuFalharAsync(evento.ContaId, cancellationToken);

        var resultado = extrato.RegistrarDebitoReconciliacao(
            evento.Versao,
            evento.TransferenciaId,
            evento.NomeBeneficiario,
            Dinheiro.De(evento.Valor, extrato.Moeda),
            Saldo.De(evento.SaldoApos, extrato.Moeda),
            evento.OcorridoEm);

        if (resultado != ResultadoAplicacao.IgnoradoJaAplicado)
        {
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        }

        logger.RegistrarResultado(resultado, "DebitoReconciliacao", evento.ContaId, evento.Versao);
    }
}
