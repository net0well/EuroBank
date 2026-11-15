using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using EuroBank.Antifraude.Api;
using EuroBank.Antifraude.Worker;
using EuroBank.Contas.Api;
using EuroBank.Contas.Worker;
using EuroBank.Extrato.Worker;
using EuroBank.Notificacoes.Worker;
using EuroBank.Simuladores.BancoExterno;
using EuroBank.Simuladores.Cambio;
using EuroBank.Transferencias.Api;
using EuroBank.Transferencias.Infrastructure.Options;
using EuroBank.Transferencias.Worker;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Npgsql;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

[assembly: AssemblyFixture(typeof(EuroBank.E2E.Tests.Fixtures.LaboratorioFixture))]

namespace EuroBank.E2E.Tests.Fixtures;

/// <summary>
/// O laboratório inteiro para os testes ponta a ponta: Postgres com replicação lógica, Kafka e Kafka Connect com
/// Debezium, Redis e o Mailpit (containers), os mesmos conectores de infra/connect/conectores, e as aplicações
/// in-process (APIs, workers e simuladores). A saga passa pelo antifraude: o worker de Transferências chama o gRPC
/// pelo TestServer da API do antifraude.
/// </summary>
public sealed class LaboratorioFixture : IAsyncLifetime
{
    public const string TopicoOutbox = "eurobank.contas.eventos";
    public const string TopicoCdc = "eurobank.cdc.public.contas";

    public const string ApiKeyAntifraude = "chave-e2e-antifraude";

    /// <summary>País de risco só deste laboratório (país SEPA, para caber numa transferência em EUR).</summary>
    public const string IbanPaisDeRisco = "MT84MALT011000012345MTLCAST001S";

    /// <summary>
    /// Regras de comportamento em modo sombra: aparecem nas análises, mas não pontuam. Os testes de saga usam contas
    /// novas, várias transferências seguidas e saldos justos, o que levaria quase tudo para a revisão; as decisões do
    /// antifraude são exercitadas pela lista de bloqueio (regra dura) e pelo país de risco somado ao beneficiário novo.
    /// </summary>
    private static readonly string[] RegrasEmSombra =
        ["ValorAcimaDaMedia", "Velocidade", "Esvaziamento", "TesteDeCartao", "ContaRecemAberta", "HorarioIncomum"];

    /// <summary>Mesmos tópicos do kafka-init do compose, com 3 partições.</summary>
    private static readonly string[] Topicos =
    [
        TopicoOutbox,
        "eurobank.contas.comandos",
        "eurobank.contas.respostas",
        "eurobank.banco-externo.comandos",
        "eurobank.banco-externo.respostas",
        "eurobank.transferencias.eventos",
        "eurobank.cambio.comandos",
        "eurobank.cambio.respostas",
        "eurobank.antifraude.eventos",
    ];

    /// <summary>
    /// Prazos curtos para a saga caber no teste. O final 97 do banco externo responde depois do primeiro prazo da
    /// liquidação (força o reenvio) e antes de os reenvios acabarem.
    /// </summary>
    private static readonly Dictionary<string, string> ConfiguracaoSaga = new()
    {
        ["Saga:PrazoCotacao"] = "00:00:05",
        ["Saga:PrazoDebito"] = "00:00:05",
        ["Saga:PrazoLiquidacao"] = "00:00:03",
        ["Saga:PrazoConsulta"] = "00:00:02",
        ["Saga:PrazoCredito"] = "00:00:05",
        ["Saga:PrazoEstorno"] = "00:00:05",
        ["Saga:PrazoReconciliacao"] = "00:00:05",
        ["Saga:PrazoCreditoDevolucao"] = "00:00:05",
        ["Saga:ReenviosPorPasso"] = "3",
        ["Saga:AnaliseRiscoAtiva"] = "true",
        ["Saga:PrazoAnaliseRisco"] = "00:00:06",
        ["Saga:IntervaloAnaliseRisco"] = "00:00:01",
        ["Saga:IntervaloConsultaRevisao"] = "00:00:05",
        ["Varredura:Intervalo"] = "00:00:00.500",
        ["Sepa:AplicarHorarioDeCorte"] = "false",
    };

    private readonly List<IAsyncDisposable> _aplicacoes = [];

    private readonly INetwork _rede = new NetworkBuilder().Build();
    private readonly PostgreSqlContainer _postgres;
    private readonly KafkaContainer _kafka;
    private readonly IContainer _connect;
    private readonly IContainer _mailpit;
    private readonly RedisContainer _redis = new RedisBuilder("redis:8.10.3").Build();
    private readonly ActivityListener _ouvinteAtividades;
    private WebApplicationFactory<MarcadorAntifraudeApi> _antifraude = null!;
    private WebApplicationFactory<MarcadorContasApi> _contas = null!;
    private WebApplicationFactory<MarcadorExtratoWorker> _extrato = null!;

    public LaboratorioFixture()
    {
        _postgres = new PostgreSqlBuilder("postgres:18.6")
            .WithNetwork(_rede)
            .WithNetworkAliases("postgres")
            .WithDatabase("eurobank_contas")
            .WithUsername("eurobank")
            .WithPassword("eurobank")
            .WithCommand("-c", "wal_level=logical")
            .Build();

        _kafka = new KafkaBuilder("apache/kafka:4.4.0")
            .WithNetwork(_rede)
            .WithNetworkAliases("kafka")
            .WithListener("kafka:19092")
            .Build();

        _connect = new ContainerBuilder("quay.io/debezium/connect:3.6.3.Final")
            .WithNetwork(_rede)
            .WithEnvironment("BOOTSTRAP_SERVERS", "kafka:19092")
            .WithEnvironment("GROUP_ID", "eurobank-connect-e2e")
            .WithEnvironment("CONFIG_STORAGE_TOPIC", "e2e_connect_configs")
            .WithEnvironment("OFFSET_STORAGE_TOPIC", "e2e_connect_offsets")
            .WithEnvironment("STATUS_STORAGE_TOPIC", "e2e_connect_statuses")
            .WithEnvironment("CONFIG_STORAGE_REPLICATION_FACTOR", "1")
            .WithEnvironment("OFFSET_STORAGE_REPLICATION_FACTOR", "1")
            .WithEnvironment("STATUS_STORAGE_REPLICATION_FACTOR", "1")
            .WithEnvironment("KEY_CONVERTER", "org.apache.kafka.connect.json.JsonConverter")
            .WithEnvironment("VALUE_CONVERTER", "org.apache.kafka.connect.json.JsonConverter")
            .WithEnvironment("CONNECT_KEY_CONVERTER_SCHEMAS_ENABLE", "false")
            .WithEnvironment("CONNECT_VALUE_CONVERTER_SCHEMAS_ENABLE", "false")
            .WithPortBinding(8083, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(requisicao => requisicao.ForPort(8083).ForPath("/connectors")))
            .Build();

        _mailpit = new ContainerBuilder("axllent/mailpit:v1.33.1")
            .WithPortBinding(1025, assignRandomHostPort: true)
            .WithPortBinding(8025, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(requisicao => requisicao.ForPort(8025).ForPath("/readyz")))
            .Build();

        // Sem um ouvinte, ASP.NET Core e Wolverine não criam Activities: nem haveria traceparent na outbox.
        _ouvinteAtividades = new ActivityListener
        {
            ShouldListenTo = fonte => fonte.Name is "Microsoft.AspNetCore" or "Wolverine",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = atividade => Atividades.Add(atividade),
        };
        ActivitySource.AddActivityListener(_ouvinteAtividades);
    }

    public HttpClient Contas { get; private set; } = null!;

    public HttpClient Extrato { get; private set; } = null!;

    public HttpClient Transferencias { get; private set; } = null!;

    /// <summary>REST do analista (e o JSON transcoding do gRPC) na API do antifraude.</summary>
    public HttpClient Antifraude { get; private set; } = null!;

    /// <summary>Ligado, as chamadas do worker de Transferências ao antifraude falham como se ele estivesse fora do ar.</summary>
    public bool AntifraudeForaDoAr { get; set; }

    /// <summary>API HTTP do Mailpit (mensagens recebidas pelo SMTP).</summary>
    public HttpClient Mailpit { get; private set; } = null!;

    public string BootstrapKafka => _kafka.GetBootstrapAddress();

    public string ConnectionStringTransferencias =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = "eurobank_transferencias" }.ConnectionString;

    public string ConnectionStringBancoExterno =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = "eurobank_banco_externo" }.ConnectionString;

    public string ConnectionStringCambio =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = "eurobank_cambio" }.ConnectionString;

    public string ConnectionStringContas => _postgres.GetConnectionString();

    public string ConnectionStringAntifraude =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = "eurobank_antifraude" }.ConnectionString;

    public string ConnectionStringNotificacoes =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = "eurobank_notificacoes" }.ConnectionString;

    public string ConnectionStringExtrato =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = "eurobank_extrato" }.ConnectionString;

    /// <summary>Activities encerradas no processo de teste (para conferir a continuidade do trace).</summary>
    public System.Collections.Concurrent.ConcurrentBag<Activity> Atividades { get; } = [];

    public async ValueTask InitializeAsync()
    {
        await _rede.CreateAsync();
        await Task.WhenAll(_postgres.StartAsync(), _kafka.StartAsync(), _mailpit.StartAsync(), _redis.StartAsync());
        Mailpit = new HttpClient { BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(8025)}") };

        await CriarBancosAsync();
        await CriarTopicosAsync();

        await _connect.StartAsync();
        await RegistrarConectoresAsync();

        _contas = new WebApplicationFactory<MarcadorContasApi>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Contas", ConnectionStringContas);
            builder.UseSetting("Seguranca:ApiKey", "chave-e2e-contas");
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
        });
        Contas = _contas.CreateClient();
        Contas.DefaultRequestHeaders.Add("X-Api-Key", "chave-e2e-contas");

        _extrato = new WebApplicationFactory<MarcadorExtratoWorker>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Extrato", ConnectionStringExtrato);
            builder.UseSetting("ConnectionStrings:Redis", _redis.GetConnectionString());
            builder.UseSetting("Kafka:BootstrapServers", BootstrapKafka);
            builder.UseSetting("Kafka:GrupoConsumidor", $"eurobank-extrato-e2e-{Guid.NewGuid():N}");
            builder.UseSetting("Seguranca:ApiKey", "chave-e2e-extrato");
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
        });
        Extrato = _extrato.CreateClient();
        Extrato.DefaultRequestHeaders.Add("X-Api-Key", "chave-e2e-extrato");

        // A API de Contas já aplicou as migrations: o worker de Contas pode subir.
        Subir(new WebApplicationFactory<MarcadorContasWorker>(), new()
        {
            ["ConnectionStrings:Contas"] = ConnectionStringContas,
            ["Kafka:BootstrapServers"] = BootstrapKafka,
            ["Kafka:GrupoConsumidor"] = $"eurobank-contas-worker-e2e-{Guid.NewGuid():N}",
        });

        await SubirAntifraudeAsync();

        var transferencias = Subir(new WebApplicationFactory<MarcadorTransferenciasApi>(), new(ConfiguracaoSaga)
        {
            ["ConnectionStrings:Transferencias"] = ConnectionStringTransferencias,
            ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
            ["Seguranca:ApiKey"] = "chave-e2e-transferencias",
        });
        Transferencias = transferencias.CreateClient();
        Transferencias.DefaultRequestHeaders.Add("X-Api-Key", "chave-e2e-transferencias");

        Subir(
            new WebApplicationFactory<MarcadorTransferenciasWorker>(),
            new(ConfiguracaoSaga)
            {
                ["ConnectionStrings:Transferencias"] = ConnectionStringTransferencias,
                ["Kafka:BootstrapServers"] = BootstrapKafka,
                ["Kafka:GrupoConsumidor"] = $"eurobank-transferencias-worker-e2e-{Guid.NewGuid():N}",
                ["Antifraude:Endereco"] = _antifraude.Server.BaseAddress.ToString(),
                ["Antifraude:ApiKey"] = ApiKeyAntifraude,
            },
            services => services.Configure<HttpClientFactoryOptions>(
                OpcoesAntifraude.NomeCliente,
                opcoes => opcoes.HttpMessageHandlerBuilderActions.Add(construtor =>
                    construtor.PrimaryHandler = new InterruptorAntifraude(() => AntifraudeForaDoAr, _antifraude.Server.CreateHandler()))));

        Subir(new WebApplicationFactory<MarcadorBancoExterno>(), new()
        {
            ["ConnectionStrings:BancoExterno"] = ConnectionStringBancoExterno,
            ["Kafka:BootstrapServers"] = BootstrapKafka,
            ["Kafka:GrupoConsumidor"] = $"eurobank-banco-externo-e2e-{Guid.NewGuid():N}",
            ["BancoExterno:LatenciaMs"] = "200",
            ["BancoExterno:AtrasoFinal97Segundos"] = "5",
            // O final 95 confirma depois de a saga desistir do banco e estornar (~15 s com os prazos acima).
            ["BancoExterno:AtrasoFinal95Segundos"] = "25",
            ["BancoExterno:AtrasoDevolucaoSegundos"] = "2",
        });

        Subir(new WebApplicationFactory<MarcadorCambio>(), new()
        {
            ["ConnectionStrings:Cambio"] = ConnectionStringCambio,
            ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
            ["Kafka:BootstrapServers"] = BootstrapKafka,
            ["Kafka:GrupoConsumidor"] = $"eurobank-cambio-e2e-{Guid.NewGuid():N}",
            ["Cambio:LatenciaMs"] = "200",
        });

        Subir(new WebApplicationFactory<MarcadorNotificacoesWorker>(), ConfiguracaoNotificacoes($"eurobank-notificacoes-e2e-{Guid.NewGuid():N}"));
    }

    /// <summary>
    /// API (aplica as migrations e semeia regras e listas) e worker do antifraude. Os casos expiram em 20 s, para o
    /// teste da expiração caber no tempo; as regras de comportamento ficam em sombra e Malta entra como país de risco.
    /// </summary>
    private async Task SubirAntifraudeAsync()
    {
        var redis = _redis.GetConnectionString();
        _antifraude = Subir(new WebApplicationFactory<MarcadorAntifraudeApi>(), new()
        {
            ["ConnectionStrings:Antifraude"] = ConnectionStringAntifraude,
            ["ConnectionStrings:Redis"] = redis,
            ["Seguranca:ApiKey"] = ApiKeyAntifraude,
            ["Revisao:PrazoCaso"] = "00:00:20",
        });
        Antifraude = _antifraude.CreateClient();
        Antifraude.DefaultRequestHeaders.Add("x-api-key", ApiKeyAntifraude);

        foreach (var codigo in RegrasEmSombra)
        {
            var regra = await Antifraude.GetFromJsonAsync<JsonElement>($"/regras/{codigo}");
            var ajuste = new { peso = regra.GetProperty("peso").GetInt32(), ativa = true, sombra = true };
            (await Antifraude.PutAsJsonAsync($"/regras/{codigo}", ajuste)).EnsureSuccessStatusCode();
        }

        (await Antifraude.PostAsJsonAsync("/listas/paises-risco", new { codigo = IbanPaisDeRisco[..2] })).EnsureSuccessStatusCode();

        Subir(new WebApplicationFactory<MarcadorAntifraudeWorker>(), new()
        {
            ["ConnectionStrings:Antifraude"] = ConnectionStringAntifraude,
            ["ConnectionStrings:Redis"] = redis,
            ["Kafka:BootstrapServers"] = BootstrapKafka,
            ["Kafka:GrupoConsumidor"] = $"eurobank-antifraude-worker-e2e-{Guid.NewGuid():N}",
            ["Expiracao:Intervalo"] = "00:00:01",
        });
    }

    /// <summary>
    /// Outra instância do worker de notificações, num consumer group novo e no mesmo banco: lê o tópico desde o
    /// início, como num reprocessamento. Quem chama descarta a instância no fim do teste.
    /// </summary>
    public IAsyncDisposable SubirOutroWorkerDeNotificacoes(string grupoConsumidor)
    {
        var aplicacao = new WebApplicationFactory<MarcadorNotificacoesWorker>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
            foreach (var (chave, valor) in ConfiguracaoNotificacoes(grupoConsumidor))
            {
                builder.UseSetting(chave, valor);
            }
        });
        using var _ = aplicacao.CreateClient();
        return aplicacao;
    }

    private Dictionary<string, string> ConfiguracaoNotificacoes(string grupoConsumidor) => new()
    {
        ["ConnectionStrings:Notificacoes"] = ConnectionStringNotificacoes,
        ["Kafka:BootstrapServers"] = BootstrapKafka,
        ["Kafka:GrupoConsumidor"] = grupoConsumidor,
        ["Smtp:Host"] = _mailpit.Hostname,
        ["Smtp:Porta"] = _mailpit.GetMappedPublicPort(1025).ToString(System.Globalization.CultureInfo.InvariantCulture),
    };

    /// <summary>Espera o consumer group confirmar os offsets até o fim de todas as partições do tópico.</summary>
    public async Task EsperarGrupoNoFimDoTopicoAsync(string grupoConsumidor, string topico, TimeSpan limite)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = BootstrapKafka }).Build();
        using var leitor = new ConsumerBuilder<Ignore, Ignore>(new ConsumerConfig
        {
            BootstrapServers = BootstrapKafka,
            GroupId = $"e2e-marcas-{Guid.NewGuid():N}",
        }).Build();

        var particoes = admin.GetMetadata(topico, TimeSpan.FromSeconds(10)).Topics.Single().Partitions
            .Select(particao => new TopicPartition(topico, particao.PartitionId))
            .ToList();
        var fins = particoes.ToDictionary(
            particao => particao.Partition.Value,
            particao => leitor.QueryWatermarkOffsets(particao, TimeSpan.FromSeconds(10)).High.Value);

        await Esperar.AteAsync(async () =>
        {
            var confirmados = await admin.ListConsumerGroupOffsetsAsync(
                [new ConsumerGroupTopicPartitions(grupoConsumidor, particoes)]);
            return confirmados.Single().Partitions.All(particao => particao.Offset.Value >= fins[particao.Partition.Value]);
        }, limite, $"o consumer group {grupoConsumidor} não chegou ao fim de {topico}");
    }

    /// <summary>Sobe uma aplicação in-process (Development, sem OTLP) e já cria o host, para ela começar a consumir.</summary>
    private WebApplicationFactory<T> Subir<T>(
        WebApplicationFactory<T> fabrica,
        Dictionary<string, string> configuracao,
        Action<IServiceCollection>? servicos = null)
        where T : class
    {
        var aplicacao = fabrica.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("OTEL_SDK_DISABLED", "true");
            foreach (var (chave, valor) in configuracao)
            {
                builder.UseSetting(chave, valor);
            }

            if (servicos is not null)
            {
                builder.ConfigureServices(servicos);
            }
        });
        using var _ = aplicacao.CreateClient();
        _aplicacoes.Add(aplicacao);
        return aplicacao;
    }

    /// <summary>Para o Kafka Connect (o replication slot passa a segurar o WAL).</summary>
    public Task PararConnectAsync() => _connect.StopAsync();

    /// <summary>Religa o Kafka Connect; ele retoma do offset salvo no Kafka.</summary>
    public async Task IniciarConnectAsync()
    {
        await _connect.StartAsync();
        await EsperarConectoresRodandoAsync();
    }

    /// <summary>Os databases além do eurobank_contas; precisam existir antes de registrar os conectores.</summary>
    private async Task CriarBancosAsync()
    {
        await using var conexao = new NpgsqlConnection(ConnectionStringContas);
        await conexao.OpenAsync();
        string[] bancos =
            ["eurobank_extrato", "eurobank_transferencias", "eurobank_banco_externo", "eurobank_cambio", "eurobank_notificacoes", "eurobank_antifraude"];
        foreach (var banco in bancos)
        {
            await using var comando = new NpgsqlCommand($"CREATE DATABASE {banco}", conexao);
            await comando.ExecuteNonQueryAsync();
        }
    }

    private async Task CriarTopicosAsync()
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = BootstrapKafka }).Build();
        await admin.CreateTopicsAsync(Topicos
            .Select(topico => new TopicSpecification { Name = topico, NumPartitions = 3, ReplicationFactor = 1 })
            .ToList());
    }

    private Uri UrlConnect => new($"http://{_connect.Hostname}:{_connect.GetMappedPublicPort(8083)}");

    private static string[] ArquivosDosConectores() =>
        Directory.GetFiles(Path.Combine(RaizDoRepositorio(), "infra", "connect", "conectores"), "*.json");

    private async Task RegistrarConectoresAsync()
    {
        using var http = new HttpClient { BaseAddress = UrlConnect };
        foreach (var arquivo in ArquivosDosConectores())
        {
            var nome = Path.GetFileNameWithoutExtension(arquivo);
            using var configuracao = new StringContent(await File.ReadAllTextAsync(arquivo), System.Text.Encoding.UTF8, "application/json");
            (await http.PutAsync($"/connectors/{nome}/config", configuracao)).EnsureSuccessStatusCode();
        }

        await EsperarConectoresRodandoAsync();
    }

    private async Task EsperarConectoresRodandoAsync()
    {
        using var http = new HttpClient { BaseAddress = UrlConnect };
        await Esperar.AteAsync(async () =>
        {
            foreach (var nome in ArquivosDosConectores().Select(Path.GetFileNameWithoutExtension))
            {
                var resposta = await http.GetAsync($"/connectors/{nome}/status");
                if (!resposta.IsSuccessStatusCode)
                {
                    return false;
                }

                using var status = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
                var tarefas = status.RootElement.GetProperty("tasks");
                if (status.RootElement.GetProperty("connector").GetProperty("state").GetString() != "RUNNING"
                    || tarefas.GetArrayLength() == 0
                    || tarefas[0].GetProperty("state").GetString() != "RUNNING")
                {
                    return false;
                }
            }

            return true;
        }, TimeSpan.FromSeconds(90), "os conectores do Debezium não ficaram RUNNING");
    }

    private static string RaizDoRepositorio()
    {
        var pasta = new DirectoryInfo(AppContext.BaseDirectory);
        while (pasta is not null && !File.Exists(Path.Combine(pasta.FullName, "EuroBank.slnx")))
        {
            pasta = pasta.Parent;
        }

        return pasta?.FullName ?? throw new InvalidOperationException("Raiz do repositório (EuroBank.slnx) não encontrada.");
    }

    public async ValueTask DisposeAsync()
    {
        _ouvinteAtividades.Dispose();
        Contas?.Dispose();
        Extrato?.Dispose();
        Transferencias?.Dispose();
        Antifraude?.Dispose();
        Mailpit?.Dispose();
        foreach (var aplicacao in Enumerable.Reverse(_aplicacoes))
        {
            await aplicacao.DisposeAsync();
        }

        if (_extrato is not null)
        {
            await _extrato.DisposeAsync();
        }

        if (_contas is not null)
        {
            await _contas.DisposeAsync();
        }

        await _connect.DisposeAsync();
        await _mailpit.DisposeAsync();
        await _redis.DisposeAsync();
        await _kafka.DisposeAsync();
        await _postgres.DisposeAsync();
        await _rede.DisposeAsync();
    }
}
