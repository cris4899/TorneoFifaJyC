# Plan de mejoras FC27

El responsable del turno se confirma en el último relevo publicado en GitHub. Las mejoras de interfaz, cuadro, corrección y CSV están integradas; queda cerrar la aceptación y la entrega con evidencias verificables.

## Primera tanda

1. **Cris · Gestión Windows.** Separar Mis torneos del detalle. Organizar el detalle en Inscritos, Partidos y Cuadro. Reunir ambos marcadores en un formulario y mostrar la opción de penaltis en empate. Aplicar fondo grafito, superficies diferenciadas, texto claro y lima para acciones principales. Mantener la navegación por teclado y los estados escritos.
2. **Juan · Consulta Android.** Aplicar el estilo compartido. Configurar y guardar la dirección de la API desde la app, probar la conexión y elegir un torneo iniciado o finalizado. Identificar el modo demo. Mostrar Próximos, Rondas y la hora de la última actualización correcta.

Cris entrega el turno tras publicar y verificar el bloque Windows. El relevo incluye el PR, las decisiones visuales y las comprobaciones. Juan devuelve el turno con su PR y pruebas Android. Solo Cris fusiona a `master`.

## Mejoras posteriores, en orden

1. Cuadro visual en Windows con rondas conectadas y consulta por ronda en Android.
2. Corrección de marcadores mientras el cruce siguiente esté pendiente, con confirmación del efecto sobre el cuadro. Si ya se jugó, explicar el bloqueo.
3. Importación CSV con vista previa y detección de duplicados, y exportación de cuadro y resultados.

## Comprobaciones

- **Windows:** crear torneo, gestionar inscritos, sortear, registrar un empate con penaltis y comprobar el campeón tras reiniciar. Verificar teclado y nombres largos.
- **Android:** elegir torneo, guardar y probar conexión, comprobar el modo demo, la última actualización y la recuperación tras desconexión. Probar el cuadro con nombres largos en el emulador. TalkBack queda fuera del alcance de las pruebas de entrega.

## Archivos CSV en Windows

En Inscritos, abre «Importar inscripciones CSV» en un torneo en borrador. El archivo usa UTF-8, cabecera nombre,correo,equipo y coma o punto y coma como separador. Revisa la vista previa y confirma. Si faltan plazas o se repite un correo en el archivo o en el torneo, se rechaza el lote completo.

En Partidos o Cuadro, abre «Exportar cuadro y resultados CSV» y elige dónde guardar. El archivo incluye un cruce por fila, los marcadores confirmados y el ganador. Los cruces pendientes dejan el marcador en blanco.

## Estado comprobado el 15 de noviembre de 2026

- En `master` (`75c1ab4`), 6 pruebas afectadas de CSV y resultados y 70 pruebas de la suite pasaron; la compilación Windows terminó con 0 errores y 0 avisos.
- P02 reveló en Windows un diálogo técnico en inglés al intentar añadir una inscripción vacía. La corrección está propuesta en el [PR #29](https://github.com/cris4899/TorneoFifaJyC/pull/29): formulario vacío, correo inválido, alta válida y duplicado con distintas mayúsculas comprobados manualmente; 70 pruebas y compilación Windows pasan también en esa rama.
- P03 se completó después en la aceptación Windows: sorteo, marcadores, penaltis, corrección, reinicio y exportación ([informe Windows](pruebas-windows.md)).
- El selector CSV de Windows se abrió, pero el diálogo del sistema no fue controlable por la automatización; la prueba manual previa está registrada en el PR #26.
- La aplicación Android compiló con 0 errores y 0 avisos. En emulador Pixel_8, el APK completo abrió la vista de consulta en modo demo: se vieron «Copa DAM», la hora de actualización, Semifinales, un marcador jugado y otro cruce pendiente. La instalación directa del primer APK de depuración no incluía las DLL de despliegue rápido; se repitió con `EmbedAssembliesIntoApk=true` y la pantalla abrió. P09/P10 con API real y desconexión se completaron después: 7 s de Windows a Android y recuperación en 4,4 s ([informe Windows](pruebas-windows.md)).

## Bloque de aceptación y entrega (completado)

Completado el 15 de noviembre de 2026 con los PR #31 a #34. Defectos abiertos y limitaciones: [entrega](entrega.md#defectos-abiertos-y-limitaciones).

Las comprobaciones de código y la prueba manual de Windows del PR #26 no sustituyen las pruebas de aceptación de `docs/pruebas.md`. Trabajad en bloques completos según `docs/relevo.md` y registrad únicamente resultados ejecutados.

### Turno de Juan · regresión y consulta Android

1. Incorporar `tests/FC27.Tests/FlujoIntegradoTests.cs` desde una rama basada en `master`. El PR debe cubrir rechazo total del CSV duplicado, propagación del ganador corregido y exportación del marcador corregido. Ejecutar primero esas pruebas y luego `dotnet test` una vez para el lote.
2. Verificar en emulador Android, si está disponible, selección de torneo, dirección de API guardada, modo demo, ronda, nombres largos, hora de actualización y reconexión. Cubrir los casos P09 y P10 de `docs/pruebas.md` y registrar versión, dispositivo, pasos y resultado observado. Si el entorno no permite arrancar el emulador o conectar con la API, indicar el bloqueo concreto y ejecutar las comprobaciones de API o cliente que sí estén disponibles.
3. Publicar en el mismo turno el PR de regresión y la evidencia Android en su descripción o en documentación de pruebas, además de los fallos reproducibles que encuentre. Devolver a Cris los enlaces y los pasos pendientes de dispositivo; no declarar superada una prueba que no se ejecutó.

### Turno de Cris · aceptación Windows e integración

1. Revisar y fusionar, si está listo, el PR de regresión. Ejecutar las comprobaciones afectadas y una suite completa del lote.
2. Completar las pruebas Windows pendientes de `docs/pruebas.md`: creación y validación de inscritos, sorteo, marcadores y penaltis, corrección, reinicio, navegación por teclado y CSV. Registrar el resultado por versión y dispositivo, con capturas sin correos.
3. Ejecutar con Juan o con los medios disponibles la comunicación Windows–Android de P09 y P10, incluida pérdida y recuperación de conexión. Registrar el tiempo observado frente al objetivo de 10 s. La demo final en móvil físico requiere el dispositivo y la red real; si faltan, dejar ese requisito explícitamente pendiente.
4. Actualizar la documentación de entrega con las pruebas realmente ejecutadas, defectos abiertos y limitaciones. Cerrar la entrega solo tras comprobar los criterios pendientes; TalkBack permanece fuera del alcance acordado.
