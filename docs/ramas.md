# Guía de ramas y pull requests

## 1. Preparar el acceso

- **Cris** da acceso de escritura a `juanrr92` en `cris4899/TorneoFifaJyC`. **Juan** acepta la invitación y clona el repositorio con su cuenta.
- Ambos comprueban que Git usa su propio correo asociado a GitHub con `git config user.email`. El autor del commit y la cuenta que autentica el push se configuran por separado.
- La rama principal es `master`. Cada tarea tiene su rama y su PR. Solo `cris4899` fusiona a `master`.

## 2. Abrir una rama por tarea

Ejemplo de Juan para FC27-02, una vez fusionado FC27-01. Partir de una carpeta sin cambios pendientes.

```powershell
git switch master
git pull --ff-only origin master
git switch -c codex/juanrr92/fc27-02-cuadro
```

Cris usa el mismo formato, por ejemplo `codex/cris4899/fc27-03-persistencia`. Antes de tocar un archivo compartido, revisad el [reparto por ruta](estructura.md).

## 3. Publicar el PR

```powershell
git status --short
git add -- <rutas-de-tu-tarea>
git diff --cached
git commit -m "FC27-02: genera el cuadro de eliminación"
git push -u origin HEAD
```

Sustituye el marcador `<rutas-de-tu-tarea>` por las rutas reales. En GitHub abre un PR con base `master` y comparación con tu rama. Usa la plantilla, indica las pruebas ejecutadas y solicita revisión a `cris4899` si eres Juan. Si la tarea sigue en curso, abre un borrador.

## 4. Incorporar cambios de master

Desde tu rama y con el trabajo guardado, actualiza antes de pedir la revisión final si se fusionó una dependencia o aparecen conflictos.

```powershell
git fetch origin
git merge origin/master
```

Si hay conflictos, revisa `git status` y acordad el resultado con quien modificó el archivo. Tras resolverlos, añade los archivos afectados y ejecuta `git merge --continue`. Si necesitas volver al estado anterior al intento, usa `git merge --abort`. Repite las comprobaciones afectadas y publica con `git push`. Evitad reescribir ramas publicadas.

## 5. Revisar, fusionar y recoger el cambio

- **Cris** comprueba el alcance, el resultado de las pruebas y que las dependencias estén en `master`. Para sus propios PR puede pedir comentarios a Juan.
- **Cris** fusiona desde GitHub cuando el PR está listo. Después, ambos ejecutan `git switch master` y `git pull --ff-only origin master` antes de la siguiente tarea. El autor puede eliminar la rama desde GitHub tras confirmar la fusión.
- La regla de equipo es trabajar mediante PR. Si se configura protección en GitHub, comprobar con ambos usuarios que el ajuste impide los pushes directos y permite a Cris fusionar sus propios PR sin intentar autoaprobarlos.
