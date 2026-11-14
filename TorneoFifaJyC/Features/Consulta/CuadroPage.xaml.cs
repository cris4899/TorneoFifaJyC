namespace TorneoFifaJyC.Features.Consulta;

/// <summary>
/// Pantalla de consulta del cuadro para Android. Se apoya en
/// <see cref="CuadroPageViewModel"/> y, en FC27-04, carga datos ficticios.
/// FC27-06 sustituirá la fuente por el servicio de lectura real.
/// </summary>
public partial class CuadroPage : ContentPage
{
    private readonly CuadroPageViewModel _modelo;

    public CuadroPage() : this(new CuadroPageViewModel(DatosFicticiosConsulta.CargarAsync))
    {
    }

    public CuadroPage(CuadroPageViewModel modelo)
    {
        InitializeComponent();
        _modelo = modelo;
        BindingContext = _modelo;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Consulta al abrir y al volver al primer plano.
        await _modelo.ActualizarAsync();
    }
}
