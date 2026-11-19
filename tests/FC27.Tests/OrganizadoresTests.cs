using System.Net;
using System.Net.Http.Json;
using FC27.Core;
using FC27.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FC27.Tests;

/// <summary>Login de organizadores con invitación e inscripción de alumnos por código.</summary>
public sealed class OrganizadoresTests : IAsyncLifetime
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"fc27-org-{Guid.NewGuid():N}.db");
    private WebApplicationFactory<Program> _factory = null!;

    private TorneoDbContext Abrir() =>
        new(new DbContextOptionsBuilder<TorneoDbContext>().UseSqlite($"Data Source={_ruta}").Options);

    public async Task InitializeAsync()
    {
        await using var db = Abrir();
        await db.Database.MigrateAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.UseSetting("Torneo:RutaBase", _ruta));
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        SqliteConnectionLimpia();
        foreach (var f in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_ruta) + "*"))
            try { File.Delete(f); } catch (IOException) { }
    }

    private static void SqliteConnectionLimpia() => Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

    [Fact]
    public async Task Invitacion_es_de_un_solo_uso_y_el_login_valida_la_contrasena()
    {
        await using var db = Abrir();
        var auth = new AutenticacionSqlite(db);
        var codigo = await auth.CrearInvitacionAsync(default);

        await auth.RegistrarAsync("Ana", "clave-segura", codigo, default);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            auth.RegistrarAsync("Luis", "clave-segura", codigo, default));

        Assert.NotNull(await auth.EntrarAsync("ANA", "clave-segura", default));
        Assert.Null(await auth.EntrarAsync("ana", "otra-clave", default));
        Assert.Null(await auth.EntrarAsync("nadie", "clave-segura", default));
    }

    [Fact]
    public async Task Alumno_se_inscribe_por_codigo_solo_en_borrador()
    {
        string codigo;
        await using (var db = Abrir())
        {
            var torneo = await new InscripcionesSqlite(db).CrearAsync("Copa", 4, default);
            codigo = torneo.Codigo!;
            Assert.StartsWith("FC27-", codigo);
        }

        var http = _factory.CreateClient();
        var alta = await http.PostAsJsonAsync($"/api/v1/inscripciones/{codigo}", new DatosInscripcion("Pepe", "pepe@x.es", "Madrid"));
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);

        var repetido = await http.PostAsJsonAsync($"/api/v1/inscripciones/{codigo.ToLowerInvariant()}", new DatosInscripcion("Pepe", "pepe@x.es", "Madrid"));
        Assert.Equal(HttpStatusCode.Conflict, repetido.StatusCode);

        var desconocido = await http.PostAsJsonAsync("/api/v1/inscripciones/FC27-ZZZZ", new DatosInscripcion("A", "a@x.es", "B"));
        Assert.Equal(HttpStatusCode.NotFound, desconocido.StatusCode);
    }
}
