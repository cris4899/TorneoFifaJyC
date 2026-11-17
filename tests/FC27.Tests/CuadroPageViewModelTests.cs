using FC27.Core;
using TorneoFifaJyC.Features.Consulta;
using Xunit;

namespace FC27.Tests;

public sealed class CuadroPageViewModelTests
{
    // Cliente de consulta controlable para las pruebas.
    private sealed class ConsultaFalsa : IConsultaTorneos
    {
        public List<Torneo> Torneos { get; } = new();
        public Dictionary<int, CuadroTorneo?> Cuadros { get; } = new();
        public Func<CancellationToken, Task>? AntesDeCuadro { get; set; }

        public Task<IReadOnlyList<Torneo>> ListarAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Torneo>>(Torneos);

        public async Task<CuadroTorneo?> ObtenerCuadroAsync(int torneoId, CancellationToken ct)
        {
            if (AntesDeCuadro is not null) await AntesDeCuadro(ct);
            return Cuadros.TryGetValue(torneoId, out var cuadro) ? cuadro : null;
        }
    }

    private static CuadroPageViewModel Demo(Func<DateTimeOffset>? reloj = null) =>
        new(new ConsultaDemo(), reloj, modoDemo: true);

    private static async Task<CuadroPageViewModel> DemoCargada()
    {
        var vm = Demo();
        await vm.CargarTorneosAsync();
        await vm.ActualizarAsync();
        return vm;
    }

    [Fact]
    public async Task CargarTorneosEligeElPrimeroYMuestraContenido()
    {
        var vm = await DemoCargada();

        Assert.True(vm.MostrarContenido);
        Assert.True(vm.HayTorneos);
        Assert.Equal("Copa DAM", vm.TorneoSeleccionado!.Nombre);
        Assert.Equal("Semifinales", vm.RondaSeleccionada!.Nombre);
        Assert.Equal(2, vm.PartidosVisibles.Count);
    }

    [Fact]
    public async Task CambiarDeRondaActualizaLosCrucesVisibles()
    {
        var vm = await DemoCargada();
        vm.RondaSeleccionada = vm.Rondas.Single(r => r.Nombre == "Final");

        Assert.Single(vm.PartidosVisibles);
        Assert.Equal("Por determinar", vm.PartidosVisibles[0].Visitante);
    }

    [Fact]
    public async Task LaFilaMuestraMarcadorYGanadorDeUnPartidoJugado()
    {
        var vm = await DemoCargada();
        var semifinal1 = vm.PartidosVisibles.Single(p => p.Posicion == 1);

        Assert.Equal("Alex · Equipo A", semifinal1.Local);
        Assert.Equal("2 - 1", semifinal1.Marcador);
        Assert.Equal("Gana Alex", semifinal1.Resultado);
    }

    [Fact]
    public async Task ProximosSoloIncluyeCrucesPendientesConLasDosPlazas()
    {
        var vm = await DemoCargada();

        var proximo = Assert.Single(vm.ProximosFilas);
        Assert.Equal("Semifinales", proximo.Ronda);
        Assert.Equal("Sam · Equipo C", proximo.Local);
        Assert.True(vm.HayProximos);
    }

    [Fact]
    public async Task AlternarVistaMuestraProximosORondas()
    {
        var vm = await DemoCargada();
        Assert.True(vm.MostrarRondas);
        Assert.False(vm.MostrarProximos);

        vm.VerProximosComando.Execute(null);

        Assert.True(vm.MostrarProximos);
        Assert.False(vm.MostrarRondas);
    }

    [Fact]
    public async Task ElModoDemoSeIdentifica()
    {
        var vm = await DemoCargada();
        Assert.True(vm.ModoDemo);
        Assert.Equal("Modo demo · datos ficticios", vm.ModoDemoTexto);
    }

    [Fact]
    public async Task MuestraLaHoraDeLaUltimaActualizacionCorrecta()
    {
        var instante = new DateTimeOffset(2026, 11, 15, 12, 32, 0, TimeSpan.Zero);
        var vm = Demo(() => instante);
        await vm.CargarTorneosAsync();
        await vm.ActualizarAsync();

        Assert.Equal(instante, vm.UltimaActualizacion);
        Assert.StartsWith("Actualizado a las", vm.UltimaActualizacionTexto);
    }

    [Fact]
    public async Task UnFalloConDatosPreviosConservaLaVistaYLaHora()
    {
        var instante = new DateTimeOffset(2026, 11, 15, 12, 32, 0, TimeSpan.Zero);
        var consulta = new ConsultaFalsa { Torneos = { new Torneo(1, "Copa DAM", 4, EstadoTorneo.EnCurso) } };
        consulta.Cuadros[1] = DatosFicticiosConsulta.Cuadro();
        var vm = new CuadroPageViewModel(consulta, () => instante);

        await vm.CargarTorneosAsync();
        await vm.ActualizarAsync();                       // contenido con hora
        consulta.AntesDeCuadro = _ => throw new HttpRequestException("sin red");
        await vm.ActualizarAsync();                       // fallo

        Assert.True(vm.EstaDesactualizado);
        Assert.Equal("Copa DAM", vm.Nombre);
        Assert.Equal(instante, vm.UltimaActualizacion);   // la hora correcta no cambia tras el fallo
    }

    [Fact]
    public async Task ReintentarRecuperaElContenido()
    {
        var consulta = new ConsultaFalsa { Torneos = { new Torneo(1, "Copa DAM", 4, EstadoTorneo.EnCurso) } };
        consulta.Cuadros[1] = DatosFicticiosConsulta.Cuadro();
        var vm = new CuadroPageViewModel(consulta);

        await vm.CargarTorneosAsync();
        await vm.ActualizarAsync();                        // contenido previo
        consulta.AntesDeCuadro = _ => throw new HttpRequestException("sin red");
        await vm.ActualizarAsync();
        Assert.True(vm.EstaDesactualizado);

        consulta.AntesDeCuadro = null;
        await vm.ActualizarAsync();
        Assert.True(vm.MostrarContenido);
        Assert.False(vm.MostrarReintentar);
    }

    [Fact]
    public async Task ElegirUnTorneoCargaSuCuadro()
    {
        var consulta = new ConsultaFalsa
        {
            Torneos =
            {
                new Torneo(1, "Copa DAM", 4, EstadoTorneo.EnCurso),
                new Torneo(2, "Liga JyC", 4, EstadoTorneo.Finalizado),
            }
        };
        var campeon = new JugadorCuadro(9, "Nico", "Equipo Z");
        consulta.Cuadros[1] = DatosFicticiosConsulta.Cuadro();
        consulta.Cuadros[2] = new CuadroTorneo(
            new Torneo(2, "Liga JyC", 4, EstadoTorneo.Finalizado),
            new[] { new RondaCuadro(1, "Final", new[]
            {
                new PartidoCuadro(1, 1, campeon, new JugadorCuadro(8, "Ivo", "Equipo Y"), 3, 1, EstadoPartido.Jugado, 9),
            }) },
            campeon);

        var vm = new CuadroPageViewModel(consulta);
        await vm.CargarTorneosAsync();

        await vm.SeleccionarTorneoAsync(consulta.Torneos[1]);

        Assert.Equal("Liga JyC", vm.Nombre);
        Assert.Equal("Finalizado", vm.EstadoTorneoTexto);
        Assert.True(vm.HayCampeon);
        Assert.Equal("Campeón: Nico · Equipo Z", vm.CampeonTexto);
    }

    [Fact]
    public async Task CambiarDeTorneoDescartaLaRespuestaAnteriorAunqueElNuevoFalle()
    {
        var consulta = new ConsultaConRespuestaTardia();
        var vm = new CuadroPageViewModel(consulta);
        await vm.SeleccionarTorneoAsync(DatosFicticiosConsulta.Cuadro().Torneo);
        Assert.NotNull(vm.UltimaActualizacion);

        consulta.Retener = true;
        var anterior = vm.ActualizarAsync();
        await vm.SeleccionarTorneoAsync(new Torneo(2, "Otro torneo", 4, EstadoTorneo.EnCurso));
        consulta.Pendiente.SetResult(DatosFicticiosConsulta.Cuadro());
        await anterior;

        Assert.True(vm.MostrarError);
        Assert.Empty(vm.Nombre);
        Assert.Empty(vm.PartidosVisibles);
        Assert.Null(vm.UltimaActualizacion);
    }

    private sealed class ConsultaConRespuestaTardia : IConsultaTorneos
    {
        public bool Retener { get; set; }
        public TaskCompletionSource<CuadroTorneo?> Pendiente { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<IReadOnlyList<Torneo>> ListarAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Torneo>>(Array.Empty<Torneo>());

        public Task<CuadroTorneo?> ObtenerCuadroAsync(int id, CancellationToken ct) =>
            id == 2 ? Task.FromException<CuadroTorneo?>(new HttpRequestException("sin red"))
                : Retener ? Pendiente.Task : Task.FromResult<CuadroTorneo?>(DatosFicticiosConsulta.Cuadro());
    }

    // Devuelve instancias nuevas en cada consulta, como el cliente real.
    private sealed class ConsultaRenovada : IConsultaTorneos
    {
        public Task<IReadOnlyList<Torneo>> ListarAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Torneo>>(new[]
            {
                new Torneo(1, "Copa DAM", 4, EstadoTorneo.EnCurso),
                new Torneo(2, "Liga JyC", 4, EstadoTorneo.Finalizado),
            });

        public Task<CuadroTorneo?> ObtenerCuadroAsync(int id, CancellationToken ct) =>
            Task.FromResult<CuadroTorneo?>(DatosFicticiosConsulta.Cuadro());
    }

    [Fact]
    public async Task TrasRecargarElTorneoSeleccionadoApuntaALaInstanciaDeLaNuevaLista()
    {
        var vm = new CuadroPageViewModel(new ConsultaRenovada());
        await vm.CargarTorneosAsync();
        await vm.SeleccionarTorneoAsync(vm.TorneosDisponibles.Single(t => t.Id == 2));

        await vm.CargarTorneosAsync(); // nueva lista con instancias distintas

        // Conserva el torneo y, además, la referencia está en la lista nueva:
        // así el Picker sigue mostrando su nombre tras recargar.
        Assert.Equal(2, vm.TorneoSeleccionado!.Id);
        Assert.Same(vm.TorneosDisponibles.Single(t => t.Id == 2), vm.TorneoSeleccionado);
    }
}
