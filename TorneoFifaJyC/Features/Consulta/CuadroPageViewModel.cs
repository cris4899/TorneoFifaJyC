using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using FC27.Core;

namespace TorneoFifaJyC.Features.Consulta;

/// <summary>
/// Fila de un cruce lista para pintar y leer con el lector de pantalla. Resuelve
/// los textos de plaza, marcador y ganador para que la página no tenga lógica.
/// </summary>
public sealed record PartidoFila(
    int Posicion, string Ronda, string Local, string Visitante, string Marcador, string Resultado, string Descripcion)
{
    private const string PorDeterminar = "Por determinar";

    private static string Jugador(JugadorCuadro? j) => j is null ? PorDeterminar : $"{j.NombreJugador} · {j.EquipoFC27}";

    /// <summary>Proyecta un partido del contrato a una fila accesible. La ronda es opcional.</summary>
    public static PartidoFila Desde(PartidoCuadro p, string ronda = "")
    {
        var local = Jugador(p.Local);
        var visitante = Jugador(p.Visitante);
        var jugado = p.Estado == EstadoPartido.Jugado;
        var marcador = jugado ? $"{p.GolesLocal} - {p.GolesVisitante}" : string.Empty;

        string resultado;
        if (!jugado)
            resultado = "Pendiente";
        else
        {
            var ganador = p.GanadorId == p.Local?.Id ? p.Local : p.GanadorId == p.Visitante?.Id ? p.Visitante : null;
            resultado = ganador is null ? "Jugado" : $"Gana {ganador.NombreJugador}";
        }

        var prefijo = string.IsNullOrEmpty(ronda) ? $"Partido {p.Posicion}" : $"{ronda}, partido {p.Posicion}";
        var descripcion = jugado
            ? $"{prefijo}. {local} {marcador} {visitante}. {resultado}."
            : $"{prefijo}. {local} contra {visitante}. {resultado}.";
        return new PartidoFila(p.Posicion, ronda, local, visitante, marcador, resultado, descripcion);
    }
}

/// <summary>Vista activa del cuadro: la ronda seleccionada o los próximos cruces.</summary>
public enum VistaCuadro { Rondas, Proximos }

/// <summary>
/// Modelo de vista de la pantalla de cuadro Android. Elige torneo, alterna entre
/// Rondas y Próximos, muestra la hora de la última actualización correcta e indica
/// el modo demo. Solo usa tipos base de .NET, así que se prueba sin el proyecto MAUI.
/// </summary>
public sealed class CuadroPageViewModel : INotifyPropertyChanged
{
    private readonly IConsultaTorneos _consulta;
    private readonly Func<DateTimeOffset> _reloj;
    private ModeloConsultaCuadro _modelo = new();
    private int _versionConsulta;
    private RondaCuadro? _rondaSeleccionada;
    private Torneo? _torneoSeleccionado;
    private DateTimeOffset? _ultima;
    private VistaCuadro _vista = VistaCuadro.Rondas;

    public CuadroPageViewModel(IConsultaTorneos consulta, Func<DateTimeOffset>? reloj = null, bool modoDemo = false)
    {
        _consulta = consulta ?? throw new ArgumentNullException(nameof(consulta));
        _reloj = reloj ?? (() => DateTimeOffset.Now);
        ModoDemo = modoDemo;
        ReintentarComando = new ComandoAsync(() => ActualizarAsync());
        VerRondasComando = new Comando(() => Vista = VistaCuadro.Rondas);
        VerProximosComando = new Comando(() => Vista = VistaCuadro.Proximos);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand ReintentarComando { get; }
    public ICommand VerRondasComando { get; }
    public ICommand VerProximosComando { get; }

    /// <summary>La pantalla sirve datos ficticios sin un servicio configurado.</summary>
    public bool ModoDemo { get; }
    public string ModoDemoTexto => ModoDemo ? "Modo demo · datos ficticios" : string.Empty;

    /// <summary>Torneos iniciados o finalizados que ofrece el servicio.</summary>
    public IReadOnlyList<Torneo> TorneosDisponibles { get; private set; } = Array.Empty<Torneo>();

    /// <summary>Torneo elegido. El cambio lo aplica <see cref="SeleccionarTorneoAsync"/>.</summary>
    public Torneo? TorneoSeleccionado
    {
        get => _torneoSeleccionado;
        private set
        {
            if (_torneoSeleccionado?.Id != value?.Id)
            {
                // El cuadro y la hora pertenecen exclusivamente al torneo seleccionado.
                _versionConsulta++;
                _modelo = new();
                _rondaSeleccionada = null;
                _ultima = null;
            }
            _torneoSeleccionado = value;
            Notificar();
        }
    }

    public bool HayTorneos => TorneosDisponibles.Count > 0;

    /// <summary>Vista activa: Rondas o Próximos.</summary>
    public VistaCuadro Vista
    {
        get => _vista;
        set
        {
            if (_vista == value) return;
            _vista = value;
            Notificar();
            Notificar(nameof(MostrarRondas));
            Notificar(nameof(MostrarProximos));
            Notificar(nameof(SinProximos));
        }
    }

    public bool MostrarRondas => MostrarContenido && _vista == VistaCuadro.Rondas;
    public bool MostrarProximos => MostrarContenido && _vista == VistaCuadro.Proximos;

    public EstadoConsulta Estado => _modelo.Estado;
    public string Nombre => _modelo.Vista?.Nombre ?? string.Empty;

    public string EstadoTorneoTexto => _modelo.Vista?.Estado switch
    {
        EstadoTorneo.EnCurso => "En curso",
        EstadoTorneo.Finalizado => "Finalizado",
        _ => string.Empty,
    };

    public IReadOnlyList<RondaCuadro> Rondas => _modelo.Vista?.Rondas ?? Array.Empty<RondaCuadro>();

    /// <summary>Ronda elegida en el selector. Al cambiarla se actualizan los cruces visibles.</summary>
    public RondaCuadro? RondaSeleccionada
    {
        get => _rondaSeleccionada;
        set
        {
            if (ReferenceEquals(_rondaSeleccionada, value)) return;
            _rondaSeleccionada = value;
            Notificar();
            Notificar(nameof(PartidosVisibles));
        }
    }

    /// <summary>Cruces de la ronda seleccionada como filas accesibles.</summary>
    public IReadOnlyList<PartidoFila> PartidosVisibles =>
        (_rondaSeleccionada?.Partidos ?? Array.Empty<PartidoCuadro>())
        .Select(p => PartidoFila.Desde(p, _rondaSeleccionada!.Nombre)).ToList();

    /// <summary>Próximos cruces jugables de todo el cuadro, con su ronda.</summary>
    public IReadOnlyList<PartidoFila> ProximosFilas =>
        (_modelo.Vista?.Rondas ?? Array.Empty<RondaCuadro>())
        .SelectMany(r => r.Partidos
            .Where(p => p.Estado == EstadoPartido.Pendiente && p.Local is not null && p.Visitante is not null)
            .Select(p => PartidoFila.Desde(p, r.Nombre)))
        .ToList();

    public bool HayProximos => ProximosFilas.Count > 0;
    public bool SinProximos => MostrarProximos && !HayProximos;

    public JugadorCuadro? Campeon => _modelo.Vista?.Campeon;
    public bool HayCampeon => Campeon is not null;
    public string CampeonTexto => Campeon is null ? string.Empty : $"Campeón: {Campeon.NombreJugador} · {Campeon.EquipoFC27}";

    public bool MostrarCargando => Estado == EstadoConsulta.Cargando;
    public bool MostrarContenido => Estado is EstadoConsulta.Contenido or EstadoConsulta.Desactualizado;
    public bool MostrarVacio => Estado == EstadoConsulta.Vacio;
    public bool MostrarError => Estado == EstadoConsulta.Error;
    public bool EstaDesactualizado => Estado == EstadoConsulta.Desactualizado;
    public bool MostrarReintentar => Estado is EstadoConsulta.Error or EstadoConsulta.Desactualizado;

    public string MensajeEstado => Estado switch
    {
        EstadoConsulta.Vacio => "No hay cuadro disponible todavía.",
        EstadoConsulta.Error => "No se pudo cargar el cuadro. Reintenta.",
        EstadoConsulta.Desactualizado => "Sin conexión. Se muestra la última versión.",
        _ => string.Empty,
    };

    /// <summary>Hora de la última consulta correcta, o aviso si aún no la hubo.</summary>
    public DateTimeOffset? UltimaActualizacion => _ultima;
    public string UltimaActualizacionTexto => _ultima is { } t
        ? $"Actualizado a las {t.ToLocalTime():HH:mm:ss}"
        : "Sin actualizar";

    /// <summary>Carga la lista de torneos y, si no hay uno elegido válido, toma el primero.</summary>
    public async Task CargarTorneosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            TorneosDisponibles = await _consulta.ListarAsync(cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { TorneosDisponibles = Array.Empty<Torneo>(); }

        // Reapunta siempre a la instancia de la nueva lista con el mismo id para que
        // el Picker conserve el torneo seleccionado tras recargar (mismo id = sin reinicio).
        TorneoSeleccionado = TorneosDisponibles.FirstOrDefault(t => t.Id == _torneoSeleccionado?.Id)
            ?? TorneosDisponibles.FirstOrDefault();

        Notificar(nameof(TorneosDisponibles));
        Notificar(nameof(HayTorneos));
        Notificar(nameof(TorneoSeleccionado));
    }

    /// <summary>Elige un torneo y recarga su cuadro.</summary>
    public async Task SeleccionarTorneoAsync(Torneo? torneo, CancellationToken cancellationToken = default)
    {
        TorneoSeleccionado = torneo;
        await ActualizarAsync(cancellationToken);
    }

    /// <summary>
    /// Consulta el cuadro del torneo elegido: muestra la carga, aplica la respuesta
    /// o marca el fallo, y anota la hora si el servicio respondió.
    /// </summary>
    public async Task ActualizarAsync(CancellationToken cancellationToken = default)
    {
        var version = ++_versionConsulta;
        _modelo.Cargando();
        NotificarTodo();
        try
        {
            var cuadro = _torneoSeleccionado is { } torneo
                ? await _consulta.ObtenerCuadroAsync(torneo.Id, cancellationToken)
                : null;
            cancellationToken.ThrowIfCancellationRequested();
            if (version != _versionConsulta) return;
            _modelo.Recibir(cuadro);
            _ultima = _reloj(); // El servicio respondió: cuenta como última actualización correcta.
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            if (version != _versionConsulta || cancellationToken.IsCancellationRequested) return;
            _modelo.Fallar();
        }

        _rondaSeleccionada = Rondas.FirstOrDefault(r => r.Orden == _rondaSeleccionada?.Orden) ?? Rondas.FirstOrDefault();
        NotificarTodo();
    }

    private void NotificarTodo()
    {
        foreach (var propiedad in new[]
        {
            nameof(Estado), nameof(Nombre), nameof(EstadoTorneoTexto), nameof(Rondas),
            nameof(RondaSeleccionada), nameof(PartidosVisibles), nameof(ProximosFilas), nameof(HayProximos),
            nameof(SinProximos),
            nameof(Campeon), nameof(HayCampeon), nameof(CampeonTexto), nameof(MostrarCargando),
            nameof(MostrarContenido), nameof(MostrarRondas), nameof(MostrarProximos), nameof(MostrarVacio),
            nameof(MostrarError), nameof(EstaDesactualizado), nameof(MostrarReintentar), nameof(MensajeEstado),
            nameof(UltimaActualizacion), nameof(UltimaActualizacionTexto),
        })
            Notificar(propiedad);
    }

    private void Notificar([CallerMemberName] string? propiedad = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
}

/// <summary>ICommand mínimo para acciones asíncronas sin dependencia de MAUI.</summary>
public sealed class ComandoAsync : ICommand
{
    private readonly Func<Task> _ejecutar;
    private bool _enCurso;

    public ComandoAsync(Func<Task> ejecutar) => _ejecutar = ejecutar ?? throw new ArgumentNullException(nameof(ejecutar));

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_enCurso;

    public async void Execute(object? parameter)
    {
        if (_enCurso) return;
        _enCurso = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try { await _ejecutar(); }
        finally
        {
            _enCurso = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

/// <summary>ICommand síncrono mínimo sin dependencia de MAUI.</summary>
public sealed class Comando : ICommand
{
    private readonly Action _ejecutar;

    public Comando(Action ejecutar) => _ejecutar = ejecutar ?? throw new ArgumentNullException(nameof(ejecutar));

    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _ejecutar();
}
