namespace EuroBank.Notificacoes.Worker.Tests.Fixtures;

/// <summary>Repete a verificação até ela passar ou o tempo acabar (o retry agendado roda em segundo plano).</summary>
internal static class Esperar
{
    public static async Task AteAsync(Func<Task<bool>> condicao, TimeSpan limite, string descricao)
    {
        var fim = DateTime.UtcNow + limite;
        while (DateTime.UtcNow < fim)
        {
            if (await condicao())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException($"Tempo esgotado ({limite.TotalSeconds:0}s): {descricao}.");
    }
}
