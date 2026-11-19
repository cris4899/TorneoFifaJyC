namespace TorneoFifaJyC.Controls;

/// <summary>Una opción del menú: texto, acción y si es la sección actual.</summary>
public sealed record OpcionMenu(string Texto, Action Accion, bool Activa = false, bool Habilitada = true);

/// <summary>
/// Botón flotante que despliega las opciones de navegación hacia arriba.
/// Se coloca en la esquina inferior derecha de un Grid, sin cubrir el resto de la pantalla.
/// </summary>
public sealed class MenuFlotante : ContentView
{
    private static Color Recurso(string clave) => (Color)Application.Current!.Resources[clave];

    private readonly VerticalStackLayout _opciones = new() { Spacing = 8, IsVisible = false, HorizontalOptions = LayoutOptions.End };
    private readonly Button _boton;
    private IReadOnlyList<OpcionMenu> _lista = [];

    public MenuFlotante()
    {
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.End;
        Margin = new Thickness(0, 0, 20, 20);
        _boton = new Button
        {
            Text = "☰", FontSize = 22, WidthRequest = 56, HeightRequest = 56, CornerRadius = 28, Padding = 0,
            BackgroundColor = Recurso("Verde"), TextColor = Recurso("Fondo"), HorizontalOptions = LayoutOptions.End
        };
        SemanticProperties.SetDescription(_boton, "Menú de navegación");
        SemanticProperties.SetHint(_boton, "Abre las secciones de la aplicación");
        _boton.Clicked += (_, _) => Alternar();
        var pila = new VerticalStackLayout { Spacing = 12, HorizontalOptions = LayoutOptions.End };
        pila.Add(_opciones);
        pila.Add(_boton);
        Content = pila;
    }

    /// <summary>Sustituye las opciones y cierra el menú.</summary>
    public void Establecer(IEnumerable<OpcionMenu> opciones)
    {
        _lista = opciones.ToList();
        _opciones.Clear();
        foreach (var opcion in _lista)
        {
            var pastilla = new Button
            {
                Text = opcion.Texto, CornerRadius = 20, MinimumHeightRequest = 44, Padding = new Thickness(18, 0),
                IsEnabled = opcion.Habilitada,
                BackgroundColor = opcion.Activa ? Recurso("Verde") : Recurso("Superficie"),
                TextColor = opcion.Activa ? Recurso("Fondo") : Recurso("Claro"),
                BorderColor = Recurso("Borde"), BorderWidth = 1, HorizontalOptions = LayoutOptions.End
            };
            if (opcion.Activa) SemanticProperties.SetHint(pastilla, "Sección actual");
            pastilla.Clicked += (_, _) =>
            {
                Cerrar();
                opcion.Accion();
            };
            _opciones.Add(pastilla);
        }
        Cerrar();
    }

    private void Alternar()
    {
        if (_opciones.IsVisible) Cerrar();
        else if (_lista.Count > 0) Abrir();
    }

    private void Abrir()
    {
        _opciones.IsVisible = true;
        _boton.Text = "✕";
    }

    private void Cerrar()
    {
        _opciones.IsVisible = false;
        _boton.Text = "☰";
    }
}
