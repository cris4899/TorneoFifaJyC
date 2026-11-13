using FC27.Core;
using Xunit;

namespace FC27.Tests;

public sealed class CuadroTests
{
    private static IReadOnlyList<int> Inscritos(int n) => Enumerable.Range(1, n).ToList();

    [Theory]
    [InlineData(4, 2, 3)]
    [InlineData(8, 3, 7)]
    [InlineData(16, 4, 15)]
    [InlineData(32, 5, 31)]
    [InlineData(64, 6, 63)]
    public void SorteoGeneraFasesYPartidosPorCapacidad(int capacidad, int fasesEsperadas, int partidosEsperados)
    {
        var plan = Cuadro.Sortear(capacidad, Inscritos(capacidad));

        Assert.Equal(fasesEsperadas, plan.Fases.Count);
        Assert.Equal(partidosEsperados, plan.Fases.Sum(f => f.Partidos.Count));
    }

    [Fact]
    public void CadaInscritoApareceUnaVezEnPrimeraRondaYLasDemasPlazasEstanVacias()
    {
        var plan = Cuadro.Sortear(8, Inscritos(8));
        var primera = plan.Fases[0];

        var plazas = primera.Partidos
            .SelectMany(p => new[] { p.LocalId, p.VisitanteId })
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .ToList();
        Assert.Equal(Inscritos(8).OrderBy(x => x), plazas.OrderBy(x => x));

        foreach (var fase in plan.Fases.Skip(1))
            Assert.All(fase.Partidos, p => Assert.True(p.LocalId is null && p.VisitanteId is null));
    }

    [Fact]
    public void LasFasesLlevanElNombreCorrectoSegunSuNumeroDePartidos()
    {
        var nombres = Cuadro.Sortear(64, Inscritos(64)).Fases.Select(f => f.Nombre).ToList();
        Assert.Equal(
            new[] { "Treintaidosavos", "Dieciseisavos", "Octavos", "Cuartos", "Semifinales", "Final" },
            nombres);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(128)]
    public void SorteoRechazaCapacidadesNoPermitidas(int capacidad)
    {
        Assert.False(Cuadro.EsCapacidadValida(capacidad));
        Assert.Throws<ArgumentException>(() => Cuadro.Sortear(capacidad, Inscritos(Math.Max(capacidad, 0))));
    }

    [Fact]
    public void SorteoRechazaCupoIncompletoOInscritosRepetidos()
    {
        Assert.Throws<ArgumentException>(() => Cuadro.Sortear(4, Inscritos(3)));
        Assert.Throws<ArgumentException>(() => Cuadro.Sortear(4, new[] { 1, 2, 3, 3 }));
    }

    [Fact]
    public void ParticipacionUnicaCadaInscritoJuegaExactamenteUnPartidoInicial()
    {
        var plan = Cuadro.Sortear(16, Inscritos(16));
        var apariciones = plan.Fases[0].Partidos
            .SelectMany(p => new[] { p.LocalId!.Value, p.VisitanteId!.Value })
            .GroupBy(id => id);
        Assert.All(apariciones, g => Assert.Single(g));
    }

    [Fact]
    public void ElGanadorImparOcupaLocalYElParVisitanteEnLaFaseSiguiente()
    {
        Assert.Equal((1, true), Cuadro.Destino(1));
        Assert.Equal((1, false), Cuadro.Destino(2));
        Assert.Equal((2, true), Cuadro.Destino(3));
        Assert.Equal((2, false), Cuadro.Destino(4));
    }

    [Fact]
    public void LosGanadoresAvanzanAlLadoCorrectoAunqueSeRegistrenEnOrdenInverso()
    {
        // Cuatro jugadores 1..4: semifinal 1 = (1 vs 2), semifinal 2 = (3 vs 4).
        var plan = Cuadro.Sortear(4, Inscritos(4));

        // Se registra primero la semifinal 2 y después la 1 (orden inverso).
        plan = Cuadro.Avanzar(plan, faseOrden: 1, posicion: 2, ganadorId: 4);
        plan = Cuadro.Avanzar(plan, faseOrden: 1, posicion: 1, ganadorId: 1);

        var final = Assert.Single(plan.Fases[1].Partidos);
        Assert.Equal(1, final.LocalId);     // ganador de la posición impar → local
        Assert.Equal(4, final.VisitanteId); // ganador de la posición par → visitante
    }

    [Fact]
    public void ElAvanceNoDependeDelOrdenDeRegistro()
    {
        var baseCuadro = Cuadro.Sortear(4, Inscritos(4));

        var directo = Cuadro.Avanzar(Cuadro.Avanzar(baseCuadro, 1, 1, 1), 1, 2, 3);
        var inverso = Cuadro.Avanzar(Cuadro.Avanzar(baseCuadro, 1, 2, 3), 1, 1, 1);

        Assert.Equal(directo.Fases[1].Partidos[0], inverso.Fases[1].Partidos[0]);
    }

    [Fact]
    public void AvanzarRechazaUnGanadorAjenoAlPartido()
    {
        var plan = Cuadro.Sortear(4, Inscritos(4));
        Assert.Throws<ArgumentException>(() => Cuadro.Avanzar(plan, 1, 1, ganadorId: 3));
    }

    [Fact]
    public void JugarLaFinalProduceCampeonYMarcadorDesigualDecideSinPenaltis()
    {
        var plan = Cuadro.Sortear(4, Inscritos(4));
        plan = Cuadro.Avanzar(plan, 1, 1, Cuadro.ResolverGanador(1, 2, new ResultadoPartido(2, 1, null)));
        plan = Cuadro.Avanzar(plan, 1, 2, Cuadro.ResolverGanador(3, 4, new ResultadoPartido(0, 3, null)));

        var final = plan.Fases[1].Partidos[0];
        Assert.Equal(1, final.LocalId);
        Assert.Equal(4, final.VisitanteId);

        var campeon = Cuadro.ResolverGanador(final.LocalId, final.VisitanteId, new ResultadoPartido(1, 1, 4));
        plan = Cuadro.Avanzar(plan, 2, 1, campeon);
        Assert.Equal(4, Cuadro.Campeon(plan));
    }

    [Fact]
    public void ElEmpateSeDecidePorPenaltisYExigeUnJugadorDelPartido()
    {
        Assert.Equal(2, Cuadro.ResolverGanador(1, 2, new ResultadoPartido(0, 0, 2)));
        Assert.Throws<ArgumentException>(() => Cuadro.ResolverGanador(1, 2, new ResultadoPartido(0, 0, null)));
        Assert.Throws<ArgumentException>(() => Cuadro.ResolverGanador(1, 2, new ResultadoPartido(0, 0, 9)));
    }

    [Fact]
    public void ResolverGanadorRechazaGolesNegativosPenaltisContradictoriosYPlazaVacia()
    {
        Assert.Throws<ArgumentException>(() => Cuadro.ResolverGanador(1, 2, new ResultadoPartido(-1, 0, null)));
        // Marcador desigual con penaltis para el perdedor.
        Assert.Throws<ArgumentException>(() => Cuadro.ResolverGanador(1, 2, new ResultadoPartido(3, 1, 2)));
        // Una plaza vacía no puede resolverse.
        Assert.Throws<ArgumentException>(() => Cuadro.ResolverGanador(null, 2, new ResultadoPartido(1, 0, null)));
    }
}
