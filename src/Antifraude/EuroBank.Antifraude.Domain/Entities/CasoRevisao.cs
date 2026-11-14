using EuroBank.Antifraude.Domain.Enums;
using EuroBank.Antifraude.Domain.Events;
using EuroBank.Antifraude.Domain.Exceptions;
using EuroBank.Antifraude.Domain.ValueObjects;
using EuroBank.Shared.Domain;

namespace EuroBank.Antifraude.Domain.Entities;

/// <summary>
/// A revisão humana de uma análise. O analista assume o caso (identificado pelo nome) e decide com justificativa,
/// podendo bloquear o IBAN ao recusar. Sem decisão até ExpiraEm, o caso expira; se o cliente cancelar a operação,
/// sai da fila. Cada ação fica na auditoria. A versão protege contra decisão e expiração ao mesmo tempo.
/// </summary>
public sealed class CasoRevisao : RaizDeAgregado<CasoId>
{
    private const string Sistema = "sistema";

    private readonly List<RegistroAuditoria> _auditoria = [];

    private CasoRevisao()
    {
        // Usado pelo EF Core.
    }

    public AnaliseId AnaliseId { get; private set; }

    public Guid OperacaoId { get; private set; }

    public Guid ContaId { get; private set; }

    public int ScoreFinal { get; private set; }

    public EstadoCaso Estado { get; private set; }

    public string? Analista { get; private set; }

    public DateTimeOffset AbertoEm { get; private set; }

    public DateTimeOffset ExpiraEm { get; private set; }

    public DateTimeOffset? AssumidoEm { get; private set; }

    public DateTimeOffset? DecididoEm { get; private set; }

    public string? Justificativa { get; private set; }

    /// <summary>A recusa pediu o bloqueio do IBAN de destino.</summary>
    public bool BloquearIban { get; private set; }

    public IReadOnlyList<RegistroAuditoria> Auditoria => _auditoria.AsReadOnly();

    /// <summary>Concorrência otimista: cresce a cada mudança.</summary>
    public int Versao { get; private set; }

    public bool EstaPendente => Estado is EstadoCaso.Aberto or EstadoCaso.EmAnalise;

    public static CasoRevisao Abrir(
        AnaliseId analiseId,
        Guid operacaoId,
        Guid contaId,
        int scoreFinal,
        TimeSpan prazo,
        DateTimeOffset agora)
    {
        var caso = new CasoRevisao
        {
            Id = CasoId.Novo(),
            AnaliseId = analiseId,
            OperacaoId = operacaoId,
            ContaId = contaId,
            ScoreFinal = scoreFinal,
            Estado = EstadoCaso.Aberto,
            AbertoEm = agora,
            ExpiraEm = agora + prazo,
        };

        caso.Auditar(agora, Sistema, "Caso aberto", $"Score {scoreFinal}; expira em {caso.ExpiraEm:u}");
        caso.Registrar(new CasoAberto(caso.Id, analiseId, operacaoId, contaId, scoreFinal, caso.ExpiraEm, agora));
        return caso;
    }

    public void Assumir(string analista, DateTimeOffset agora)
    {
        ExigirAnalista(analista);
        ExigirPendente(agora);

        if (Estado == EstadoCaso.EmAnalise)
        {
            if (Analista == analista)
            {
                return;
            }

            throw new ExcecaoCasoDeOutroAnalista(Analista!);
        }

        Estado = EstadoCaso.EmAnalise;
        Analista = analista;
        AssumidoEm = agora;
        Auditar(agora, analista, "Caso assumido", detalhe: null);
    }

    public void Decidir(string analista, bool aprovar, string justificativa, bool bloquearIban, DateTimeOffset agora)
    {
        ExigirAnalista(analista);
        if (string.IsNullOrWhiteSpace(justificativa))
        {
            throw new ExcecaoJustificativaObrigatoria();
        }

        if (aprovar && bloquearIban)
        {
            throw new ExcecaoBloqueioSemRecusa();
        }

        ExigirPendente(agora);
        if (Estado == EstadoCaso.Aberto)
        {
            throw new ExcecaoCasoNaoAssumido();
        }

        if (Analista != analista)
        {
            throw new ExcecaoCasoDeOutroAnalista(Analista!);
        }

        Estado = aprovar ? EstadoCaso.Aprovado : EstadoCaso.Recusado;
        Justificativa = justificativa;
        BloquearIban = bloquearIban;
        DecididoEm = agora;
        Auditar(agora, analista, aprovar ? "Aprovado" : "Recusado", bloquearIban ? $"{justificativa} (IBAN bloqueado)" : justificativa);
    }

    /// <summary>Fecha o caso que passou do prazo; antes do prazo ou já encerrado, não faz nada.</summary>
    public bool Expirar(DateTimeOffset agora)
    {
        if (!EstaPendente || agora < ExpiraEm)
        {
            return false;
        }

        Estado = EstadoCaso.Expirado;
        DecididoEm = agora;
        Auditar(agora, Sistema, "Expirado", "Sem decisão dentro do prazo");
        return true;
    }

    /// <summary>O cliente cancelou a operação: o caso sai da fila. Já encerrado, não faz nada.</summary>
    public bool CancelarPeloCliente(DateTimeOffset agora)
    {
        if (!EstaPendente)
        {
            return false;
        }

        Estado = EstadoCaso.CanceladoPeloCliente;
        DecididoEm = agora;
        Auditar(agora, Sistema, "Cancelado pelo cliente", "A operação foi cancelada antes da decisão");
        return true;
    }

    private void ExigirPendente(DateTimeOffset agora)
    {
        if (!EstaPendente)
        {
            throw new ExcecaoCasoEncerrado(Estado);
        }

        if (agora >= ExpiraEm)
        {
            throw new ExcecaoCasoExpirado(ExpiraEm);
        }
    }

    private static void ExigirAnalista(string analista)
    {
        if (string.IsNullOrWhiteSpace(analista))
        {
            throw new ExcecaoAnalistaObrigatorio();
        }
    }

    private void Auditar(DateTimeOffset quando, string quem, string acao, string? detalhe)
    {
        _auditoria.Add(new RegistroAuditoria(_auditoria.Count + 1, quando, quem, acao, detalhe));
        Versao++;
    }
}
