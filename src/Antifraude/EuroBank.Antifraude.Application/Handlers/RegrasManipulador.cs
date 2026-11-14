using EuroBank.Antifraude.Application.Commands;
using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.Queries;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.Repositories;
using EuroBank.Shared.Domain;

namespace EuroBank.Antifraude.Application.Handlers;

/// <summary>Regras e configuração de decisão: o ajuste vale para a próxima avaliação, sem reiniciar nada.</summary>
public sealed class RegrasManipulador(
    IRepositorioRegras regras,
    IRepositorioConfiguracao configuracoes,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<IReadOnlyList<RegraDto>> Handle(ListarRegras consulta, CancellationToken cancellationToken) =>
        (await regras.ListarAsync(cancellationToken)).Select(regra => regra.ParaDto()).ToList();

    public async Task<RegraDto> Handle(AjustarRegra comando, CancellationToken cancellationToken)
    {
        var regra = await regras.ObterAsync(comando.Codigo, cancellationToken)
            ?? throw new ExcecaoRegraNaoEncontrada(comando.Codigo.ToString());
        regra.Ajustar(comando.Peso, comando.Ativa, comando.Sombra, comando.Parametros, relogio.GetUtcNow());
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return regra.ParaDto();
    }

    public async Task<ConfiguracaoDecisaoDto> Handle(ObterConfiguracao consulta, CancellationToken cancellationToken) =>
        (await AtualAsync(cancellationToken)).ParaDto();

    /// <summary>Duas alterações ao mesmo tempo criariam a mesma versão: uma perde (409) e pode repetir.</summary>
    public async Task<ConfiguracaoDecisaoDto> Handle(AlterarConfiguracao comando, CancellationToken cancellationToken)
    {
        var nova = (await AtualAsync(cancellationToken))
            .NovaVersao(comando.LimiarRevisao, comando.LimiarRecusa, comando.PesoModelo, relogio.GetUtcNow());
        configuracoes.Adicionar(nova);
        await unidadeDeTrabalho.SalvarAsync(cancellationToken);
        return nova.ParaDto();
    }

    private async Task<Domain.Entities.ConfiguracaoDecisao> AtualAsync(CancellationToken cancellationToken) =>
        await configuracoes.AtualAsync(cancellationToken)
        ?? throw new InvalidOperationException("Configuração de decisão ausente: os dados iniciais não foram gravados.");
}
