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

    /// <summary>Corrige un resultado y su plaza de destino si el siguiente cruce sigue pendiente.</summary>
    public async Task CorregirResultadoAsync(int torneoId, int partidoId, ResultadoPartido resultado, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var cruces = await (
            from fase in db.Fases.AsNoTracking()
            join partido in db.Partidos.AsNoTracking() on fase.Id equals partido.FaseId
            where fase.TorneoId == torneoId
            select new { fase.Orden, Partido = partido }).ToListAsync(ct);
        var origen = cruces.SingleOrDefault(c => c.Partido.Id == partidoId)
            ?? throw new KeyNotFoundException("El partido no pertenece al torneo.");
        var partidoActual = origen.Partido;
        if (partidoActual.EstadoPartido != EstadoPartido.Jugado)
            throw new InvalidOperationException("Solo se corrigen marcadores confirmados.");

        var (posicion, esLocal) = Cuadro.Destino(partidoActual.Posicion);
        var destino = cruces.SingleOrDefault(c => c.Orden == origen.Orden + 1 && c.Partido.Posicion == posicion)?.Partido;
        if (destino is null)
            throw new InvalidOperationException("La final confirmada no se puede corregir.");
        if (destino.EstadoPartido != EstadoPartido.Pendiente)
            throw new InvalidOperationException("El siguiente cruce ya se ha jugado.");

        var ganador = Cuadro.ResolverGanador(partidoActual.EquipoLocalId, partidoActual.EquipoVisitanteId, resultado);
        // El marcador y la plaza que sale de este cruce se actualizan juntos.
        await db.Partidos.Where(p => p.Id == partidoId).ExecuteUpdateAsync(p => p
            .SetProperty(x => x.GolesLocal, resultado.GolesLocal)
            .SetProperty(x => x.GolesVisitante, resultado.GolesVisitante)
            .SetProperty(x => x.GanadorId, ganador), ct);
        var partidosDestino = db.Partidos.Where(p => p.Id == destino.Id);
        if (esLocal)
            await partidosDestino.ExecuteUpdateAsync(p => p.SetProperty(x => x.EquipoLocalId, ganador), ct);
        else
            await partidosDestino.ExecuteUpdateAsync(p => p.SetProperty(x => x.EquipoVisitanteId, ganador), ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>Confirma el marcador y el avance del ganador en una transacción.</summary>
    public async Task RegistrarResultadoAsync(int torneoId, int partidoId, ResultadoPartido resultado, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var torneo = await db.Torneos.AsNoTracking().SingleAsync(t => t.Id == torneoId, ct);
        if (torneo.Estado == EstadoTorneo.Borrador)
            throw new InvalidOperationException("El torneo todavía no se ha iniciado.");

        var cruces = await (
            from fase in db.Fases.AsNoTracking()
            join partido in db.Partidos.AsNoTracking() on fase.Id equals partido.FaseId
            where fase.TorneoId == torneoId
            orderby fase.Orden, partido.Posicion
            select new { fase.Orden, Partido = partido }).ToListAsync(ct);
        var origen = cruces.SingleOrDefault(c => c.Partido.Id == partidoId)
            ?? throw new KeyNotFoundException("El partido no pertenece al torneo.");
        var partidoActual = origen.Partido;
        var ganador = Cuadro.ResolverGanador(partidoActual.EquipoLocalId, partidoActual.EquipoVisitanteId, resultado);
        if (partidoActual.EstadoPartido == EstadoPartido.Jugado)
        {
            if (partidoActual.GanadorId == ganador && partidoActual.GolesLocal == resultado.GolesLocal
                && partidoActual.GolesVisitante == resultado.GolesVisitante)
                return;
            throw new InvalidOperationException("El resultado ya está confirmado.");
        }
        if (torneo.Estado != EstadoTorneo.EnCurso)
            throw new InvalidOperationException("El torneo ya ha finalizado.");

        var plan = new CuadroPlan(torneo.NumeroParticipantes, cruces.GroupBy(c => c.Orden)
            .Select(f => new FasePlan(f.Key, Cuadro.NombreFase(f.Count()), f.Select(c =>
                new PartidoPlan(c.Orden, c.Partido.Posicion, c.Partido.EquipoLocalId,
                    c.Partido.EquipoVisitanteId, c.Partido.GanadorId)).ToArray())).ToArray());
        var actualizado = Cuadro.Avanzar(plan, origen.Orden, partidoActual.Posicion, ganador);
        await db.Partidos.Where(p => p.Id == partidoId).ExecuteUpdateAsync(p => p
            .SetProperty(x => x.GolesLocal, resultado.GolesLocal)
            .SetProperty(x => x.GolesVisitante, resultado.GolesVisitante)
            .SetProperty(x => x.GanadorId, ganador)
            .SetProperty(x => x.EstadoPartido, EstadoPartido.Jugado), ct);

        // El resultado y la plaza de la siguiente fase se guardan juntos.
        if (Cuadro.Campeon(actualizado) is not null)
        {
            await db.Torneos.Where(t => t.Id == torneoId)
                .ExecuteUpdateAsync(t => t.SetProperty(x => x.Estado, EstadoTorneo.Finalizado), ct);
        }
        else
        {
            var (posicion, _) = Cuadro.Destino(partidoActual.Posicion);
            var destino = actualizado.Fases[origen.Orden].Partidos.Single(p => p.Posicion == posicion);
            var destinoId = cruces.Single(c => c.Orden == destino.Orden && c.Partido.Posicion == posicion).Partido.Id;
            await db.Partidos.Where(p => p.Id == destinoId).ExecuteUpdateAsync(p => p
                .SetProperty(x => x.EquipoLocalId, destino.LocalId)
                .SetProperty(x => x.EquipoVisitanteId, destino.VisitanteId), ct);
        }
        await tx.CommitAsync(ct);
    }
}
