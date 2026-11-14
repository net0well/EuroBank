using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>IBANs bloqueados e países de risco. Incluir de novo devolve o que já existe; remover o que não existe: 404.</summary>
public sealed class ListasManipulador(
    IRepositorioListas listas,
    IConsultaAntifraude consulta,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public Task<IReadOnlyList<IbanBloqueadoDto>> Handle(ListarIbansBloqueados consultaIbans, CancellationToken cancellationToken) =>
        consulta.ListarIbansBloqueadosAsync(cancellationToken);

    public Task<IReadOnlyList<PaisRiscoDto>> Handle(ListarPaisesRisco consultaPaises, CancellationToken cancellationToken) =>
        consulta.ListarPaisesRiscoAsync(cancellationToken);

    public async Task<IbanBloqueadoDto> Handle(BloquearIban comando, CancellationToken cancellationToken)
    {
        var novo = IbanBloqueado.Criar(comando.Iban, comando.Motivo, comando.Analista?.Trim() ?? string.Empty, relogio.GetUtcNow());
        if (string.IsNullOrWhiteSpace(novo.BloqueadoPor))
        {
            throw new ExcecaoAnalistaObrigatorio();
        }

        if (await listas.ObterIbanAsync(novo.Iban, cancellationToken) is { } existente)
        {
            return existente.ParaDto();
        }

        listas.Adicionar(novo);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return novo.ParaDto();
    }

    public async Task Handle(DesbloquearIban comando, CancellationToken cancellationToken)
    {
        var bloqueado = await listas.ObterIbanAsync(comando.Iban, cancellationToken) ?? throw new ExcecaoIbanNaoBloqueado(comando.Iban);
        listas.Remover(bloqueado);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
    }

    public async Task<PaisRiscoDto> Handle(IncluirPaisRisco comando, CancellationToken cancellationToken)
    {
        var novo = PaisRisco.Criar(comando.Codigo, relogio.GetUtcNow());
        if (await listas.ObterPaisAsync(novo.Codigo, cancellationToken) is { } existente)
        {
            return existente.ParaDto();
        }

        listas.Adicionar(novo);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return novo.ParaDto();
    }

    public async Task Handle(RemoverPaisRisco comando, CancellationToken cancellationToken)
    {
        var pais = await listas.ObterPaisAsync(comando.Codigo, cancellationToken) ?? throw new ExcecaoPaisNaoListado(comando.Codigo);
        listas.Remover(pais);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
    }
}
