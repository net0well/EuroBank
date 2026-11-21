namespace EuroBank.Antifraude.Domain.Enums;

/// <summary>
/// Onde está uma execução de treino. Solicitada e EmAndamento são as ativas (só uma por vez); as outras são finais.
/// AbaixoDoMinimo avaliou o modelo, mas a AUC-PR não chegou ao mínimo: há relatório e não há versão.
/// </summary>
public enum StatusTreino
{
    Solicitada = 1,
    EmAndamento = 2,
    Concluida = 3,
    AbaixoDoMinimo = 4,
    Falhou = 5,
    Cancelada = 6,
}
