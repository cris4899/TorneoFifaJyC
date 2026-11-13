# Reparto de tareas

`cris4899` mantiene la integración y la administración Windows. `juanrr92` desarrolla el cuadro y la consulta Android. Cada fila corresponde a un PR. `cris4899` acepta y fusiona los PR a `master`. En un PR propio, `juanrr92` puede revisar y comentar antes de que `cris4899` lo fusione.

| ID | Responsable | Entrega y comprobación | Depende de |
| --- | --- | --- | --- |
| FC27-00 | cris4899 | Integrar la documentación inicial. Cerrar la revisión del contrato y las reglas. La prueba de red pasa a FC27-06. | — |
| FC27-01 | cris4899 | Preparar solución, proyectos, entidades, contratos compartidos y composición MAUI. Verificar que ambos pueden trabajar desde el mismo punto de partida. | FC27-00 |
| FC27-02 | juanrr92 | Desarrollar el sorteo, cálculo del avance y resolución del ganador como lógica de dominio. Cubrir los cinco tamaños y los desempates con pruebas unitarias. | FC27-01 |
| FC27-03 | cris4899 | Implementar SQLite y repositorios, altas y validaciones. Probar unicidad, cupo, claves foráneas y aislamiento por torneo. | FC27-01 |
| FC27-04 | juanrr92 | Crear listado y cuadro Android con datos de prueba. Cubrir carga, vacío, próxima ronda, campeón y desconexión. | FC27-01 |
| FC27-05 | cris4899 | Integrar sorteo, registro transaccional y pantallas Windows. Ejecutar P01–P08 del [plan de pruebas](pruebas.md). | FC27-02, FC27-03 |
| FC27-06 | juanrr92 | Implementar el servicio de lectura acordado y conectar Android. Ejecutar P09–P11 con Cris y verificar la actualización y la exclusión de correos. | FC27-04, FC27-05 |
| FC27-07 | juanrr92 | Completar diagramas, manual Android y evidencias de consulta para la memoria. | FC27-06 |
| FC27-08 | cris4899 | Completar manual Windows, infraestructura, análisis de mercado, evidencias y ensamblaje de la memoria final. | FC27-06, FC27-07 |

## Turnos exclusivos

| Turno | Persona y trabajo | Entrega |
| --- | --- | --- |

| 1 | **Cris.** Cerrar FC27-00, preparar e integrar FC27-01 y FC27-03. | Base compartida y SQLite en `master`. |
| 2 | **Juan.** Preparar y publicar FC27-02 y FC27-04 en ramas separadas. Puede terminar ambos PR antes de devolver el turno. | Los dos PR, pruebas y datos de ejemplo para Cris. |
| 3 | **Cris.** Revisar y fusionar los PR del turno anterior. Completar e integrar FC27-05. | Gestión Windows, fuente de consulta y datos de prueba para Juan. |
| 4 | **Juan.** Preparar FC27-06 y verificarlo en su rama. Con esa versión prepara FC27-07 en una rama dependiente. | PR, capturas y manual Android para Cris. Cris integra primero FC27-06 y después FC27-07. |
| 5 | **Cris.** Revisar y fusionar los PR de Juan y completar FC27-08. | Memoria y versión verificada. Comunica a Juan el cierre. |

Cada tarea conserva su rama, issue y PR. Solo la persona del turno trabaja en el proyecto. Si Cris detecta cambios necesarios en un PR de Juan, le devuelve el turno con un prompt que enumera las correcciones. Juan devuelve el turno al publicar los ajustes.
