# Verificación Android

En el emulador Pixel 7 (API 36), el APK de depuración compilado con `-p:EmbedAssembliesIntoApk=true` instaló y abrió correctamente. La pantalla de consulta mostró Copa DAM, dos semifinales y el resultado 2–1 con Alex como ganador. El árbol de accesibilidad expuso descripciones completas de los dos cruces. Estos datos son ficticios.

Para reproducir el APK instalable, ejecuta `dotnet build TorneoFifaJyC/TorneoFifaJyC.csproj -f net10.0-android -p:EmbedAssembliesIntoApk=true` e instala el APK firmado de `bin/Debug/net10.0-android` con `adb install -r`.

La API en Windows leyó la base de la Copa QA FC27. Un APK temporal conectado por un túnel ADB a localhost mostró el torneo finalizado, los marcadores y el campeón Alex QA. Se usó HTTP únicamente en la copia de prueba. Al retirar y restaurar el túnel se conservó el torneo visible, pero la inspección no confirmó el aviso ni el tiempo de recuperación.

Quedan validar la red real con un dispositivo, medir la actualización tras confirmar un resultado, comprobar el aviso y la recuperación, verificar el nombre largo y TalkBack, y tomar las capturas.

## Desconexión y recuperación

En el mismo emulador y con ADB reverse, detuve el proceso de la API de Windows. Android conservó los marcadores y el campeón, mostró «Sin conexión. Se muestra la última versión.» y ofreció «Reintentar». Al reiniciar la API, el cuadro volvió a consultarse automáticamente y el aviso desapareció. Esto verifica P10 en el entorno local, excepto el escenario de lista vacía. Quedan la latencia al registrar un resultado, TalkBack y la red física.

## Latencia de actualización

En Pixel 7 API 36 con túnel ADB y API en Windows, se confirmó una final 3–1 en una transacción SQL sobre una copia temporal de la base. Android pasó de En curso a mostrar el campeón en 4,99 s, incluida la lectura del árbol de accesibilidad. Cumplió el objetivo de 10 s en este entorno. La prueba mide desde la escritura en SQLite y queda por repetir desde la interfaz Windows con un móvil en la red de entrega.

## Estado vacío y nombre largo

En Pixel 7 API 36, el cuadro mostró «No hay cuadro disponible todavía» cuando el torneo de la copia temporal quedó en borrador. Tras restaurar el torneo finalizado, el árbol de accesibilidad expuso el nombre ficticio largo completo en la descripción del partido y en el campeón. TalkBack estaba instalado e inactivo. Queda verificar la lectura en voz y el ajuste visual, además de tomar las capturas y probar la red física.
