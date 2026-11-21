namespace EuroBank.Antifraude.Infrastructure.Perfil;

/// <summary>
/// Chaves do perfil de uma conta no Redis. O id entre chaves é a hash tag: as três chaves de uma conta caem no mesmo
/// slot, o que o script Lua exige num Redis Cluster (no laboratório é Sentinel, mas a chave já fica pronta).
/// </summary>
public static class ChavesPerfil
{
    /// <summary>Hash: versao, aberta_em (Unix ms), saldo, moeda.</summary>
    public static string Dados(Guid contaId) => $"antifraude:{{{contaId}}}:perfil";

    /// <summary>Sorted set: score = momento (Unix ms), membro = "transferência|valor|IBAN".</summary>
    public static string Saidas(Guid contaId) => $"antifraude:{{{contaId}}}:saidas";

    /// <summary>Sorted set: score = momento (Unix ms), membro = "versão|valor" (a versão da conta torna o membro único).</summary>
    public static string Entradas(Guid contaId) => $"antifraude:{{{contaId}}}:entradas";

    /// <summary>Set de IBANs (normalizados) para os quais a conta já transferiu.</summary>
    public static string Beneficiarios(Guid contaId) => $"antifraude:{{{contaId}}}:beneficiarios";

    /// <summary>Set dos dispositivos das operações aprovadas.</summary>
    public static string Dispositivos(Guid contaId) => $"antifraude:{{{contaId}}}:dispositivos";

    /// <summary>
    /// Sorted set: score = momento da operação (Unix ms), membro = "análise|latitude|longitude|país" (a análise torna o
    /// membro único).
    /// </summary>
    public static string Localizacoes(Guid contaId) => $"antifraude:{{{contaId}}}:localizacoes";
}
