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
    int Posicion, string Local, string Visitante, string Marcador, string Resultado, string Descripcion)
{
    private const string PorDeterminar = "Por determinar";

    private static string Jugador(JugadorCuadro? j) => j is null ? PorDeterminar : $"{j.NombreJugador} · {j.EquipoFC27}";

    /// <summary>Proyecta un partido del contrato a una fila accesible.</summary>
    public static PartidoFila Desde(PartidoCuadro p)
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

        var descripcion = jugado
            ? $"Partido {p.Posicion}. {local} {marcador} {visitante}. {resultado}."
            : $"Partido {p.Posicion}. {local} contra {visitante}. {resultado}.";
        return new PartidoFila(p.Posicion, local, visitante, marcador, resultado, descripcion);
    }
}

/// <summary>
/// Modelo de vista de la pantalla de cuadro Android. Enlaza <see cref="ModeloConsultaCuadro"/>
/// con la interfaz: selector de ronda, filas de cruce, campeón y reintento. Solo
/// usa tipos base de .NET, así que se prueba sin el proyecto MAUI.
/// </summary>
public sealed class CuadroPageViewModel : INotifyPropertyChanged
{
    private readonly ModeloConsultaCuadro _modelo = new();
    private readonly Func<CancellationToken, Task<CuadroTorneo?>> _cargar;
    private RondaCuadro? _rondaSeleccionada;

    /// <param name="cargar">Fuente de datos del cuadro. En FC27-04 son datos ficticios; FC27-06 la sustituye por el servicio real.</param>
    public CuadroPageViewModel(Func<CancellationToken, Task<CuadroTorneo?>> cargar)
    {
        _cargar = cargar ?? throw new ArgumentNullException(nameof(cargar));
        ReintentarComando = new ComandoAsync(() => ActualizarAsync());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Comando del botón «Reintentar».</summary>
    public ICommand ReintentarComando { get; }

    /// <summary>Estado actual de la consulta.</summary>
    public EstadoConsulta Estado => _modelo.Estado;

    /// <summary>Nombre del torneo, o vacío mientras no hay vista.</summary>
    public string Nombre => _modelo.Vista?.Nombre ?? string.Empty;

    /// <summary>Texto del estado del torneo para la cabecera.</summary>
    public string EstadoTorneoTexto => _modelo.Vista?.Estado switch
    {
        EstadoTorneo.EnCurso => "En curso",
        EstadoTorneo.Finalizado => "Finalizado",
        _ => string.Empty,
    };

    /// <summary>Rondas disponibles para el selector.</summary>
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
        (_rondaSeleccionada?.Partidos ?? Array.Empty<PartidoCuadro>()).Select(PartidoFila.Desde).ToList();

    /// <summary>Campeón cuando el torneo ha finalizado.</summary>
    public JugadorCuadro? Campeon => _modelo.Vista?.Campeon;

    public bool HayCampeon => Campeon is not null;
    public string CampeonTexto => Campeon is null ? string.Empty : $"Campeón: {Campeon.NombreJugador} · {Campeon.EquipoFC27}";

    public bool MostrarCargando => Estado == EstadoConsulta.Cargando;
    public bool MostrarContenido => Estado is EstadoConsulta.Contenido or EstadoConsulta.Desactualizado;
    public bool MostrarVacio => Estado == EstadoConsulta.Vacio;
    public bool MostrarError => Estado == EstadoConsulta.Error;
    public bool EstaDesactualizado => Estado == EstadoConsulta.Desactualizado;

    /// <summary>El botón «Reintentar» aparece tras un error o con datos desactualizados.</summary>
    public bool MostrarReintentar => Estado is EstadoConsulta.Error or EstadoConsulta.Desactualizado;

    /// <summary>Mensaje de estado para el aviso de vacío, error o desactualizado.</summary>
    public string MensajeEstado => Estado switch
    {
        EstadoConsulta.Vacio => "No hay cuadro disponible todavía.",
        EstadoConsulta.Error => "No se pudo cargar el cuadro. Reintenta.",
        EstadoConsulta.Desactualizado => "Sin conexión. Se muestra la última versión.",
        _ => string.Empty,
    };

    /// <summary>
    /// Lanza una consulta: muestra la carga, aplica la respuesta o marca el fallo y
    /// deja seleccionada la primera ronda cuando llega contenido nuevo.
    /// </summary>
    public async Task ActualizarAsync(CancellationToken cancellationToken = default)
    {
        _modelo.Cargando();
        NotificarTodo();
        try
        {
            var cuadro = await _cargar(cancellationToken).ConfigureAwait(false);
            _modelo.Recibir(cuadro);
        }
        catch (Exception)
        {
            // Cualquier fallo de la fuente se trata como pérdida de conexión.
            _modelo.Fallar();
        }

        // Conserva la ronda elegida si sigue existiendo; si no, elige la primera.
        _rondaSeleccionada = Rondas.FirstOrDefault(r => r.Orden == _rondaSeleccionada?.Orden) ?? Rondas.FirstOrDefault();
        NotificarTodo();
    }

    private void NotificarTodo()
    {
        foreach (var propiedad in new[]
        {
            nameof(Estado), nameof(Nombre), nameof(EstadoTorneoTexto), nameof(Rondas),
            nameof(RondaSeleccionada), nameof(PartidosVisibles), nameof(Campeon), nameof(HayCampeon),
            nameof(CampeonTexto), nameof(MostrarCargando), nameof(MostrarContenido), nameof(MostrarVacio),
            nameof(MostrarError), nameof(EstaDesactualizado), nameof(MostrarReintentar), nameof(MensajeEstado),
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
        try { await _ejecutar().ConfigureAwait(false); }
        finally
        {
            _enCurso = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
