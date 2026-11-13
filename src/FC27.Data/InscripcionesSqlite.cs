using FC27.Core;
using Microsoft.EntityFrameworkCore;

namespace FC27.Data;

/// <summary>Altas de torneos y cambios de inscripción durante el borrador.</summary>
public sealed class InscripcionesSqlite(TorneoDbContext db)
{
    /// <summary>Crea un torneo en borrador con una de las capacidades admitidas.</summary>
    public async Task<Torneo> CrearAsync(string nombre, int capacidad, CancellationToken ct)
    {
        if (capacidad is not (4 or 8 or 16 or 32 or 64))
            throw new ArgumentOutOfRangeException(nameof(capacidad));

        var torneo = new Torneo(0, Requerido(nombre, nameof(nombre)), capacidad, EstadoTorneo.Borrador);
        db.Torneos.Add(torneo);
        try
        {
            await db.SaveChangesAsync(ct);
            return torneo;
        }
        finally
        {
            db.Entry(torneo).State = EntityState.Detached;
        }
    }

    /// <summary>Añade una inscripción si el borrador todavía tiene plazas.</summary>
    public async Task<Participante> InscribirAsync(int torneoId, DatosInscripcion datos, CancellationToken ct)
    {
        var (nombre, correo, equipo) = Normalizar(datos);
        // SQLite adquiere el bloqueo de escritura al iniciar la transacción.
        // El recuento de plazas y el alta se ejecutan con ese mismo bloqueo.
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var torneo = await ObtenerBorradorAsync(torneoId, ct);
        if (await Inscritos(torneoId).CountAsync(ct) >= torneo.NumeroParticipantes)
            throw new InvalidOperationException("El torneo ya está completo.");

        var inscrito = new Participante(0, torneoId, nombre, correo, equipo);
        db.Participantes.Add(inscrito);
        try
        {
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return inscrito;
        }
        finally
        {
            db.Entry(inscrito).State = EntityState.Detached;
        }
    }

    /// <summary>Actualiza los datos de una inscripción del torneo indicado.</summary>
    public async Task ActualizarInscripcionAsync(int torneoId, int participanteId, DatosInscripcion datos, CancellationToken ct)
    {
        var (nombre, correo, equipo) = Normalizar(datos);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await ObtenerBorradorAsync(torneoId, ct);
        var cambios = await Inscritos(torneoId).Where(p => p.Id == participanteId).ExecuteUpdateAsync(
            setters => setters.SetProperty(p => p.NombreJugador, nombre)
                .SetProperty(p => p.Email, correo)
                .SetProperty(p => p.EquipoFC27, equipo), ct);
        ComprobarInscripcion(cambios);
        await tx.CommitAsync(ct);
    }

    /// <summary>Retira una inscripción del torneo indicado mientras siga en borrador.</summary>
    public async Task RetirarInscripcionAsync(int torneoId, int participanteId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await ObtenerBorradorAsync(torneoId, ct);
        var cambios = await Inscritos(torneoId).Where(p => p.Id == participanteId).ExecuteDeleteAsync(ct);
        ComprobarInscripcion(cambios);
        await tx.CommitAsync(ct);
    }

    private IQueryable<Participante> Inscritos(int torneoId) =>
        db.Participantes.Where(p => p.TorneoId == torneoId);

    private async Task<Torneo> ObtenerBorradorAsync(int torneoId, CancellationToken ct)
    {
        var torneo = await db.Torneos.AsNoTracking().SingleOrDefaultAsync(t => t.Id == torneoId, ct)
            ?? throw new KeyNotFoundException("El torneo no existe.");
        if (torneo.Estado != EstadoTorneo.Borrador)
            throw new InvalidOperationException("Solo se pueden editar inscripciones en borrador.");
        return torneo;
    }

    private static void ComprobarInscripcion(int cambios)
    {
        if (cambios == 0)
            throw new KeyNotFoundException("La inscripción no pertenece a este torneo.");
    }

    private static DatosInscripcion Normalizar(DatosInscripcion datos) =>
        new(Requerido(datos.NombreJugador, nameof(datos.NombreJugador)),
            Requerido(datos.Email, nameof(datos.Email)).ToLowerInvariant(),
            Requerido(datos.EquipoFC27, nameof(datos.EquipoFC27)));

    private static string Requerido(string valor, string campo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(valor, campo);
        return valor.Trim();
    }
}
