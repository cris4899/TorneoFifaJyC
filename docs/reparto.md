# Reparto de tareas

`cris4899` mantiene la integración y la administración Windows. `juanrr92` desarrolla el cuadro y la consulta Android. Cada fila corresponde a un PR. `cris4899` acepta y fusiona los PR a `master`. En un PR propio, `juanrr92` puede revisar y comentar antes de que `cris4899` lo fusione.

| ID | Responsable | Entrega y comprobación | Depende de |
| --- | --- | --- | --- |
| FC27-00 | cris4899 | Integrar la documentación inicial. Acordar con Juan el contrato de consulta, las reglas pendientes y la prueba de red. | — |
| FC27-01 | cris4899 | Preparar solución, proyectos, entidades, contratos compartidos y composición MAUI. Verificar que ambos pueden trabajar desde el mismo punto de partida. | FC27-00 |
| FC27-02 | juanrr92 | Desarrollar el sorteo, cálculo del avance y resolución del ganador como lógica de dominio. Cubrir los cinco tamaños y los desempates con pruebas unitarias. | FC27-01 |
| FC27-03 | cris4899 | Implementar SQLite y repositorios, altas y validaciones. Probar unicidad, cupo, claves foráneas y aislamiento por torneo. | FC27-01 |
| FC27-04 | juanrr92 | Crear listado y cuadro Android con datos de prueba. Cubrir carga, vacío, próxima ronda, campeón y desconexión. | FC27-01 |
| FC27-05 | cris4899 | Integrar sorteo, registro transaccional y pantallas Windows. Ejecutar P01–P08 del [plan de pruebas](pruebas.md). | FC27-02, FC27-03 |
| FC27-06 | juanrr92 | Implementar el servicio de lectura acordado y conectar Android. Ejecutar P09–P11 con Cris y verificar la actualización y la exclusión de correos. | FC27-04, FC27-05 |
| FC27-07 | juanrr92 | Completar diagramas, manual Android y evidencias de consulta para la memoria. | FC27-06 |
| FC27-08 | cris4899 | Completar manual Windows, infraestructura, análisis de mercado, evidencias y ensamblaje de la memoria final. | FC27-06, FC27-07 |

Tras FC27-01, Cris puede trabajar en persistencia mientras Juan trabaja en dominio y Android. En FC27-04, Juan usa datos ficticios ajustados al contrato acordado, de modo que puede avanzar antes de que exista la API.

**Entrega entre personas:** cada PR indica qué contrato, ruta o instrucción consume la siguiente tarea. Si ese contrato cambia, Cris y Juan acuerdan el cambio y lo integran antes de modificar los dos lados.
