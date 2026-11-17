namespace FC27.Core;

/// <summary>Totales de un jugador en los partidos con resultado confirmado.</summary>
public sealed record EstadisticaJugador(JugadorCuadro Jugador, int Jugados, int GolesFavor, int GolesContra)
{
    public int Diferencia => GolesFavor - GolesContra;
}

/// <summary>Estadísticas derivadas del cuadro. Los penaltis no cuentan como goles.</summary>
public static class Estadisticas
{
    /// <summary>Una fila por jugador con algún partido jugado, de más a menos goles a favor.</summary>
    public static IReadOnlyList<EstadisticaJugador> PorJugador(CuadroTorneo cuadro)
    {
        var totales = new Dictionary<int, EstadisticaJugador>();
        void Sumar(JugadorCuadro? jugador, int favor, int contra)
        {
            if (jugador is null) return;
            var previo = totales.GetValueOrDefault(jugador.Id) ?? new EstadisticaJugador(jugador, 0, 0, 0);
            totales[jugador.Id] = previo with
            {
                Jugados = previo.Jugados + 1,
                GolesFavor = previo.GolesFavor + favor,
                GolesContra = previo.GolesContra + contra
            };
        }

        foreach (var partido in cuadro.Rondas.SelectMany(r => r.Partidos).Where(p => p.Estado == EstadoPartido.Jugado))
        {
            Sumar(partido.Local, partido.GolesLocal, partido.GolesVisitante);
            Sumar(partido.Visitante, partido.GolesVisitante, partido.GolesLocal);
        }

        return totales.Values
            .OrderByDescending(e => e.GolesFavor)
            .ThenByDescending(e => e.Diferencia)
            .ThenBy(e => e.Jugador.NombreJugador, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    /// <summary>Jugadores empatados con más goles a favor; vacío si nadie ha marcado.</summary>
    public static IReadOnlyList<EstadisticaJugador> MaximosGoleadores(IReadOnlyList<EstadisticaJugador> estadisticas)
    {
        var maximo = estadisticas.Count == 0 ? 0 : estadisticas.Max(e => e.GolesFavor);
        return maximo == 0 ? [] : estadisticas.Where(e => e.GolesFavor == maximo).ToList();
    }
}
