# Plan de mejoras FC27

El turno activo es de Cris. Juan puede consultar el plan y empieza su bloque cuando reciba el nuevo prompt de relevo.

## Primera tanda

1. **Cris · Gestión Windows.** Separar Mis torneos del detalle. Organizar el detalle en Inscritos, Partidos y Cuadro. Reunir ambos marcadores en un formulario y mostrar la opción de penaltis en empate. Aplicar fondo grafito, superficies diferenciadas, texto claro y lima para acciones principales. Mantener la navegación por teclado y los estados escritos.
2. **Juan · Consulta Android.** Aplicar el estilo compartido. Configurar y guardar la dirección de la API desde la app, probar la conexión y elegir un torneo iniciado o finalizado. Identificar el modo demo. Mostrar Próximos, Rondas y la hora de la última actualización correcta.

Cris entrega el turno tras publicar y verificar el bloque Windows. El relevo incluye el PR, las decisiones visuales y las comprobaciones. Juan devuelve el turno con su PR y pruebas Android. Solo Cris fusiona a `master`.

## Mejoras posteriores, en orden

1. Cuadro visual en Windows con rondas conectadas y consulta por ronda en Android.
2. Corrección de marcadores mientras el cruce siguiente esté pendiente, con confirmación del efecto sobre el cuadro. Si ya se jugó, explicar el bloqueo.
3. Importación CSV con vista previa y detección de duplicados, y exportación de cuadro y resultados.

## Comprobaciones

- **Windows:** crear torneo, gestionar inscritos, sortear, registrar un empate con penaltis y comprobar el campeón tras reiniciar. Verificar teclado y nombres largos.
- **Android:** elegir torneo, guardar y probar conexión, comprobar el modo demo, la última actualización y la recuperación tras desconexión. Probar el cuadro con nombres largos en el emulador. TalkBack queda fuera del alcance de las pruebas de entrega.
