namespace EuroBank.Shared.TransferObjects;

/// <summary>Mensagem com um tipo que o Extrato não conhece (ex.: evento novo publicado por outro contexto). É ignorada com alerta.</summary>
public sealed record EventoDesconhecido(string Tipo);
