using FC27.Core;
using FC27.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FC27.Tests;

public sealed class PersistenciaTests
{
    [Fact]
    public async Task LasAltasRespetanCapacidadCorreoGlobalYTorneo()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await conexion.OpenAsync();
        await using var db = new TorneoDbContext(new DbContextOptionsBuilder<TorneoDbContext>()
            .UseSqlite(conexion).Options);
        await db.Database.MigrateAsync();
        var altas = new InscripcionesSqlite(db);
        var ct = CancellationToken.None;
        var copa = await altas.CrearAsync("Copa", 4, ct);
        var otro = await altas.CrearAsync("Otro torneo", 8, ct);
        var alex = await altas.InscribirAsync(copa.Id, new("Alex", " A@ejemplo.test ", "Equipo A"), ct);

        Assert.Equal("a@ejemplo.test", alex.Email);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            altas.InscribirAsync(otro.Id, new("Alex", "a@ejemplo.test", "Equipo B"), ct));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => altas.RetirarInscripcionAsync(otro.Id, alex.Id, ct));
        for (var plaza = 2; plaza <= 4; plaza++)
            await altas.InscribirAsync(copa.Id, new($"Jugador {plaza}", $"{plaza}@ejemplo.test", "Equipo B"), ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            altas.InscribirAsync(copa.Id, new("Extra", "extra@ejemplo.test", "Equipo C"), ct));

        var consulta = new ConsultaTorneosSqlite(db);
        Assert.Empty(await consulta.ListarAsync(ct));
        Assert.Null(await consulta.ObtenerCuadroAsync(copa.Id, ct));
        Assert.Equal(4, await db.Participantes.CountAsync(ct));
    }

    [Fact]
    public async Task ElCuadroOrdenaLosPartidosYDevuelveElCampeon()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await conexion.OpenAsync();
        await using var db = new TorneoDbContext(new DbContextOptionsBuilder<TorneoDbContext>()
            .UseSqlite(conexion).Options);
        await db.Database.MigrateAsync();
        db.Add(new Torneo(1, "Copa", 4, EstadoTorneo.Finalizado));
        db.AddRange(
            new Participante(1, 1, "Alex", "alex@ejemplo.test", "A"),
            new Participante(2, 1, "Dani", "dani@ejemplo.test", "B"),
            new Participante(3, 1, "Eli", "eli@ejemplo.test", "C"),
            new Participante(4, 1, "Sam", "sam@ejemplo.test", "D"));
        db.AddRange(new Fase(1, 1, 1), new Fase(2, 1, 2));
        db.AddRange(
            new Partido(2, 1, 2, 3, 4, 1, 0, EstadoPartido.Jugado, 3),
            new Partido(1, 1, 1, 1, 2, 2, 1, EstadoPartido.Jugado, 1),
            new Partido(3, 2, 1, 1, 3, 1, 1, EstadoPartido.Jugado, 1));
        await db.SaveChangesAsync();

        var consulta = new ConsultaTorneosSqlite(db);
        var cuadro = Assert.IsType<CuadroTorneo>(await consulta.ObtenerCuadroAsync(1, CancellationToken.None));
        Assert.Equal(new[] { 1, 2 }, cuadro.Rondas[0].Partidos.Select(p => p.Id));
        Assert.Equal("Final", cuadro.Rondas[1].Nombre);
        Assert.Equal(1, cuadro.Campeon?.Id);
    }
}
