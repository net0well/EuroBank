namespace EuroBank.Extrato.Infrastructure.Enums;

/// <summary>O que aconteceu ao aplicar um evento no Redis.</summary>
public enum ResultadoEscritaCache
{
    /// <summary>A conta não estava no cache e a escrita exigia que estivesse (o write-through grava o retrato do Postgres).</summary>
    NaoExiste = -1,

    /// <summary>O cache já tinha uma versão igual ou mais nova.</summary>
    VersaoAntiga = 0,

    Aplicado = 1,
}
