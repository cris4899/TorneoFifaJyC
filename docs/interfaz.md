# Diseño de interfaz

## Windows · Gestor

```text
Torneos  >  Copa DAM
Borrador · 3 de 4 plazas

Inscritos              Equipo                  Acciones
Alex                   Equipo A                Editar · Retirar
...
[Añadir participante]                [Sortear e iniciar]
```

- La lista de torneos muestra nombre, estado y ocupación. «Nuevo torneo» abre el formulario con capacidad seleccionable y error de nombre junto al campo.
- El formulario de inscripción contiene nombre, correo y equipo. El contador y el mensaje junto a «Sortear e iniciar» indican las plazas pendientes.
- EnCurso, el cuadro muestra rondas y cruces. «Registrar resultado» se ofrece en cruces con dos jugadores. El diálogo pide goles, habilita la elección del ganador cuando empatan y presenta una confirmación con los nombres.
- Una plaza vacía muestra «Por determinar». Al terminar se muestra el campeón sobre el cuadro y se puede consultar el resultado de cada ronda.

## Android · Alumno y espectador

```text
‹ Torneos      Copa DAM
En curso · Actualizado a las 12:32

[Próximos]  [Semifinales]  [Final]

Semifinal 2 · Pendiente
Sam   · Equipo C
Dani  · Equipo D
```

- «Próximos» agrupa los cruces disponibles por ronda. Si faltan resultados previos, indica que aún no hay cruces disponibles. Cada ronda mantiene visible su orden y presenta sus partidos como tarjetas verticales.
- En un partido jugado, se indica el ganador con texto y se muestra el marcador. En el torneo finalizado, el campeón se destaca por nombre y equipo.
- Ante un fallo de red, se ofrece «Reintentar». Con datos previos se conserva el cuadro y se indica que puede estar desactualizado. Sin datos previos se muestra el error con la acción de reintento.
- Botones y campos tienen nombres legibles por el lector de pantalla. El estado y el ganador se identifican con texto. Validar navegación con teclado en Windows y la legibilidad de nombres y estados en Android.

## Navegación con menú flotante

Android y Windows comparten el control `MenuFlotante` (`TorneoFifaJyC/Controls`), un botón en la esquina inferior derecha que despliega las secciones. Android: Próximos, Rondas y Ajustes (la dirección del servicio ya no ocupa la pantalla principal; las rondas se eligen con chips). Windows: Mis torneos, Inscritos, Partidos y Cuadro. La paleta vive solo en `Resources/Styles/Colors.xaml`; los estilos `Tarjeta`, `BotonPrimario` y `BotonSecundario` están en `Styles.xaml`.
