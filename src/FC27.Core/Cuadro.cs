namespace FC27.Core;

/// <summary>
/// Plan de un cruce dentro de una fase. Las plazas quedan vacías hasta conocer
/// a los jugadores que llegan por el avance de la ronda anterior.
/// </summary>
public sealed record PartidoPlan(int Orden, int Posicion, int? LocalId, int? VisitanteId, int? GanadorId = null);

/// <summary>Plan de una ronda con sus partidos ordenados por posición.</summary>
public sealed record FasePlan(int Orden, string Nombre, IReadOnlyList<PartidoPlan> Partidos);

/// <summary>Plan completo del cuadro generado por el sorteo, sin persistencia.</summary>
public sealed record CuadroPlan(int Capacidad, IReadOnlyList<FasePlan> Fases);

/// <summary>
/// Reglas de dominio del cuadro de eliminación: capacidades válidas, etiquetas de
/// fase, sorteo, posición de destino del ganador y resolución del resultado.
/// No depende de EF Core, HTTP ni MAUI. FC27-05 la conecta con la persistencia.
/// </summary>
public static class Cuadro
{
    /// <summary>Capacidades admitidas. Cada una es una potencia de dos entre 4 y 64.</summary>
    public static readonly IReadOnlyList<int> CapacidadesValidas = new[] { 4, 8, 16, 32, 64 };

    /// <summary>Indica si la capacidad es una de las cinco permitidas.</summary>
    public static bool EsCapacidadValida(int capacidad) => CapacidadesValidas.Contains(capacidad);

    /// <summary>Nombre visible de una fase a partir de su número de partidos.</summary>
    public static string NombreFase(int numeroPartidos) => numeroPartidos switch
    {
        32 => "Treintaidosavos",
        16 => "Dieciseisavos",
        8 => "Octavos",
        4 => "Cuartos",
        2 => "Semifinales",
        1 => "Final",
        _ => throw new ArgumentOutOfRangeException(nameof(numeroPartidos),
            numeroPartidos, "El número de partidos de una fase debe ser una potencia de dos entre 1 y 32."),
    };

    /// <summary>
    /// Posición y plaza que ocupa el ganador en la fase siguiente. El ganador del
    /// partido en posición p pasa al partido techo(p/2). Las posiciones impares
    /// alimentan la plaza local y las pares la visitante.
    /// </summary>
    public static (int PosicionDestino, bool EsLocal) Destino(int posicion)
    {
        if (posicion < 1) throw new ArgumentOutOfRangeException(nameof(posicion), posicion, "La posición es base 1.");
        return ((posicion + 1) / 2, posicion % 2 == 1);
    }

    /// <summary>
    /// Baraja los inscritos con el generador indicado. Separa el azar del sorteo
    /// para que las pruebas usen un orden controlado.
    /// </summary>
    public static IReadOnlyList<int> Barajar(IEnumerable<int> participanteIds, Random azar)
    {
        ArgumentNullException.ThrowIfNull(azar);
        var lista = participanteIds.ToList();
        for (var i = lista.Count - 1; i > 0; i--)
        {
            var j = azar.Next(i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
        return lista;
    }

    /// <summary>
    /// Construye el cuadro a partir de la capacidad y de los inscritos ya ordenados.
    /// Empareja consecutivamente la primera fase y deja vacías las plazas de las
    /// rondas posteriores. Crea log₂(N) fases y N−1 partidos.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Si la capacidad no es válida, si hay inscritos repetidos o si el número de
    /// inscritos distintos no coincide con la capacidad.
    /// </exception>
    public static CuadroPlan Sortear(int capacidad, IReadOnlyList<int> participantesEnOrden)
    {
        ArgumentNullException.ThrowIfNull(participantesEnOrden);
        if (!EsCapacidadValida(capacidad))
            throw new ArgumentException($"Capacidad {capacidad} no permitida. Usa 4, 8, 16, 32 o 64.", nameof(capacidad));
        if (participantesEnOrden.Distinct().Count() != participantesEnOrden.Count)
            throw new ArgumentException("Hay inscritos repetidos en el sorteo.", nameof(participantesEnOrden));
        if (participantesEnOrden.Count != capacidad)
            throw new ArgumentException(
                $"El sorteo necesita exactamente {capacidad} inscritos distintos y recibió {participantesEnOrden.Count}.",
                nameof(participantesEnOrden));

        var fases = new List<FasePlan>();
        var numeroFases = (int)Math.Log2(capacidad);

        for (var indice = 0; indice < numeroFases; indice++)
        {
            var orden = indice + 1;
            var numeroPartidos = capacidad >> orden; // capacidad / 2^orden
            var partidos = new List<PartidoPlan>(numeroPartidos);

            for (var posicion = 1; posicion <= numeroPartidos; posicion++)
            {
                if (indice == 0)
                {
                    // Primera ronda: empareja inscritos consecutivos.
                    var local = participantesEnOrden[(posicion - 1) * 2];
                    var visitante = participantesEnOrden[(posicion - 1) * 2 + 1];
                    partidos.Add(new PartidoPlan(orden, posicion, local, visitante));
                }
                else
                {
                    // Rondas posteriores: ambas plazas vacías hasta el avance.
                    partidos.Add(new PartidoPlan(orden, posicion, null, null));
                }
            }

            fases.Add(new FasePlan(orden, NombreFase(numeroPartidos), partidos));
        }

        return new CuadroPlan(capacidad, fases);
    }

    /// <summary>
    /// Coloca al ganador del partido (faseOrden, posicion) en su plaza de la fase
    /// siguiente y devuelve un cuadro nuevo. El resultado no depende del orden en
    /// que se registren los partidos de una fase, porque cada destino es una plaza
    /// distinta. En la final no hay fase siguiente: solo se fija el ganador.
    ///
    /// Exige que las dos plazas estén asignadas. Un reintento con el mismo ganador
    /// devuelve el cuadro sin cambios; registrar un ganador distinto para un partido
    /// ya confirmado se rechaza.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Si el partido no existe, tiene una plaza vacía o el ganador no juega ese partido.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Si el partido ya tiene confirmado un ganador distinto del indicado.
    /// </exception>
    public static CuadroPlan Avanzar(CuadroPlan cuadro, int faseOrden, int posicion, int ganadorId)
    {
        ArgumentNullException.ThrowIfNull(cuadro);
        var fase = cuadro.Fases.FirstOrDefault(f => f.Orden == faseOrden)
            ?? throw new ArgumentException($"No existe la fase {faseOrden}.", nameof(faseOrden));
        var partido = fase.Partidos.FirstOrDefault(p => p.Posicion == posicion)
            ?? throw new ArgumentException($"No existe el partido {posicion} en la fase {faseOrden}.", nameof(posicion));
        if (partido.LocalId is null || partido.VisitanteId is null)
            throw new ArgumentException("No se puede avanzar un partido con una plaza vacía.", nameof(posicion));
        if (ganadorId != partido.LocalId && ganadorId != partido.VisitanteId)
            throw new ArgumentException("El ganador debe ser una de las dos plazas del partido.", nameof(ganadorId));

        // Idempotencia: el mismo ganador no altera el cuadro. Un ganador distinto
        // para un partido ya confirmado se rechaza y se muestra al gestor.
        if (partido.GanadorId is int confirmado)
            return confirmado == ganadorId
                ? cuadro
                : throw new InvalidOperationException(
                    "El partido ya tiene un ganador confirmado y no puede cambiarse.");

        var fases = cuadro.Fases.ToList();

        // Registra el ganador en el propio partido.
        fases[faseOrden - 1] = ReemplazarPartido(fase, partido with { GanadorId = ganadorId });

        // Si hay fase siguiente, el ganador ocupa su plaza de destino.
        if (faseOrden < cuadro.Fases.Count)
        {
            var (posicionDestino, esLocal) = Destino(posicion);
            var siguiente = fases[faseOrden];
            var destino = siguiente.Partidos.First(p => p.Posicion == posicionDestino);
            var actualizado = esLocal
                ? destino with { LocalId = ganadorId }
                : destino with { VisitanteId = ganadorId };
            fases[faseOrden] = ReemplazarPartido(siguiente, actualizado);
        }

        return cuadro with { Fases = fases };
    }

    /// <summary>Devuelve el campeón cuando la final tiene ganador, o null si aún no existe.</summary>
    public static int? Campeon(CuadroPlan cuadro)
    {
        ArgumentNullException.ThrowIfNull(cuadro);
        var final = cuadro.Fases.LastOrDefault();
        return final?.Partidos.Count == 1 ? final.Partidos[0].GanadorId : null;
    }

    /// <summary>
    /// Valida un marcador y resuelve el ganador del cruce. Exige goles enteros no
    /// negativos y las dos plazas asignadas. Con marcador desigual gana quien marcó
    /// más goles y los penaltis no pueden contradecirlo. Con empate se exige un
    /// ganador por penaltis que sea una de las dos plazas.
    /// </summary>
    /// <returns>El identificador del jugador ganador.</returns>
    /// <exception cref="ArgumentException">Si el resultado incumple alguna regla de RF-04.</exception>
    public static int ResolverGanador(int? localId, int? visitanteId, ResultadoPartido resultado)
    {
        ArgumentNullException.ThrowIfNull(resultado);
        if (localId is null || visitanteId is null)
            throw new ArgumentException("No se puede registrar un resultado con una plaza vacía.");
        if (resultado.GolesLocal < 0 || resultado.GolesVisitante < 0)
            throw new ArgumentException("Los goles no pueden ser negativos.");

        if (resultado.GolesLocal != resultado.GolesVisitante)
        {
            var ganaMarcador = resultado.GolesLocal > resultado.GolesVisitante ? localId.Value : visitanteId.Value;
            if (resultado.GanadorPorPenaltisId is int penaltis && penaltis != ganaMarcador)
                throw new ArgumentException("Con marcador desigual el ganador es quien marcó más goles.");
            return ganaMarcador;
        }

        // Empate: la tanda de penaltis decide y debe nombrar a una de las dos plazas.
        if (resultado.GanadorPorPenaltisId is not int ganadorPenaltis)
            throw new ArgumentException("Un empate exige un ganador por penaltis.");
        if (ganadorPenaltis != localId.Value && ganadorPenaltis != visitanteId.Value)
            throw new ArgumentException("El ganador por penaltis debe ser uno de los dos jugadores del partido.");
        return ganadorPenaltis;
    }

    private static FasePlan ReemplazarPartido(FasePlan fase, PartidoPlan nuevo)
    {
        var partidos = fase.Partidos.ToList();
        var indice = partidos.FindIndex(p => p.Posicion == nuevo.Posicion);
        partidos[indice] = nuevo;
        return fase with { Partidos = partidos };
    }
}
