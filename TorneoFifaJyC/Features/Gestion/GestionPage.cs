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
    private Seccion _seccion = Seccion.Inscritos;
    private enum Seccion { Inscritos, Partidos, Cuadro }

    private static readonly Color Fondo = Color.FromArgb("#151719");
    private static readonly Color Superficie = Color.FromArgb("#24282C");
    private static readonly Color Claro = Color.FromArgb("#F2F4F5");
    private static readonly Color Lima = Color.FromArgb("#C6F653");
    private bool _ocupada;

    public GestionPage()
    {
        Title = "Mis torneos";
        BackgroundColor = Fondo;
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
        var seleccionado = torneos.SingleOrDefault(t => t.Id == _seleccion);
        if (seleccionado is null)
        {
            Texto("Mis torneos", 32);
            if (torneos.Count == 0) Texto("Todavía no hay torneos.", 16);
            foreach (var torneo in torneos)
            {
                Accion($"{torneo.Nombre} · {Estado(torneo.Estado)} · {torneo.NumeroParticipantes} plazas", _ =>
                {
                    Abrir(torneo);
                    return Task.CompletedTask;
                });
            }

            Texto("Nuevo torneo", 22);
            var nombre = Campo("Nombre del torneo");
            var capacidad = new Picker
            {
                Title = "Capacidad", ItemsSource = Cuadro.CapacidadesValidas.ToArray(), SelectedIndex = 0,
                BackgroundColor = Superficie, TextColor = Claro, TitleColor = Claro
            };
            SemanticProperties.SetDescription(capacidad, "Capacidad del torneo");
            _panel.Add(capacidad);
            Accion("Crear torneo", async contexto =>
                Abrir(await new InscripcionesSqlite(contexto).CrearAsync(nombre.Text,
                    (int)capacidad.SelectedItem, CancellationToken.None)), principal: true);
            return;
        }

        Accion("Volver a Mis torneos", _ =>
        {
            _seleccion = null;
            return Task.CompletedTask;
        });
        Texto(seleccionado.Nombre, 32);
        Texto(Estado(seleccionado.Estado), 16);
        var navegacion = new HorizontalStackLayout { Spacing = 8 };
        foreach (var seccion in Enum.GetValues<Seccion>())
        {
            var boton = Boton(seccion.ToString(), _ =>
            {
                _seccion = seccion;
                return Task.CompletedTask;
            }, seccion == _seccion);
            boton.IsEnabled = seleccionado.Estado != EstadoTorneo.Borrador || seccion == Seccion.Inscritos;
            if (seccion == _seccion)
                SemanticProperties.SetDescription(boton, $"{seccion}, sección actual");
            navegacion.Add(boton);
        }
        _panel.Add(navegacion);
        if (_seccion == Seccion.Inscritos)
            await MostrarInscripcionesAsync(db, seleccionado);
        else
            await MostrarCuadroAsync(db, seleccionado);
    }

    private void Abrir(Torneo torneo)
    {
        _seleccion = torneo.Id;
        _seccion = torneo.Estado == EstadoTorneo.Borrador ? Seccion.Inscritos : Seccion.Partidos;
    }

    private static string Estado(EstadoTorneo estado) =>
        estado == EstadoTorneo.EnCurso ? "En curso" : estado.ToString();

    private async Task MostrarInscripcionesAsync(TorneoDbContext db, Torneo torneo)
    {
        var inscritos = await db.Participantes.AsNoTracking().Where(p => p.TorneoId == torneo.Id)
            .OrderBy(p => p.NombreJugador).ToListAsync();
        Texto($"{inscritos.Count} de {torneo.NumeroParticipantes} plazas", 16);
        if (torneo.Estado != EstadoTorneo.Borrador)
        {
            foreach (var inscrito in inscritos)
                Texto($"{inscrito.NombreJugador} · {inscrito.EquipoFC27}", 16);
            return;
        }

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
        var iniciar = Accion("Sortear e iniciar", async contexto =>
        {
            await new CuadroSqlite(contexto).IniciarAsync(torneo.Id, CancellationToken.None);
            _seccion = Seccion.Partidos;
        }, principal: true);
        iniciar.IsEnabled = inscritos.Count == torneo.NumeroParticipantes;
    }

    private async Task MostrarCuadroAsync(TorneoDbContext db, Torneo torneo)
    {
        var cuadro = await new ConsultaTorneosSqlite(db).ObtenerCuadroAsync(torneo.Id, CancellationToken.None);
        if (cuadro is null) return;
        if (cuadro.Campeon is { } campeon) Texto($"Campeón: {Nombre(campeon)}", 20);
        if (_seccion == Seccion.Cuadro)
        {
            MostrarCuadroVisual(cuadro);
            return;
        }
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
                    if (_seccion == Seccion.Partidos && partido.Local is { } local && partido.Visitante is { } visitante)
                        Accion($"Registrar resultado: {local.NombreJugador} — {visitante.NombreJugador}",
                            contexto => RegistrarAsync(contexto, torneo.Id, partido.Id, local, visitante));
                }
            }
        }
    }

    private void MostrarCuadroVisual(CuadroTorneo cuadro)
    {
        const double ancho = 250;
        const double separacion = 44;
        const double alto = 116;
        const double paso = 132;
        var rondas = cuadro.Rondas;
        if (rondas.Count == 0) return;

        var lienzo = new AbsoluteLayout
        {
            WidthRequest = rondas.Count * (ancho + separacion) - separacion,
            HeightRequest = 48 + rondas[0].Partidos.Count * paso
        };
        double Centro(int ronda, int posicion) => 42 + ((posicion - 0.5) * (1 << ronda)) * paso;

        void Colocar(View vista, double x, double y, double a, double h)
        {
            AbsoluteLayout.SetLayoutBounds(vista, new Rect(x, y, a, h));
            lienzo.Children.Add(vista);
        }

        void Linea(double x, double y, double a, double h) =>
            Colocar(new BoxView { Color = Color.FromArgb("#929BA3"), InputTransparent = true }, x, y, a, h);

        for (var indice = 0; indice < rondas.Count; indice++)
        {
            var x = indice * (ancho + separacion);
            Colocar(new Label { Text = rondas[indice].Nombre, TextColor = Claro, FontSize = 18 }, x, 0, ancho, 36);
            foreach (var partido in rondas[indice].Partidos)
            {
                var centro = Centro(indice, partido.Posicion);
                if (indice + 1 < rondas.Count)
                {
                    // Cada pareja de cruces converge en la posición calculada por el dominio.
                    var destino = Centro(indice + 1, Cuadro.Destino(partido.Posicion).PosicionDestino);
                    var union = x + ancho + separacion / 2;
                    Linea(x + ancho, centro, separacion / 2, 2);
                    Linea(union, Math.Min(centro, destino), 2, Math.Abs(centro - destino) + 2);
                    Linea(union, destino, separacion / 2, 2);
                }

                var jugado = partido.Estado == EstadoPartido.Jugado;
                var marcador = jugado ? $"{partido.GolesLocal}–{partido.GolesVisitante}" : "Pendiente";
                var tarjeta = new VerticalStackLayout { Padding = 10, Spacing = 2, BackgroundColor = Superficie };
                foreach (var jugador in new[] { partido.Local, partido.Visitante })
                {
                    var gano = jugado && jugador is not null && jugador.Id == partido.GanadorId;
                    tarjeta.Add(new Label
                    {
                        Text = gano ? $"{Nombre(jugador)} · Ganador" : Nombre(jugador),
                        TextColor = gano ? Lima : Claro,
                        FontSize = 14,
                        MaxLines = 2,
                        LineBreakMode = LineBreakMode.TailTruncation
                    });
                }
                tarjeta.Add(new Label { Text = marcador, TextColor = Claro, FontSize = 16, FontAttributes = FontAttributes.Bold });
                var ganador = partido.GanadorId == partido.Local?.Id ? partido.Local : partido.Visitante;
                SemanticProperties.SetDescription(tarjeta,
                    $"{rondas[indice].Nombre}, partido {partido.Posicion}. {Nombre(partido.Local)} contra {Nombre(partido.Visitante)}. {marcador}."
                    + (jugado ? $" Ganador: {Nombre(ganador)}." : string.Empty));
                Colocar(tarjeta, x, centro - alto / 2, ancho, alto);
            }
        }

        _panel.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = lienzo });
    }

    private async Task RegistrarAsync(TorneoDbContext db, int torneoId, int partidoId,
        JugadorCuadro local, JugadorCuadro visitante)
    {
        var formulario = new ContentPage { Title = "Registrar resultado", BackgroundColor = Fondo };
        var campos = new VerticalStackLayout { Padding = 24, Spacing = 12 };
        var golesLocal = new Entry { Keyboard = Keyboard.Numeric, BackgroundColor = Superficie, TextColor = Claro };
        var golesVisitante = new Entry { Keyboard = Keyboard.Numeric, BackgroundColor = Superficie, TextColor = Claro };
        var penaltis = new Picker
        {
            Title = "Ganador por penaltis", BackgroundColor = Superficie, TextColor = Claro, TitleColor = Claro,
            ItemsSource = new[] { Nombre(local), Nombre(visitante) }, IsVisible = false
        };
        var error = new Label { TextColor = Claro };
        var guardar = new Button { Text = "Revisar marcador", BackgroundColor = Lima, TextColor = Fondo };
        var cancelar = new Button { Text = "Cancelar", BackgroundColor = Superficie, TextColor = Claro };
        var respuesta = new TaskCompletionSource<ResultadoPartido?>(TaskCreationOptions.RunContinuationsAsynchronously);

        void AgregarMarcador(JugadorCuadro jugador, Entry campo)
        {
            var etiqueta = $"Goles de {Nombre(jugador)}";
            campos.Add(new Label { Text = etiqueta, TextColor = Claro });
            SemanticProperties.SetDescription(campo, etiqueta);
            campos.Add(campo);
        }

        bool LeerGoles(out int local, out int visitante)
        {
            var localValido = int.TryParse(golesLocal.Text, out local) && local >= 0;
            var visitanteValido = int.TryParse(golesVisitante.Text, out visitante) && visitante >= 0;
            return localValido && visitanteValido;
        }

        void ActualizarDesempate(object? sender, TextChangedEventArgs e)
        {
            // La elección solo vale para el empate que se está editando.
            penaltis.SelectedIndex = -1;
            penaltis.IsVisible = LeerGoles(out var local, out var visitante) && local == visitante;
            error.Text = "";
        }

        golesLocal.TextChanged += ActualizarDesempate;
        golesVisitante.TextChanged += ActualizarDesempate;
        guardar.Clicked += (_, _) =>
        {
            if (!LeerGoles(out var localGoles, out var visitanteGoles))
            {
                error.Text = "Introduce goles enteros no negativos en ambos campos.";
                return;
            }

            int? ganadorPenaltis = null;
            if (localGoles == visitanteGoles)
            {
                if (penaltis.SelectedIndex < 0)
                {
                    error.Text = "Selecciona el ganador por penaltis.";
                    penaltis.Focus();
                    return;
                }
                ganadorPenaltis = penaltis.SelectedIndex == 0 ? local.Id : visitante.Id;
            }
            respuesta.TrySetResult(new ResultadoPartido(localGoles, visitanteGoles, ganadorPenaltis));
        };
        cancelar.Clicked += (_, _) => respuesta.TrySetResult(null);
        formulario.Disappearing += (_, _) => respuesta.TrySetResult(null);
        AgregarMarcador(local, golesLocal);
        AgregarMarcador(visitante, golesVisitante);
        SemanticProperties.SetDescription(penaltis, "Ganador por penaltis");
        campos.Add(penaltis);
        campos.Add(error);
        campos.Add(guardar);
        campos.Add(cancelar);
        formulario.Content = new ScrollView { Content = campos };

        await Navigation.PushModalAsync(formulario);
        var resultado = await respuesta.Task;
        if (Navigation.ModalStack.LastOrDefault() == formulario)
            await Navigation.PopModalAsync();
        if (resultado is null) return;

        var ganador = Cuadro.ResolverGanador(local.Id, visitante.Id, resultado);
        if (await DisplayAlertAsync("Confirmar resultado",
            $"{Nombre(local)} {resultado.GolesLocal}–{resultado.GolesVisitante} {Nombre(visitante)}\nGanador: {Nombre(ganador == local.Id ? local : visitante)}",
            "Confirmar", "Cancelar"))
            await new CuadroSqlite(db).RegistrarResultadoAsync(torneoId, partidoId, resultado, CancellationToken.None);
    }

    private static string Nombre(JugadorCuadro? jugador) =>
        jugador is null ? "Por determinar" : $"{jugador.NombreJugador} ({jugador.EquipoFC27})";

    private void Texto(string texto, double tamano) => _panel.Add(new Label { Text = texto, FontSize = tamano, TextColor = Claro });

    private Entry Campo(string etiqueta)
    {
        Texto(etiqueta, 14);
        var campo = new Entry { BackgroundColor = Superficie, TextColor = Claro };
        SemanticProperties.SetDescription(campo, etiqueta);
        _panel.Add(campo);
        return campo;
    }

    private Button Accion(string texto, Func<TorneoDbContext, Task> accion, bool principal = false)
    {
        var boton = Boton(texto, accion, principal);
        _panel.Add(boton);
        return boton;
    }

    private Button Boton(string texto, Func<TorneoDbContext, Task> accion, bool principal)
    {
        var boton = new Button
        {
            Text = texto, CornerRadius = 8, MinimumHeightRequest = 42,
            BackgroundColor = principal ? Lima : Superficie,
            TextColor = principal ? Fondo : Claro
        };
        boton.Clicked += async (_, _) => await EjecutarAsync(accion);
        return boton;
    }
}
#endif
