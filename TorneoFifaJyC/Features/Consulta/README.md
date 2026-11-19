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
- El servicio de lectura vive en `src/FC27.Api` (minimal API sobre `IConsultaTorneos`). Pruebas de integración en `tests/FC27.Tests/ApiConsultaTests.cs` con SQLite temporal: listado visible, cuadro ordenado, 404 de borrador/inexistente y exclusión de correos.

## Mejoras de la consulta (configurable y estilo)

Estilo compartido grafito y lima (`#151719`, `#24282C`, `#F2F4F5`, `#C6F653`).

- `ConfiguracionConsulta.cs`: guarda la dirección del servicio por dispositivo (`IAlmacenPreferencias`, en Android MAUI Preferences), valida la URL e identifica el **modo demo** (sin dirección). `ProbadorConexion` prueba la conexión con el listado. `OrigenConsulta.Crear` compone el servicio real o `ConsultaDemo`.
- `CuadroPageViewModel`: elige un torneo iniciado o finalizado (`TorneosDisponibles`, `SeleccionarTorneoAsync`), alterna **Rondas** y **Próximos** (`VerRondasComando`/`VerProximosComando`), muestra la **hora de la última actualización correcta** (`UltimaActualizacionTexto`) y marca el modo demo. Recibe un reloj inyectable para las pruebas.
- `CuadroPage.xaml`/`.cs`: ajustes de API (dirección, «Probar conexión», «Guardar»), selector de torneo, aviso de modo demo, pestañas Rondas/Próximos, última actualización y el estilo grafito/lima. Consulta al abrir, repite cada 5 s (`PeriodicTimer`) y cancela al salir.

Pruebas (`dotnet test`: 64 correctas): modelo de vista (selección de torneo, Próximos/Rondas, última actualización, desactualizado y recuperación), `ConfiguracionConsulta` (persistencia, validación, modo demo) y `ProbadorConexion`.

## Pendiente

- **Alta de `src/FC27.Api` en la solución (Cris):** `TorneoFifaJyC.slnx` es suyo; el proyecto compila, falta registrarlo.
- **Registro de `CuadroPage` en navegación (Cris, archivos compartidos):** `AppShell.xaml` y, si se usa DI, `MauiProgram.cs`.
- **Validación de red Windows–Android y P09–P11 (demo real):** confirmar que el móvil alcanza el Windows del gestor y cronometrar la actualización (objetivo 10 s) antes de fijar la topología LAN.
- **Verificación en Android (emulador o dispositivo):** selección de torneo, guardar/probar conexión, modo demo, recuperación tras desconexión, nombre largo y TalkBack. No ejecutable en este contenedor Linux.
