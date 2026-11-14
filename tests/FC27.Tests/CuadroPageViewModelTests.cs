using FC27.Core;
using TorneoFifaJyC.Features.Consulta;
using Xunit;

namespace FC27.Tests;

public sealed class CuadroPageViewModelTests
{
    private static Func<CancellationToken, Task<CuadroTorneo?>> Ok =>
        _ => Task.FromResult<CuadroTorneo?>(DatosFicticiosConsulta.Cuadro());

    private static Func<CancellationToken, Task<CuadroTorneo?>> Falla =>
        _ => throw new InvalidOperationException("sin red");

    [Fact]
    public async Task AlCargarSeleccionaLaPrimeraRondaYMuestraSusCruces()
    {
        var vm = new CuadroPageViewModel(Ok);
        await vm.ActualizarAsync();

        Assert.True(vm.MostrarContenido);
        Assert.Equal("Semifinales", vm.RondaSeleccionada!.Nombre);
        Assert.Equal(2, vm.PartidosVisibles.Count);
        Assert.Equal("Copa DAM", vm.Nombre);
        Assert.Equal("En curso", vm.EstadoTorneoTexto);
    }

    [Fact]
    public async Task CambiarDeRondaActualizaLosCrucesVisibles()
    {
        var vm = new CuadroPageViewModel(Ok);
        await vm.ActualizarAsync();

        vm.RondaSeleccionada = vm.Rondas.Single(r => r.Nombre == "Final");

        Assert.Single(vm.PartidosVisibles);
        Assert.Equal("Por determinar", vm.PartidosVisibles[0].Visitante);
    }

    [Fact]
    public async Task LaFilaMuestraMarcadorYGanadorDeUnPartidoJugado()
    {
        var vm = new CuadroPageViewModel(Ok);
        await vm.ActualizarAsync();

        var semifinal1 = vm.PartidosVisibles.Single(p => p.Posicion == 1);
        Assert.Equal("Alex · Equipo A", semifinal1.Local);
        Assert.Equal("2 - 1", semifinal1.Marcador);
        Assert.Equal("Gana Alex", semifinal1.Resultado);
    }

    [Fact]
    public async Task UnFalloSinDatosPreviosMuestraErrorYReintentar()
    {
        var vm = new CuadroPageViewModel(Falla);
        await vm.ActualizarAsync();

        Assert.True(vm.MostrarError);
        Assert.True(vm.MostrarReintentar);
        Assert.False(vm.MostrarContenido);
    }

    [Fact]
    public async Task UnFalloConDatosPreviosConservaLaVistaComoDesactualizada()
    {
        var fuentes = new Queue<Func<CancellationToken, Task<CuadroTorneo?>>>(new[] { Ok, Falla });
        var vm = new CuadroPageViewModel(ct => fuentes.Dequeue()(ct));

        await vm.ActualizarAsync(); // contenido
        await vm.ActualizarAsync(); // fallo con datos previos

        Assert.True(vm.EstaDesactualizado);
        Assert.True(vm.MostrarContenido);
        Assert.True(vm.MostrarReintentar);
        Assert.Equal("Copa DAM", vm.Nombre);
    }

    [Fact]
    public async Task ReintentarRecuperaElContenido()
    {
        var fuentes = new Queue<Func<CancellationToken, Task<CuadroTorneo?>>>(new[] { Ok, Falla, Ok });
        var vm = new CuadroPageViewModel(ct => fuentes.Dequeue()(ct));

        await vm.ActualizarAsync(); // contenido
        await vm.ActualizarAsync(); // desactualizado
        vm.ReintentarComando.Execute(null);

        Assert.True(vm.MostrarContenido);
        Assert.False(vm.MostrarReintentar);
    }

    [Fact]
    public async Task UnTorneoFinalizadoDestacaAlCampeon()
    {
        var alex = new JugadorCuadro(1, "Alex", "Equipo A");
        var basico = DatosFicticiosConsulta.Cuadro();
        var finalizado = basico with
        {
            Torneo = basico.Torneo with { Estado = EstadoTorneo.Finalizado },
            Campeon = alex,
        };
        var vm = new CuadroPageViewModel(_ => Task.FromResult<CuadroTorneo?>(finalizado));

        await vm.ActualizarAsync();

        Assert.True(vm.HayCampeon);
        Assert.Equal("Campeón: Alex · Equipo A", vm.CampeonTexto);
    }

    [Fact]
    public async Task UnCuadroNoDisponibleDejaLaPantallaVacia()
    {
        var vm = new CuadroPageViewModel(_ => Task.FromResult<CuadroTorneo?>(null));
        await vm.ActualizarAsync();

        Assert.True(vm.MostrarVacio);
        Assert.Equal("No hay cuadro disponible todavía.", vm.MensajeEstado);
    }
}
