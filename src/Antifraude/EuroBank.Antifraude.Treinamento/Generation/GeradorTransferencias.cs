using EuroBank.Antifraude.Features.Services;
using EuroBank.Antifraude.Features.ValueObjects;
using EuroBank.Antifraude.Treinamento.Enums;
using EuroBank.Antifraude.Treinamento.TransferObjects;

namespace EuroBank.Antifraude.Treinamento.Generation;

/// <summary>
/// Gera o mundo sintético do treino: clientes com salário, beneficiários habituais, valor típico e horário próprios,
/// mais os quatro padrões de fraude. Cada conta é simulada em ordem de tempo com o mesmo acumulador e a mesma
/// calculadora de features da produção, e cada transferência vira uma linha com as features de antes dela. Há 30 dias
/// de aquecimento antes do período, para as janelas de 30 dias já começarem cheias.
/// </summary>
public sealed class GeradorTransferencias(OpcoesGeracao opcoes)
{
    /// <summary>Os países de risco do mundo sintético (os mesmos da lista inicial do antifraude).</summary>
    public static readonly IReadOnlySet<string> PaisesRisco = new HashSet<string>(["IR", "KP", "MM"], StringComparer.Ordinal);

    private static readonly string[] PaisesComuns = ["DE", "FR", "ES", "PT", "IT", "NL", "BE", "AT"];

    private static readonly TimeSpan Aquecimento = TimeSpan.FromDays(30);

    private readonly Random _aleatorio = new(opcoes.Semente);

    public IReadOnlyList<TransferenciaSintetica> Gerar()
    {
        var clientes = Enumerable.Range(0, opcoes.Contas).Select(_ => SortearCliente()).ToList();
        Distribuir(clientes, PadraoFraude.TomadaDeConta, opcoes.Episodios(220));
        Distribuir(clientes, PadraoFraude.TesteDeCartao, opcoes.Episodios(80));
        Distribuir(clientes, PadraoFraude.EngenhariaSocial, opcoes.Episodios(300));

        var linhas = new List<TransferenciaSintetica>();
        foreach (var cliente in clientes)
        {
            linhas.AddRange(Simular(cliente, AgendaDoCliente(cliente)));
        }

        for (var i = 0; i < opcoes.Mulas; i++)
        {
            var mula = SortearMula();
            linhas.AddRange(Simular(mula, AgendaDaMula(mula)));
        }

        return [.. linhas.OrderBy(linha => linha.Momento).ThenBy(linha => linha.ContaId)];
    }

    private ClienteSimulado SortearCliente()
    {
        var salario = Arredondar(2_500m * (decimal)Math.Exp(Normal(0, 0.5)));
        var abreDurante = _aleatorio.NextDouble() < 0.08;
        var abertaEm = abreDurante
            ? opcoes.InicioEfetivo.AddDays(_aleatorio.Next(0, Math.Max(1, opcoes.Dias - 10))).AddHours(_aleatorio.Next(8, 20))
            : opcoes.InicioEfetivo - Aquecimento - TimeSpan.FromDays(_aleatorio.Next(1, 1_500));
        return new ClienteSimulado(
            NovoId(),
            abertaEm,
            salario,
            DiaSalario: _aleatorio.Next(1, 29),
            TaxaDiaria: 0.15 + (_aleatorio.NextDouble() * 0.85),
            ValorTipico: Math.Max(3m, Arredondar(salario * (decimal)(0.01 + (_aleatorio.NextDouble() * 0.03)))),
            HoraPico: _aleatorio.Next(9, 21),
            Beneficiarios: Enumerable.Range(0, _aleatorio.Next(3, 9)).Select(_ => NovoIban(risco: false)).ToList(),
            Mula: false);
    }

    private ClienteSimulado SortearMula() => new(
        NovoId(),
        opcoes.InicioEfetivo.AddDays(_aleatorio.Next(0, Math.Max(1, opcoes.Dias - 12))).AddHours(_aleatorio.Next(8, 22)),
        Salario: 0,
        DiaSalario: 0,
        TaxaDiaria: 0,
        ValorTipico: Arredondar(20m + (decimal)(_aleatorio.NextDouble() * 60)),
        HoraPico: _aleatorio.Next(10, 22),
        Beneficiarios: [NovoIban(risco: false)],
        Mula: true);

    /// <summary>Episódios em clientes sorteados, em qualquer dia do período (o treino e o teste temporal recebem os dois).</summary>
    private void Distribuir(List<ClienteSimulado> clientes, PadraoFraude padrao, int episodios)
    {
        for (var i = 0; i < episodios; i++)
        {
            var cliente = clientes[_aleatorio.Next(clientes.Count)];
            var dia = opcoes.InicioEfetivo.AddDays(_aleatorio.Next(0, opcoes.Dias)).UtcDateTime.Date;
            var hora = padrao switch
            {
                PadraoFraude.TomadaDeConta => _aleatorio.Next(0, 6),
                PadraoFraude.EngenhariaSocial => _aleatorio.Next(9, 21),
                _ => _aleatorio.Next(0, 24),
            };
            var momento = new DateTimeOffset(dia, TimeSpan.Zero).AddHours(hora).AddMinutes(_aleatorio.Next(60));
            if (momento > cliente.AbertaEm.AddDays(2))
            {
                cliente.Episodios.Add((padrao, momento));
            }
        }
    }

    /// <summary>Salário, depósitos avulsos e transferências do dia a dia, do aquecimento ao fim do período.</summary>
    private List<IntencaoSimulada> AgendaDoCliente(ClienteSimulado cliente)
    {
        var agenda = new List<IntencaoSimulada> { new(cliente.AbertaEm, TipoIntencao.Abertura, PadraoFraude.Nenhum) };
        var inicio = Maximo(cliente.AbertaEm, opcoes.InicioEfetivo - Aquecimento);
        agenda.Add(new(inicio.AddHours(1), TipoIntencao.Entrada, PadraoFraude.Nenhum, cliente.Salario * (decimal)(0.5 + (_aleatorio.NextDouble() * 2.5))));

        for (var meiaNoite = new DateTimeOffset(inicio.UtcDateTime.Date, TimeSpan.Zero); meiaNoite < opcoes.Fim; meiaNoite = meiaNoite.AddDays(1))
        {
            if (meiaNoite.Day == cliente.DiaSalario)
            {
                agenda.Add(new(meiaNoite.AddHours(8).AddMinutes(_aleatorio.Next(120)), TipoIntencao.Entrada, PadraoFraude.Nenhum,
                    cliente.Salario * (decimal)(0.95 + (_aleatorio.NextDouble() * 0.1))));
            }

            if (_aleatorio.NextDouble() < 0.05)
            {
                agenda.Add(new(meiaNoite.AddHours(_aleatorio.Next(8, 20)), TipoIntencao.Entrada, PadraoFraude.Nenhum,
                    cliente.ValorTipico * (decimal)(1 + (_aleatorio.NextDouble() * 9))));
            }

            for (var i = Poisson(cliente.TaxaDiaria); i > 0; i--)
            {
                agenda.Add(new(meiaNoite.Add(HorarioDoCliente(cliente)), TipoIntencao.SaidaNormal, PadraoFraude.Nenhum));
            }
        }

        agenda.AddRange(cliente.Episodios.Select(episodio => new IntencaoSimulada(episodio.Momento, TipoIntencao.Episodio, episodio.Padrao)));
        return agenda;
    }

    /// <summary>A mula: um depósito pequeno, uns gastos para disfarçar e de um a três ciclos de recebe e repassa.</summary>
    private List<IntencaoSimulada> AgendaDaMula(ClienteSimulado mula)
    {
        var agenda = new List<IntencaoSimulada>
        {
            new(mula.AbertaEm, TipoIntencao.Abertura, PadraoFraude.Nenhum),
            new(mula.AbertaEm.AddMinutes(30), TipoIntencao.Entrada, PadraoFraude.Nenhum, 10m + (decimal)(_aleatorio.NextDouble() * 90)),
        };

        for (var i = _aleatorio.Next(0, 4); i > 0; i--)
        {
            agenda.Add(new(mula.AbertaEm.AddHours(_aleatorio.Next(2, 48)), TipoIntencao.SaidaNormal, PadraoFraude.Nenhum));
        }

        var ciclo = mula.AbertaEm.AddDays(0.5 + (_aleatorio.NextDouble() * 7));
        for (var i = _aleatorio.Next(1, 4); i > 0 && ciclo < opcoes.Fim; i--)
        {
            agenda.Add(new(ciclo, TipoIntencao.Episodio, PadraoFraude.MulaDeDinheiro));
            ciclo = ciclo.AddDays(1 + (_aleatorio.NextDouble() * 5));
        }

        return agenda;
    }

    private IEnumerable<TransferenciaSintetica> Simular(ClienteSimulado cliente, List<IntencaoSimulada> agenda)
    {
        var fila = new PriorityQueue<IntencaoSimulada, (DateTimeOffset, long)>();
        long sequencia = 0;
        foreach (var intencao in agenda)
        {
            fila.Enqueue(intencao, (intencao.Momento, sequencia++));
        }

        var acumulador = new AcumuladorHistorico();
        var versao = 0;
        decimal saldo = 0;
        var linhas = new List<TransferenciaSintetica>();

        while (fila.TryDequeue(out var intencao, out _))
        {
            switch (intencao.Tipo)
            {
                case TipoIntencao.Abertura:
                    acumulador.Abrir(++versao, intencao.Momento);
                    break;
                case TipoIntencao.Entrada:
                    var entrada = Arredondar(intencao.Valor);
                    saldo += entrada;
                    acumulador.Entrada(++versao, intencao.Momento, entrada, saldo);
                    break;
                case TipoIntencao.SaidaNormal:
                    SaidaNormal(intencao.Momento);
                    break;
                case TipoIntencao.Saida:
                    Sair(intencao.Momento, intencao.Valor, intencao.Iban!, intencao.Padrao);
                    break;
                case TipoIntencao.Episodio:
                    foreach (var passo in Episodio(cliente, intencao, saldo))
                    {
                        fila.Enqueue(passo, (passo.Momento, sequencia++));
                    }

                    break;
            }
        }

        return linhas;

        void SaidaNormal(DateTimeOffset momento)
        {
            var habitual = _aleatorio.NextDouble() < 0.85;
            var iban = habitual
                ? cliente.Beneficiarios[_aleatorio.Next(cliente.Beneficiarios.Count)]
                : NovoIban(risco: _aleatorio.NextDouble() < 0.005);
            if (!habitual && _aleatorio.NextDouble() < 0.3)
            {
                cliente.Beneficiarios.Add(iban);
            }

            var valor = cliente.ValorTipico * (decimal)Math.Exp(Normal(0, 0.6));
            if (_aleatorio.NextDouble() < 0.02)
            {
                // Uma compra grande legítima: o ruído que a engenharia social precisa superar.
                valor *= (decimal)(5 + (_aleatorio.NextDouble() * 10));
            }

            Sair(momento, valor, iban, PadraoFraude.Nenhum);
        }

        void Sair(DateTimeOffset momento, decimal valorBruto, string iban, PadraoFraude padrao)
        {
            var valor = Math.Max(1m, Arredondar(valorBruto));
            if (valor > saldo)
            {
                return;
            }

            var paisDeRisco = PaisesRisco.Contains(iban[..2]);
            var features = CalculadoraFeatures.Calcular(acumulador.Retrato(momento), new OperacaoAvaliada(momento, valor, iban, paisDeRisco));
            saldo -= valor;
            acumulador.Saida(++versao, momento, valor, iban, saldo);
            if (momento >= opcoes.InicioEfetivo && momento < opcoes.Fim)
            {
                linhas.Add(new TransferenciaSintetica(momento, cliente.ContaId, valor, padrao, features));
            }
        }
    }

    /// <summary>Os passos de um episódio de fraude, com os valores calculados sobre o saldo do começo dele.</summary>
    private IEnumerable<IntencaoSimulada> Episodio(ClienteSimulado cliente, IntencaoSimulada episodio, decimal saldo)
    {
        var momento = episodio.Momento;
        switch (episodio.Padrao)
        {
            case PadraoFraude.TomadaDeConta when saldo > 50:
                {
                    var destino = NovoIban(risco: _aleatorio.NextDouble() < 0.1);
                    var restante = saldo;
                    for (var i = _aleatorio.Next(1, 4); i > 0; i--)
                    {
                        var valor = restante * (decimal)(i == 1 ? 0.85 + (_aleatorio.NextDouble() * 0.14) : 0.5 + (_aleatorio.NextDouble() * 0.3));
                        restante -= valor;
                        yield return new(momento, TipoIntencao.Saida, episodio.Padrao, valor, destino);
                        momento = momento.AddMinutes(_aleatorio.Next(2, 16));
                    }

                    break;
                }

            case PadraoFraude.EngenhariaSocial when saldo > 100:
                {
                    var valor = Math.Min(
                        saldo * 0.98m,
                        Math.Max(cliente.ValorTipico * (decimal)(6 + (_aleatorio.NextDouble() * 14)), saldo * (decimal)(0.4 + (_aleatorio.NextDouble() * 0.5))));
                    yield return new(momento, TipoIntencao.Saida, episodio.Padrao, valor, NovoIban(risco: _aleatorio.NextDouble() < 0.25));
                    break;
                }

            case PadraoFraude.TesteDeCartao when saldo > 20:
                {
                    for (var i = _aleatorio.Next(3, 7); i > 0; i--)
                    {
                        yield return new(momento, TipoIntencao.Saida, episodio.Padrao, 1m + (decimal)(_aleatorio.NextDouble() * 8.99), NovoIban(risco: false));
                        momento = momento.AddSeconds(_aleatorio.Next(30, 181));
                    }

                    if (_aleatorio.NextDouble() < 0.5)
                    {
                        var valor = Math.Min(saldo * 0.9m, 200m + (decimal)(_aleatorio.NextDouble() * 800));
                        yield return new(momento.AddMinutes(_aleatorio.Next(5, 31)), TipoIntencao.Saida, episodio.Padrao, valor, NovoIban(risco: false));
                    }

                    break;
                }

            case PadraoFraude.MulaDeDinheiro:
                {
                    decimal recebido = 0;
                    for (var i = _aleatorio.Next(2, 6); i > 0; i--)
                    {
                        var entrada = 400m + (decimal)(_aleatorio.NextDouble() * 2_600);
                        recebido += entrada;
                        yield return new(momento, TipoIntencao.Entrada, PadraoFraude.Nenhum, entrada);
                        momento = momento.AddMinutes(_aleatorio.Next(10, 91));
                    }

                    var repasse = recebido * (decimal)(0.85 + (_aleatorio.NextDouble() * 0.1));
                    var partes = _aleatorio.Next(1, 4);
                    for (var i = 0; i < partes; i++)
                    {
                        momento = momento.AddMinutes(_aleatorio.Next(20, 360));
                        yield return new(momento, TipoIntencao.Saida, episodio.Padrao, repasse / partes, NovoIban(risco: _aleatorio.NextDouble() < 0.3));
                    }

                    break;
                }
        }
    }

    /// <summary>Hora do dia em torno do pico do cliente; de vez em quando, a qualquer hora (inclusive de madrugada).</summary>
    private TimeSpan HorarioDoCliente(ClienteSimulado cliente)
    {
        var hora = _aleatorio.NextDouble() < 0.03
            ? _aleatorio.Next(0, 24)
            : (int)Math.Clamp(Math.Round(Normal(cliente.HoraPico, 3)), 6, 23);
        return TimeSpan.FromHours(hora) + TimeSpan.FromSeconds(_aleatorio.Next(3_600));
    }

    /// <summary>Id tirado do gerador, para a mesma semente dar as mesmas contas.</summary>
    private Guid NovoId()
    {
        var bytes = new byte[16];
        _aleatorio.NextBytes(bytes);
        return new Guid(bytes);
    }

    private string NovoIban(bool risco)
    {
        var pais = risco ? PaisesRisco.ElementAt(_aleatorio.Next(PaisesRisco.Count)) : PaisesComuns[_aleatorio.Next(PaisesComuns.Length)];
        return $"{pais}{_aleatorio.Next(10, 100)}{_aleatorio.NextInt64(100_000_000_000_000_000, 999_999_999_999_999_999)}";
    }

    private double Normal(double media, double desvio)
    {
        // Box-Muller.
        var u1 = 1.0 - _aleatorio.NextDouble();
        var u2 = _aleatorio.NextDouble();
        return media + (desvio * Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
    }

    private int Poisson(double lambda)
    {
        var limite = Math.Exp(-lambda);
        var k = 0;
        var p = 1.0;
        do
        {
            k++;
            p *= _aleatorio.NextDouble();
        }
        while (p > limite);
        return k - 1;
    }

    private static decimal Arredondar(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static DateTimeOffset Maximo(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;
}
