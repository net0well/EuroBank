using EuroBank.Contas.Dominio.Comum;
using EuroBank.Contas.Dominio.Contas.Eventos;

namespace EuroBank.Contas.Dominio.Contas;

/// <summary>
/// Agregado Conta. Ciclo de vida: Ativa, Bloqueada (só recebe créditos) e Encerrada (final, só com saldo zero).
/// Toda operação bem-sucedida incrementa <see cref="Versao"/> e registra exatamente um evento de domínio.
/// </summary>
public sealed class Conta : RaizDeAgregado<ContaId>
{
    private Conta()
    {
        // Usado pelo EF Core.
    }

    public Iban Iban { get; private set; } = null!;

    public Titular Titular { get; private set; } = null!;

    public Dinheiro Saldo { get; private set; } = Dinheiro.Zero;

    public StatusConta Status { get; private set; }

    /// <summary>Versão de negócio da conta; também é o token de concorrência otimista.</summary>
    public int Versao { get; private set; }

    public DateTimeOffset AbertaEm { get; private set; }

    public static Conta Abrir(Iban iban, Titular titular, DateTimeOffset agora)
    {
        var conta = new Conta
        {
            Id = ContaId.Nova(),
            Iban = iban,
            Titular = titular,
            Saldo = Dinheiro.Zero,
            Status = StatusConta.Ativa,
            Versao = 1,
            AbertaEm = agora,
        };

        conta.Registrar(new ContaAberta(conta.Id, iban, titular, conta.Versao, agora));
        return conta;
    }

    public void Depositar(Dinheiro valor, DateTimeOffset agora)
    {
        GarantirPodeReceber(valor);

        Saldo += valor;
        Versao++;
        Registrar(new DepositoRealizado(Id, valor, Saldo, Versao, agora));
    }

    public void Sacar(Dinheiro valor, DateTimeOffset agora)
    {
        GarantirPodeDebitar(valor);

        Saldo -= valor;
        Versao++;
        Registrar(new SaqueRealizado(Id, valor, Saldo, Versao, agora));
    }

    public void Bloquear(DateTimeOffset agora)
    {
        GarantirNaoEncerrada();
        if (Status == StatusConta.Bloqueada)
        {
            throw new ExcecaoContaJaBloqueada();
        }

        Status = StatusConta.Bloqueada;
        Versao++;
        Registrar(new ContaBloqueada(Id, Versao, agora));
    }

    public void Desbloquear(DateTimeOffset agora)
    {
        GarantirNaoEncerrada();
        if (Status != StatusConta.Bloqueada)
        {
            throw new ExcecaoContaNaoBloqueada();
        }

        Status = StatusConta.Ativa;
        Versao++;
        Registrar(new ContaDesbloqueada(Id, Versao, agora));
    }

    public void Encerrar(DateTimeOffset agora)
    {
        GarantirNaoEncerrada();
        if (!Saldo.EhZero)
        {
            throw new ExcecaoContaComSaldo(Saldo);
        }

        Status = StatusConta.Encerrada;
        Versao++;
        Registrar(new ContaEncerrada(Id, Versao, agora));
    }

    // As operações de transferência são internas: só o ServicoTransferencia as coordena,
    // garantindo que débito e crédito aconteçam juntos.

    internal void GarantirPodeEnviar(Dinheiro valor) => GarantirPodeDebitar(valor);

    internal void GarantirPodeReceber(Dinheiro valor)
    {
        GarantirValorPositivo(valor);
        GarantirNaoEncerrada();
    }

    internal void EnviarTransferencia(Guid transferenciaId, Iban destino, Dinheiro valor, DateTimeOffset agora)
    {
        GarantirPodeEnviar(valor);

        Saldo -= valor;
        Versao++;
        Registrar(new TransferenciaEnviada(Id, transferenciaId, destino, valor, Saldo, Versao, agora));
    }

    internal void ReceberTransferencia(Guid transferenciaId, Iban origem, Dinheiro valor, DateTimeOffset agora)
    {
        GarantirPodeReceber(valor);

        Saldo += valor;
        Versao++;
        Registrar(new TransferenciaRecebida(Id, transferenciaId, origem, valor, Saldo, Versao, agora));
    }

    private void GarantirPodeDebitar(Dinheiro valor)
    {
        GarantirValorPositivo(valor);
        GarantirNaoEncerrada();
        if (Status == StatusConta.Bloqueada)
        {
            throw new ExcecaoContaBloqueada();
        }

        if (valor > Saldo)
        {
            throw new ExcecaoSaldoInsuficiente(Saldo, valor);
        }
    }

    private void GarantirNaoEncerrada()
    {
        if (Status == StatusConta.Encerrada)
        {
            throw new ExcecaoContaEncerrada();
        }
    }

    private static void GarantirValorPositivo(Dinheiro valor)
    {
        if (valor.EhZero)
        {
            throw new ExcecaoValorInvalido("o valor da operação deve ser maior que zero");
        }
    }
}
