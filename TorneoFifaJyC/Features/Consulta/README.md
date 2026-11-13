# Consulta Android

Juan implementará las pantallas, modelos de vista y cliente de consulta.

## Entregado en FC27-04

- `ConsultaCuadro.cs`: lógica de presentación sin dependencia de MAUI, probada en `tests/FC27.Tests/ConsultaCuadroTests.cs` con los datos ficticios de `cuadro-ejemplo.json`.
  - `CuadroVista.Desde`: arma la vista desde el contrato `CuadroTorneo`. `Proximos` son los cruces pendientes con las dos plazas asignadas (RF-06).
  - `ModeloConsultaCuadro`: estados Cargando, Contenido, Vacío, Error y Desactualizado; conserva la última vista válida y la recupera al reconectar.
  - `ListadoConsulta`: distingue lista vacía de contenido.

## Pendiente (requiere Windows/Android)

- Página MAUI (`CuadroPage.xaml` + code-behind) que enlaza `ModeloConsultaCuadro`: selector de ronda, tarjetas de cruce, marcador, ganador y campeón, con botón «Reintentar» en Error/Desactualizado.
- Verificación en Android (emulador o dispositivo): nombre largo y lectura con TalkBack.
- Cliente de consulta real: lo aporta FC27-06 contra el servicio de lectura.
