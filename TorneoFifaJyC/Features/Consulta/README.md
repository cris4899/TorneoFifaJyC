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

## Datos reales (FC27-06)

- `ConsultaTorneosHttp.cs`: cliente `IConsultaTorneos` sobre `HttpClient` que consume el servicio de lectura `FC27.Api` (`GET /api/v1/torneos`, `GET /api/v1/torneos/{id}/cuadro`). Traduce 404 a «no disponible» (null). Probado con un handler simulado.
- `ConsultaAndroid.CrearCargador(torneoId, baseAddress)`: usa el servicio real cuando hay dirección base; con `null` sirve los datos ficticios de FC27-04.
- `CuadroPage` (`ConsultaConfig`): consulta al abrir, repite cada `Intervalo` (5 s) con `PeriodicTimer`, mantiene una sola consulta en curso y la cancela al salir (`OnDisappearing`).

El servicio de lectura vive en `src/FC27.Api` (minimal API sobre `IConsultaTorneos`). Pruebas de integración en `tests/FC27.Tests/ApiConsultaTests.cs` con SQLite temporal: listado visible, cuadro ordenado, 404 de borrador/inexistente y exclusión de correos.

## Pendiente

- **Alta de `src/FC27.Api` en la solución (Cris):** `TorneoFifaJyC.slnx` es suyo; el proyecto está creado y compila, falta registrarlo en la solución.
- **Registro de `CuadroPage` en navegación (Cris, archivos compartidos):** `AppShell.xaml` y, si se usa DI, `MauiProgram.cs`.
- **Validación de red Windows–Android y P09–P11 (requiere la demo real):** confirmar que el móvil alcanza el Windows del gestor y cronometrar la actualización (objetivo 10 s) antes de fijar la topología LAN. Fijar `ConsultaConfig.BaseAddress` con la IP del gestor.
- **Verificación en Android (emulador o dispositivo):** render, nombre largo y TalkBack. No ejecutable en este contenedor Linux.
