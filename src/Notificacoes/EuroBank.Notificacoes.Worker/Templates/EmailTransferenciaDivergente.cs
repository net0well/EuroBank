using EuroBank.Notificacoes.Worker.TransferObjects;
using static EuroBank.Notificacoes.Worker.Templates.DetalhesTransferencia;

namespace EuroBank.Notificacoes.Worker.Templates;

public static class EmailTransferenciaDivergente
{
    public static ConteudoEmail Montar(EventoTransferenciaDivergente evento)
    {
        var resumo = Exigir(evento.Resumo);
        var beneficiario = resumo.NomeBeneficiario;
        string[] paragrafos = evento.Motivo switch
        {
            "ConfirmadaAposEstorno" =>
            [
                $"O banco do beneficiário confirmou a sua transferência para {beneficiario} depois de o valor ter voltado para a sua conta.",
                "A operação ficou em análise e será reconciliada: o valor e a tarifa podem ser debitados novamente.",
            ],
            "DevolucaoNaoCreditada" =>
            [
                $"O banco do beneficiário devolveu a sua transferência para {beneficiario}, mas não foi possível creditar o valor na sua conta.",
                "A operação ficou em análise e a equipe do EuroBank vai tratá-la.",
            ],
            _ => [$"A sua transferência para {beneficiario} ficou em análise: {DescricoesMotivo.Descrever(evento.Motivo)}."],
        };

        return ModeloEmail.Montar(
            $"Transferência em análise: {ValorPedido(resumo)} para {beneficiario}",
            resumo.NomeTitular,
            paragrafos,
            [
                .. Destino(resumo),
                ("Valor", ValorPedido(resumo)),
                .. Opcionais(resumo),
                .. Rodape(evento.TransferenciaId, evento.OcorridoEm),
            ]);
    }
}
