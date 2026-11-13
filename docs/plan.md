# Plan de implementación del Gestor FC27

**Objetivo:** entregar un torneo completo con administración Windows y consulta Android.

**Arquitectura:** dominio compartido, SQLite con EF Core en Windows y API LAN de lectura propuesta. La interfaz parte de la solución MAUI 10 existente.

**Especificaciones:** [requisitos](requisitos.md), [SDD](sdd.md), [interfaz](interfaz.md) y [pruebas](pruebas.md).

Los responsables, dependencias y bloques se mantienen en el [reparto](reparto.md). Abre una rama por tarea y aplica las [reglas de comentarios](comentarios.md). Registra el avance por tarea en GitHub y prepara el [relevo](relevo.md) en los puntos de entrega del bloque. Los siguientes pasos son trabajo previsto para cuando se autorice el desarrollo.

## 1. Acordar la base de trabajo

### FC27-00 · Documentación y decisiones

- [ ] Cris da acceso a Juan. Juan verifica acceso al repositorio y que puede trabajar en una rama propia.
- [ ] Cris cierra las observaciones sobre el contrato, correo, penaltis y correcciones. La prueba Android–Windows y la decisión de red pasan a FC27-06.
- [ ] Revisad y concretad campos, estados y errores de la consulta Android. Cris publica e integra el PR de documentación.

### FC27-01 · Solución y contratos

- [ ] Cris comprueba el entorno de ambos, incluidos SDK, workloads MAUI, Windows y emulador o dispositivo Android. Documenta los comandos de compilación y el resultado de ejecutarlos.
- [ ] Crea los proyectos Core, Data y pruebas en la solución. Si la topología acordada necesita API, reserva su proyecto y su integración en la solución. Mantén las referencias dirigidas hacia Core según el SDD.
- [ ] Reserva la configuración de red Android. Si se aprueba HTTP para pruebas locales, limita el tráfico en claro a la configuración de desarrollo y al destino previsto cuando sea posible. Mantén HTTPS como configuración normal de distribución.
- [ ] Define entidades, estados y contratos para altas, sorteo, resultado y consulta. Entrega datos de ejemplo acordados para que Android pueda avanzar. Reserva los puntos compartidos de navegación e inyección de dependencias.
- [ ] Compila los proyectos y comprueba los contratos. Cris publica e integra el PR y continúa con FC27-03 dentro del mismo turno.

## 2. Preparar SQLite, el cuadro y la consulta

Sigue el orden de los [turnos exclusivos](reparto.md). Cris termina FC27-03 antes de que Juan empiece FC27-02 y FC27-04.

### FC27-02 · Cuadro y resultados de dominio

- [ ] Juan añade pruebas de capacidades válidas, cupo incompleto, rondas, número de partidos y participación única.
- [ ] Implementa el sorteo y la posición de destino. Prueba con un orden controlable que los ganadores avanzan al lado correcto, incluso si se registran los partidos en orden inverso.
- [ ] Implementa la validación de goles y la resolución del ganador. Cubre marcador desigual, empate y final. Entrega a Cris el contrato y las pruebas de P04–P06 que correspondan al dominio.

### FC27-03 · Persistencia y altas

- [ ] Cris configura EF Core y SQLite, define claves y restricciones y prepara la migración inicial. Comprueba que una base nueva puede crearse y abrirse.
- [ ] Implementa alta de torneos y gestión de inscripciones en borrador. Prueba capacidad, campos obligatorios, duplicados, cambios tras inicio e integridad referencial con SQLite temporal.
- [ ] Prepara la proyección que usará la consulta. Comprueba que no incluye correos y acuerda con Juan los puntos de integración. Documenta la ruta de la base y la forma de preparar datos ficticios.

### FC27-04 · Android con datos ficticios

- [ ] Juan crea el listado y el cuadro. Añade selector de ronda, próximos cruces, resultados y campeón.
- [ ] Simula carga, lista vacía, cuadro válido, error, datos desactualizados y recuperación con el contrato acordado.
- [ ] Comprueba la vista en Android, con nombre largo y TalkBack. Indica en el relevo qué necesita del servicio real.

## 3. Integrar las plataformas

### FC27-05 · Administración Windows

- [ ] Cris conecta la creación de torneos y los formularios de inscripción con el acceso a datos. Muestra errores y el contador de plazas.
- [ ] Conecta el sorteo y el registro de resultados con el dominio. Guarda el cuadro y el avance en transacciones. Cubre fallos parciales, reintentos y confirmaciones concurrentes.
- [ ] Completa un torneo con penaltis y final. Reinicia y verifica que se conserva. Ejecuta P01–P08 y la parte Windows de P11. Entrega a Juan los datos de demo y cómo acceder a la fuente de consulta.

### FC27-06 · Consulta Android con datos reales

- [ ] Juan comprueba el acceso desde el móvil físico y el emulador al host de prueba y registra el resultado. Acordad la topología antes de conectar la consulta real.

- [ ] Juan implementa el servicio de lectura acordado y comprueba listado, cuadro, orden de rondas, respuesta de torneo no disponible y exclusión de correos.
- [ ] Conecta Android al servicio. Comprueba actualización periódica, reintento, cancelación al salir y recuperación al reconectar. Pide a Cris los cambios compartidos que hagan falta.
- [ ] Ejecutad P09–P11 en el entorno de demo y guardad tiempos y evidencias. Si se mantiene la propuesta LAN, el objetivo es ver el resultado confirmado en 10 segundos o menos.

## 4. Preparar y cerrar la entrega DAM

### FC27-07 · Documentación de consulta

- [ ] Juan actualiza diagramas, redacta el manual Android y recoge capturas con datos ficticios, incluido un fallo de conexión.
- [ ] Entrega a Cris las evidencias y la configuración reproducible de Android y del servicio de consulta.

### FC27-08 · Verificación y entrega final

- [ ] Cris prepara una instalación limpia desde la versión candidata y verifica con Juan la creación de la base, el arranque en Windows y la conexión Android.
- [ ] Recorred el [guion de demostración](pruebas.md) completo. Registrad el commit, los dispositivos, las pruebas y los defectos. Corregid en ramas de tarea y repetid los casos afectados.
- [ ] Cris completa análisis de mercado, infraestructura, manual Windows, esquema SQL, datos de ejemplo y bibliografía. Integra las aportaciones de Juan en la memoria.
- [ ] Confirmad que el torneo se puede crear, jugar y consultar hasta el campeón desde una instalación limpia. Identificad el commit verificado y adjuntad instrucciones de arranque, memoria y evidencias. Cris integra el último PR.
