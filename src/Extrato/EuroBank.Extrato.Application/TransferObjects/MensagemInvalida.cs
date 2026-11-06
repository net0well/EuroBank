namespace EuroBank.Extrato.Application.TransferObjects;

/// <summary>Mensagem que não segue o contrato do tópico (ex.: sem o header tipo). Vai para o dead letter.</summary>
public sealed record MensagemInvalida(string Motivo);
