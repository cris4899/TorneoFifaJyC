namespace TorneoFifaJyC
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
#if WINDOWS
            Items.Clear();
            Items.Add(new ShellContent
            {
                Title = "Torneos",
                Route = "Gestion",
                ContentTemplate = new DataTemplate(typeof(Features.Gestion.GestionPage))
            });
#elif ANDROID
            // La consulta de demostración es la pantalla inicial del alumno.
            Items.Clear();
            Items.Add(new ShellContent
            {
                Title = "Cuadro",
                Route = "Cuadro",
                ContentTemplate = new DataTemplate(typeof(Features.Consulta.CuadroPage))
            });
#endif
        }
    }
}
