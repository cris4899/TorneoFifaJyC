# Informe de pruebas Android · P09 y P10

Verificación de la consulta Android contra la API real ([P09 y P10](pruebas.md)).

## Versión y entorno

- **Commit base:** `master` 75c1ab4 (importación/exportación CSV incluida).
- **Entorno de ejecución:** contenedor Linux en la nube (sesión de la IA). SDK .NET **10.0.404**.
- **Qué se ejecutó aquí:** el servicio de lectura real `FC27.Api` (minimal API sobre `IConsultaTorneos`) sobre una base SQLite sembrada, y el **cliente Android real** `ConsultaTorneosHttp` más el modelo de estado `ModeloConsultaCuadro`, consultando por HTTP (`http://127.0.0.1`).
- **Fuera de alcance aquí:** el emulador **Pixel_8**, el móvil físico, la lectura con **TalkBack** y el objetivo de 10 s en la **red LAN real**. Este contenedor no tiene emulador Android ni el render de MAUI; esas comprobaciones quedan para un equipo Windows con el workload de Android (ver «Bloqueos»).

## Datos de prueba

Base sembrada con `FC27.Data`: torneo **«Copa DAM»** (4 plazas, iniciado) con un nombre largo (`Maximiliano Fernández-Larrañaga del Bosque`, 42 car.) y un segundo torneo **«Borrador QA»** en borrador (no debe ser visible).

## P09 · Consulta con API real (RF-06, RNF-01, RNF-03)

| Paso | Resultado observado |
| --- | --- |
| Listado de torneos (`GET /api/v1/torneos`) | 1 torneo: `Copa DAM / EnCurso`. El borrador **no aparece**. 172 ms. |
| Cuadro (`GET /api/v1/torneos/{id}/cuadro`) | Rondas en orden: **Semifinales → Final**. 108 ms. |
| Exclusión de correos | El cuerpo del cuadro **no contiene `email`**. |
| Nombre largo | El nombre de 42 caracteres **viaja íntegro** en la proyección. |
| Actualización tras cambio en Windows | Windows registra 2–1 en la semifinal 1; al reconsultar, Android ve `SF1 2-1 Jugado` y la **final recibe al ganador** como local. |

**P09: correcto** en la ruta cliente↔API. El objetivo de 10 s sobre LAN no se mide aquí (localhost); queda para el emulador/dispositivo.

## P10 · Lista vacía, desconexión y recuperación (RF-06, RF-07)

| Paso | Resultado observado |
| --- | --- |
| Con datos | Estado **Contenido** (Copa DAM). |
| Desconexión (cliente contra un extremo caído) | Estado **Desactualizado**, conserva la última vista (Copa DAM). |
| Reconexión | Estado **Contenido** de nuevo. |
| Torneo no disponible (borrador → 404) | Estado **Vacío** («no disponible»). |

**P10: correcto** en el modelo de estado contra la API real. La lista vacía, el error y el cuadro desactualizado se distinguen; al recuperar la conexión vuelve la versión reciente.

## Pruebas automáticas

- Afectadas (consulta/API/cliente/configuración): **27/27**.
- Suite completa en `master`: **70/70**. Con la regresión del flujo integrado (PR #27): **72/72**.

## Bloqueos (pendiente en equipo Windows + Android)

1. **Emulador Pixel_8 / móvil físico:** render de la pantalla, selección de torneo y reconexión *en la interfaz*, no ejecutables en este contenedor Linux.
2. **TalkBack:** lectura de cruces, estado y ganador.
3. **Objetivo de 10 s en LAN real** (RNF-03) con el móvil y el Windows del gestor en la misma red.
4. **Legibilidad visual** del nombre largo en la tarjeta (a nivel de datos viaja íntegro; el recorte en pantalla depende del render).
