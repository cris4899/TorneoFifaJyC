# Estructura del repositorio

La solución MAUI se conserva en su ubicación actual. Las carpetas nuevas contienen un README para indicar propietario y alcance. Los proyectos y su registro en la solución se crearán en FC27-01.

| Ruta | Responsable y contenido |
| --- | --- |
| `TorneoFifaJyC.slnx` | **Cris.** Alta de proyectos. |
| `src/FC27.Core/` | **Cris** define entidades y contratos compartidos. **Juan** desarrolla el sorteo, la resolución del ganador y el cálculo de avance en archivos propios. |
| `src/FC27.Data/` | **Cris.** EF Core, SQLite, repositorios, migraciones y transacciones. |
| `src/FC27.Api/` | **Juan.** Servicio de consulta según la decisión de red. Consume la proyección que acuerde con Cris. |
| `TorneoFifaJyC/Features/Gestion/` | **Cris.** Pantallas y modelos de vista del gestor Windows. |
| `TorneoFifaJyC/Features/Consulta/` | **Juan.** Pantallas, modelos de vista y cliente de consulta Android. |
| `TorneoFifaJyC/Platforms/Android/` | **Juan.** Configuración específica de Android. |
| `TorneoFifaJyC/Platforms/Windows/` | **Cris.** Configuración específica de Windows. |
| `TorneoFifaJyC/MauiProgram.cs`, `AppShell.xaml` y `TorneoFifaJyC.csproj` | **Cris.** Configuración compartida. Juan indica en su PR qué rutas, dependencias o registros necesita. |
| `tests/` | **Juan** escribe las pruebas del cuadro y consulta. **Cris** escribe las de persistencia y gestión. Cada persona mantiene sus archivos. |
| `docs/` | Cada persona prepara su parte de la memoria. **Cris** integra cambios en el índice y el documento final. |

Si una tarea requiere editar un archivo compartido, acordad quién incorpora el cambio y en qué PR. La otra rama incorpora ese PR desde `master` antes de editar el archivo.
