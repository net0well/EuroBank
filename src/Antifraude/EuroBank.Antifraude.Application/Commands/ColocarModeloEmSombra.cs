namespace EuroBank.Antifraude.Application.Commands;

/// <summary>A versão passa a pontuar em sombra (challenger), sem decidir; o challenger anterior é aposentado.</summary>
public sealed record ColocarModeloEmSombra(int Versao);
