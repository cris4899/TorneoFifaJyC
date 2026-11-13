using FC27.Core;
using Microsoft.EntityFrameworkCore;

namespace FC27.Data;

/// <summary>Mapeo relacional de los torneos y el cuadro en SQLite.</summary>
public sealed class TorneoDbContext(DbContextOptions<TorneoDbContext> options) : DbContext(options)
{
    public DbSet<Torneo> Torneos => Set<Torneo>();
    public DbSet<Participante> Participantes => Set<Participante>();
    public DbSet<Fase> Fases => Set<Fase>();
    public DbSet<Partido> Partidos => Set<Partido>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var torneos = model.Entity<Torneo>();
        torneos.HasIndex(t => t.Nombre).IsUnique();
        torneos.Property(t => t.Estado).HasConversion<string>();
        torneos.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Torneo_Capacidad", "NumeroParticipantes IN (4, 8, 16, 32, 64)");
            t.HasCheckConstraint("CK_Torneo_Estado", "Estado IN ('Borrador', 'EnCurso', 'Finalizado')");
        });

        var inscritos = model.Entity<Participante>();
        inscritos.HasIndex(p => p.Email).IsUnique();
        inscritos.HasOne<Torneo>().WithMany().HasForeignKey(p => p.TorneoId).OnDelete(DeleteBehavior.Restrict);

        var fases = model.Entity<Fase>();
        fases.HasIndex(f => new { f.TorneoId, f.Orden }).IsUnique();
        fases.HasOne<Torneo>().WithMany().HasForeignKey(f => f.TorneoId).OnDelete(DeleteBehavior.Restrict);
        fases.ToTable(t => t.HasCheckConstraint("CK_Fase_Orden", "Orden >= 1"));

        var partidos = model.Entity<Partido>();
        partidos.HasIndex(p => new { p.FaseId, p.Posicion }).IsUnique();
        partidos.Property(p => p.EstadoPartido).HasConversion<string>();
        partidos.Property(p => p.GolesLocal).HasDefaultValue(0);
        partidos.Property(p => p.GolesVisitante).HasDefaultValue(0);
        partidos.HasOne<Fase>().WithMany().HasForeignKey(p => p.FaseId).OnDelete(DeleteBehavior.Restrict);
        partidos.HasOne<Participante>().WithMany().HasForeignKey(p => p.EquipoLocalId).OnDelete(DeleteBehavior.Restrict);
        partidos.HasOne<Participante>().WithMany().HasForeignKey(p => p.EquipoVisitanteId).OnDelete(DeleteBehavior.Restrict);
        partidos.HasOne<Participante>().WithMany().HasForeignKey(p => p.GanadorId).OnDelete(DeleteBehavior.Restrict);
        partidos.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Partido_Posicion", "Posicion >= 1");
            t.HasCheckConstraint("CK_Partido_Goles", "GolesLocal >= 0 AND GolesVisitante >= 0");
            t.HasCheckConstraint("CK_Partido_Plazas", "EquipoLocalId <> EquipoVisitanteId");
            t.HasCheckConstraint("CK_Partido_Resultado", """
                (EstadoPartido = 'Pendiente' AND GolesLocal = 0 AND GolesVisitante = 0 AND GanadorId IS NULL)
                OR
                (EstadoPartido = 'Jugado' AND EquipoLocalId IS NOT NULL AND EquipoVisitanteId IS NOT NULL
                  AND GanadorId IS NOT NULL AND GanadorId IN (EquipoLocalId, EquipoVisitanteId)
                  AND (GolesLocal = GolesVisitante
                    OR (GolesLocal > GolesVisitante AND GanadorId = EquipoLocalId)
                    OR (GolesVisitante > GolesLocal AND GanadorId = EquipoVisitanteId)))
                """);
        });
    }
}
