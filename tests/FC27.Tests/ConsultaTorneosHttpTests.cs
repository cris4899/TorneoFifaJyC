using System.Net;
using System.Text;
using FC27.Core;
using TorneoFifaJyC.Features.Consulta;
using Xunit;

namespace FC27.Tests;

/// <summary>
/// Pruebas del cliente HTTP de consulta Android con un handler simulado: mapea el
/// contrato, traduce 404 a «no disponible» y respeta la cancelación.
/// </summary>
public sealed class ConsultaTorneosHttpTests
{
    // Handler simulado que responde con un cuerpo y un estado fijos.
    private sealed class HandlerFalso(HttpStatusCode estado, string cuerpo) : HttpMessageHandler
    {
        public string? UltimaRuta { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UltimaRuta = request.RequestUri?.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(estado)
            {
                Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
            });
        }
    }

    private static HttpClient Cliente(HttpMessageHandler handler) =>
        new(handler) { BaseAddress = new Uri("http://192.168.1.50:5080") };

    private const string CuadroJson = """
    {
      "torneo": { "id": 1, "nombre": "Copa DAM", "numeroParticipantes": 4, "estado": "EnCurso" },
      "rondas": [
        { "orden": 1, "nombre": "Semifinales", "partidos": [
          { "id": 1, "posicion": 1,
            "local": { "id": 1, "nombreJugador": "Alex", "equipoFC27": "Equipo A" },
            "visitante": { "id": 2, "nombreJugador": "Dani", "equipoFC27": "Equipo B" },
            "golesLocal": 2, "golesVisitante": 1, "estado": "Jugado", "ganadorId": 1 } ] }
      ],
      "campeon": null
    }
    """;

    [Fact]
    public async Task ObtenerCuadroDeserializaElContrato()
    {
        var handler = new HandlerFalso(HttpStatusCode.OK, CuadroJson);
        var consulta = new ConsultaTorneosHttp(Cliente(handler));

        var cuadro = await consulta.ObtenerCuadroAsync(1, CancellationToken.None);

        Assert.NotNull(cuadro);
        Assert.Equal("Copa DAM", cuadro!.Torneo.Nombre);
        Assert.Equal("Alex", cuadro.Rondas[0].Partidos[0].Local!.NombreJugador);
        Assert.Equal("api/v1/torneos/1/cuadro", handler.UltimaRuta?.TrimStart('/'));
    }

    [Fact]
    public async Task UnTorneoNoDisponibleSeTraduceEnNull()
    {
        var consulta = new ConsultaTorneosHttp(Cliente(new HandlerFalso(HttpStatusCode.NotFound, "")));
        Assert.Null(await consulta.ObtenerCuadroAsync(7, CancellationToken.None));
    }

    [Fact]
    public async Task ListarDevuelveLosTorneos()
    {
        const string json = """[{ "id": 1, "nombre": "Copa DAM", "numeroParticipantes": 4, "estado": "EnCurso" }]""";
        var consulta = new ConsultaTorneosHttp(Cliente(new HandlerFalso(HttpStatusCode.OK, json)));

        var torneos = await consulta.ListarAsync(CancellationToken.None);

        Assert.Equal(EstadoTorneo.EnCurso, Assert.Single(torneos).Estado);
    }

    [Fact]
    public async Task LaConsultaRespetaLaCancelacion()
    {
        var consulta = new ConsultaTorneosHttp(Cliente(new HandlerFalso(HttpStatusCode.OK, CuadroJson)));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => consulta.ObtenerCuadroAsync(1, cts.Token));
    }
}
