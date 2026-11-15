using System.Text;
using FC27.Core;
using FC27.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FC27.Tests;

public sealed class TorneoCsvTests
{
    [Theory]
    [InlineData(",")]
    [InlineData(";")]
    public void LeeComillasYSeparadores(string separador)
    {
        using var archivo = new MemoryStream(Encoding.UTF8.GetBytes(
            $"nombre{separador}correo{separador}equipo\r\n\"Díaz, Ana; Jr.\"{separador}ana@example.com{separador}\"Club \"\"A\"\"\""));
        var fila = Assert.Single(TorneoCsv.LeerInscripciones(archivo));
        Assert.Equal("Díaz, Ana; Jr.", fila.NombreJugador);
        Assert.Equal("Club \"A\"", fila.EquipoFC27);
    }

    [Fact]
    public async Task ImportacionRevalidaDuplicadosAntesDeEscribirElLote()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
        await conexion.OpenAsync();
        await using var db = new TorneoDbContext(new DbContextOptionsBuilder<TorneoDbContext>().UseSqlite(conexion).Options);
        await db.Database.MigrateAsync();
        var altas = new InscripcionesSqlite(db);
        var ct = CancellationToken.None;
        var torneo = await altas.CrearAsync("Copa", 4, ct);
        DatosInscripcion[] lote = [new("Ana", "ANA@example.com", "A"), new("Beto", "beto@example.com", "B")];
        await altas.ValidarImportacionAsync(torneo.Id, lote, ct);
        await altas.InscribirAsync(torneo.Id, new("Otra Ana", "ana@example.com", "C"), ct);
        await Assert.ThrowsAsync<InvalidOperationException>(() => altas.ImportarAsync(torneo.Id, lote, ct));
        Assert.Equal(1, await db.Participantes.CountAsync(ct));
        await altas.ImportarAsync(torneo.Id, [lote[1]], ct);
        Assert.Equal(2, await db.Participantes.CountAsync(ct));
    }

    [Fact]
    public void ExportacionConservaResultadosYCitaTextoDeUsuario()
    {
        var ana = new JugadorCuadro(1, "=Ana", "Club \"A\"");
        var beto = new JugadorCuadro(2, "Beto", "Club; B");
        var ronda = new RondaCuadro(1, "Semifinales",
        [
            new(1, 1, ana, beto, 2, 1, EstadoPartido.Jugado, ana.Id),
            new(2, 2, null, null, 0, 0, EstadoPartido.Pendiente, null)
        ]);
        var csv = TorneoCsv.ExportarCuadro(new(new(1, "Copa", 4, EstadoTorneo.EnCurso), [ronda], null));
        Assert.Contains("\"'=Ana\";\"Club \"\"A\"\"\";\"Beto\";\"Club; B\";\"2\";\"1\";\"Jugado\";\"'=Ana\"", csv);
        Assert.Contains("\"2\";;;;;;;\"Pendiente\";", csv);
        Assert.DoesNotContain("correo", csv);
    }
}