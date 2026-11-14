# Consulta Android

Pantallas, modelos de vista y cliente de consulta del alumno (Juan).

## Entregado en FC27-04

Lógica de presentación sin dependencia de MAUI, probada en `tests/FC27.Tests/` con los datos ficticios de `cuadro-ejemplo.json` (20 pruebas, `dotnet test`):

- `ConsultaCuadro.cs`
  - `CuadroVista.Desde`: arma la vista desde el contrato `CuadroTorneo`. `Proximos` son los cruces pendientes con las dos plazas asignadas (RF-06).
  - `ModeloConsultaCuadro`: estados Cargando, Contenido, Vacío, Error y Desactualizado; conserva la última vista válida y la recupera al reconectar.
  - `ListadoConsulta`: distingue lista vacía de contenido.
- `CuadroPageViewModel.cs`: modelo de vista enlazable (`INotifyPropertyChanged`). Selector de ronda, filas de cruce accesibles (`PartidoFila`), campeón y comando «Reintentar». `ComandoAsync` es un `ICommand` mínimo.
- `DatosFicticiosConsulta.cs`: cuadro de ejemplo en memoria y fuente de datos para la página.

## Página MAUI

- `CuadroPage.xaml` + `CuadroPage.xaml.cs`: enlazan `CuadroPageViewModel`. Cabecera con nombre y estado, `Picker` de ronda, `CollectionView` de cruces con marcador y ganador, aviso de carga/vacío/error, campeón y botón «Reintentar». Accesibilidad con `SemanticProperties` (encabezados, descripción por cruce, pista del botón). XML bien formado (`xmllint`).

## Pendiente

- **Registro en navegación (Cris, archivos compartidos):** registrar `CuadroPage` en `AppShell.xaml` o su ruta y, si se usa DI, en `MauiProgram.cs`.
- **Verificación en Android (requiere Windows con workload de Android y emulador o dispositivo):** render de la página, nombre largo y lectura con TalkBack. No ejecutable en este contenedor Linux.
- **Cliente de consulta real:** lo aporta FC27-06 contra el servicio de lectura; sustituye `DatosFicticiosConsulta.CargarAsync` por la llamada al servicio.
