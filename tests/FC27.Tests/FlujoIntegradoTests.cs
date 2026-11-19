using System.Text;
using FC27.Core;
using FC27.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FC27.Tests;

/// <summary>
/// Flujo integrado sobre SQLite: importación CSV (un duplicado rechaza el lote),
/// corrección de marcador que actualiza el siguiente cruce y exportación que
/// refleja el resultado corregido.
/// </summary>
public sealed class FlujoIntegradoTests : IAsyncLifetime
{
    private SqliteConnection _conexion = null!;
    private DbContextOptions<TorneoDbContext> _opciones = null!;

    public async Task InitializeAsync()
    {
        _conexion = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await _conexion.OpenAsync();
        _opciones = new DbContextOptionsBuilder<TorneoDbContext>().UseSqlite(_conexion).Options;
        await using var db = new TorneoDbContext(_opciones);
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _conexion.DisposeAsync().AsTask();

    private TorneoDbContext Db() => new(_opciones);

    private static Stream Csv(string texto) => new MemoryStream(Encoding.UTF8.GetBytes(texto));

    [Fact]
    public async Task UnaFilaDuplicadaRechazaElLoteCompleto()
    {
        await using var db = Db();
        var torneo = await new InscripcionesSqlite(db).CrearAsync("Copa", 4, default);

        // El tercer y cuarto correo coinciden: el lote debe rechazarse entero.
        var inscritos = TorneoCsv.LeerInscripciones(Csv(
            "nombre,correo,equipo\n" +
            "Uno,uno@ejemplo.test,A\n" +
            "Dos,dos@ejemplo.test,B\n" +
            "Tres,tres@ejemplo.test,C\n" +
            "Cuatro,tres@ejemplo.test,D\n"));

        var altas = new InscripcionesSqlite(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => altas.ImportarAsync(torneo.Id, inscritos, default));

        var guardados = await db.Participantes.CountAsync(p => p.TorneoId == torneo.Id);
        Assert.Equal(0, guardados);
    }

    [Fact]
    public async Task CorregirElGanadorActualizaLaFinalYElCsvLoRefleja()
    {
        int torneoId;
        await using (var db = Db())
        {
            var torneo = await new InscripcionesSqlite(db).CrearAsync("Copa DAM", 4, default);
            torneoId = torneo.Id;
            var inscritos = TorneoCsv.LeerInscripciones(Csv(
                "nombre,correo,equipo\n" +
                "Uno,uno@ejemplo.test,Equipo 1\n" +
                "Dos,dos@ejemplo.test,Equipo 2\n" +
                "Tres,tres@ejemplo.test,Equipo 3\n" +
                "Cuatro,cuatro@ejemplo.test,Equipo 4\n"));
            await new InscripcionesSqlite(db).ImportarAsync(torneo.Id, inscritos, default);
            await new CuadroSqlite(db).IniciarAsync(torneo.Id, default);
        }

        // Lee el cuadro sorteado para conocer las plazas reales de cada semifinal.
        CuadroTorneo cuadro;
        await using (var db = Db())
            cuadro = (await new ConsultaTorneosSqlite(db).ObtenerCuadroAsync(torneoId, default))!;
        var semifinales = cuadro.Rondas[0].Partidos;
        var sf1 = semifinales[0];
        var sf2 = semifinales[1];

        // Gana el local en ambas semifinales: la final queda sf1.Local vs sf2.Local.
        await using (var db = Db())
        {
            var reg = new CuadroSqlite(db);
            await reg.RegistrarResultadoAsync(torneoId, sf1.Id, new ResultadoPartido(2, 1, null), default);
            await reg.RegistrarResultadoAsync(torneoId, sf2.Id, new ResultadoPartido(2, 1, null), default);
        }

        // Corrige la semifinal 1: ahora gana el visitante (1-2), con la final pendiente.
        await using (var db = Db())
            await new CuadroSqlite(db).CorregirResultadoAsync(torneoId, sf1.Id, new ResultadoPartido(1, 2, null), default);

        CuadroTorneo corregido;
        await using (var db = Db())
            corregido = (await new ConsultaTorneosSqlite(db).ObtenerCuadroAsync(torneoId, default))!;

        var sf1c = corregido.Rondas[0].Partidos.Single(p => p.Id == sf1.Id);
        var final = corregido.Rondas[1].Partidos.Single();

        // El ganador de la semifinal 1 pasó a ser el visitante y la final lo recoge.
        Assert.Equal(sf1.Visitante!.Id, sf1c.GanadorId);
        Assert.Equal(sf1.Visitante!.Id, final.Local!.Id);
        Assert.Equal((1, 2), (sf1c.GolesLocal, sf1c.GolesVisitante));

        // El CSV exportado refleja el marcador corregido y el nuevo ganador.
        var csv = TorneoCsv.ExportarCuadro(corregido);
        Assert.Contains($";\"1\";\"2\";\"Jugado\";\"{sf1.Visitante!.NombreJugador}\"", csv);
        Assert.Contains(sf1.Visitante!.NombreJugador, csv);
    }
}
