# Empezar desde cada ordenador

## 1. Configurar el acceso una vez

1. **Cris (`cris4899`)** abre el [repositorio](https://github.com/cris4899/TorneoFifaJyC) y comprueba que Juan tenga acceso de escritura. Si falta, abre **Settings → Collaborators**, busca `juanrr92` y envía la invitación.
2. **Juan (`juanrr92`)** inicia sesión con su cuenta y acepta la invitación. Abre el repositorio y comprueba que puede ver **Issues** y **Pull requests**.
3. **Cada persona** instala Git y GitHub CLI en su ordenador. Abre PowerShell, ejecuta `gh auth login`, elige GitHub.com, HTTPS y el inicio de sesión por navegador. Después ejecuta `gh api user --jq .login`. Debe salir su propio usuario.
4. **Cada persona** clona el repositorio si aún no tiene copia. Ejecuta `gh repo clone cris4899/TorneoFifaJyC` en la carpeta donde quiera guardarlo, entra con `cd TorneoFifaJyC` y comprueba `git status --short --branch`. En una copia recién clonada debe aparecer `master` siguiendo a `origin/master`. Configura en esa copia tu nombre y el correo asociado a tu cuenta.

## 2. Elegir una tarea con tu IA

Pega este mensaje al abrir un chat nuevo. Cambia la cuenta por la tuya.

```text
Soy [cris4899 o juanrr92] y trabajo desde mi ordenador en
cris4899/TorneoFifaJyC. Confirma con qué cuenta está conectado GitHub.
Lee AGENTS.md e index.md. Comprueba Issues, Pull requests y master.
Dime qué tarea de mi reparto puedo hacer ahora y por qué está disponible.
Guíame paso a paso desde mi estado actual. Explícame dónde hacer clic
o qué comando ejecutar y qué resultado debería ver. Si estoy bloqueado,
dime quién puede desbloquearme y genera «Pásale este prompt a tu IA».
```

La IA debe confirmar la identidad, el turno activo y el ID FC27. Si el turno es de la otra persona, espera a recibir su prompt. Si ya existe un PR para la tarea, recupera la rama existente y su contexto.

### Formato de las instrucciones

1. **Haz esto.** Indica quién actúa y da una acción concreta. Ejemplo para Cris: abre el [PR #1](https://github.com/cris4899/TorneoFifaJyC/pull/1) y comprueba que las observaciones estén resueltas.
2. **Luego esto.** Da el siguiente paso y el resultado esperado. Si depende del primero, dilo en una frase. Ejemplo: si encuentras el resultado, anótalo en el SDD. Si falta, pide a Juan que lo publique.
3. **Prompt de relevo.** Inclúyelo solo cuando corresponda según [el protocolo](relevo.md). Usa «Pásale este prompt a tu IA» y un bloque listo para copiar con el destinatario, el enlace y la siguiente acción.

Da uno o dos pasos por turno cuando haga falta confirmar un resultado antes de seguir. Si se pueden ejecutar seguidos, agrúpalos en una lista breve.

## 3. Hacer visible el trabajo

1. **Issue:** es la ficha de la tarea. Tiene ID FC27, responsable, dependencias y criterio de cierre. Antes de empezar, comprueba que las dependencias estén en `master`. Usa el issue existente si ya hay uno para ese ID.
2. **Rama:** es tu copia de trabajo publicada. Créala desde `master` actualizado, con tu usuario e ID en el nombre. Sigue la [guía de ramas](ramas.md).
3. **Pull request:** enlaza el issue y describe qué comprobaste. Puedes abrirlo como borrador. Cris revisa y fusiona durante su turno.
4. **Relevo:** consulta [cuándo preparar un prompt](relevo.md). Los avances y las dependencias se registran en GitHub para poder retomar desde otro equipo.

GitHub guarda los documentos versionados, el código, el estado de las tareas, las decisiones, los PR y las instrucciones de entrega. Usad datos ficticios para ejemplos y evidencias. Guardad las credenciales y la base local con datos reales fuera del repositorio.

## 4. Orden de arranque

1. **Cris** toma el turno. La prueba Android–Windows queda aplazada hasta FC27-06.
2. **Cris** cierra FC27-00 y completa FC27-01 y FC27-03 antes de pasar el turno a Juan.
3. **Juan** prepara FC27-02 y FC27-04. Consulta los [turnos restantes](reparto.md) hasta la entrega.
