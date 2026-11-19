using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FC27.Core;
using FC27.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FC27.Tests;

/// <summary>
/// Pruebas de integración del servicio de lectura FC27.Api sobre una base SQLite
/// temporal sembrada con FC27.Data: listado visible, cuadro ordenado, torneo no
/// disponible y exclusión de correos.
/// </summary>
public sealed class ApiConsultaTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"fc27-api-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _factory = null!;
    private int _iniciado;
    private int _borrador;

    public async Task InitializeAsync()
    {
        var opciones = new DbContextOptionsBuilder<TorneoDbContext>().UseSqlite($"Data Source={_ruta}").Options;
        await using (var db = new TorneoDbContext(opciones))
        {
            await db.Database.MigrateAsync();
            var altas = new InscripcionesSqlite(db);
            var cuadro = new CuadroSqlite(db);

            var copa = await altas.CrearAsync("Copa DAM", 4, default);
            for (var i = 1; i <= 4; i++)
                await altas.InscribirAsync(copa.Id, new($"Jugador {i}", $"j{i}@ejemplo.test", $"Equipo {i}"), default);
            await cuadro.IniciarAsync(copa.Id, default);
            _iniciado = copa.Id;

            var borrador = await altas.CrearAsync("En preparación", 4, default);
            _borrador = borrador.Id;
        }

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("Torneo:RutaBase", _ruta));
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        try { File.Delete(_ruta); } catch { /* temporal */ }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ElListadoSoloMuestraTorneosIniciadosOFinalizados()
    {
        var cliente = _factory.CreateClient();

        var torneos = await cliente.GetFromJsonAsync<List<Torneo>>("api/v1/torneos", Opciones);

        var torneo = Assert.Single(torneos!);
        Assert.Equal(_iniciado, torneo.Id);
        Assert.Equal(EstadoTorneo.EnCurso, torneo.Estado);
        Assert.DoesNotContain(torneos!, t => t.Id == _borrador);
    }

    [Fact]
    public async Task ElCuadroDevuelveLasRondasOrdenadasYSinCorreos()
    {
        var cliente = _factory.CreateClient();

        var cuerpo = await cliente.GetStringAsync($"api/v1/torneos/{_iniciado}/cuadro");
        var cuadro = JsonSerializer.Deserialize<CuadroTorneo>(cuerpo, Opciones)!;

        Assert.Equal(new[] { "Semifinales", "Final" }, cuadro.Rondas.Select(r => r.Nombre));
        Assert.Equal(new[] { 1, 2 }, cuadro.Rondas.Select(r => r.Orden));
        Assert.DoesNotContain("email", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnTorneoEnBorradorNoEstaDisponible()
    {
        var cliente = _factory.CreateClient();
        var respuesta = await cliente.GetAsync($"api/v1/torneos/{_borrador}/cuadro");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task UnTorneoInexistenteDevuelve404()
    {
        var cliente = _factory.CreateClient();
        var respuesta = await cliente.GetAsync("api/v1/torneos/9999/cuadro");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
