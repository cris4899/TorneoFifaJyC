namespace FC27.Core;

/// <summary>Estado del ciclo de vida del torneo.</summary>
public enum EstadoTorneo { Borrador, EnCurso, Finalizado }

/// <summary>Indica si el partido tiene un resultado confirmado.</summary>
public enum EstadoPartido { Pendiente, Jugado }

/// <summary>Torneo con una capacidad prevista entre 4 y 64 plazas.</summary>
public sealed record Torneo(int Id, string Nombre, int NumeroParticipantes, EstadoTorneo Estado)
{
    /// <summary>Organizador que creó el torneo. Nulo en torneos anteriores al login.</summary>
    public int? OrganizadorId { get; init; }

    /// <summary>Código corto con el que los alumnos se inscriben mientras el torneo está en borrador.</summary>
    public string? Codigo { get; init; }
}

/// <summary>Cuenta de organizador. La contraseña se guarda solo como hash PBKDF2 con sal.</summary>
public sealed record Organizador(int Id, string Usuario, byte[] Hash, byte[] Sal);

/// <summary>Código de invitación de un solo uso para dar de alta a un organizador.</summary>
public sealed record Invitacion(int Id, string Codigo, int? UsadaPorId);

/// <summary>Inscripción de un jugador en un torneo y con un equipo FC27.</summary>
public sealed record Participante(int Id, int TorneoId, string NombreJugador, string Email, string EquipoFC27);

/// <summary>Ronda del torneo numerada desde uno, en orden de juego.</summary>
public sealed record Fase(int Id, int TorneoId, int Orden);

/// <summary>Cruce del cuadro. Las plazas quedan vacías hasta conocer a los jugadores.</summary>
public sealed record Partido(
    int Id, int FaseId, int Posicion, int? EquipoLocalId, int? EquipoVisitanteId,
    int GolesLocal, int GolesVisitante, EstadoPartido EstadoPartido, int? GanadorId);
