using System.Text.Json;
using System.Text.Json.Serialization;
using FC27.Core;
using Xunit;

namespace FC27.Tests;

public sealed class ContratoConsultaTests
{
    [Fact]
    public void ElCuadroDeEjemploRepresentaUnaFinalConUnaPlazaPendiente()
    {
        var opciones = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };
        // La muestra se comparte con Android para probar un cuadro parcialmente jugado.
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "cuadro-ejemplo.json"));
        var cuadro = Assert.IsType<CuadroTorneo>(JsonSerializer.Deserialize<CuadroTorneo>(json, opciones));
        var semifinales = cuadro.Rondas[0].Partidos;
        var final = Assert.Single(cuadro.Rondas[1].Partidos);

        Assert.Equal(EstadoTorneo.EnCurso, cuadro.Torneo.Estado);
        Assert.Equal(semifinales[0].GanadorId, final.Local?.Id);
        Assert.Null(final.Visitante);
        Assert.Null(cuadro.Campeon);
        Assert.DoesNotContain("email", JsonSerializer.Serialize(cuadro, opciones), StringComparison.OrdinalIgnoreCase);
    }
}
