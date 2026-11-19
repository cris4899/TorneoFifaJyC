#if WINDOWS
using FC27.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace TorneoFifaJyC.Features.Gestion;

/// <summary>Organizador con la sesión abierta en esta ventana. Se pierde al cerrar la app.</summary>
public static class SesionOrganizador
{
    public static int? Id { get; set; }
    public static string? Usuario { get; set; }
}

/// <summary>
/// Entrada de organizadores: usuario y contraseña, o alta con un código de invitación.
/// El primer organizador se da de alta con el código maestro de la variable FC27_CODIGO_MAESTRO.
/// </summary>
public sealed class LoginPage : ContentPage
{
    private static Color Recurso(string clave) => (Color)Application.Current!.Resources[clave];

    private readonly DbContextOptions<TorneoDbContext> _options;
    private readonly Action _entrar;
    private readonly Entry _usuario = new() { Placeholder = "Usuario" };
    private readonly Entry _contrasena = new() { Placeholder = "Contraseña", IsPassword = true };
    private readonly Entry _invitacion = new() { Placeholder = "Código de invitación", IsVisible = false };
    private readonly Label _error = new() { TextColor = Recurso("Oro"), IsVisible = false };
    private readonly Button _accion = new() { Text = "Entrar", CornerRadius = 8, MinimumHeightRequest = 48 };
    private readonly Button _alternar = new() { Text = "Tengo una invitación", BackgroundColor = Colors.Transparent, MinimumHeightRequest = 48 };
    private bool _alta;

    public LoginPage(Action entrar)
    {
        _entrar = entrar;
        Title = "Organizadores";
        BackgroundColor = Recurso("Fondo");
        var conexion = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(FileSystem.AppDataDirectory, "fc27.db"),
            ForeignKeys = true
        };
        _options = new DbContextOptionsBuilder<TorneoDbContext>().UseSqlite(conexion.ToString()).Options;

        foreach (var campo in new[] { _usuario, _contrasena, _invitacion })
        {
            campo.BackgroundColor = Recurso("Superficie");
            campo.TextColor = Recurso("Claro");
            campo.PlaceholderColor = Recurso("Atenuado");
            SemanticProperties.SetDescription(campo, campo.Placeholder);
        }
        _accion.BackgroundColor = Recurso("Verde");
        _accion.TextColor = Recurso("Fondo");
        _alternar.TextColor = Recurso("Claro");
        _accion.Clicked += async (_, _) => await EnviarAsync();
        _alternar.Clicked += (_, _) =>
        {
            _alta = !_alta;
            _invitacion.IsVisible = _alta;
            _accion.Text = _alta ? "Crear cuenta" : "Entrar";
            _alternar.Text = _alta ? "Ya tengo cuenta" : "Tengo una invitación";
            _error.IsVisible = false;
        };

        Content = new VerticalStackLayout
        {
            MaximumWidthRequest = 420, VerticalOptions = LayoutOptions.Center, Padding = 24, Spacing = 12,
            Children =
            {
                new Label { Text = "FC27 · Organizadores", FontSize = 28, FontFamily = "OpenSansSemibold", TextColor = Recurso("Claro") },
                _usuario, _contrasena, _invitacion, _error, _accion, _alternar
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await using var db = new TorneoDbContext(_options);
        await db.Database.MigrateAsync();
    }

    private async Task EnviarAsync()
    {
        _accion.IsEnabled = false;
        try
        {
            await using var db = new TorneoDbContext(_options);
            var auth = new AutenticacionSqlite(db);
            FC27.Core.Organizador? organizador;
            if (_alta)
            {
                var codigo = _invitacion.Text?.Trim() ?? "";
                // El código maestro solo vale mientras no exista ningún organizador.
                var maestro = Environment.GetEnvironmentVariable("FC27_CODIGO_MAESTRO");
                if (!string.IsNullOrEmpty(maestro) && codigo == maestro && !await db.Organizadores.AnyAsync())
                    codigo = await auth.CrearInvitacionAsync(CancellationToken.None);
                organizador = await auth.RegistrarAsync(_usuario.Text ?? "", _contrasena.Text ?? "", codigo, CancellationToken.None);
            }
            else
            {
                organizador = await auth.EntrarAsync(_usuario.Text ?? "", _contrasena.Text ?? "", CancellationToken.None);
                if (organizador is null) throw new InvalidOperationException("Usuario o contraseña incorrectos.");
            }
            SesionOrganizador.Id = organizador.Id;
            SesionOrganizador.Usuario = organizador.Usuario;
            _contrasena.Text = "";
            _entrar();
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or DbUpdateException)
        {
            _error.Text = e is DbUpdateException ? "Ese usuario ya existe." : e.Message;
            _error.IsVisible = true;
        }
        finally
        {
            _accion.IsEnabled = true;
        }
    }
}
#endif
