namespace EuroBank.Antifraude.Domain.Enums;

/// <summary>As etapas de um treino, na ordem em que acontecem.</summary>
public enum EtapaTreino
{
    /// <summary>Lê o CSV sintético, gerando-o antes se faltar (ou se a execução pediu dados novos).</summary>
    PreparandoDados = 1,

    /// <summary>Junta os rótulos reais: decisões dos analistas e fraudes confirmadas.</summary>
    CarregandoRotulos = 2,

    /// <summary>Os dias mais recentes dos dados sintéticos ficam para o teste temporal.</summary>
    SeparandoTreinoETeste = 3,

    /// <summary>As árvores do FastTree, uma a uma.</summary>
    Treinando = 4,

    /// <summary>AUC-PR no teste e o limiar recomendado pela meta.</summary>
    Avaliando = 5,

    /// <summary>Curva de aprendizado, importância das features, distribuição dos scores, calibração e padrões.</summary>
    Analisando = 6,

    /// <summary>Grava a versão nova, Registrada.</summary>
    Registrando = 7,
}
