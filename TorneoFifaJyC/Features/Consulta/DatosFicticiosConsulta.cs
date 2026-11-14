using FC27.Core;

namespace TorneoFifaJyC.Features.Consulta;

/// <summary>
/// Datos ficticios para la pantalla Android mientras no existe el servicio real
/// (FC27-06). Reproduce `tests/FC27.Tests/cuadro-ejemplo.json`: una «Copa DAM» de
/// cuatro jugadores con una semifinal jugada, otra pendiente y la final abierta.
/// </summary>
public static class DatosFicticiosConsulta
{
    private static readonly JugadorCuadro Alex = new(1, "Alex", "Equipo A");
    private static readonly JugadorCuadro Dani = new(2, "Dani", "Equipo B");
    private static readonly JugadorCuadro Sam = new(3, "Sam", "Equipo C");
    private static readonly JugadorCuadro Eli = new(4, "Eli", "Equipo D");

    /// <summary>Construye el cuadro de ejemplo en memoria.</summary>
    public static CuadroTorneo Cuadro()
    {
        var semifinales = new RondaCuadro(1, "Semifinales", new[]
        {
            new PartidoCuadro(1, 1, Alex, Dani, 2, 1, EstadoPartido.Jugado, Alex.Id),
            new PartidoCuadro(2, 2, Sam, Eli, 0, 0, EstadoPartido.Pendiente, null),
        });
        var final = new RondaCuadro(2, "Final", new[]
        {
            new PartidoCuadro(3, 1, Alex, null, 0, 0, EstadoPartido.Pendiente, null),
        });
        var torneo = new Torneo(1, "Copa DAM", 4, EstadoTorneo.EnCurso);
        return new CuadroTorneo(torneo, new[] { semifinales, final }, Campeon: null);
    }

    /// <summary>Fuente de datos para el modelo de vista. Simula una consulta correcta.</summary>
    public static Task<CuadroTorneo?> CargarAsync(CancellationToken cancellationToken) =>
        Task.FromResult<CuadroTorneo?>(Cuadro());
}
