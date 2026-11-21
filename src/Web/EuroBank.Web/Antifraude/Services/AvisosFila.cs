using EuroBank.Web.Antifraude.TransferObjects;

namespace EuroBank.Web.Antifraude.Services;

/// <summary>
/// Leva os eventos do Kafka para as telas abertas: cada tela assina ao abrir e cancela ao fechar; o manipulador
/// publica. Uma instância por painel (cada uma tem o próprio consumer group e recebe todos os eventos).
/// </summary>
public sealed class AvisosFila(ILogger<AvisosFila> logger)
{
    private readonly Lock _trava = new();
    private readonly List<Action<AvisoFila>> _assinantes = [];

    public IDisposable Assinar(Action<AvisoFila> assinante)
    {
        lock (_trava)
        {
            _assinantes.Add(assinante);
        }

        return new Assinatura(() =>
        {
            lock (_trava)
            {
                _assinantes.Remove(assinante);
            }
        });
    }

    public void Publicar(AvisoFila aviso)
    {
        Action<AvisoFila>[] assinantes;
        lock (_trava)
        {
            assinantes = [.. _assinantes];
        }

        foreach (var assinante in assinantes)
        {
            try
            {
                assinante(aviso);
            }
            catch (Exception erro) when (erro is not OutOfMemoryException)
            {
                // Uma tela que já fechou (ou quebrou) não impede as outras de receber o aviso.
                logger.LogWarning(erro, "Uma tela não recebeu o aviso da operação {OperacaoId}", aviso.OperacaoId);
            }
        }
    }

    private sealed class Assinatura(Action cancelar) : IDisposable
    {
        public void Dispose() => cancelar();
    }
}
