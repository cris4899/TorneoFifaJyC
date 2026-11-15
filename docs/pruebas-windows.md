# Informe de aceptación Windows

Pruebas Windows pendientes de [pruebas.md](pruebas.md), según el turno de Cris de [plan-mejoras.md](plan-mejoras.md).

## Versión y entorno

- **Commit:** `master` c35fada (PR #27–#30 incluidos).
- **Equipo:** Windows 11 Pro 10.0.26300, SDK .NET 10.0.403, destino `net10.0-windows10.0.19041.0`, compilación Debug sin errores.
- **Método:** la IA manejó la app con UI Automation (invocar botones, escribir en campos y leer el árbol de accesibilidad). El teclado se probó con pulsaciones reales de Tab. Las capturas son de la ventana de la app.
- **Datos:** ficticios, con dominio `ejemplo.test`. Las capturas no muestran correos.

## Resultados

| Bloque | Pasos | Resultado |
| --- | --- | --- |
| Validación de torneo | Crear un torneo con el nombre vacío. | «Indica el nombre del torneo.» aparece junto al campo. Correcto. |
| Validación de inscritos (#29) | Añadir con todo vacío, después con un correo inválido y después con un correo repetido escrito en mayúsculas. | Cada error aparece junto a su campo («Indica el nombre del jugador.», «Introduce un correo válido.», «Este correo ya está inscrito en el torneo.») y el foco pasa al primer campo inválido. Correcto. |
| Sorteo | Con 1 de 4 plazas, y después con 4 de 4. | Con plazas libres, «Sortear e iniciar» está desactivado y el texto «1 de 4 plazas» explica el estado. Con 4 de 4 se generan las semifinales y el torneo pasa a «En curso». Correcto. |
| Marcadores y penaltis | Registrar −1, después 1–1 sin ganador y después 1–1 con ganador por penaltis. | Con −1: «Introduce goles enteros no negativos en ambos campos.» Con 1–1 aparece el selector de penaltis con «Selecciona el ganador por penaltis.». La revisión muestra el marcador y el ganador antes de confirmar. Correcto. |
| Corrección | Cambiar la semifinal 2 de 2–0 a 0–3 antes de jugar la final. | Aviso: «Si cambia el ganador, se sustituirá su plaza en el siguiente cruce.» La final pasa de Ana–Carla a Ana–Dani. Después de la final, las semifinales muestran «El siguiente cruce ya se ha jugado.» Correcto. |
| Campeón | Final 2–1. | «Finalizado» y «Campeón: Ana (Real)». Correcto. |
| Reinicio | Cerrar la app y volver a abrirla. | «Copa Windows QA · Finalizado · 4 plazas» sigue en Mis torneos. Correcto. |
| Exportar CSV | Exportar el torneo finalizado. | Se exportan 3 filas, separadas por `;`, con rondas, marcadores, estado y ganador, y sin correos. Correcto. |
| Importar CSV | Importar un fichero con una fila inválida y después uno válido de 2 filas. | El fichero inválido se rechaza entero con «Fila 3: el correo no es válido.». El válido muestra la vista previa «Importar 2 inscripciones» y, al confirmar, el torneo queda con 2 de 4 plazas. Correcto. |
| Teclado (P11) | Recorrer con Tab desde «Nombre del jugador» en Inscritos. | Nombre, Correo, Equipo, Añadir, Editar y Retirar de cada inscrito, Volver, pestaña Inscritos, Importar CSV y vuelta a Nombre. El orden sigue el visual y los controles desactivados no reciben el foco. Correcto. |

## Observaciones

- La exportación CSV no muestra confirmación en la app al terminar. El fichero sí se guarda.
- El reinicio solo se probó con el torneo finalizado, no a mitad de una ronda.
- No se probó con lector de pantalla (Narrador). Los nombres accesibles sí se revisaron en el árbol de UI Automation.

## Capturas

`w01-torneo`, `w02-validacion`, `w03-inscritos`, `w04-sorteo`, `w05-confirmar`, `w06-correccion`, `w07-final` y `w08-csv-error`, en [capturas](capturas/).
