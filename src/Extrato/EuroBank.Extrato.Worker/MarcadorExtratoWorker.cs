namespace EuroBank.Extrato.Worker;

/// <summary>
/// Tipo de referência do assembly do worker, usado por WebApplicationFactory nos testes (evita depender da classe
/// Program, que colidiria com a da API de Contas no projeto de testes ponta a ponta).
/// </summary>
public sealed class MarcadorExtratoWorker;
