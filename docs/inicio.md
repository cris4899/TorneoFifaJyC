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

La IA debe confirmar la identidad y buscar el ID FC27 en GitHub. Si el titular es la otra persona, ofrece una tarea propia disponible o prepara el relevo. Si ya existe un PR para esa tarea, recupera la rama existente y su contexto.

## 3. Hacer visible el trabajo

1. **Issue:** es la ficha de la tarea. Tiene ID FC27, responsable, dependencias y criterio de cierre. Antes de empezar, comprueba que las dependencias estén en `master`. Usa el issue existente si ya hay uno para ese ID.
2. **Rama:** es tu copia de trabajo publicada. Créala desde `master` actualizado, con tu usuario e ID en el nombre. Sigue la [guía de ramas](ramas.md).
3. **Pull request:** es la propuesta para incorporar el trabajo. Enlaza el issue, describe qué comprobaste y añade el [relevo](relevo.md). Puedes abrirlo como borrador. Cris revisa el cambio y lo fusiona cuando esté listo.
4. **Bloqueo:** indica en el issue o PR qué falta y deja el prompt dirigido a la persona que puede desbloquearlo. El enlace de GitHub permite retomar desde otro equipo o chat.

GitHub guarda los documentos versionados, el código, el estado de las tareas, las decisiones, los PR y las instrucciones de entrega. Usad datos ficticios para ejemplos y evidencias. Guardad las credenciales y la base local con datos reales fuera del repositorio.

## 4. Orden de arranque

1. **Cris** abre FC27-00 con esta documentación. **Juan** revisa el acceso y prepara la prueba de conexión Android–Windows.
2. **Cris** fusiona FC27-00 cuando esté revisado y prepara FC27-01. **Juan** puede revisar el contrato y preparar el entorno mientras espera la base compartida.
3. Con FC27-01 en `master`, **Cris** toma FC27-03 y **Juan** toma FC27-02. Después, Juan puede hacer FC27-04 con datos ficticios. El [plan](plan.md) indica el orden hasta la entrega.
