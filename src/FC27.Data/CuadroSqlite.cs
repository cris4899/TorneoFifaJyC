using FC27.Core;
using Microsoft.EntityFrameworkCore;

namespace FC27.Data;

/// <summary>Persiste el cuadro de eliminación y sus resultados.</summary>
public sealed class CuadroSqlite(TorneoDbContext db)
{
    /// <summary>Crea todas las fases y abre el torneo en una sola transacción.</summary>
    public async Task IniciarAsync(int torneoId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var torneo = await db.Torneos.AsNoTracking().SingleOrDefaultAsync(t => t.Id == torneoId, ct)
            ?? throw new KeyNotFoundException("El torneo no existe.");
        if (torneo.Estado != EstadoTorneo.Borrador)
            throw new InvalidOperationException("El torneo ya se ha iniciado.");

        var inscritos = await db.Participantes.Where(p => p.TorneoId == torneoId)
            .Select(p => p.Id).ToListAsync(ct);
        var plan = Cuadro.Sortear(torneo.NumeroParticipantes, Cuadro.Barajar(inscritos, Random.Shared));
        var fases = plan.Fases.Select(f => new Fase(0, torneoId, f.Orden)).ToArray();

        // Las claves de las fases se generan antes de insertar sus partidos.
        db.Fases.AddRange(fases);
        await db.SaveChangesAsync(ct);
        foreach (var fase in plan.Fases)
        {
            var faseId = fases[fase.Orden - 1].Id;
            db.Partidos.AddRange(fase.Partidos.Select(p => new Partido(
                0, faseId, p.Posicion, p.LocalId, p.VisitanteId, 0, 0, EstadoPartido.Pendiente, null)));
        }

        await db.SaveChangesAsync(ct);
        await db.Torneos.Where(t => t.Id == torneoId)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.Estado, EstadoTorneo.EnCurso), ct);
        await tx.CommitAsync(ct);
    }
}
