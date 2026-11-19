namespace FC27.Core;

/// <summary>Datos requeridos para dar de alta o actualizar una inscripción.</summary>
public sealed record DatosInscripcion(string NombreJugador, string Email, string EquipoFC27);

/// <summary>Marcador confirmado. En empate, indica qué inscrito ganó por penaltis.</summary>
public sealed record ResultadoPartido(int GolesLocal, int GolesVisitante, int? GanadorPorPenaltisId);

/// <summary>Operaciones de gestión. Iniciar y registrar un resultado requieren escrituras atómicas.</summary>
public interface IGestionTorneo
{
    Task<Torneo> CrearAsync(string nombre, int capacidad, CancellationToken cancellationToken);
    Task<Participante> InscribirAsync(int torneoId, DatosInscripcion datos, CancellationToken cancellationToken);
    Task ActualizarInscripcionAsync(int torneoId, int participanteId, DatosInscripcion datos, CancellationToken cancellationToken);
    Task RetirarInscripcionAsync(int torneoId, int participanteId, CancellationToken cancellationToken);
    Task IniciarAsync(int torneoId, CancellationToken cancellationToken);
    Task RegistrarResultadoAsync(int torneoId, int partidoId, ResultadoPartido resultado, CancellationToken cancellationToken);
}

/// <summary>Consulta para el alumno. Solo expone torneos iniciados y finalizados.</summary>
public interface IConsultaTorneos
{
    Task<IReadOnlyList<Torneo>> ListarAsync(CancellationToken cancellationToken);

    /// <summary>Devuelve el cuadro ordenado o null si el torneo no está disponible.</summary>
    Task<CuadroTorneo?> ObtenerCuadroAsync(int torneoId, CancellationToken cancellationToken);
}

/// <summary>Identidad visible en el cuadro. La proyección excluye el correo.</summary>
public sealed record JugadorCuadro(int Id, string NombreJugador, string EquipoFC27);

/// <summary>Marcador y plazas de un cruce. GanadorId se asigna al confirmar el resultado.</summary>
public sealed record PartidoCuadro(
    int Id, int Posicion, JugadorCuadro? Local, JugadorCuadro? Visitante,
    int GolesLocal, int GolesVisitante, EstadoPartido Estado, int? GanadorId);

/// <summary>Partidos de una ronda, ordenados por su posición.</summary>
public sealed record RondaCuadro(int Orden, string Nombre, IReadOnlyList<PartidoCuadro> Partidos);

/// <summary>Instantánea de lectura con el campeón cuando finaliza el torneo.</summary>
public sealed record CuadroTorneo(Torneo Torneo, IReadOnlyList<RondaCuadro> Rondas, JugadorCuadro? Campeon);
