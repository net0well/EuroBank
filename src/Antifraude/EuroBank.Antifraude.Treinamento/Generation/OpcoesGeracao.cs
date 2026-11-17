namespace EuroBank.Antifraude.Treinamento.Generation;

/// <summary>
/// O tamanho do mundo simulado. O padrão dá cerca de 100 mil transferências em 90 dias, com cerca de 1,5% de fraude;
/// os testes usam um mundo menor. A mesma semente gera sempre os mesmos dados.
/// </summary>
public sealed record OpcoesGeracao(
    int Semente = 42,
    int Contas = 2_000,
    int Dias = 90,
    DateTimeOffset? Inicio = null)
{
    public DateTimeOffset InicioEfetivo => Inicio ?? new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    public DateTimeOffset Fim => InicioEfetivo.AddDays(Dias);

    /// <summary>Quantas contas são mulas (abertas durante o período só para repassar dinheiro).</summary>
    public int Mulas => Math.Max(2, Contas / 20);

    /// <summary>Episódios de cada padrão nas contas de clientes, proporcionais ao tamanho do mundo.</summary>
    public int Episodios(int porDuasMilContas) => Math.Max(2, porDuasMilContas * Contas / 2_000);
}
