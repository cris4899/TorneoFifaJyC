using FC27.Core;
using TorneoFifaJyC.Features.Consulta;
using Xunit;

namespace FC27.Tests;

public sealed class ConfiguracionConsultaTests
{
    private sealed class AlmacenMemoria : IAlmacenPreferencias
    {
        private readonly Dictionary<string, string> _datos = new();
        public string? Leer(string clave) => _datos.TryGetValue(clave, out var v) ? v : null;
        public void Guardar(string clave, string valor) => _datos[clave] = valor;
    }

    [Fact]
    public void SinDireccionLaPantallaEstaEnModoDemo()
    {
        var config = new ConfiguracionConsulta(new AlmacenMemoria());
        Assert.True(config.ModoDemo);
        Assert.Null(config.BaseAddress);
    }

    [Fact]
    public void GuardarUnaDireccionValidaSaleDelModoDemoYPersiste()
    {
        var almacen = new AlmacenMemoria();
        new ConfiguracionConsulta(almacen).GuardarDireccion("http://192.168.1.50:5080");

        var recargada = new ConfiguracionConsulta(almacen);
        Assert.False(recargada.ModoDemo);
        Assert.Equal(new Uri("http://192.168.1.50:5080"), recargada.BaseAddress);
    }

    [Fact]
    public void UnaDireccionInvalidaSeRechaza()
    {
        var config = new ConfiguracionConsulta(new AlmacenMemoria());
        Assert.Throws<ArgumentException>(() => config.GuardarDireccion("192.168.1.50"));
    }

    [Fact]
    public void GuardarUnaDireccionVaciaVuelveAlModoDemo()
    {
        var almacen = new AlmacenMemoria();
        var config = new ConfiguracionConsulta(almacen);
        config.GuardarDireccion("http://192.168.1.50:5080");
        config.GuardarDireccion("");
        Assert.True(config.ModoDemo);
    }

    [Fact]
    public void ElTorneoElegidoPersiste()
    {
        var almacen = new AlmacenMemoria();
        new ConfiguracionConsulta(almacen).TorneoId = 7;
        Assert.Equal(7, new ConfiguracionConsulta(almacen).TorneoId);
    }

    [Fact]
    public void OrigenConsultaDevuelveDemoSinDireccion()
    {
        var (consulta, demo) = OrigenConsulta.Crear(new ConfiguracionConsulta(new AlmacenMemoria()));
        Assert.True(demo);
        Assert.IsType<ConsultaDemo>(consulta);
    }
}

public sealed class ProbadorConexionTests
{
    private sealed class ConsultaQueFalla(Exception error) : IConsultaTorneos
    {
        public Task<IReadOnlyList<Torneo>> ListarAsync(CancellationToken ct) => throw error;
        public Task<CuadroTorneo?> ObtenerCuadroAsync(int torneoId, CancellationToken ct) => throw error;
    }

    [Fact]
    public async Task UnaConexionCorrectaInformaDeLosTorneos()
    {
        var resultado = await ProbadorConexion.ProbarAsync(new ConsultaDemo(), CancellationToken.None);

        Assert.True(resultado.Exito);
        Assert.Equal(1, resultado.Torneos);
        Assert.Contains("Conectado", resultado.Mensaje);
    }

    [Fact]
    public async Task UnFalloDevuelveElMotivoSinLanzar()
    {
        var resultado = await ProbadorConexion.ProbarAsync(
            new ConsultaQueFalla(new HttpRequestException("host inaccesible")), CancellationToken.None);

        Assert.False(resultado.Exito);
        Assert.Contains("host inaccesible", resultado.Mensaje);
    }

    [Fact]
    public async Task LaPruebaRespetaLaCancelacion()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ProbadorConexion.ProbarAsync(new ConsultaQueFalla(new OperationCanceledException()), cts.Token));
    }
}
