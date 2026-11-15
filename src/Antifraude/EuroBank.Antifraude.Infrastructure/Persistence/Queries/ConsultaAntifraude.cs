using EuroBank.Antifraude.Application.Interfaces;
using EuroBank.Antifraude.Application.Mappers;
using EuroBank.Antifraude.Application.TransferObjects;
using EuroBank.Antifraude.Domain.Entities;
using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Shared.TransferObjects;
using EuroBank.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EuroBank.Antifraude.Infrastructure.Persistence.Queries;

public sealed class ConsultaAntifraude(AntifraudeDbContext contexto) : IConsultaAntifraude
{
    private static readonly EstadoCaso[] Pendentes = [EstadoCaso.Aberto, EstadoCaso.EmAnalise];

    public async Task<ResultadoPaginado<CasoResumoDto>> ListarCasosAsync(EstadoCaso? status, Paginacao paginacao, CancellationToken cancellationToken)
    {
        var casos = contexto.Casos.AsNoTracking();
        casos = status is { } estado
            ? casos.Where(caso => caso.Estado == estado).OrderByDescending(caso => caso.AbertoEm)
            : casos.Where(caso => Pendentes.Contains(caso.Estado)).OrderBy(caso => caso.ExpiraEm);

        var total = await casos.LongCountAsync(cancellationToken);
        var pagina = await casos.Skip(paginacao.Saltar).Take(paginacao.Tamanho)
            .Join(contexto.Analises.AsNoTracking(), caso => caso.AnaliseId, analise => analise.Id, (caso, analise) => new { caso, analise })
            .ToListAsync(cancellationToken);

        var itens = pagina.Select(par =>
        {
            var operacao = par.analise.Contexto;
            return new CasoResumoDto(
                par.caso.Id.Valor,
                par.analise.Id.Valor,
                par.caso.OperacaoId,
                par.caso.ContaId,
                operacao.Tipo.ToString(),
                operacao.Valor.Valor,
                operacao.Valor.Moeda.ToString(),
                operacao.IbanDestino,
                par.caso.ScoreFinal,
                par.caso.Estado.ToString(),
                par.caso.Analista,
                par.caso.AbertoEm,
                par.caso.ExpiraEm,
                par.caso.DecididoEm);
        }).ToList();
        return new ResultadoPaginado<CasoResumoDto>(itens, paginacao.Pagina, paginacao.Tamanho, total);
    }

    public async Task<CasoDetalheDto?> ObterCasoAsync(Guid casoId, CancellationToken cancellationToken)
    {
        var caso = await contexto.Casos.AsNoTracking().SingleOrDefaultAsync(caso => caso.Id == new CasoId(casoId), cancellationToken);
        if (caso is null)
        {
            return null;
        }

        var analise = await contexto.Analises.AsNoTracking().SingleAsync(analise => analise.Id == caso.AnaliseId, cancellationToken);
        return new CasoDetalheDto(
            caso.Id.Valor,
            caso.Estado.ToString(),
            caso.Analista,
            caso.AbertoEm,
            caso.ExpiraEm,
            caso.AssumidoEm,
            caso.DecididoEm,
            caso.Justificativa,
            caso.BloquearIban,
            caso.Versao,
            Detalhe(analise),
            caso.Auditoria.OrderBy(registro => registro.Ordem)
                .Select(registro => new RegistroAuditoriaDto(registro.Ordem, registro.Quando, registro.Quem, registro.Acao, registro.Detalhe))
                .ToList());
    }

    public async Task<IReadOnlyList<IbanBloqueadoDto>> ListarIbansBloqueadosAsync(CancellationToken cancellationToken) =>
        await contexto.IbansBloqueados.AsNoTracking()
            .OrderBy(bloqueado => bloqueado.Iban)
            .Select(bloqueado => new IbanBloqueadoDto(bloqueado.Iban, bloqueado.Motivo, bloqueado.BloqueadoPor, bloqueado.BloqueadoEm))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PaisRiscoDto>> ListarPaisesRiscoAsync(CancellationToken cancellationToken) =>
        await contexto.PaisesRisco.AsNoTracking()
            .OrderBy(pais => pais.Codigo)
            .Select(pais => new PaisRiscoDto(pais.Codigo, pais.IncluidoEm))
            .ToListAsync(cancellationToken);

    public async Task<VisaoGeralDto> VisaoGeralAsync(DateTimeOffset desde, CancellationToken cancellationToken)
    {
        var analises = contexto.Analises.AsNoTracking().Where(analise => analise.CriadaEm >= desde);
        var porDecisao = await analises.GroupBy(analise => analise.Decisao)
            .Select(grupo => new { Decisao = grupo.Key, Quantidade = grupo.Count() })
            .ToListAsync(cancellationToken);
        var total = porDecisao.Sum(grupo => grupo.Quantidade);

        var casos = contexto.Casos.AsNoTracking();
        var abertos = await casos.CountAsync(caso => caso.AbertoEm >= desde, cancellationToken);
        var pendentes = await casos.CountAsync(caso => Pendentes.Contains(caso.Estado), cancellationToken);
        var expirados = await casos.CountAsync(caso => caso.Estado == EstadoCaso.Expirado && caso.DecididoEm >= desde, cancellationToken);
        var decididos = await casos
            .Where(caso => (caso.Estado == EstadoCaso.Aprovado || caso.Estado == EstadoCaso.Recusado) && caso.DecididoEm >= desde)
            .Select(caso => new { caso.AbertoEm, caso.DecididoEm })
            .ToListAsync(cancellationToken);
        var recusadas = porDecisao.Where(grupo => grupo.Decisao == DecisaoAnalise.Recusada).Sum(grupo => grupo.Quantidade);

        // Sinais em jsonb: o Postgres abre o array e conta as regras que dispararam de verdade (fora do modo sombra).
        var regras = await contexto.Database.SqlQuery<RegraDisparadaDto>(
            $"""
            SELECT sinal->>'codigo' AS "Codigo", count(*)::int AS "Vezes"
            FROM analises, jsonb_array_elements(sinais) AS sinal
            WHERE criada_em >= {desde} AND (sinal->>'disparou')::boolean AND NOT (sinal->>'sombra')::boolean
            GROUP BY sinal->>'codigo'
            ORDER BY count(*) DESC, sinal->>'codigo'
            LIMIT 5
            """).ToListAsync(cancellationToken);

        return new VisaoGeralDto(
            desde,
            total,
            porDecisao.ToDictionary(grupo => grupo.Decisao.ToString(), grupo => grupo.Quantidade),
            total == 0 ? 0m : Math.Round((decimal)abertos / total, 4),
            total == 0 ? 0m : Math.Round((decimal)recusadas / total, 4),
            pendentes,
            decididos.Count,
            expirados,
            decididos.Count == 0 ? null : Math.Round(decididos.Average(caso => (caso.DecididoEm!.Value - caso.AbertoEm).TotalMinutes), 1),
            regras);
    }

    /// <summary>
    /// Projeta as colunas (sem o arquivo) e monta o resumo em memória: métricas e dados são jsonb com conversor, e o EF
    /// não traduz um membro de dentro deles.
    /// </summary>
    public async Task<IReadOnlyList<VersaoModeloResumoDto>> ListarModelosAsync(CancellationToken cancellationToken)
    {
        var versoes = await contexto.VersoesModelo.AsNoTracking()
            .OrderByDescending(versao => versao.Versao)
            .Select(versao => new
            {
                versao.Versao,
                versao.Status,
                versao.Metricas,
                versao.LimiarRecomendado,
                versao.Dados,
                versao.TreinadaEm,
                versao.AtivadaEm,
                versao.AposentadaEm,
            })
            .ToListAsync(cancellationToken);

        return versoes
            .Select(versao => new VersaoModeloResumoDto(
                versao.Versao,
                versao.Status.ToString(),
                versao.Metricas.AucPr,
                versao.Metricas.Precisao,
                versao.Metricas.Recall,
                versao.Metricas.F1,
                versao.LimiarRecomendado,
                versao.Dados.MetaAtingida,
                versao.TreinadaEm,
                versao.AtivadaEm,
                versao.AposentadaEm))
            .ToList();
    }

    /// <summary>Sem o arquivo (centenas de KB): só o tamanho, calculado no banco.</summary>
    public async Task<VersaoModeloDetalheDto?> ObterModeloAsync(int versao, CancellationToken cancellationToken) =>
        await contexto.VersoesModelo.AsNoTracking()
            .Where(modelo => modelo.Versao == versao)
            .Select(modelo => new VersaoModeloDetalheDto(
                modelo.Versao,
                modelo.Status.ToString(),
                modelo.LimiarRecomendado,
                modelo.Metricas,
                modelo.Dados,
                EF.Property<List<DistribuicaoFeature>>(modelo, "_distribuicao"),
                modelo.Arquivo.Length,
                modelo.TreinadaEm,
                modelo.AtivadaEm,
                modelo.AposentadaEm))
            .SingleOrDefaultAsync(cancellationToken);

    private static AnaliseDetalheDto Detalhe(Analise analise)
    {
        var operacao = analise.Contexto;
        return new AnaliseDetalheDto(
            analise.Id.Valor,
            analise.OperacaoId,
            operacao.Tipo.ToString(),
            analise.ContaId,
            operacao.Valor.Valor,
            operacao.Valor.Moeda.ToString(),
            operacao.IbanDestino,
            operacao.Momento,
            analise.Decisao.ToString(),
            analise.ScoreFinal,
            analise.VersaoConfiguracao,
            analise.CriadaEm,
            analise.DecididaEm,
            analise.Sinais.Select(sinal => new SinalRegraDto(
                sinal.Codigo.ToString(), sinal.Tipo.ToString(), sinal.Disparou, sinal.Pontos, sinal.Sombra, sinal.Motivo)).ToList(),
            analise.Features?.ParaDicionario(),
            analise.Modelo?.ParaDto(),
            analise.Challenger?.ParaDto());
    }
}
