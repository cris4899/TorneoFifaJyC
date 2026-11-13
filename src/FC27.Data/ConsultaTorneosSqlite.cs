using FC27.Core;
using Microsoft.EntityFrameworkCore;

namespace FC27.Data;

/// <summary>Lee el cuadro confirmado y proyecta únicamente los datos visibles para el alumno.</summary>
public sealed class ConsultaTorneosSqlite(TorneoDbContext db) : IConsultaTorneos
{
    private IQueryable<Torneo> Visibles =>
        db.Torneos.AsNoTracking().Where(t => t.Estado != EstadoTorneo.Borrador);

    public async Task<IReadOnlyList<Torneo>> ListarAsync(CancellationToken cancellationToken) =>
        await Visibles.OrderBy(t => t.Nombre).ThenBy(t => t.Id).ToListAsync(cancellationToken);

    public async Task<CuadroTorneo?> ObtenerCuadroAsync(int torneoId, CancellationToken cancellationToken)
    {
        // Una transacción de lectura mantiene todas las consultas en la misma instantánea.
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var conexion = (Microsoft.Data.Sqlite.SqliteConnection)db.Database.GetDbConnection();
            await using var tx = conexion.BeginTransaction(deferred: true);
            await using var vinculada = await db.Database.UseTransactionAsync(tx, cancellationToken);
            var torneo = await Visibles.SingleOrDefaultAsync(t => t.Id == torneoId, cancellationToken);
            if (torneo is null)
                return null;

            var jugadores = await db.Participantes.AsNoTracking().Where(p => p.TorneoId == torneoId)
                .Select(p => new JugadorCuadro(p.Id, p.NombreJugador, p.EquipoFC27))
                .ToDictionaryAsync(p => p.Id, cancellationToken);
            var partidos = await (
                from fase in db.Fases.AsNoTracking()
                join partido in db.Partidos.AsNoTracking() on fase.Id equals partido.FaseId
                where fase.TorneoId == torneoId
                orderby fase.Orden, partido.Posicion
                select new { fase.Orden, Partido = partido }).ToListAsync(cancellationToken);

            JugadorCuadro? Jugador(int? id) => id is int valor ? jugadores[valor] : null;
            var rondas = partidos.GroupBy(p => p.Orden).Select(r =>
                new RondaCuadro(r.Key, NombreRonda(r.Count()), r.Select(p =>
                    new PartidoCuadro(p.Partido.Id, p.Partido.Posicion, Jugador(p.Partido.EquipoLocalId),
                        Jugador(p.Partido.EquipoVisitanteId), p.Partido.GolesLocal, p.Partido.GolesVisitante,
                        p.Partido.EstadoPartido, p.Partido.GanadorId)).ToArray())).ToArray();
            var campeon = torneo.Estado == EstadoTorneo.Finalizado
                ? Jugador(rondas.Last().Partidos.Single().GanadorId)
                : null;
            await tx.CommitAsync(cancellationToken);
            return new CuadroTorneo(torneo, rondas, campeon);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static string NombreRonda(int partidos) => partidos switch
    {
        32 => "Treintaidosavos",
        16 => "Dieciseisavos",
        8 => "Octavos",
        4 => "Cuartos",
        2 => "Semifinales",
        1 => "Final",
        _ => throw new InvalidOperationException("La fase tiene un número de partidos inesperado.")
    };
}
