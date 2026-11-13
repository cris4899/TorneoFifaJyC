# Relevo entre Cris y Juan

## Cuándo generar el mensaje

1. Al terminar una tarea, indica si el cambio está local, en un PR abierto o fusionado. Identifica la primera tarea que queda habilitada y a quién corresponde.
2. Si una dependencia te bloquea, indica qué PR, archivo o decisión falta y quién puede desbloquearla. Si tienes otra tarea disponible, puedes seguir con ella después de preparar el relevo.
3. Entrega el bloque listo para pegar. Si hay PR, añade el mismo relevo a su descripción cuando estés autorizado a editarla. Para un bloqueo sin PR, conserva el relevo en el canal de coordinación que use el equipo.

Un PR abierto permite revisar y preparar la siguiente tarea. Su fusión permite partir de ese trabajo en una rama nueva. El estado y las pruebas se escriben según lo observado.

## Formato de salida

**Pásale este prompt a tu IA**

```text
Trabajas en cris4899/TorneoFifaJyC como @DESTINATARIO.
Abre index.md y AGENTS.md. Tu siguiente acción es [revisar, desbloquear o
continuar FC27-XX]. El resultado esperado es [entregable comprobable].

Contexto: @AUTOR ha dejado FC27-XX [local / PR abierto / fusionado / bloqueado].
Referencia: [URL del PR, rama o commit disponible].
Cambios que puedes usar: [rutas, contrato y decisiones acordadas].
Comprobaciones: [comando y resultado observado, o motivo de no ejecución].
Dependencia pendiente: [qué falta y quién debe resolverlo, o ninguna].

Comprueba el estado real del PR y de master. Si falta la fusión de una
dependencia, limita el trabajo dependiente a revisión o preparación y
avanza en una tarea independiente autorizada cuando exista. Si está lista,
parte de master actualizado, usa una rama propia y sigue los pasos y las
pruebas de docs/plan.md. Respeta las rutas de docs/estructura.md y las reglas
de comentarios de docs/comentarios.md. Solo cris4899 fusiona a master.

Al cerrar la tarea o detectar otro bloqueo, devuelve un nuevo mensaje
«Pásale este prompt a tu IA» con las referencias y el siguiente paso.
```

La IA que genera el relevo sustituye los marcadores por hechos concretos. Si falta un dato, escribe qué falta y cómo verificarlo.

## Primer relevo listo para Juan

**Pásale este prompt a tu IA**

```text
Colaboras como juanrr92 en https://github.com/cris4899/TorneoFifaJyC.
Ayúdame a revisar y desbloquear FC27-00 junto con cris4899. Comprueba si tengo
acceso y si index.md y docs/plan.md están en master o en el PR de documentación.
Si aún están solo en local, pídele a Cris el enlace cuando publique el PR.

Cuando puedas leerlos, revisa docs/sdd.md y prepara para Cris una propuesta
breve sobre cómo conectar Android al Windows de la demo, qué necesita el
contrato de lectura y cómo probarlo en la red del centro. Señala las decisiones
que requieren acuerdo. Después de que Cris fusione FC27-01, mi primera
tarea de desarrollo es FC27-02 (cuadro), seguida de FC27-04 (Android con
datos ficticios). En esta fase documental, mantén el trabajo en revisión
y preparación. Solo cris4899 fusiona los PR a master.

Al terminar la revisión o si te falta un acceso o un PR, devuelve un mensaje
«Pásale este prompt a tu IA» dirigido a la IA de Cris. Incluye la evidencia,
la decisión pendiente y el siguiente paso concreto.
```
