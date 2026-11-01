using EuroBank.Contas.Dominio.Comum;

namespace EuroBank.Contas.Dominio.Contas;

public sealed class ExcecaoIbanInvalido(string valor)
    : ExcecaoValidacao("iban_invalido", $"IBAN inválido: '{valor}'.");

public sealed class ExcecaoTitularInvalido(string motivo)
    : ExcecaoValidacao("titular_invalido", $"Titular inválido: {motivo}.");

public sealed class ExcecaoContaNaoEncontrada(ContaId contaId)
    : ExcecaoNaoEncontrado("conta_nao_encontrada", $"A conta {contaId} não existe.");

public sealed class ExcecaoIbanNaoEncontrado(Iban iban)
    : ExcecaoNaoEncontrado("iban_nao_encontrado", $"Não existe conta com o IBAN {iban}.");

public sealed class ExcecaoSaldoInsuficiente(Dinheiro saldo, Dinheiro valor)
    : ExcecaoRegraNegocio("saldo_insuficiente", $"Saldo insuficiente: saldo {saldo} EUR, valor pedido {valor} EUR.");

public sealed class ExcecaoContaBloqueada()
    : ExcecaoRegraNegocio("conta_bloqueada", "A conta está bloqueada e só pode receber créditos.");

public sealed class ExcecaoContaJaBloqueada()
    : ExcecaoRegraNegocio("conta_ja_bloqueada", "A conta já está bloqueada.");

public sealed class ExcecaoContaNaoBloqueada()
    : ExcecaoRegraNegocio("conta_nao_bloqueada", "Só uma conta bloqueada pode ser desbloqueada.");

public sealed class ExcecaoContaEncerrada()
    : ExcecaoRegraNegocio("conta_encerrada", "A conta está encerrada e não aceita operações.");

public sealed class ExcecaoContaComSaldo(Dinheiro saldo)
    : ExcecaoRegraNegocio("conta_com_saldo", $"A conta só pode ser encerrada com saldo zero (saldo atual {saldo} EUR).");
