using FC27.Core;
using FC27.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FC27.Tests;

public sealed class ResultadoSqliteTests
{
    [Fact]
    public async Task ConfirmaElCampeonYConservaElResultadoAnteReintentos()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await conexion.OpenAsync();
        await using var db = new TorneoDbContext(new DbContextOptionsBuilder<TorneoDbContext>().UseSqlite(conexion).Options);
        await db.Database.MigrateAsync();
        var ct = CancellationToken.None;
        var inscripciones = new InscripcionesSqlite(db);
        var torneo = await inscripciones.CrearAsync("Copa", 4, ct);
        for (var id = 1; id <= 4; id++)
            await inscripciones.InscribirAsync(torneo.Id, new($"Jugador {id}", $"{id}@example.com", "Equipo"), ct);

        var cuadro = new CuadroSqlite(db);
        await cuadro.IniciarAsync(torneo.Id, ct);
        var consulta = new ConsultaTorneosSqlite(db);
        var inicial = Assert.IsType<CuadroTorneo>(await consulta.ObtenerCuadroAsync(torneo.Id, ct));
        var semifinales = inicial.Rondas[0].Partidos;
        foreach (var semifinal in semifinales.Reverse())
            await cuadro.RegistrarResultadoAsync(torneo.Id, semifinal.Id, new(1, 0, null), ct);

        var antes = Assert.IsType<CuadroTorneo>(await consulta.ObtenerCuadroAsync(torneo.Id, ct));
        var final = Assert.Single(antes.Rondas[1].Partidos);
        Assert.Equal(semifinales[0].Local?.Id, final.Local?.Id);
        Assert.Equal(semifinales[1].Local?.Id, final.Visitante?.Id);
        var resultado = new ResultadoPartido(0, 0, final.Visitante!.Id);
        await cuadro.RegistrarResultadoAsync(torneo.Id, final.Id, resultado, ct);
        await cuadro.RegistrarResultadoAsync(torneo.Id, final.Id, resultado, ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cuadro.RegistrarResultadoAsync(torneo.Id, final.Id, new(2, 0, null), ct));

        var terminado = Assert.IsType<CuadroTorneo>(await consulta.ObtenerCuadroAsync(torneo.Id, ct));
        Assert.Equal(EstadoTorneo.Finalizado, terminado.Torneo.Estado);
        Assert.Equal(final.Visitante.Id, terminado.Campeon?.Id);
    }
}
