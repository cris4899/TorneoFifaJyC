using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FC27.Core;

namespace TorneoFifaJyC.Features.Consulta;

/// <summary>
/// Cliente de consulta Android contra el servicio de lectura de FC27.Api. Usa el
/// contrato acordado: lista de torneos y cuadro por id. Un torneo no disponible
/// (404) se traduce en null. No depende de MAUI, así que se prueba con un
/// <see cref="HttpMessageHandler"/> simulado.
/// </summary>
public sealed class ConsultaTorneosHttp(HttpClient http) : IConsultaTorneos
{
    // Mismo formato que el servidor: camelCase y enums como texto.
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IReadOnlyList<Torneo>> ListarAsync(CancellationToken cancellationToken)
    {
        var torneos = await http.GetFromJsonAsync<List<Torneo>>("api/v1/torneos", Opciones, cancellationToken)
            .ConfigureAwait(false);
        return torneos ?? [];
    }

    public async Task<CuadroTorneo?> ObtenerCuadroAsync(int torneoId, CancellationToken cancellationToken)
    {
        using var respuesta = await http.GetAsync($"api/v1/torneos/{torneoId}/cuadro", cancellationToken)
            .ConfigureAwait(false);
        if (respuesta.StatusCode == HttpStatusCode.NotFound)
            return null;
        respuesta.EnsureSuccessStatusCode();
        return await respuesta.Content.ReadFromJsonAsync<CuadroTorneo>(Opciones, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Composición del origen de datos de la pantalla Android. Con una dirección base
/// configurada usa el servicio real; sin ella, los datos ficticios de FC27-04.
/// </summary>
public static class ConsultaAndroid
{
    /// <summary>
    /// Devuelve el cargador del cuadro para la página. Si <paramref name="baseAddress"/>
    /// es null, sirve datos ficticios para poder probar la pantalla sin red.
    /// </summary>
    public static Func<CancellationToken, Task<CuadroTorneo?>> CrearCargador(int torneoId, Uri? baseAddress)
    {
        if (baseAddress is null)
            return DatosFicticiosConsulta.CargarAsync;

        var http = new HttpClient { BaseAddress = baseAddress };
        var cliente = new ConsultaTorneosHttp(http);
        return ct => cliente.ObtenerCuadroAsync(torneoId, ct);
    }
}
