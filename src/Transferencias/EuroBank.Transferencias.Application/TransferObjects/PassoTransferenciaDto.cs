namespace EuroBank.Transferencias.Application.TransferObjects;

public sealed record PassoTransferenciaDto(int Ordem, string EstadoApos, string Descricao, DateTimeOffset OcorridoEm);
