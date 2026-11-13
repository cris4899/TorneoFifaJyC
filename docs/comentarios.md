# Comentarios de código

1. Escribe comentarios breves en español. Usa documentación XML (`///`) en los contratos públicos propios de Core, Data y API para explicar entradas, resultados y restricciones relevantes.
2. Comenta las reglas de negocio donde se aplican. Explica el destino impar/par del ganador, el desempate por penaltis y la transacción que guarda el resultado y el avance.
3. Explica decisiones técnicas que afecten al comportamiento. Incluye el motivo de cancelar consultas al salir de Android, conservar la última vista válida o excluir correos de la proyección.
4. Mantén los comentarios junto a la lógica y actualízalos con el código. Usa nombres claros para que las operaciones ordinarias se entiendan al leerlas.
5. En la revisión del PR, comprueba que los comentarios coincidan con la implementación y con las pruebas de las reglas que describen.
