using EuroBank.Antifraude.Painel.TransferObjects;

namespace EuroBank.Antifraude.Painel.Tests.Fixtures;

/// <summary>Casos, análises e versões do modelo prontos para os testes das telas.</summary>
public static class Exemplos
{
    public static readonly DateTimeOffset Agora = new(2026, 11, 17, 15, 0, 0, TimeSpan.Zero);

    public static CasoResumo Resumo(int score = 50, string estado = "Aberto", string iban = "FR1420041010050500013M02606") =>
        new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), "Externa", 1_000m, "EUR", iban, score, estado, null, Agora, Agora.AddMinutes(30), null);

    public static PontuacaoModelo Modelo(int versao = 3, double probabilidade = 0.93, int pontos = 47) =>
        new(versao, probabilidade, pontos, [new("LogValor", 1.38), new("FracaoDoSaldo", 0.8), new("SaidasUltimaHora", -0.2)]);

    public static CasoDetalhe Caso(string estado = "Aberto", string? analista = null, PontuacaoModelo? challenger = null)
    {
        var analise = new AnaliseDetalhe(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Externa",
            Guid.CreateVersion7(),
            1_000m,
            "EUR",
            "FR1420041010050500013M02606",
            Agora,
            "celular-da-ana",
            "48.8566,2.3522,FR",
            "EmRevisao",
            57,
            2,
            Agora,
            null,
            [
                new("BeneficiarioNovo", "Peso", true, 10, false, "IBAN nunca usado pela conta"),
                new("ModeloFraude", "Peso", true, 47, false, "Probabilidade de fraude 0.930 na versão 3 do modelo"),
            ],
            new Dictionary<string, double> { ["LogValor"] = 3.0004, ["BeneficiarioNovo"] = 1 },
            Modelo(),
            challenger);
        return new CasoDetalhe(Guid.CreateVersion7(), estado, analista, Agora, Agora.AddMinutes(30), null, null, null, false, 1, analise, []);
    }

    public static VersaoModeloResumo Versao(int versao, string status, double aucPr = 0.99) =>
        new(versao, status, aucPr, 0.97, 0.94, 0.95, 0.99, true, Agora, status == "Ativa" ? Agora : null, null);
}
