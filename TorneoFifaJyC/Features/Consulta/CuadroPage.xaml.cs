using FC27.Core;
using TorneoFifaJyC.Controls;

namespace TorneoFifaJyC.Features.Consulta;

/// <summary>Preferencias por dispositivo sobre MAUI Preferences.</summary>
public sealed class MauiPreferencias : IAlmacenPreferencias
{
    public string? Leer(string clave) => Preferences.Get(clave, null);
    public void Guardar(string clave, string valor) => Preferences.Set(clave, valor);
}

/// <summary>
/// Pantalla de consulta del cuadro para Android. Configura la dirección del
/// servicio, elige un torneo iniciado o finalizado, alterna Rondas y Próximos,
/// muestra la última actualización y, sin servicio, trabaja en modo demo.
/// Consulta al abrir, repite cada pocos segundos y cancela al salir.
/// </summary>
public partial class CuadroPage : ContentPage
{
    private readonly ConfiguracionConsulta _config = new(new MauiPreferencias());
    private CuadroPageViewModel _modelo = null!;
    private CancellationTokenSource? _ciclo;

    public CuadroPage()
    {
        InitializeComponent();
        ReconstruirModelo();
        DireccionEntry.Text = _config.DireccionApi;
    }

    // Crea el modelo de vista con el origen actual (servicio real o demo).
    private void ReconstruirModelo()
    {
        var (consulta, demo) = OrigenConsulta.Crear(_config);
        _modelo = new CuadroPageViewModel(consulta, modoDemo: demo);
        BindingContext = _modelo;
        Aplicar();
    }

    // Las vistas se cambian desde el menú flotante; Ajustes oculta el contenido del torneo.
    private bool _ajustes;

    private void Mostrar(VistaCuadro vista)
    {
        _ajustes = false;
        _modelo.Vista = vista;
        Aplicar();
    }

    private void Aplicar()
    {
        AjustesPanel.IsVisible = _ajustes;
        ContenidoPanel.IsVisible = !_ajustes;
        Menu.Establecer(
        [
            new OpcionMenu("Próximos", () => Mostrar(VistaCuadro.Proximos), !_ajustes && _modelo.Vista == VistaCuadro.Proximos),
            new OpcionMenu("Rondas", () => Mostrar(VistaCuadro.Rondas), !_ajustes && _modelo.Vista == VistaCuadro.Rondas),
            new OpcionMenu("Ajustes", () => { _ajustes = true; Aplicar(); }, _ajustes)
        ]);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ciclo = new CancellationTokenSource();
        _ = IniciarAsync(_ciclo.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _ciclo?.Cancel();
        _ciclo?.Dispose();
        _ciclo = null;
    }

    private async Task IniciarAsync(CancellationToken ct)
    {
        try
        {
            await _modelo.CargarTorneosAsync(ct);
            if (_config.TorneoId is { } guardado)
            {
                var torneo = _modelo.TorneosDisponibles.FirstOrDefault(t => t.Id == guardado);
                if (torneo is not null) await _modelo.SeleccionarTorneoAsync(torneo, ct);
                else await _modelo.ActualizarAsync(ct);
            }
            else
            {
                await _modelo.ActualizarAsync(ct);
            }

            using var temporizador = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while (await temporizador.WaitForNextTickAsync(ct))
                await _modelo.ActualizarAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Salida normal al abandonar la pantalla.
        }
    }

    private async void OnProbarConexion(object? sender, EventArgs e)
    {
        EstadoConexionLabel.Text = "Probando…";
        try
        {
            var resultado = string.IsNullOrWhiteSpace(DireccionEntry.Text)
                ? await ProbadorConexion.ProbarAsync(new ConsultaDemo(), CancellationToken.None)
                : await ProbarDireccionAsync(DireccionEntry.Text);
            EstadoConexionLabel.Text = resultado.Mensaje;
        }
        catch (Exception error)
        {
            EstadoConexionLabel.Text = $"No se pudo probar: {error.Message}";
        }
    }

    private static async Task<ResultadoPrueba> ProbarDireccionAsync(string direccion)
    {
        if (!Uri.TryCreate(direccion.Trim(), UriKind.Absolute, out var uri))
            return ResultadoPrueba.Fallo("la dirección no es válida.");
        return await ProbadorConexion.ProbarAsync(uri, CancellationToken.None);
    }

    private async void OnGuardarDireccion(object? sender, EventArgs e)
    {
        try
        {
            _config.GuardarDireccion(DireccionEntry.Text);
        }
        catch (ArgumentException error)
        {
            EstadoConexionLabel.Text = error.Message;
            return;
        }

        EstadoConexionLabel.Text = _config.ModoDemo ? "Modo demo activado." : "Dirección guardada.";
        ReconstruirModelo();
        _ciclo?.Cancel();
        _ciclo?.Dispose();
        _ciclo = new CancellationTokenSource();
        await IniciarAsync(_ciclo.Token);
    }

    private async void OnTorneoCambiado(object? sender, EventArgs e)
    {
        if (TorneoPicker.SelectedItem is not Torneo torneo) return;
        _config.TorneoId = torneo.Id;
        if (_ciclo is { } ciclo) await _modelo.SeleccionarTorneoAsync(torneo, ciclo.Token);
    }
}
