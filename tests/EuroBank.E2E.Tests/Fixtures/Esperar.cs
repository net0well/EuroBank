namespace EuroBank.E2E.Tests.Fixtures;

/// <summary>Consistência eventual: repete a verificação até ela passar ou o tempo acabar.</summary>
public static class Esperar
{
    public static async Task AteAsync(Func<Task<bool>> condicao, TimeSpan limite, string descricao)
    {
        var fim = DateTime.UtcNow + limite;
        Exception? ultimoErro = null;
        while (DateTime.UtcNow < fim)
        {
            try
            {
                if (await condicao())
                {
                    return;
                }
            }
            catch (Exception erro) when (erro is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                ultimoErro = erro;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        throw new TimeoutException($"Tempo esgotado ({limite.TotalSeconds:0}s): {descricao}.", ultimoErro);
    }
}
