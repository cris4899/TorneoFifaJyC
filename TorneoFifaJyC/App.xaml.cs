using Microsoft.Extensions.DependencyInjection;

namespace TorneoFifaJyC
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
#if WINDOWS
            // En Windows se gestiona con cuenta: primero el login, luego la gestión.
            var ventana = new Window();
            ventana.Page = new Features.Gestion.LoginPage(() => ventana.Page = new AppShell());
            return ventana;
#else
            return new Window(new AppShell());
#endif
        }
    }
}