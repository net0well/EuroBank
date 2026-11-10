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

public sealed class TransferenciaCambioEnviadaManipulador(
    IRepositorioExtratos repositorio,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    ILogger<TransferenciaCambioEnviadaManipulador> logger)
{
    public async Task Handle(EventoTransferenciaCambioEnviada evento, CancellationToken cancellationToken)
    {
        var extrato = await repositorio.ObterOuFalharAsync(evento.ContaId, cancellationToken);

        var resultado = extrato.RegistrarTransferenciaCambioEnviada(
            evento.Versao,
            evento.TransferenciaId,
            evento.IbanDestino,
            evento.NomeDestino,
            evento.Descricao,
            Dinheiro.De(evento.Valor, extrato.Moeda),
            Dinheiro.De(evento.Tarifa, extrato.Moeda),
            Saldo.De(evento.SaldoApos, extrato.Moeda),
            new CambioDoLancamento(Moedas.Ler(evento.MoedaDestino), evento.ValorDestino, evento.Cotacao),
            evento.OcorridoEm);

        if (resultado != ResultadoAplicacao.IgnoradoJaAplicado)
        {
            await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        }

        logger.RegistrarResultado(resultado, "TransferenciaCambioEnviada", evento.ContaId, evento.Versao);
    }
}
