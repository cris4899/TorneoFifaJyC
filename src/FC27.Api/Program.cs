using System.Text.Json.Serialization;
using FC27.Core;
using FC27.Data;
using Microsoft.EntityFrameworkCore;

// Servicio de solo lectura para la consulta Android (SDD · contrato de lectura).
// Abre el mismo archivo SQLite que gestiona Windows y publica las proyecciones
// sin correos. Si la API cae, Windows sigue gestionando el torneo.
var builder = WebApplication.CreateBuilder(args);

// El cuadro usa enums como texto ("EnCurso") en todo el contrato.
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Ruta de la base de datos del gestor. En la demo apunta al archivo real de Windows.
var rutaBase = builder.Configuration["Torneo:RutaBase"] ?? "torneo.db";
builder.Services.AddDbContext<TorneoDbContext>(opciones => opciones.UseSqlite($"Data Source={rutaBase}"));
builder.Services.AddScoped<IConsultaTorneos, ConsultaTorneosSqlite>();
builder.Services.AddScoped<InscripcionesSqlite>();

var app = builder.Build();

var api = app.MapGroup("/api/v1");

// Lista de torneos iniciados y finalizados. Lista vacía válida.
api.MapGet("/torneos", async (IConsultaTorneos consulta, CancellationToken ct) =>
    await consulta.ListarAsync(ct));

// Cuadro ordenado por rondas o 404 si el torneo no está disponible (inexistente o borrador).
api.MapGet("/torneos/{id:int}/cuadro", async (int id, IConsultaTorneos consulta, CancellationToken ct) =>
    await consulta.ObtenerCuadroAsync(id, ct) is { } cuadro ? Results.Ok(cuadro) : Results.NotFound());

// Única escritura abierta: el alumno se apunta con el código del torneo mientras es borrador.
api.MapGet("/inscripciones/{codigo}", async (string codigo, InscripcionesSqlite altas, CancellationToken ct) =>
    await altas.BuscarPorCodigoAsync(codigo, ct) is { } t
        ? Results.Ok(new { t.Nombre, Plazas = t.NumeroParticipantes })
        : Results.NotFound());

api.MapPost("/inscripciones/{codigo}", async (string codigo, DatosInscripcion datos, InscripcionesSqlite altas, CancellationToken ct) =>
{
    if (await altas.BuscarPorCodigoAsync(codigo, ct) is not { } t) return Results.NotFound();
    try
    {
        var p = await altas.InscribirAsync(t.Id, datos, ct);
        return Results.Created($"/api/v1/inscripciones/{t.Codigo}", new { p.Id });
    }
    catch (Exception e) when (e is ArgumentException or InvalidOperationException or DbUpdateException)
    {
        return Results.Conflict(new { error = e.Message });
    }
});

app.Run();

// Punto de entrada visible para las pruebas de integración (WebApplicationFactory).
public partial class Program;
