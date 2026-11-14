#if WINDOWS
using FC27.Core;
using FC27.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TorneoFifaJyC.Features.Gestion;

/// <summary>Administración local de torneos desde Windows.</summary>
public sealed class GestionPage : ContentPage
{
    private readonly VerticalStackLayout _panel = new() { Padding = 24, Spacing = 12 };
    private readonly DbContextOptions<TorneoDbContext> _options;
    private int? _seleccion;
    private bool _ocupada;

    public GestionPage()
    {
        Title = "Torneos";
        var conexion = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(FileSystem.AppDataDirectory, "fc27.db"),
            ForeignKeys = true
        };
        _options = new DbContextOptionsBuilder<TorneoDbContext>().UseSqlite(conexion.ToString()).Options;
        Content = new ScrollView { Content = _panel };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await EjecutarAsync(db => db.Database.MigrateAsync());
    }

    private async Task EjecutarAsync(Func<TorneoDbContext, Task> accion)
    {
        if (_ocupada) return;
        _ocupada = true;
        _panel.IsEnabled = false;
        try
        {
            // Cada acción usa su propio contexto para leer el estado confirmado en SQLite.
            await using var db = new TorneoDbContext(_options);
            await accion(db);
            await MostrarAsync(db);
        }
        catch (Exception error)
        {
            var mensaje = (error.GetBaseException() as SqliteException)?.SqliteExtendedErrorCode == 2067
                ? "El nombre del torneo o el correo ya está registrado."
                : error.Message;
            await DisplayAlertAsync("No se pudo completar la acción", mensaje, "Cerrar");
        }
        finally
        {
            _panel.IsEnabled = true;
            _ocupada = false;
        }
    }

    private async Task MostrarAsync(TorneoDbContext db)
    {
        var torneos = await db.Torneos.AsNoTracking().OrderBy(t => t.Nombre).ToListAsync();
        _panel.Clear();
        Texto("Torneos", 26);
        var nombre = Campo("Nombre del torneo");
        var capacidad = new Picker { Title = "Capacidad", ItemsSource = Cuadro.CapacidadesValidas.ToArray(), SelectedIndex = 0 };
        _panel.Add(capacidad);
        Accion("Crear torneo", async contexto =>
        {
            var torneo = await new InscripcionesSqlite(contexto).CrearAsync(nombre.Text,
                (int)capacidad.SelectedItem, CancellationToken.None);
            _seleccion = torneo.Id;
        });
        foreach (var torneo in torneos)
            Accion($"{torneo.Nombre} · {torneo.Estado}", _ =>
            {
                _seleccion = torneo.Id;
                return Task.CompletedTask;
            });

        var seleccionado = torneos.SingleOrDefault(t => t.Id == _seleccion);
        if (seleccionado is null) return;
        Texto(seleccionado.Nombre, 22);
        if (seleccionado.Estado == EstadoTorneo.Borrador)
            await MostrarInscripcionesAsync(db, seleccionado);
        else
            await MostrarCuadroAsync(db, seleccionado);
    }

    private async Task MostrarInscripcionesAsync(TorneoDbContext db, Torneo torneo)
    {
        var inscritos = await db.Participantes.AsNoTracking().Where(p => p.TorneoId == torneo.Id)
            .OrderBy(p => p.NombreJugador).ToListAsync();
        Texto($"{inscritos.Count} de {torneo.NumeroParticipantes} plazas", 16);
        var nombre = Campo("Nombre del jugador");
        var correo = Campo("Correo");
        correo.Keyboard = Keyboard.Email;
        var equipo = Campo("Equipo FC27");
        int? editando = null;
        var guardar = Accion("Añadir participante", async contexto =>
        {
            var altas = new InscripcionesSqlite(contexto);
            var datos = new DatosInscripcion(nombre.Text, correo.Text, equipo.Text);
            if (editando is int id)
                await altas.ActualizarInscripcionAsync(torneo.Id, id, datos, CancellationToken.None);
            else
                await altas.InscribirAsync(torneo.Id, datos, CancellationToken.None);
        });
        foreach (var inscrito in inscritos)
        {
            var editar = new Button { Text = $"Editar {inscrito.NombreJugador} · {inscrito.EquipoFC27}" };
            editar.Clicked += (_, _) =>
            {
                editando = inscrito.Id;
                nombre.Text = inscrito.NombreJugador;
                correo.Text = inscrito.Email;
                equipo.Text = inscrito.EquipoFC27;
                guardar.Text = "Guardar inscripción";
                nombre.Focus();
            };
            _panel.Add(editar);
            Accion($"Retirar a {inscrito.NombreJugador}", async contexto =>
            {
                if (await DisplayAlertAsync("Retirar inscripción", inscrito.NombreJugador, "Retirar", "Cancelar"))
                    await new InscripcionesSqlite(contexto).RetirarInscripcionAsync(torneo.Id, inscrito.Id, CancellationToken.None);
            });
        }
        var iniciar = Accion("Sortear e iniciar", contexto =>
            new CuadroSqlite(contexto).IniciarAsync(torneo.Id, CancellationToken.None));
        iniciar.IsEnabled = inscritos.Count == torneo.NumeroParticipantes;
    }

    private async Task MostrarCuadroAsync(TorneoDbContext db, Torneo torneo)
    {
        var cuadro = await new ConsultaTorneosSqlite(db).ObtenerCuadroAsync(torneo.Id, CancellationToken.None);
        if (cuadro is null) return;
        if (cuadro.Campeon is { } campeon) Texto($"Campeón: {Nombre(campeon)}", 20);
        foreach (var ronda in cuadro.Rondas)
        {
            Texto(ronda.Nombre, 20);
            foreach (var partido in ronda.Partidos)
            {
                var cruce = $"{Nombre(partido.Local)} — {Nombre(partido.Visitante)}";
                if (partido.Estado == EstadoPartido.Jugado)
                {
                    var ganador = partido.GanadorId == partido.Local?.Id ? partido.Local : partido.Visitante;
                    Texto($"{cruce} · {partido.GolesLocal}–{partido.GolesVisitante} · Ganador: {Nombre(ganador)}", 16);
                }
                else
                {
                    Texto(cruce, 16);
                    if (partido.Local is { } local && partido.Visitante is { } visitante)
                        Accion($"Registrar resultado: {local.NombreJugador} — {visitante.NombreJugador}",
                            contexto => RegistrarAsync(contexto, torneo.Id, partido.Id, local, visitante));
                }
            }
        }
    }

    private async Task RegistrarAsync(TorneoDbContext db, int torneoId, int partidoId,
        JugadorCuadro local, JugadorCuadro visitante)
    {
        var textoLocal = await DisplayPromptAsync("Marcador", $"Goles de {Nombre(local)}", keyboard: Keyboard.Numeric);
        if (textoLocal is null) return;
        var textoVisitante = await DisplayPromptAsync("Marcador", $"Goles de {Nombre(visitante)}", keyboard: Keyboard.Numeric);
        if (textoVisitante is null) return;
        if (!int.TryParse(textoLocal, out var golesLocal) || golesLocal < 0 ||
            !int.TryParse(textoVisitante, out var golesVisitante) || golesVisitante < 0)
            throw new ArgumentException("Introduce goles enteros no negativos.");

        int? penaltis = null;
        if (golesLocal == golesVisitante)
        {
            var opcion = await DisplayActionSheetAsync("Ganador por penaltis", "Cancelar", null,
                $"Local: {Nombre(local)}", $"Visitante: {Nombre(visitante)}");
            penaltis = opcion switch
            {
                var valor when valor == $"Local: {Nombre(local)}" => local.Id,
                var valor when valor == $"Visitante: {Nombre(visitante)}" => visitante.Id,
                _ => null
            };
            if (penaltis is null) return;
        }

        var resultado = new ResultadoPartido(golesLocal, golesVisitante, penaltis);
        var ganador = Cuadro.ResolverGanador(local.Id, visitante.Id, resultado);
        if (await DisplayAlertAsync("Confirmar resultado",
            $"{Nombre(local)} {golesLocal}–{golesVisitante} {Nombre(visitante)}\nGanador: {Nombre(ganador == local.Id ? local : visitante)}",
            "Confirmar", "Cancelar"))
            await new CuadroSqlite(db).RegistrarResultadoAsync(torneoId, partidoId, resultado, CancellationToken.None);
    }

    private static string Nombre(JugadorCuadro? jugador) =>
        jugador is null ? "Por determinar" : $"{jugador.NombreJugador} ({jugador.EquipoFC27})";

    private void Texto(string texto, double tamano) => _panel.Add(new Label { Text = texto, FontSize = tamano });

    private Entry Campo(string etiqueta)
    {
        Texto(etiqueta, 14);
        var campo = new Entry();
        SemanticProperties.SetDescription(campo, etiqueta);
        _panel.Add(campo);
        return campo;
    }

    private Button Accion(string texto, Func<TorneoDbContext, Task> accion)
    {
        var boton = new Button { Text = texto };
        boton.Clicked += async (_, _) => await EjecutarAsync(accion);
        _panel.Add(boton);
        return boton;
    }
}
#endif
