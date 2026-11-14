namespace EuroBank.Notificacoes.Worker.TransferObjects;

/// <summary>O que o e-mail diz: assunto e as duas versões do corpo (multipart/alternative).</summary>
public sealed record ConteudoEmail(string Assunto, string Html, string Texto);
