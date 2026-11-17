using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using EuroBank.Antifraude.Painel.TransferObjects;
using EuroBank.Shared.TransferObjects;

namespace EuroBank.Antifraude.Painel.Clients;

/// <summary>
/// A REST do antifraude (e o Simular, pelo JSON transcoding), chamada do servidor do painel com a API key: o
/// navegador nunca vê a key. Uma resposta de erro vira <see cref="ExcecaoApiAntifraude"/> com a mensagem do
/// ProblemDetails.
/// </summary>
public sealed class ClienteAntifraude(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ResultadoPaginado<CasoResumo>> ListarCasosAsync(string? status, int pagina, int tamanho, CancellationToken cancellationToken)
    {
        var filtro = string.IsNullOrWhiteSpace(status) ? string.Empty : $"status={Uri.EscapeDataString(status)}&";
        return ObterAsync<ResultadoPaginado<CasoResumo>>(
            string.Create(CultureInfo.InvariantCulture, $"casos?{filtro}pagina={pagina}&tamanho={tamanho}"), cancellationToken);
    }

    public Task<CasoDetalhe> ObterCasoAsync(Guid casoId, CancellationToken cancellationToken) =>
        ObterAsync<CasoDetalhe>($"casos/{casoId}", cancellationToken);

    public Task<CasoDetalhe> AssumirAsync(Guid casoId, string analista, CancellationToken cancellationToken) =>
        EnviarAsync<CasoDetalhe>(HttpMethod.Post, $"casos/{casoId}/atribuicao", new AtribuicaoRequisicao(analista), cancellationToken);

    public Task<CasoDetalhe> DecidirAsync(Guid casoId, DecisaoRequisicao decisao, CancellationToken cancellationToken) =>
        EnviarAsync<CasoDetalhe>(HttpMethod.Post, $"casos/{casoId}/decisao", decisao, cancellationToken);

    public Task<FraudeConfirmada> ConfirmarFraudeAsync(FraudeConfirmadaRequisicao fraude, CancellationToken cancellationToken) =>
        EnviarAsync<FraudeConfirmada>(HttpMethod.Post, "fraudes-confirmadas", fraude, cancellationToken);

    public Task<IReadOnlyList<Regra>> ListarRegrasAsync(CancellationToken cancellationToken) =>
        ObterAsync<IReadOnlyList<Regra>>("regras", cancellationToken);

    public Task<Regra> AjustarRegraAsync(string codigo, AjusteRegraRequisicao ajuste, CancellationToken cancellationToken) =>
        EnviarAsync<Regra>(HttpMethod.Put, $"regras/{Uri.EscapeDataString(codigo)}", ajuste, cancellationToken);

    public Task<ConfiguracaoDecisao> ObterConfiguracaoAsync(CancellationToken cancellationToken) =>
        ObterAsync<ConfiguracaoDecisao>("configuracao-decisao", cancellationToken);

    public Task<ConfiguracaoDecisao> AlterarConfiguracaoAsync(ConfiguracaoRequisicao configuracao, CancellationToken cancellationToken) =>
        EnviarAsync<ConfiguracaoDecisao>(HttpMethod.Put, "configuracao-decisao", configuracao, cancellationToken);

    public Task<IReadOnlyList<IbanBloqueado>> ListarIbansBloqueadosAsync(CancellationToken cancellationToken) =>
        ObterAsync<IReadOnlyList<IbanBloqueado>>("listas/bloqueio", cancellationToken);

    public Task<IbanBloqueado> BloquearIbanAsync(BloqueioIbanRequisicao bloqueio, CancellationToken cancellationToken) =>
        EnviarAsync<IbanBloqueado>(HttpMethod.Post, "listas/bloqueio", bloqueio, cancellationToken);

    public Task DesbloquearIbanAsync(string iban, CancellationToken cancellationToken) =>
        ExcluirAsync($"listas/bloqueio/{Uri.EscapeDataString(iban)}", cancellationToken);

    public Task<IReadOnlyList<PaisRisco>> ListarPaisesRiscoAsync(CancellationToken cancellationToken) =>
        ObterAsync<IReadOnlyList<PaisRisco>>("listas/paises-risco", cancellationToken);

    public Task<PaisRisco> IncluirPaisRiscoAsync(string codigo, CancellationToken cancellationToken) =>
        EnviarAsync<PaisRisco>(HttpMethod.Post, "listas/paises-risco", new PaisRiscoRequisicao(codigo), cancellationToken);

    public Task RemoverPaisRiscoAsync(string codigo, CancellationToken cancellationToken) =>
        ExcluirAsync($"listas/paises-risco/{Uri.EscapeDataString(codigo)}", cancellationToken);

    public Task<VisaoGeral> ObterVisaoGeralAsync(CancellationToken cancellationToken) =>
        ObterAsync<VisaoGeral>("visao-geral", cancellationToken);

    public Task<IReadOnlyList<VersaoModeloResumo>> ListarModelosAsync(CancellationToken cancellationToken) =>
        ObterAsync<IReadOnlyList<VersaoModeloResumo>>("modelos", cancellationToken);

    public Task<VersaoModeloDetalhe> ObterModeloAsync(int versao, CancellationToken cancellationToken) =>
        ObterAsync<VersaoModeloDetalhe>(string.Create(CultureInfo.InvariantCulture, $"modelos/{versao}"), cancellationToken);

    public Task<VersaoModeloResumo> AtivarModeloAsync(int versao, CancellationToken cancellationToken) =>
        EnviarAsync<VersaoModeloResumo>(HttpMethod.Post, string.Create(CultureInfo.InvariantCulture, $"modelos/{versao}/ativacao"), null, cancellationToken);

    public Task<VersaoModeloResumo> ColocarEmSombraAsync(int versao, CancellationToken cancellationToken) =>
        EnviarAsync<VersaoModeloResumo>(HttpMethod.Post, string.Create(CultureInfo.InvariantCulture, $"modelos/{versao}/challenger"), null, cancellationToken);

    public Task RetirarChallengerAsync(CancellationToken cancellationToken) => ExcluirAsync("modelos/challenger", cancellationToken);

    /// <summary>Uma avaliação hipotética, sem gravar análise nem abrir caso.</summary>
    public Task<ResultadoSimulacao> SimularAsync(SolicitacaoSimulacao solicitacao, CancellationToken cancellationToken) =>
        EnviarAsync<ResultadoSimulacao>(HttpMethod.Post, "v1/simulacoes", solicitacao, cancellationToken);

    private Task<T> ObterAsync<T>(string rota, CancellationToken cancellationToken) =>
        EnviarAsync<T>(HttpMethod.Get, rota, null, cancellationToken);

    private async Task ExcluirAsync(string rota, CancellationToken cancellationToken)
    {
        using var resposta = await http.DeleteAsync(new Uri(rota, UriKind.Relative), cancellationToken);
        await GarantirSucessoAsync(resposta, cancellationToken);
    }

    private async Task<T> EnviarAsync<T>(HttpMethod metodo, string rota, object? corpo, CancellationToken cancellationToken)
    {
        using var pedido = new HttpRequestMessage(metodo, new Uri(rota, UriKind.Relative))
        {
            Content = corpo is null ? null : JsonContent.Create(corpo, corpo.GetType(), options: Json),
        };
        using var resposta = await http.SendAsync(pedido, cancellationToken);
        await GarantirSucessoAsync(resposta, cancellationToken);
        return await resposta.Content.ReadFromJsonAsync<T>(Json, cancellationToken)
            ?? throw new ExcecaoApiAntifraude(resposta.StatusCode, null, $"Resposta vazia de {rota}.");
    }

    private static async Task GarantirSucessoAsync(HttpResponseMessage resposta, CancellationToken cancellationToken)
    {
        if (resposta.IsSuccessStatusCode)
        {
            return;
        }

        var corpo = await resposta.Content.ReadAsStringAsync(cancellationToken);
        string? codigo = null;
        var mensagem = $"O antifraude respondeu {(int)resposta.StatusCode}.";
        try
        {
            using var problema = JsonDocument.Parse(corpo);
            if (problema.RootElement.TryGetProperty("codigo", out var codigoProblema))
            {
                codigo = codigoProblema.GetString();
            }

            if (problema.RootElement.TryGetProperty("detail", out var detalhe) && detalhe.GetString() is { Length: > 0 } texto)
            {
                mensagem = texto;
            }
        }
        catch (JsonException)
        {
            // Corpo que não é ProblemDetails: fica a mensagem com o status.
        }

        throw new ExcecaoApiAntifraude(resposta.StatusCode, codigo, mensagem);
    }
}
