namespace TorneoFifaJyC.Features.Consulta;

/// <summary>
/// Configuración de la consulta Android. La dirección base se fija en el
/// dispositivo con la IP del Windows del gestor tras validar la red (FC27-06).
/// Mientras sea null, la pantalla usa datos ficticios.
/// </summary>
public static class ConsultaConfig
{
    /// <summary>Id del torneo que muestra la demo.</summary>
    public static int TorneoId { get; set; } = 1;

    /// <summary>Dirección base del servicio de lectura, o null para datos ficticios.</summary>
    public static Uri? BaseAddress { get; set; }

    /// <summary>Intervalo de actualización mientras la pantalla está visible.</summary>
    public static TimeSpan Intervalo { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Pantalla de consulta del cuadro para Android. Consulta al abrir, repite cada
/// pocos segundos mientras está visible, mantiene una sola consulta en curso y
/// cancela al salir. FC27-06 conecta el servicio real; FC27-04 dejó la vista.
/// </summary>
public partial class CuadroPage : ContentPage
{
    private readonly CuadroPageViewModel _modelo;
    private CancellationTokenSource? _ciclo;

    public CuadroPage()
        : this(new CuadroPageViewModel(
            ConsultaAndroid.CrearCargador(ConsultaConfig.TorneoId, ConsultaConfig.BaseAddress)))
    {
    }

    public CuadroPage(CuadroPageViewModel modelo)
    {
        InitializeComponent();
        _modelo = modelo;
        BindingContext = _modelo;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ciclo = new CancellationTokenSource();
        _ = ActualizarEnBucleAsync(_ciclo.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Cancela la consulta en curso y detiene el ciclo al salir de la pantalla.
        _ciclo?.Cancel();
        _ciclo?.Dispose();
        _ciclo = null;
    }

    private async Task ActualizarEnBucleAsync(CancellationToken ct)
    {
        try
        {
            await _modelo.ActualizarAsync(ct);
            using var temporizador = new PeriodicTimer(ConsultaConfig.Intervalo);
            while (await temporizador.WaitForNextTickAsync(ct))
                await _modelo.ActualizarAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Salida normal al abandonar la pantalla.
        }
    }
}
