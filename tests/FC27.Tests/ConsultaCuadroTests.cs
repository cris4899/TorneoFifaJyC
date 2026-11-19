using System.Text.Json;
using System.Text.Json.Serialization;
using FC27.Core;
using TorneoFifaJyC.Features.Consulta;
using Xunit;

namespace FC27.Tests;

public sealed class ConsultaCuadroTests
{
    private static readonly JsonSerializerOptions Opciones = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    // Carga los datos ficticios compartidos con Android.
    private static CuadroTorneo CuadroEjemplo()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "cuadro-ejemplo.json"));
        return JsonSerializer.Deserialize<CuadroTorneo>(json, Opciones)!;
    }

    [Fact]
    public void LaCargaMuestraContenidoYConservaLasRondasParaElSelector()
    {
        var modelo = new ModeloConsultaCuadro();
        Assert.Equal(EstadoConsulta.Cargando, modelo.Estado);

        modelo.Recibir(CuadroEjemplo());

        Assert.Equal(EstadoConsulta.Contenido, modelo.Estado);
        Assert.Equal(new[] { "Semifinales", "Final" }, modelo.Vista!.Rondas.Select(r => r.Nombre));
    }

    [Fact]
    public void ProximosSoloIncluyeCrucesPendientesConLasDosPlazasAsignadas()
    {
        var vista = CuadroVista.Desde(CuadroEjemplo());

        // Semifinal 2 (Sam vs Eli, pendiente) es próxima; la final tiene la plaza
        // visitante vacía y la semifinal 1 ya está jugada.
        var proximo = Assert.Single(vista.Proximos);
        Assert.Equal(2, proximo.Posicion);
        Assert.Equal("Sam", proximo.Local!.NombreJugador);
        Assert.Equal("Eli", proximo.Visitante!.NombreJugador);
    }

    [Fact]
    public void ElResultadoJugadoConservaMarcadorYGanador()
    {
        var vista = CuadroVista.Desde(CuadroEjemplo());
        var semifinal1 = vista.Rondas[0].Partidos[0];

        Assert.Equal(EstadoPartido.Jugado, semifinal1.Estado);
        Assert.Equal((2, 1), (semifinal1.GolesLocal, semifinal1.GolesVisitante));
        Assert.Equal(semifinal1.Local!.Id, semifinal1.GanadorId);
    }

    [Fact]
    public void UnTorneoFinalizadoDestacaAlCampeon()
    {
        var baseCuadro = CuadroEjemplo();
        var alex = baseCuadro.Rondas[0].Partidos[0].Local!;
        var finalJugada = baseCuadro with
        {
            Torneo = baseCuadro.Torneo with { Estado = EstadoTorneo.Finalizado },
            Campeon = alex,
        };

        var vista = CuadroVista.Desde(finalJugada);

        Assert.Equal(EstadoTorneo.Finalizado, vista.Estado);
        Assert.Equal("Alex", vista.Campeon!.NombreJugador);
    }

    [Fact]
    public void ElListadoDistingueVacioDeContenido()
    {
        Assert.Equal(EstadoConsulta.Vacio, ListadoConsulta.Estado(Array.Empty<Torneo>()));
        Assert.Equal(EstadoConsulta.Contenido,
            ListadoConsulta.Estado(new[] { new Torneo(1, "Copa DAM", 4, EstadoTorneo.EnCurso) }));
    }

    [Fact]
    public void UnCuadroNoDisponibleDejaLaPantallaVacia()
    {
        var modelo = new ModeloConsultaCuadro();
        modelo.Recibir(cuadro: null);

        Assert.Equal(EstadoConsulta.Vacio, modelo.Estado);
        Assert.Null(modelo.Vista);
    }

    [Fact]
    public void UnFalloSinDatosPreviosMuestraError()
    {
        var modelo = new ModeloConsultaCuadro();
        modelo.Cargando();
        modelo.Fallar();

        Assert.Equal(EstadoConsulta.Error, modelo.Estado);
        Assert.Null(modelo.Vista);
    }

    [Fact]
    public void UnFalloConDatosPreviosConservaLaVistaComoDesactualizada()
    {
        var modelo = new ModeloConsultaCuadro();
        modelo.Recibir(CuadroEjemplo());
        modelo.Fallar();

        Assert.Equal(EstadoConsulta.Desactualizado, modelo.Estado);
        Assert.Equal("Copa DAM", modelo.Vista!.Nombre);
    }

    [Fact]
    public void AlRecuperarLaConexionVuelveLaVistaReciente()
    {
        var modelo = new ModeloConsultaCuadro();
        modelo.Recibir(CuadroEjemplo());
        modelo.Fallar();
        Assert.Equal(EstadoConsulta.Desactualizado, modelo.Estado);

        modelo.Cargando();
        modelo.Recibir(CuadroEjemplo());

        Assert.Equal(EstadoConsulta.Contenido, modelo.Estado);
    }
}
