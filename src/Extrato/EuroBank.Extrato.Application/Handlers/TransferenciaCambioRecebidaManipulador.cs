using EuroBank.Extrato.Application.Extensions;
using EuroBank.Extrato.Application.TransferObjects;
using EuroBank.Extrato.Domain.Enums;
using EuroBank.Extrato.Domain.Repositories;
using EuroBank.Extrato.Domain.ValueObjects;
using EuroBank.Shared.Domain;
using EuroBank.Shared.Enums;
using EuroBank.Shared.ValueObjects;
using Microsoft.Extensions.Logging;

namespace EuroBank.Extrato.Application.Handlers;

public sealed class TransferenciaCambioRecebidaManipulador(
    IRepositorioExtratos repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ILogger<TransferenciaCambioRecebidaManipulador> logger)
{
    public async Task Handle(EventoTransferenciaCambioRecebida evento, CancellationToken cancellationToken)
    {
        var extrato = await repositorio.ObterOuFalharAsync(evento.ContaId, cancellationToken);

        var resultado = extrato.RegistrarTransferenciaCambioRecebida(
            evento.Versao,
            evento.TransferenciaId,
            evento.IbanOrigem,
            evento.NomeOrigem,
            evento.Descricao,
            Dinheiro.De(evento.Valor, extrato.Moeda),
            Saldo.De(evento.SaldoApos, extrato.Moeda),
            new CambioDoLancamento(Moedas.Ler(evento.MoedaOrigem), evento.ValorOrigem, evento.Cotacao),
            evento.OcorridoEm);

        if (resultado != ResultadoAplicacao.IgnoradoJaAplicado)
        {
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        }

        logger.RegistrarResultado(resultado, "TransferenciaCambioRecebida", evento.ContaId, evento.Versao);
    }
}
