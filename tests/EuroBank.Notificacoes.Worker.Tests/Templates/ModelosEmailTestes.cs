using EuroBank.Notificacoes.Worker.Templates;
using EuroBank.Notificacoes.Worker.Tests.Fixtures;

namespace EuroBank.Notificacoes.Worker.Tests.Templates;

/// <summary>Conteúdo dos e-mails em pt-BR: assunto, versão HTML e versão texto de cada evento final.</summary>
public sealed class ModelosEmailTestes
{
    [Fact]
    public void Concluida_AssuntoEValoresEmFormatoBrasileiro()
    {
        var evento = Eventos.Concluida();

        var email = EmailTransferenciaConcluida.Montar(evento);

        email.Assunto.ShouldBe("Transferência concluída: 1.234,50 EUR para Bruno Souza");
        email.Texto.ShouldContain("Olá, Ana Lima.");
        email.Texto.ShouldContain("Sua transferência para Bruno Souza foi concluída.");
        email.Texto.ShouldContain("Valor enviado: 1.234,50 EUR");
        email.Texto.ShouldContain("Debitado da conta: 1.237,00 EUR (1.234,50 EUR + tarifa de 2,50 EUR)");
        email.Texto.ShouldContain("IBAN de destino: DE89370400440532013000");
        email.Texto.ShouldContain("Descrição: Aluguel de novembro");
        email.Texto.ShouldContain("Data: 13/11/2026 às 17:05 UTC");
        email.Texto.ShouldContain($"Transferência: {evento.TransferenciaId}");
        email.Html.ShouldContain("<html");
        email.Html.ShouldContain("1.234,50 EUR");
    }

    [Fact]
    public void ConcluidaComCambio_MostraACotacaoEOsDoisValores()
    {
        var email = EmailTransferenciaConcluida.Montar(Eventos.Concluida(Eventos.ResumoComCambio()));

        email.Assunto.ShouldBe("Transferência concluída: 100,00 USD para John Smith");
        email.Texto.ShouldContain("Valor enviado: 100,00 USD");
        email.Texto.ShouldContain("Debitado da conta: 96,03 EUR (93,53 EUR + tarifa de 2,50 EUR)");
        email.Texto.ShouldContain("Cotação: 1 EUR = 1,0692 USD");
        email.Texto.ShouldNotContain("Descrição:");
    }

    [Fact]
    public void ConcluidaPorReconciliacao_ExplicaOSegundoDebito()
    {
        var email = EmailTransferenciaConcluida.Montar(Eventos.Concluida(motivo: "Reconciliada"));

        email.Assunto.ShouldBe("Transferência concluída após reconciliação: 1.234,50 EUR para Bruno Souza");
        email.Texto.ShouldContain("o valor e a tarifa foram debitados novamente");
    }

    [Theory]
    [InlineData("RecusadaPeloBanco", "o banco do beneficiário recusou a transferência")]
    [InlineData("SemRespostaDoBanco", "o banco do beneficiário não respondeu a tempo")]
    [InlineData("CanceladaPeloCliente", "você pediu o cancelamento")]
    [InlineData("DestinoEncerrado", "a conta de destino está encerrada")]
    public void Cancelada_ExplicaOMotivoEQueODinheiroVoltou(string motivo, string explicacao)
    {
        var email = EmailTransferenciaCancelada.Montar(Eventos.Cancelada(motivo));

        email.Assunto.ShouldBe("Transferência cancelada: 1.234,50 EUR para Bruno Souza");
        email.Texto.ShouldContain($"Sua transferência para Bruno Souza foi cancelada: {explicacao}.");
        email.Texto.ShouldContain("voltou para a sua conta junto com a tarifa");
    }

    [Theory]
    [InlineData("SaldoInsuficiente", "saldo insuficiente")]
    [InlineData("LimiteDiarioExcedido", "o limite diário de transferências foi atingido")]
    [InlineData("CambioIndisponivel", "não foi possível cotar o câmbio entre as moedas")]
    [InlineData("SaldoNegativo", "a conta de origem está com saldo negativo")]
    [InlineData("MotivoNovo", "MotivoNovo")]
    public void Recusada_ExplicaOMotivoEQueNadaFoiDebitado(string motivo, string explicacao)
    {
        var email = EmailTransferenciaRecusada.Montar(Eventos.Recusada(motivo));

        email.Assunto.ShouldBe("Transferência não realizada: 1.234,50 EUR para Bruno Souza");
        email.Texto.ShouldContain($"Não foi possível fazer a sua transferência para Bruno Souza: {explicacao}.");
        email.Texto.ShouldContain("Nada foi debitado da sua conta.");
    }

    [Fact]
    public void Divergente_PorConfirmacaoTardia_AvisaDaReconciliacao()
    {
        var email = EmailTransferenciaDivergente.Montar(Eventos.Divergente("ConfirmadaAposEstorno"));

        email.Assunto.ShouldBe("Transferência em análise: 1.234,50 EUR para Bruno Souza");
        email.Texto.ShouldContain("confirmou a sua transferência para Bruno Souza depois de o valor ter voltado para a sua conta");
        email.Texto.ShouldContain("o valor e a tarifa podem ser debitados novamente");
    }

    [Fact]
    public void Divergente_PorDevolucaoNaoCreditada_AvisaDoTratamentoManual()
    {
        var email = EmailTransferenciaDivergente.Montar(Eventos.Divergente("DevolucaoNaoCreditada"));

        email.Texto.ShouldContain("devolveu a sua transferência para Bruno Souza, mas não foi possível creditar o valor");
    }

    [Fact]
    public void Devolvida_InformaOValorDeVoltaEATarifaRetida()
    {
        var email = EmailTransferenciaDevolvida.Montar(Eventos.Devolvida());

        email.Assunto.ShouldBe("Transferência devolvida: 1.234,50 EUR para Bruno Souza");
        email.Texto.ShouldContain("O valor de 1.234,50 EUR voltou para a sua conta; a tarifa de 2,50 EUR não é devolvida.");
    }

    [Fact]
    public void TextoDoCliente_EhEscapadoNoHtml()
    {
        var resumo = Eventos.Resumo(beneficiario: "<script>alert(1)</script>", descricao: "Aluguel & condomínio");

        var email = EmailTransferenciaConcluida.Montar(Eventos.Concluida(resumo));

        email.Html.ShouldNotContain("<script>");
        email.Html.ShouldContain("&lt;script&gt;");
        email.Html.ShouldContain("Aluguel &amp; condom");
        email.Texto.ShouldContain("<script>alert(1)</script>");
    }

    [Fact]
    public void QuebraDeLinhaNoNome_NaoVaiParaOAssunto()
    {
        var resumo = Eventos.Resumo(beneficiario: "Bruno\r\nBcc: alguem@exemplo.com");

        var email = EmailTransferenciaRecusada.Montar(Eventos.Recusada(resumo: resumo));

        email.Assunto.ShouldNotContain("\r");
        email.Assunto.ShouldNotContain("\n");
        email.Assunto.ShouldBe("Transferência não realizada: 1.234,50 EUR para Bruno Bcc: alguem@exemplo.com");
    }
}
