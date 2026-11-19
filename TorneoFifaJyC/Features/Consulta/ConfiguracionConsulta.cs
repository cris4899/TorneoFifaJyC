using FC27.Core;

namespace TorneoFifaJyC.Features.Consulta;

/// <summary>Almacén de preferencias por dispositivo. En Android lo cubre MAUI Preferences.</summary>
public interface IAlmacenPreferencias
{
    string? Leer(string clave);
    void Guardar(string clave, string valor);
}

/// <summary>
/// Configuración de la consulta Android, persistida por dispositivo: la dirección
/// del servicio de lectura y el torneo elegido. Sin dirección válida, la pantalla
/// funciona en modo demo con datos ficticios.
/// </summary>
public sealed class ConfiguracionConsulta(IAlmacenPreferencias almacen)
{
    private const string ClaveDireccion = "consulta.direccionApi";
    private const string ClaveTorneo = "consulta.torneoId";

    /// <summary>Dirección base del servicio, tal y como la escribió la persona.</summary>
    public string? DireccionApi
    {
        get => almacen.Leer(ClaveDireccion);
        private set => almacen.Guardar(ClaveDireccion, value ?? string.Empty);
    }

    /// <summary>Último torneo elegido, o null si no hay ninguno guardado.</summary>
    public int? TorneoId
    {
        get => int.TryParse(almacen.Leer(ClaveTorneo), out var id) ? id : null;
        set => almacen.Guardar(ClaveTorneo, value?.ToString() ?? string.Empty);
    }

    /// <summary>Sin una dirección válida, la pantalla usa datos ficticios.</summary>
    public bool ModoDemo => !TryDireccion(out _);

    /// <summary>Dirección base válida, o null en modo demo.</summary>
    public Uri? BaseAddress => TryDireccion(out var uri) ? uri : null;

    /// <summary>
    /// Valida y guarda la dirección del servicio. Acepta una URL http(s) absoluta;
    /// un valor vacío deja la pantalla en modo demo.
    /// </summary>
    /// <exception cref="ArgumentException">Si la dirección no es una URL http(s) absoluta.</exception>
    public void GuardarDireccion(string? direccion)
    {
        if (!string.IsNullOrWhiteSpace(direccion) && !EsValida(direccion, out _))
            throw new ArgumentException("Escribe una dirección http(s) válida, por ejemplo http://192.168.1.50:5080.");
        DireccionApi = direccion?.Trim();
    }

    private bool TryDireccion(out Uri uri) => EsValida(DireccionApi, out uri);

    private static bool EsValida(string? direccion, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(direccion))
            return false;
        return Uri.TryCreate(direccion.Trim(), UriKind.Absolute, out uri!)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>Resultado de probar la conexión con el servicio de lectura.</summary>
public sealed record ResultadoPrueba(bool Exito, int Torneos, string Mensaje)
{
    public static ResultadoPrueba Ok(int torneos) =>
        new(true, torneos, torneos == 0
            ? "Conectado. No hay torneos disponibles todavía."
            : $"Conectado. {torneos} torneo(s) disponibles.");

    public static ResultadoPrueba Fallo(string mensaje) => new(false, 0, $"Sin conexión: {mensaje}");
}

/// <summary>Comprueba que el servicio de lectura responde el listado de torneos.</summary>
public static class ProbadorConexion
{
    /// <summary>Prueba la conexión con un cliente ya construido.</summary>
    public static async Task<ResultadoPrueba> ProbarAsync(IConsultaTorneos consulta, CancellationToken ct)
    {
        try
        {
            var torneos = await consulta.ListarAsync(ct).ConfigureAwait(false);
            return ResultadoPrueba.Ok(torneos.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception error)
        {
            return ResultadoPrueba.Fallo(error.Message);
        }
    }

    /// <summary>Prueba la conexión contra una dirección concreta.</summary>
    public static async Task<ResultadoPrueba> ProbarAsync(Uri baseAddress, CancellationToken ct)
    {
        using var http = new HttpClient { BaseAddress = baseAddress, Timeout = TimeSpan.FromSeconds(5) };
        return await ProbarAsync(new ConsultaTorneosHttp(http), ct).ConfigureAwait(false);
    }
}

/// <summary>
/// Compone el origen de datos de la pantalla según la configuración: el servicio
/// real cuando hay dirección válida, o los datos ficticios en modo demo.
/// </summary>
public static class OrigenConsulta
{
    /// <summary>Devuelve el cliente de consulta y si la pantalla está en modo demo.</summary>
    public static (IConsultaTorneos Consulta, bool ModoDemo) Crear(ConfiguracionConsulta config)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (config.BaseAddress is { } direccion)
        {
            var http = new HttpClient { BaseAddress = direccion };
            return (new ConsultaTorneosHttp(http), false);
        }
        return (new ConsultaDemo(), true);
    }
}
