using FC27.Core;
using Xunit;

namespace FC27.Tests;

public sealed class EstadisticasTests
{
    private static readonly JugadorCuadro Ana = new(1, "Ana", "Real");
    private static readonly JugadorCuadro Bea = new(2, "Bea", "Milan");
    private static readonly JugadorCuadro Carla = new(3, "Carla", "Ajax");
    private static readonly JugadorCuadro Dani = new(4, "Dani", "Porto");

    private static CuadroTorneo Cuadro(params RondaCuadro[] rondas) =>
        new(new Torneo(1, "Copa", 4, EstadoTorneo.EnCurso), rondas, null);

    [Fact]
    public void SumaGolesSoloDePartidosJugadosYNoCuentaPenaltis()
    {
        var cuadro = Cuadro(
            new RondaCuadro(1, "Semifinales", [
                new PartidoCuadro(1, 1, Carla, Bea, 1, 1, EstadoPartido.Jugado, Carla.Id),
                new PartidoCuadro(2, 2, Dani, Ana, 0, 3, EstadoPartido.Jugado, Ana.Id)]),
            new RondaCuadro(2, "Final", [
                new PartidoCuadro(3, 1, Carla, Ana, 0, 0, EstadoPartido.Pendiente, null)]));

        var filas = Estadisticas.PorJugador(cuadro);

        Assert.Equal(["Ana", "Bea", "Carla", "Dani"], filas.Select(f => f.Jugador.NombreJugador));
        var ana = filas[0];
        Assert.Equal((1, 3, 0, 3), (ana.Jugados, ana.GolesFavor, ana.GolesContra, ana.Diferencia));
        var carla = filas.Single(f => f.Jugador.Id == Carla.Id);
        Assert.Equal((1, 1, 1), (carla.Jugados, carla.GolesFavor, carla.GolesContra));
        Assert.Equal([Ana.Id], Estadisticas.MaximosGoleadores(filas).Select(f => f.Jugador.Id));
    }

    [Fact]
    public void EmpateEnGolesDevuelveVariosMaximosGoleadores()
    {
        var cuadro = Cuadro(new RondaCuadro(1, "Semifinales", [
            new PartidoCuadro(1, 1, Ana, Bea, 2, 0, EstadoPartido.Jugado, Ana.Id),
            new PartidoCuadro(2, 2, Carla, Dani, 2, 1, EstadoPartido.Jugado, Carla.Id)]));

        var maximos = Estadisticas.MaximosGoleadores(Estadisticas.PorJugador(cuadro));

        Assert.Equal([Ana.Id, Carla.Id], maximos.Select(f => f.Jugador.Id).Order());
    }

    [Fact]
    public void SinGolesNoHayMaximoGoleador()
    {
        var cuadro = Cuadro(new RondaCuadro(1, "Final", [
            new PartidoCuadro(1, 1, Ana, Bea, 0, 0, EstadoPartido.Pendiente, null)]));

        var filas = Estadisticas.PorJugador(cuadro);

        Assert.Empty(filas);
        Assert.Empty(Estadisticas.MaximosGoleadores(filas));
    }
}
