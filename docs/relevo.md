# Relevo entre Cris y Juan

## Cuándo generar el mensaje

1. Comprueba quién tiene el turno en el último relevo de GitHub. Al empezar, registra «Turno activo: @usuario» en el issue o PR del relevo.
2. Completa las tareas del [turno](reparto.md). Al terminar, o si el compañero debe resolver un bloqueo, registra «Turno entregado a @usuario» con los PR, pruebas y siguiente paso. Genera un único prompt para que se lo pasen al compañero.
3. El destinatario empieza tras recibir el prompt y verificar el relevo en GitHub. Quien lo entrega espera su siguiente turno. Al cerrar el proyecto, el último prompt comunica la entrega y sus referencias.

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
