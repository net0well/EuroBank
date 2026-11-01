using System.Diagnostics;
using EuroBank.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EuroBank.Contas.Infrastructure.Outbox;

/// <summary>
/// Antes de cada SaveChanges, transforma os eventos de domínio dos agregados rastreados em linhas da outbox.
/// Como as linhas entram no mesmo SaveChanges, estado e eventos são gravados na mesma transação: ou os dois
/// existem, ou nenhum existe. É isso que torna o outbox confiável.
/// </summary>
public sealed class InterceptorOutbox(MapeadorMensagensOutbox mapeador) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AdicionarMensagens(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AdicionarMensagens(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AdicionarMensagens(DbContext? contexto)
    {
        if (contexto is null)
        {
            return;
        }

        var agregados = contexto.ChangeTracker
            .Entries<IRaizDeAgregado>()
            .Select(entrada => entrada.Entity)
            .Where(agregado => agregado.EventosDominio.Count > 0)
            .ToList();

        // Activity.Current é a requisição HTTP em andamento; o Id no formato W3C é o traceparent.
        var traceparent = Activity.Current?.Id;

        foreach (var agregado in agregados)
        {
            foreach (var evento in agregado.EventosDominio)
            {
                contexto.Set<MensagemOutbox>().Add(mapeador.Mapear(evento, traceparent));
            }

            // Limpa já aqui para um segundo SaveChanges no mesmo escopo não duplicar as linhas.
            agregado.LimparEventos();
        }
    }
}
