using System.Text;
using FC27.Core;
using Microsoft.VisualBasic.FileIO;

namespace FC27.Data;

/// <summary>Lee inscripciones y genera el CSV de cruces del torneo.</summary>
public static class TorneoCsv
{
    /// <summary>Lee hasta 64 inscripciones de un CSV UTF-8 con coma o punto y coma.</summary>
    public static IReadOnlyList<DatosInscripcion> LeerInscripciones(Stream stream)
    {
        using var csv = new TextFieldParser(stream, new UTF8Encoding(false, true), true, true)
        {
            HasFieldsEnclosedInQuotes = true
        };
        csv.SetDelimiters(",", ";");
        try
        {
            var cabecera = csv.ReadFields();
            if (cabecera is null || !cabecera.SequenceEqual(new[] { "nombre", "correo", "equipo" }, StringComparer.OrdinalIgnoreCase))
                throw new FormatException("La cabecera debe ser nombre,correo,equipo.");

            // La cabecera fija el separador; el otro puede aparecer en los datos.
            if (!stream.CanSeek)
                throw new ArgumentException("El archivo CSV debe permitir relectura.", nameof(stream));
            stream.Position = 0;
            using var lector = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
            var primeraLinea = lector.ReadLine()!;
            var separador = primeraLinea.Contains(';') ? ";" : ",";
            stream.Position = 0;
            using var filas = new TextFieldParser(stream, new UTF8Encoding(false, true), true, true)
            {
                HasFieldsEnclosedInQuotes = true
            };
            filas.SetDelimiters(separador);
            filas.ReadFields();
            var inscritos = new List<DatosInscripcion>();
            while (!filas.EndOfData)
            {
                var linea = filas.LineNumber;
                var campos = filas.ReadFields();
                if (campos?.Length != 3 || campos.Any(string.IsNullOrWhiteSpace))
                    throw new FormatException($"Fila {linea}: se necesitan nombre, correo y equipo.");
                if (!System.Net.Mail.MailAddress.TryCreate(campos[1], out var correo) || correo.Address != campos[1])
                    throw new FormatException($"Fila {linea}: el correo no es válido.");
                if (inscritos.Count == 64)
                    throw new FormatException("El archivo supera las 64 inscripciones.");
                inscritos.Add(new(campos[0], campos[1], campos[2]));
            }
            if (inscritos.Count == 0)
                throw new FormatException("El archivo no contiene inscripciones.");
            return inscritos;
        }
        catch (MalformedLineException ex)
        {
            throw new FormatException($"Fila {ex.LineNumber}: formato CSV incorrecto.", ex);
        }
    }

    /// <summary>Genera un CSV de rondas, cruces y resultados sin correos.</summary>
    public static string ExportarCuadro(CuadroTorneo cuadro)
    {
        var csv = new StringBuilder("torneo;ronda;posicion;local;equipo_local;visitante;equipo_visitante;goles_local;goles_visitante;estado;ganador\r\n");
        foreach (var ronda in cuadro.Rondas)
        foreach (var partido in ronda.Partidos)
        {
            var jugado = partido.Estado == EstadoPartido.Jugado;
            var ganador = partido.GanadorId == partido.Local?.Id ? partido.Local : partido.Visitante;
            var campos = new[]
            {
                cuadro.Torneo.Nombre, ronda.Nombre, partido.Posicion.ToString(System.Globalization.CultureInfo.InvariantCulture),
                partido.Local?.NombreJugador, partido.Local?.EquipoFC27, partido.Visitante?.NombreJugador, partido.Visitante?.EquipoFC27,
                jugado ? partido.GolesLocal.ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
                jugado ? partido.GolesVisitante.ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
                partido.Estado.ToString(), jugado ? ganador?.NombreJugador : null
            };
            csv.AppendJoin(';', campos.Select(Celda)).Append("\r\n");
        }
        return csv.ToString();
    }

    private static string Celda(string? dato)
    {
        if (dato is null) return "";
        // Las hojas de cálculo pueden interpretar texto introducido por el usuario como fórmula.
        if (dato.TrimStart().StartsWith('=') || dato.TrimStart().StartsWith('+')
            || dato.TrimStart().StartsWith('-') || dato.TrimStart().StartsWith('@'))
            dato = "'" + dato;
        return "\"" + dato.Replace("\"", "\"\"") + "\"";
    }
}