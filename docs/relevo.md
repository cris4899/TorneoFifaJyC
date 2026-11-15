# Relevo entre Cris y Juan

## Cuándo generar el mensaje

1. Comprueba quién tiene el turno en el último relevo de GitHub. Al empezar, registra «Turno activo: @usuario» en el issue o PR del relevo.
2. Completa las tareas del [turno](reparto.md). Al terminar, o si el compañero debe resolver un bloqueo, registra «Turno entregado a @usuario» con los PR, pruebas y siguiente paso. Genera un único prompt para que se lo pasen al compañero.
3. El destinatario empieza tras recibir el prompt y verificar el relevo en GitHub. Quien lo entrega espera su siguiente turno. Al cerrar el proyecto, el último prompt comunica la entrega y sus referencias.

Un PR abierto permite revisar y preparar la siguiente tarea. Su fusión permite partir de ese trabajo en una rama nueva. El estado y las pruebas se escriben según lo observado.

## Alcance de cada turno

- Antes de entregar el turno, agrupa las acciones consecutivas y desbloqueadas del mismo responsable en un bloque con entregables comprobables. Incluye implementación o pruebas, verificación y documentación del resultado cuando correspondan.
- No entregues el turno solo para pedir una decisión, añadir una prueba aislada o comunicar un resultado si puedes completar más trabajo autorizado del mismo bloque. Si aparece un bloqueo, avanza en las tareas independientes disponibles y registra el impedimento exacto.
- El relevo enumera todas las acciones del siguiente bloque en orden, quién las ejecuta, qué depende de una fusión o de un dispositivo y qué evidencia cierra cada acción. Ejecuta primero las pruebas afectadas y la suite completa una vez por lote; comunica solo resultados observados.
- Un turno puede acabar con una sola acción cuando no exista otra tarea autorizada y disponible, haya un bloqueo que impida avanzar o la entrega esté realmente cerrada. Explica cuál de estos casos aplica. No inventes trabajo para cumplir una cuota.
- La persona que recibe el turno comprueba el estado real antes de actuar. Si cambia el plan, registra el nuevo alcance en GitHub y sustituye explícitamente el relevo anterior. Solo trabaja una persona a la vez.

## Formato de salida

**Pásale este prompt a tu IA**

```text
Trabajas en cris4899/TorneoFifaJyC como @DESTINATARIO.
Abre index.md y AGENTS.md. Tu bloque, en orden, es:
1. [Primera acción y entregable comprobable].
2. [Siguiente acción desbloqueada y entregable comprobable].
3. [Verificación, documentación y publicación del lote, si procede].

Contexto: @AUTOR ha dejado FC27-XX [local / PR abierto / fusionado / bloqueado].
Referencia: [URL del PR, rama o commit disponible].
Cambios que puedes usar: [rutas, contrato y decisiones acordadas].
Comprobaciones: [comando y resultado observado, o motivo de no ejecución].
Dependencias y medios: [qué requiere fusión, dispositivo o intervención humana; o ninguna].
Cierre del bloque: [PR, evidencias y resultado esperados].

Comprueba el estado real del PR y de master. Si falta la fusión de una
dependencia, limita el trabajo dependiente a revisión o preparación y
avanza en una tarea independiente autorizada cuando exista. Si está lista,
parte de master actualizado, usa una rama propia y sigue los pasos y las
pruebas de docs/plan.md. Respeta las rutas de docs/estructura.md y las reglas
de comentarios de docs/comentarios.md. Solo cris4899 fusiona a master.

Al cerrar el turno o necesitar que el compañero resuelva un bloqueo, devuelve un mensaje
«Pásale este prompt a tu IA» con las referencias y el siguiente paso.
```

La IA que genera el relevo sustituye los marcadores por hechos concretos. Si falta un dato, escribe qué falta y cómo verificarlo.


## Primer relevo de desarrollo para Juan

Cris entrega este prompt cuando FC27-01 y FC27-03 estén en master.
Completa las referencias antes de enviarlo.

**Pásale este prompt a tu IA**

```text
Soy juanrr92. He recibido el turno de Cris en cris4899/TorneoFifaJyC.
Haz esto: verifica mi cuenta y que FC27-01 y FC27-03 estén en master.
Lee index.md y completa FC27-02 en una rama propia. Abre su PR con las pruebas.
Luego esto: prepara FC27-04 con datos ficticios en otra rama y abre su PR.
La prueba de red se hará en FC27-06.
Prompt de relevo: devuelve a cris4899 los enlaces a ambos PR, los resultados
de las pruebas y el siguiente paso, FC27-05. Espera el siguiente turno.
```
