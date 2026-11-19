#if WINDOWS
using FC27.Core;
using FC27.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TorneoFifaJyC.Controls;

namespace TorneoFifaJyC.Features.Gestion;

/// <summary>Administración local de torneos desde Windows.</summary>
public sealed class GestionPage : ContentPage
{
    private readonly VerticalStackLayout _panel = new() { Padding = new Thickness(24, 24, 24, 96), Spacing = 12 };
    private readonly MenuFlotante _menu = new();
    private readonly DbContextOptions<TorneoDbContext> _options;
    private int? _seleccion;
    private Seccion _seccion = Seccion.Inscritos;
    private enum Seccion { Inscritos, Partidos, Cuadro }

    private static readonly Color Fondo = (Color)Application.Current!.Resources["Fondo"];
    private static readonly Color Superficie = (Color)Application.Current!.Resources["Superficie"];
    private static readonly Color Borde = (Color)Application.Current!.Resources["Borde"];
    private static readonly Color Claro = (Color)Application.Current!.Resources["Claro"];
    private static readonly Color Atenuado = (Color)Application.Current!.Resources["Atenuado"];
    private static readonly Color Lima = (Color)Application.Current!.Resources["Verde"];
    private static readonly Color Oro = (Color)Application.Current!.Resources["Oro"];
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
        Content = new Grid { Children = { new ScrollView { Content = _panel }, _menu } };
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
        ActualizarMenu(seleccionado);
        if (seleccionado is null)
        {
            Titulo("Mis torneos");
            if (torneos.Count == 0) Texto("Todavía no hay torneos. Crea el primero abajo.", 16);
            var inscritos = await db.Participantes.AsNoTracking().GroupBy(p => p.TorneoId)
                .Select(g => new { g.Key, Total = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Total);
            foreach (var torneo in torneos)
            {
                var abrir = Boton("Abrir", _ =>
                {
                    Abrir(torneo);
                    return Task.CompletedTask;
                }, principal: true);
                SemanticProperties.SetDescription(abrir, $"Abrir {torneo.Nombre}");
                var fila = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(new(GridLength.Star), new(GridLength.Auto)), ColumnSpacing = 12 };
                var datos = new VerticalStackLayout { Spacing = 6 };
                datos.Add(new Label { Text = torneo.Nombre, FontSize = 20, FontFamily = "OpenSansSemibold", TextColor = Claro });
                var detalle = new HorizontalStackLayout { Spacing = 8 };
                detalle.Add(Etiqueta(Estado(torneo.Estado), ColorEstado(torneo.Estado)));
                detalle.Add(new Label { Text = $"{inscritos.GetValueOrDefault(torneo.Id)}/{torneo.NumeroParticipantes} jugadores", TextColor = Atenuado, VerticalOptions = LayoutOptions.Center });
                datos.Add(detalle);
                fila.Add(datos);
                fila.Add(abrir, 1);
                abrir.VerticalOptions = LayoutOptions.Center;
                _panel.Add(Tarjeta(fila));
            }

            Seccionar("Nuevo torneo");
            var nombre = Campo("Nombre del torneo");
            var errorNombre = MensajeCampo();
            var capacidad = new Picker
            {
                Title = "Capacidad", ItemsSource = Cuadro.CapacidadesValidas.ToArray(), SelectedIndex = 0,
                BackgroundColor = Superficie, TextColor = Claro, TitleColor = Claro
            };
            SemanticProperties.SetDescription(capacidad, "Capacidad del torneo");
            _panel.Add(capacidad);
            var crear = new Button
            {
                Text = "Crear torneo", CornerRadius = 8, MinimumHeightRequest = 42,
                BackgroundColor = Lima, TextColor = Fondo
            };
            crear.Clicked += async (_, _) =>
            {
                var nombreTexto = nombre.Text?.Trim();
                var nombreVacio = string.IsNullOrWhiteSpace(nombreTexto);
                var nombreDuplicado = !nombreVacio && torneos.Any(torneo =>
                    string.Equals(torneo.Nombre, nombreTexto, StringComparison.OrdinalIgnoreCase));
                errorNombre.Text = nombreVacio ? "Indica el nombre del torneo." : "Ya existe un torneo con ese nombre.";
                errorNombre.IsVisible = nombreVacio || nombreDuplicado;
                if (errorNombre.IsVisible)
                {
                    nombre.Focus();
                    return;
                }

                await EjecutarAsync(async contexto =>
                    Abrir(await new InscripcionesSqlite(contexto).CrearAsync(nombreTexto!,
                        (int)capacidad.SelectedItem, CancellationToken.None)));
            };
            _panel.Add(crear);
            return;
        }

        Titulo(seleccionado.Nombre);
        _panel.Add(Etiqueta(Estado(seleccionado.Estado), ColorEstado(seleccionado.Estado)));
        if (_seccion == Seccion.Inscritos)
            await MostrarInscripcionesAsync(db, seleccionado);
        else
            await MostrarCuadroAsync(db, seleccionado);
    }

    // Menú flotante: «Mis torneos» siempre; con un torneo abierto, sus secciones.
    private void ActualizarMenu(Torneo? torneo)
    {
        async void Ir(int? seleccion, Seccion seccion)
        {
            _seleccion = seleccion;
            _seccion = seccion;
            await EjecutarAsync(_ => Task.CompletedTask);
        }

        var opciones = new List<OpcionMenu> { new("Mis torneos", () => Ir(null, _seccion), torneo is null) };
        if (torneo is not null)
        {
            var borrador = torneo.Estado == EstadoTorneo.Borrador;
            foreach (var (seccion, texto) in new[] { (Seccion.Inscritos, "Inscritos"), (Seccion.Partidos, "Partidos"), (Seccion.Cuadro, "Cuadro") })
                opciones.Add(new OpcionMenu(texto, () => Ir(torneo.Id, seccion), _seccion == seccion, !borrador || seccion == Seccion.Inscritos));
        }
        _menu.Establecer(opciones);
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
                _panel.Add(Tarjeta(FilaJugador(inscrito.NombreJugador, inscrito.EquipoFC27, null, true)));
            return;
        }
        var faltan = torneo.NumeroParticipantes - inscritos.Count;
        if (faltan > 0)
            Texto(faltan == 1 ? "Añade 1 jugador más para sortear." : $"Añade {faltan} jugadores más para sortear.", 14);

        Accion("Importar inscripciones CSV", contexto => ImportarCsvAsync(contexto, torneo.Id));
        var nombre = Campo("Nombre del jugador");
        var errorNombre = MensajeCampo();
        var correo = Campo("Correo");
        correo.Keyboard = Keyboard.Email;
        var errorCorreo = MensajeCampo();
        var equipo = Campo("Equipo FC27");
        var errorEquipo = MensajeCampo();
        int? editando = null;
        var guardar = new Button
        {
            Text = "Añadir participante", CornerRadius = 8, MinimumHeightRequest = 42,
            BackgroundColor = Superficie, TextColor = Claro
        };
        guardar.Clicked += async (_, _) =>
        {
            var correoTexto = correo.Text?.Trim();
            var correoVacio = string.IsNullOrWhiteSpace(correoTexto);
            var correoInvalido = !correoVacio && (!System.Net.Mail.MailAddress.TryCreate(correoTexto, out var direccion)
                || direccion.Address != correoTexto);
            var correoDuplicado = !correoVacio && !correoInvalido && inscritos.Any(inscrito =>
                inscrito.Id != editando && string.Equals(inscrito.Email, correoTexto, StringComparison.OrdinalIgnoreCase));
            errorNombre.Text = "Indica el nombre del jugador.";
            errorNombre.IsVisible = string.IsNullOrWhiteSpace(nombre.Text);
            errorCorreo.Text = correoVacio ? "Indica el correo." : correoInvalido
                ? "Introduce un correo válido." : "Este correo ya está inscrito en el torneo.";
            errorCorreo.IsVisible = correoVacio || correoInvalido || correoDuplicado;
            errorEquipo.Text = "Indica el equipo FC27.";
            errorEquipo.IsVisible = string.IsNullOrWhiteSpace(equipo.Text);
            if (errorNombre.IsVisible || errorCorreo.IsVisible || errorEquipo.IsVisible)
            {
                (errorNombre.IsVisible ? nombre : errorCorreo.IsVisible ? correo : equipo).Focus();
                return;
            }

            await EjecutarAsync(async contexto =>
            {
                var altas = new InscripcionesSqlite(contexto);
                var datos = new DatosInscripcion(nombre.Text, correoTexto!, equipo.Text);
                if (editando is int id)
                    await altas.ActualizarInscripcionAsync(torneo.Id, id, datos, CancellationToken.None);
                else
                    await altas.InscribirAsync(torneo.Id, datos, CancellationToken.None);
            });
        };
        _panel.Add(guardar);
        foreach (var inscrito in inscritos)
        {
            var editar = new Button { Text = "Editar", CornerRadius = 8, BackgroundColor = Superficie, TextColor = Claro, BorderColor = Borde, BorderWidth = 1 };
            SemanticProperties.SetDescription(editar, $"Editar {inscrito.NombreJugador} · {inscrito.EquipoFC27}");
            editar.Clicked += (_, _) =>
            {
                editando = inscrito.Id;
                nombre.Text = inscrito.NombreJugador;
                correo.Text = inscrito.Email;
                equipo.Text = inscrito.EquipoFC27;
                guardar.Text = "Guardar inscripción";
                nombre.Focus();
            };
            var retirar = Boton("Retirar", async contexto =>
            {
                if (await DisplayAlertAsync("Retirar inscripción", inscrito.NombreJugador, "Retirar", "Cancelar"))
                    await new InscripcionesSqlite(contexto).RetirarInscripcionAsync(torneo.Id, inscrito.Id, CancellationToken.None);
            }, false);
            SemanticProperties.SetDescription(retirar, $"Retirar a {inscrito.NombreJugador}");
            var fila = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto)), ColumnSpacing = 8 };
            fila.Add(FilaJugador(inscrito.NombreJugador, inscrito.EquipoFC27, null, true));
            fila.Add(editar, 1);
            fila.Add(retirar, 2);
            _panel.Add(Tarjeta(fila));
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
        Accion("Exportar cuadro y resultados CSV", _ => ExportarCsvAsync(cuadro));
        if (cuadro.Campeon is { } campeon)
        {
            var trofeo = new VerticalStackLayout { Spacing = 2 };
            trofeo.Add(new Label { Text = "🏆 CAMPEÓN", FontFamily = "OpenSansSemibold", CharacterSpacing = 2, TextColor = Fondo });
            trofeo.Add(new Label { Text = Nombre(campeon), FontSize = 26, FontFamily = "OpenSansSemibold", TextColor = Fondo });
            var tarjeta = Tarjeta(trofeo);
            tarjeta.BackgroundColor = Oro;
            tarjeta.Stroke = Oro;
            SemanticProperties.SetDescription(tarjeta, $"Campeón: {Nombre(campeon)}");
            _panel.Add(tarjeta);
        }
        if (_seccion == Seccion.Cuadro)
        {
            Accion("Modo presentación", _ => PresentarAsync(cuadro));
            _panel.Add(new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = CrearCuadroVisual(cuadro) });
            MostrarEstadisticas(cuadro);
            return;
        }
        foreach (var ronda in cuadro.Rondas)
        {
            Seccionar(ronda.Nombre);
            foreach (var partido in ronda.Partidos)
            {
                var jugado = partido.Estado == EstadoPartido.Jugado;
                var contenido = new VerticalStackLayout { Spacing = 8 };
                var (posicion, _) = Cuadro.Destino(partido.Posicion);
                var destino = cuadro.Rondas.SingleOrDefault(r => r.Orden == ronda.Orden + 1)?
                    .Partidos.SingleOrDefault(p => p.Posicion == posicion);
                var corregible = jugado && destino?.Estado == EstadoPartido.Pendiente;
                contenido.Add(Etiqueta(!jugado ? "PENDIENTE" : corregible ? "JUGADO" : "CERRADO",
                    !jugado ? Oro : corregible ? Lima : Atenuado));
                foreach (var jugador in new[] { partido.Local, partido.Visitante })
                {
                    int? goles = jugado ? (jugador == partido.Local ? partido.GolesLocal : partido.GolesVisitante) : null;
                    var gana = !jugado || jugador?.Id == partido.GanadorId;
                    contenido.Add(FilaJugador(jugador?.NombreJugador ?? "Por determinar", jugador?.EquipoFC27, goles, gana));
                }
                if (jugado && partido.GolesLocal == partido.GolesVisitante)
                    contenido.Add(new Label { Text = $"Penaltis: gana {Nombre(partido.GanadorId == partido.Local?.Id ? partido.Local : partido.Visitante)}", TextColor = Atenuado, FontSize = 13 });
                if (partido.Local is { } local && partido.Visitante is { } visitante)
                {
                    Button? accion = null;
                    if (corregible)
                        accion = Boton("Corregir marcador", contexto => RegistrarAsync(contexto, torneo.Id, partido.Id, local, visitante, partido), false);
                    else if (!jugado && _seccion == Seccion.Partidos)
                        accion = Boton("Registrar resultado", contexto => RegistrarAsync(contexto, torneo.Id, partido.Id, local, visitante), true);
                    if (accion is not null)
                    {
                        SemanticProperties.SetDescription(accion, $"{accion.Text}: {local.NombreJugador} — {visitante.NombreJugador}");
                        accion.HorizontalOptions = LayoutOptions.Start;
                        contenido.Add(accion);
                    }
                }
                if (jugado && !corregible && destino is not null)
                    contenido.Add(new Label { Text = "El siguiente cruce ya se ha jugado.", TextColor = Atenuado, FontSize = 13 });
                _panel.Add(Tarjeta(contenido));
            }
        }
    }

    private async Task ImportarCsvAsync(TorneoDbContext db, int torneoId)
    {
        var archivo = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "Seleccionar inscripciones CSV",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.WinUI] = new[] { ".csv" }
            })
        });
        if (archivo is null) return;
        await using var origen = await archivo.OpenReadAsync();
        using var copia = new MemoryStream();
        var buffer = new byte[8192];
        int leidos;
        while ((leidos = await origen.ReadAsync(buffer)) != 0)
        {
            if (copia.Length + leidos > 1024 * 1024)
                throw new FormatException("El archivo CSV supera 1 MB.");
            copia.Write(buffer, 0, leidos);
        }
        copia.Position = 0;
        var datos = TorneoCsv.LeerInscripciones(copia);
        var altas = new InscripcionesSqlite(db);
        await altas.ValidarImportacionAsync(torneoId, datos, CancellationToken.None);

        var vista = new ContentPage { BackgroundColor = Fondo };
        var contenido = new VerticalStackLayout { Padding = 24, Spacing = 10 };
        contenido.Add(new Label { Text = $"Importar {datos.Count} inscripciones", FontSize = 24, TextColor = Claro });
        foreach (var fila in datos)
            contenido.Add(new Label { Text = $"{fila.NombreJugador} · {fila.Email} · {fila.EquipoFC27}", TextColor = Claro });
        var confirmar = new Button { Text = "Importar inscripciones", BackgroundColor = Lima, TextColor = Fondo };
        var cancelar = new Button { Text = "Cancelar" };
        var respuesta = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        confirmar.Clicked += (_, _) => respuesta.TrySetResult(true);
        cancelar.Clicked += (_, _) => respuesta.TrySetResult(false);
        vista.Disappearing += (_, _) => respuesta.TrySetResult(false);
        contenido.Add(confirmar);
        contenido.Add(cancelar);
        vista.Content = new ScrollView { Content = contenido };
        await Navigation.PushModalAsync(vista);
        var aceptado = await respuesta.Task;
        if (Navigation.ModalStack.LastOrDefault() == vista)
            await Navigation.PopModalAsync();
        if (aceptado)
            await altas.ImportarAsync(torneoId, datos, CancellationToken.None);
    }

    private async Task ExportarCsvAsync(CuadroTorneo cuadro)
    {
        var ventana = (Microsoft.UI.Xaml.Window)(Window?.Handler?.PlatformView
            ?? throw new InvalidOperationException("No hay una ventana disponible para guardar el CSV."));
        var selector = new Windows.Storage.Pickers.FileSavePicker { SuggestedFileName = $"cuadro-{cuadro.Torneo.Id}" };
        selector.FileTypeChoices.Add("Archivo CSV", new[] { ".csv" });
        WinRT.Interop.InitializeWithWindow.Initialize(selector, WinRT.Interop.WindowNative.GetWindowHandle(ventana));
        var archivo = await selector.PickSaveFileAsync();
        if (archivo is null) return;
        var csv = TorneoCsv.ExportarCuadro(cuadro);
        // La marca UTF-8 ayuda a las hojas de cálculo a detectar tildes y eñes.
        await File.WriteAllTextAsync(archivo.Path, csv, new System.Text.UTF8Encoding(true));
        await DisplayAlertAsync("CSV exportado", $"Se guardó en {archivo.Path}", "Cerrar");
    }

    private static View CrearCuadroVisual(CuadroTorneo cuadro)
    {
        const double ancho = 250;
        const double separacion = 44;
        const double alto = 140;
        const double paso = alto + 16;
        var rondas = cuadro.Rondas;
        if (rondas.Count == 0) return new ContentView();

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

        void Linea(double x, double y, double a, double h, Color color) =>
            Colocar(new BoxView { Color = color, InputTransparent = true }, x, y, a, h);

        for (var indice = 0; indice < rondas.Count; indice++)
        {
            var x = indice * (ancho + separacion);
            Colocar(new Label { Text = rondas[indice].Nombre.ToUpperInvariant(), TextColor = Lima, FontSize = 14, CharacterSpacing = 2, FontFamily = "OpenSansSemibold" }, x, 0, ancho, 36);
            foreach (var partido in rondas[indice].Partidos)
            {
                var centro = Centro(indice, partido.Posicion);
                if (indice + 1 < rondas.Count)
                {
                    // Cada pareja de cruces converge en la posición calculada por el dominio.
                    var destino = Centro(indice + 1, Cuadro.Destino(partido.Posicion).PosicionDestino);
                    var union = x + ancho + separacion / 2;
                    // El ganador avanza por una línea verde; los cruces pendientes quedan en gris.
                    var color = partido.Estado == EstadoPartido.Jugado ? Lima : Borde;
                    Linea(x + ancho, centro, separacion / 2, 2, color);
                    Linea(union, Math.Min(centro, destino), 2, Math.Abs(centro - destino) + 2, color);
                    Linea(union, destino, separacion / 2, 2, color);
                }

                var jugado = partido.Estado == EstadoPartido.Jugado;
                var marcador = jugado ? $"{partido.GolesLocal}–{partido.GolesVisitante}" : "Pendiente";
                var filas = new VerticalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
                foreach (var jugador in new[] { partido.Local, partido.Visitante })
                {
                    int? goles = jugado ? (jugador == partido.Local ? partido.GolesLocal : partido.GolesVisitante) : null;
                    filas.Add(FilaJugador(jugador?.NombreJugador ?? "Por determinar", jugador?.EquipoFC27, goles,
                        !jugado || jugador?.Id == partido.GanadorId));
                }
                if (!jugado) filas.Add(new Label { Text = "PENDIENTE", TextColor = Oro, FontSize = 11, CharacterSpacing = 1 });
                var tarjeta = Tarjeta(filas);
                var ganador = partido.GanadorId == partido.Local?.Id ? partido.Local : partido.Visitante;
                SemanticProperties.SetDescription(tarjeta,
                    $"{rondas[indice].Nombre}, partido {partido.Posicion}. {Nombre(partido.Local)} contra {Nombre(partido.Visitante)}. {marcador}."
                    + (jugado ? $" Ganador: {Nombre(ganador)}." : string.Empty));
                Colocar(tarjeta, x, centro - alto / 2, ancho, alto);
            }
        }

        lienzo.HorizontalOptions = LayoutOptions.Start;
        return lienzo;
    }

    private void MostrarEstadisticas(CuadroTorneo cuadro)
    {
        var filas = Estadisticas.PorJugador(cuadro);
        if (filas.Count == 0) return;
        Seccionar("Estadísticas");
        var maximos = Estadisticas.MaximosGoleadores(filas);
        if (maximos.Count > 0)
            _panel.Add(new Label
            {
                Text = $"Máximo goleador: {string.Join(", ", maximos.Select(m => Nombre(m.Jugador)))} · {maximos[0].GolesFavor} goles",
                TextColor = Oro, FontFamily = "OpenSansSemibold"
            });

        var tabla = new Grid { ColumnSpacing = 24, RowSpacing = 6 };
        tabla.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        for (var i = 0; i < 4; i++) tabla.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        void Fila(int fila, Color color, params string[] celdas)
        {
            tabla.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            for (var columna = 0; columna < celdas.Length; columna++)
                tabla.Add(new Label
                {
                    Text = celdas[columna], TextColor = color,
                    HorizontalTextAlignment = columna == 0 ? TextAlignment.Start : TextAlignment.End
                }, columna, fila);
        }
        Fila(0, Atenuado, "Jugador", "PJ", "GF", "GC", "DG");
        for (var i = 0; i < filas.Count; i++)
        {
            var e = filas[i];
            Fila(i + 1, Claro, Nombre(e.Jugador), e.Jugados.ToString(), e.GolesFavor.ToString(), e.GolesContra.ToString(),
                e.Diferencia.ToString("+0;−0;0"));
        }
        SemanticProperties.SetDescription(tabla, "Estadísticas por jugador: " + string.Join(". ", filas.Select(e =>
            $"{Nombre(e.Jugador)}, partidos jugados {e.Jugados}, goles a favor {e.GolesFavor}, goles en contra {e.GolesContra}")));
        _panel.Add(Tarjeta(tabla));
    }

    private async Task PresentarAsync(CuadroTorneo cuadro)
    {
        var contenido = new VerticalStackLayout { Padding = 32, Spacing = 24 };
        contenido.Add(new Label { Text = cuadro.Torneo.Nombre, FontSize = 40, FontFamily = "OpenSansSemibold", TextColor = Claro });
        if (cuadro.Campeon is { } campeon)
            contenido.Add(new Label { Text = $"🏆 Campeón: {Nombre(campeon)}", FontSize = 28, FontFamily = "OpenSansSemibold", TextColor = Oro });
        contenido.Add(new Label { Text = "Esc para salir", TextColor = Atenuado });
        // El lienzo se amplía para que se lea desde lejos al proyectarlo.
        var lienzo = CrearCuadroVisual(cuadro);
        lienzo.Scale = 1.4;
        lienzo.AnchorX = 0;
        lienzo.AnchorY = 0;
        contenido.Add(lienzo);
        var vista = new ContentPage
        {
            Title = "Modo presentación", BackgroundColor = Fondo,
            Content = new ScrollView { Orientation = ScrollOrientation.Both, Content = contenido }
        };

        var ventana = (Microsoft.UI.Xaml.Window?)Window?.Handler?.PlatformView;
        void Salir()
        {
            ventana?.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
            if (Navigation.ModalStack.LastOrDefault() == vista)
                _ = Navigation.PopModalAsync();
        }
        vista.HandlerChanged += (_, _) =>
        {
            if (vista.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement nativo) return;
            var esc = new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = Windows.System.VirtualKey.Escape };
            esc.Invoked += (_, args) => { args.Handled = true; Salir(); };
            nativo.KeyboardAccelerators.Add(esc);
        };
        vista.Disappearing += (_, _) =>
            ventana?.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
        await Navigation.PushModalAsync(vista);
        ventana?.AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
    }

    private async Task RegistrarAsync(TorneoDbContext db, int torneoId, int partidoId,
        JugadorCuadro local, JugadorCuadro visitante, PartidoCuadro? previo = null)
    {
        var formulario = new ContentPage { Title = previo is null ? "Registrar resultado" : "Corregir marcador", BackgroundColor = Fondo };
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
            campo.WidthRequest = 80;
            campo.FontSize = 24;
            campo.HorizontalTextAlignment = TextAlignment.Center;
            Button Paso(string texto, int cambio, string descripcion)
            {
                // Fuera del Tab: el foco salta de un marcador al otro y luego a penaltis y botones.
                var boton = new Button { Text = texto, WidthRequest = 48, FontSize = 20, CornerRadius = 8, BackgroundColor = Superficie, TextColor = Claro };
                boton.HandlerChanged += (_, _) =>
                {
                    if (boton.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.Control nativo) nativo.IsTabStop = false;
                };
                SemanticProperties.SetDescription(boton, $"{descripcion} un gol a {jugador.NombreJugador}");
                boton.Clicked += (_, _) =>
                    campo.Text = Math.Max(0, (int.TryParse(campo.Text, out var goles) ? goles : 0) + cambio).ToString();
                return boton;
            }
            var fila = new HorizontalStackLayout { Spacing = 8 };
            fila.Add(Paso("−", -1, "Quitar"));
            fila.Add(campo);
            fila.Add(Paso("+", 1, "Sumar"));
            campos.Add(fila);
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
        if (previo is not null)
        {
            golesLocal.Text = previo.GolesLocal.ToString();
            golesVisitante.Text = previo.GolesVisitante.ToString();
            if (penaltis.IsVisible)
                penaltis.SelectedIndex = previo.GanadorId == local.Id ? 0 : 1;
        }
        // Enter en cualquiera de los marcadores equivale a pulsar «Revisar marcador».
        golesLocal.Completed += (_, _) => guardar.SendClicked();
        golesVisitante.Completed += (_, _) => guardar.SendClicked();
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
        // Esc cierra el formulario igual que «Cancelar».
        formulario.HandlerChanged += (_, _) =>
        {
            if (formulario.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement nativo) return;
            var esc = new Microsoft.UI.Xaml.Input.KeyboardAccelerator { Key = Windows.System.VirtualKey.Escape };
            esc.Invoked += (_, args) => { args.Handled = true; respuesta.TrySetResult(null); };
            nativo.KeyboardAccelerators.Add(esc);
        };
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
        var efecto = previo is null ? "" : "\nSi cambia el ganador, se sustituirá su plaza en el siguiente cruce.";
        if (await DisplayAlertAsync(previo is null ? "Confirmar resultado" : "Confirmar corrección",
            $"{Nombre(local)} {resultado.GolesLocal}–{resultado.GolesVisitante} {Nombre(visitante)}\nGanador: {Nombre(ganador == local.Id ? local : visitante)}{efecto}",
            "Confirmar", "Cancelar"))
        {
            var cuadro = new CuadroSqlite(db);
            if (previo is null)
                await cuadro.RegistrarResultadoAsync(torneoId, partidoId, resultado, CancellationToken.None);
            else
                await cuadro.CorregirResultadoAsync(torneoId, partidoId, resultado, CancellationToken.None);
        }
    }

    private static string Nombre(JugadorCuadro? jugador) =>
        jugador is null ? "Por determinar" : $"{jugador.NombreJugador} ({jugador.EquipoFC27})";

    private void Texto(string texto, double tamano) => _panel.Add(new Label { Text = texto, FontSize = tamano, TextColor = Claro });

    private void Titulo(string texto) =>
        _panel.Add(new Label { Text = texto, FontSize = 32, FontFamily = "OpenSansSemibold", TextColor = Claro });

    private void Seccionar(string texto) => _panel.Add(new Label
    {
        Text = texto.ToUpperInvariant(), FontSize = 14, CharacterSpacing = 2, FontFamily = "OpenSansSemibold",
        TextColor = Lima, Margin = new Thickness(0, 12, 0, 0)
    });

    private static Border Tarjeta(View contenido) => new()
    {
        BackgroundColor = Superficie, Stroke = Borde, StrokeThickness = 1, Padding = 14,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }, Content = contenido
    };

    private static Label Etiqueta(string texto, Color color) => new()
    {
        Text = texto.ToUpperInvariant(), FontSize = 11, CharacterSpacing = 1, FontFamily = "OpenSansSemibold",
        TextColor = color, HorizontalOptions = LayoutOptions.Start
    };

    private static Color ColorEstado(EstadoTorneo estado) =>
        estado == EstadoTorneo.EnCurso ? Lima : estado == EstadoTorneo.Borrador ? Oro : Atenuado;

    /// <summary>Fila de marcador: avatar con iniciales, jugador y equipo, y goles a la derecha.</summary>
    private static Grid FilaJugador(string nombre, string? equipo, int? goles, bool destacado)
    {
        var fila = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection(new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)),
            ColumnSpacing = 10
        };
        var iniciales = string.Concat(nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(p => char.ToUpperInvariant(p[0])));
        // Paleta fija y hash estable: el mismo jugador conserva su color entre sesiones.
        string[] paleta = ["#2BD46B", "#3FA7FF", "#F5C542", "#FF7A59", "#B48CFF", "#33D6C2"];
        var semilla = nombre.Aggregate(0, (suma, letra) => suma * 31 + letra);
        var avatar = new Border
        {
            WidthRequest = 32, HeightRequest = 32, StrokeThickness = 0,
            BackgroundColor = Color.FromArgb(paleta[(semilla & int.MaxValue) % paleta.Length]),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.Ellipse(),
            Content = new Label { Text = iniciales, FontSize = 12, FontFamily = "OpenSansSemibold", TextColor = Fondo,
                HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };
        SemanticProperties.SetDescription(avatar, string.Empty);
        var color = destacado ? Claro : Atenuado;
        var texto = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center };
        texto.Add(new Label { Text = nombre, TextColor = color, FontSize = 15, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation,
            FontFamily = destacado ? "OpenSansSemibold" : "OpenSansRegular" });
        if (equipo is not null)
            texto.Add(new Label { Text = equipo, TextColor = Atenuado, FontSize = 12, MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation });
        fila.Add(avatar);
        fila.Add(texto, 1);
        if (goles is int marcador)
            fila.Add(new Label { Text = marcador.ToString(), FontSize = 24, FontFamily = "OpenSansSemibold", TextColor = destacado ? Lima : Atenuado,
                VerticalOptions = LayoutOptions.Center }, 2);
        return fila;
    }

    private Entry Campo(string etiqueta)
    {
        Texto(etiqueta, 14);
        var campo = new Entry { BackgroundColor = Superficie, TextColor = Claro };
        SemanticProperties.SetDescription(campo, etiqueta);
        _panel.Add(campo);
        return campo;
    }

    private Label MensajeCampo()
    {
        var mensaje = new Label { TextColor = Color.FromArgb("#FFB4AB"), IsVisible = false };
        _panel.Add(mensaje);
        return mensaje;
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
