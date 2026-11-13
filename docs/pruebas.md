# Plan de pruebas y aceptación

Pruebas previstas. Usar datos ficticios y una base SQLite temporal para integración. Repetir la prueba de comunicación con Windows y un Android físico o emulador con ruta hacia el host. Para la demo final, comprobar también un móvil en la red real.

| ID | Requisito | Preparación y acción | Resultado esperado |
| --- | --- | --- | --- |
| P01 | RF-01 | Crear con 4, 8, 16, 32, 64. Probar también 0, −4, 2, 6 y 128. | Se guardan solo las cinco capacidades permitidas. |
| P02 | RF-01, RF-02 | Repetir nombre exacto, omitir campos y repetir correo con espacios o mayúsculas, incluido en otro torneo. | Error junto al campo y ninguna fila adicional. |
| P03 | RF-02, RF-03 | Con capacidad 4, intentar iniciar con 3, completar, probar una quinta alta e iniciar. Intentar editar inscritos y sortear otra vez. | Se rechaza cada operación inválida. Queda un solo cuadro y la lista de inscritos se conserva. |
| P04 | RF-03 | Sortear con 4, 8, 16, 32 y 64 inscritos. | Respectivamente 2, 3, 4, 5 y 6 fases, y 3, 7, 15, 31 y 63 partidos. Cada inscrito aparece una vez en primera ronda. Las demás plazas se crean vacías y se muestran los nombres de fase correctos. |
| P05 | RF-04, RF-05 | Completar semifinales de cuatro jugadores en orden inverso. Registrar una victoria 2–1 y un empate 0–0 decidido por penaltis. Jugar la final. | El ganador impar ocupa local y el par visitante. Al terminar hay un campeón y el torneo está Finalizado. |
| P06 | RF-04 | Intentar guardar goles negativos, empate sin ganador, ganador ajeno o resultado con una plaza vacía. | Se rechaza y permanecen los datos previos. |
| P07 | RNF-02 | Provocar un fallo entre guardar marcador y actualizar destino. Repetir tras recuperar. Enviar dos confirmaciones simultáneas del mismo partido. | El fallo revierte todo. Reintento idéntico devuelve lo guardado. Una confirmación incompatible se rechaza. |
| P08 | RF-02, RF-04 | Intentar persistir FK inexistente, la misma inscripción en ambas plazas y un jugador de otro torneo. | SQLite rechaza la FK inexistente y la fila con plazas duplicadas. El caso de uso rechaza la inscripción cruzada. |
| P09 | RF-06, RNF-01, RNF-03 | Abrir el cuadro Android, registrar un resultado en Windows y cronometrar hasta que se vea. Inspeccionar listado y respuesta del cuadro. | La demo cumple el objetivo de 10 s, solo muestra torneos iniciados o finalizados y ninguna respuesta contiene correos. |
| P10 | RF-06, RF-07 | Abrir sin torneos, desconectar antes y después de una consulta válida, reconectar y reintentar. | Se distinguen lista vacía, error y cuadro desactualizado. Al recuperar la conexión se vuelve a mostrar la versión reciente. |
| P11 | RF-02, RF-06 | Navegar con teclado en Windows y TalkBack en Android. Probar un nombre largo. | El foco sigue el orden visual, los errores y estados tienen texto y el nombre completo puede leerse. |

## Guion de demostración

1. Crear «Copa DAM» con cuatro jugadores ficticios. Intentar sortear con tres y completar la inscripción.
2. Abrir el cuadro en Android. Registrar las semifinales, incluida una resuelta por penaltis, y medir una actualización.
3. Interrumpir la conexión, comprobar el aviso y recuperarla. Registrar la final y comprobar el campeón en ambos dispositivos.

Para la memoria, registrar por prueba la versión, dispositivo, pasos, resultado observado y evidencia. Adjuntar capturas sin correos.
