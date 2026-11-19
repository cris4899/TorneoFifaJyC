using FC27.Core;

namespace TorneoFifaJyC.Features.Consulta;

/// <summary>
/// Estado de la pantalla de consulta Android. Separa la carga en curso del
/// contenido, la lista vacía, el error sin datos y la vista desactualizada que
/// conserva la última consulta válida.
/// </summary>
public enum EstadoConsulta
{
    /// <summary>Consulta en curso sin resultado todavía.</summary>
    Cargando,
    /// <summary>Hay datos para mostrar.</summary>
    Contenido,
    /// <summary>La consulta respondió sin torneos o sin cruces.</summary>
    Vacio,
    /// <summary>Falló la consulta y no hay datos previos que mostrar.</summary>
    Error,
    /// <summary>Falló la consulta pero se conserva la última vista válida.</summary>
    Desactualizado,
}

/// <summary>
/// Vista del cuadro lista para pintar: título, rondas para el selector, los
/// próximos cruces jugables y el campeón cuando existe. No excluye nada que el
/// contrato ya traiga; la proyección del servicio es la que oculta los correos.
/// </summary>
public sealed record CuadroVista(
    string Nombre,
    EstadoTorneo Estado,
    IReadOnlyList<RondaCuadro> Rondas,
    IReadOnlyList<PartidoCuadro> Proximos,
    JugadorCuadro? Campeon)
{
    /// <summary>
    /// Construye la vista a partir del contrato. Un cruce es «próximo» si está
    /// pendiente y tiene las dos plazas asignadas (RF-06).
    /// </summary>
    public static CuadroVista Desde(CuadroTorneo cuadro)
    {
        ArgumentNullException.ThrowIfNull(cuadro);
        var proximos = cuadro.Rondas
            .SelectMany(r => r.Partidos)
            .Where(p => p.Estado == EstadoPartido.Pendiente && p.Local is not null && p.Visitante is not null)
            .ToList();
        return new CuadroVista(cuadro.Torneo.Nombre, cuadro.Torneo.Estado, cuadro.Rondas, proximos, cuadro.Campeon);
    }

    /// <summary>Indica si el cuadro no tiene ningún partido que mostrar.</summary>
    public bool SinCruces => Rondas.All(r => r.Partidos.Count == 0);
}

/// <summary>
/// Modelo de la pantalla de cuadro Android. Mantiene la última vista válida en
/// memoria durante la sesión y aplica las transiciones de carga, contenido,
/// vacío, error y recuperación que prueba FC27-04.
/// </summary>
public sealed class ModeloConsultaCuadro
{
    /// <summary>Estado actual de la pantalla.</summary>
    public EstadoConsulta Estado { get; private set; } = EstadoConsulta.Cargando;

    /// <summary>Última vista válida recibida, o null si aún no hubo ninguna.</summary>
    public CuadroVista? Vista { get; private set; }

    /// <summary>Marca el inicio de una consulta. Conserva la vista previa mientras llega la respuesta.</summary>
    public void Cargando() => Estado = EstadoConsulta.Cargando;

    /// <summary>
    /// Aplica la respuesta del servicio. Un cuadro null (torneo no disponible) o
    /// sin cruces deja la pantalla en vacío; en otro caso guarda la nueva vista.
    /// </summary>
    public void Recibir(CuadroTorneo? cuadro)
    {
        if (cuadro is null)
        {
            Vista = null;
            Estado = EstadoConsulta.Vacio;
            return;
        }

        var vista = CuadroVista.Desde(cuadro);
        Vista = vista;
        Estado = vista.SinCruces ? EstadoConsulta.Vacio : EstadoConsulta.Contenido;
    }

    /// <summary>
    /// Registra un fallo de red. Con una vista previa, la conserva y la marca como
    /// desactualizada; sin datos previos, muestra el error.
    /// </summary>
    public void Fallar() =>
        Estado = Vista is null ? EstadoConsulta.Error : EstadoConsulta.Desactualizado;
}

/// <summary>Estado del listado de torneos del alumno. Distingue la lista vacía del contenido.</summary>
public static class ListadoConsulta
{
    /// <summary>Devuelve el estado del listado según haya o no torneos visibles.</summary>
    public static EstadoConsulta Estado(IReadOnlyList<Torneo> torneos)
    {
        ArgumentNullException.ThrowIfNull(torneos);
        return torneos.Count == 0 ? EstadoConsulta.Vacio : EstadoConsulta.Contenido;
    }
}
