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

var app = builder.Build();

var api = app.MapGroup("/api/v1");

// Lista de torneos iniciados y finalizados. Lista vacía válida.
api.MapGet("/torneos", async (IConsultaTorneos consulta, CancellationToken ct) =>
    await consulta.ListarAsync(ct));

// Cuadro ordenado por rondas o 404 si el torneo no está disponible (inexistente o borrador).
api.MapGet("/torneos/{id:int}/cuadro", async (int id, IConsultaTorneos consulta, CancellationToken ct) =>
    await consulta.ObtenerCuadroAsync(id, ct) is { } cuadro ? Results.Ok(cuadro) : Results.NotFound());

app.Run();

// Punto de entrada visible para las pruebas de integración (WebApplicationFactory).
public partial class Program;
