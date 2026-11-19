using System.Net;
using System.Net.Http.Json;
using FC27.Core;

namespace TorneoFifaJyC.Features.Consulta;

/// <summary>Inscripción del alumno con el código del torneo. No hay cuenta de alumno.</summary>
public sealed class InscripcionPage : ContentPage
{
    private static Color Recurso(string clave) => (Color)Application.Current!.Resources[clave];

    private readonly ConfiguracionConsulta _config;
    private readonly Entry _codigo = new() { Placeholder = "Código del torneo (FC27-XXXX)", Keyboard = Keyboard.Create(KeyboardFlags.None) };
    private readonly Entry _nombre = new() { Placeholder = "Nombre" };
    private readonly Entry _correo = new() { Placeholder = "Correo", Keyboard = Keyboard.Email };
    private readonly Entry _equipo = new() { Placeholder = "Equipo FC27" };
    private readonly Label _mensaje = new() { IsVisible = false };
    private readonly Button _enviar = new() { Text = "Inscribirme", CornerRadius = 8, MinimumHeightRequest = 48 };

    public InscripcionPage(ConfiguracionConsulta config)
    {
        _config = config;
        Title = "Inscribirme";
        BackgroundColor = Recurso("Fondo");
        foreach (var campo in new[] { _codigo, _nombre, _correo, _equipo })
        {
            campo.BackgroundColor = Recurso("Superficie");
            campo.TextColor = Recurso("Claro");
            campo.PlaceholderColor = Recurso("Atenuado");
            SemanticProperties.SetDescription(campo, campo.Placeholder);
        }
        _enviar.BackgroundColor = Recurso("Verde");
        _enviar.TextColor = Recurso("Fondo");
        _enviar.Clicked += async (_, _) => await EnviarAsync();
        var volver = new Button { Text = "Volver", BackgroundColor = Colors.Transparent, TextColor = Recurso("Claro"), MinimumHeightRequest = 48 };
        volver.Clicked += async (_, _) => await Navigation.PopModalAsync();

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24, Spacing = 12,
                Children =
                {
                    new Label { Text = "Inscribirme en un torneo", FontSize = 24, FontFamily = "OpenSansSemibold", TextColor = Recurso("Claro") },
                    _codigo, _nombre, _correo, _equipo, _mensaje, _enviar, volver
                }
            }
        };
    }

    private async Task EnviarAsync()
    {
        if (_config.BaseAddress is not { } baseUri)
        {
            Avisar("Configura la dirección del servicio en Ajustes para inscribirte.", error: true);
            return;
        }
        var codigo = _codigo.Text?.Trim() ?? "";
        if (codigo.Length == 0 || string.IsNullOrWhiteSpace(_nombre.Text) || string.IsNullOrWhiteSpace(_correo.Text) || string.IsNullOrWhiteSpace(_equipo.Text))
        {
            Avisar("Rellena todos los campos.", error: true);
            return;
        }

        _enviar.IsEnabled = false;
        try
        {
            using var http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromSeconds(10) };
            var respuesta = await http.PostAsJsonAsync($"api/v1/inscripciones/{Uri.EscapeDataString(codigo)}",
                new DatosInscripcion(_nombre.Text.Trim(), _correo.Text.Trim(), _equipo.Text.Trim()));
            switch (respuesta.StatusCode)
            {
                case HttpStatusCode.Created: Avisar("¡Inscrito! Sigue el torneo desde Próximos y Rondas.", error: false); break;
                case HttpStatusCode.NotFound: Avisar("No existe ese torneo o la inscripción ya está cerrada.", error: true); break;
                default: Avisar("No se pudo inscribir: el torneo está completo o ya estás apuntado.", error: true); break;
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Avisar("No se pudo conectar con el servicio.", error: true);
        }
        finally
        {
            _enviar.IsEnabled = true;
        }
    }

    private void Avisar(string texto, bool error)
    {
        _mensaje.Text = texto;
        _mensaje.TextColor = Recurso(error ? "Oro" : "Verde");
        _mensaje.IsVisible = true;
    }
}
