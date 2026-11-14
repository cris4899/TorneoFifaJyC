# Verificación Android

En el emulador Pixel 7 (API 36), el APK de depuración compilado con `-p:EmbedAssembliesIntoApk=true` instaló y abrió correctamente. La pantalla de consulta mostró Copa DAM, dos semifinales y el resultado 2–1 con Alex como ganador. El árbol de accesibilidad expuso descripciones completas de los dos cruces. Estos datos son ficticios.

Para reproducir el APK instalable, ejecuta `dotnet build TorneoFifaJyC/TorneoFifaJyC.csproj -f net10.0-android -p:EmbedAssembliesIntoApk=true` e instala el APK firmado de `bin/Debug/net10.0-android` con `adb install -r`.

La API en Windows leyó la base de la Copa QA FC27. Un APK temporal conectado por un túnel ADB a localhost mostró el torneo finalizado, los marcadores y el campeón Alex QA. Se usó HTTP únicamente en la copia de prueba. Al retirar y restaurar el túnel se conservó el torneo visible, pero la inspección no confirmó el aviso ni el tiempo de recuperación.

Quedan validar la red real con un dispositivo, medir la actualización tras confirmar un resultado, comprobar el aviso y la recuperación, verificar el nombre largo y TalkBack, y tomar las capturas.
