# Verificación Android

En el emulador Pixel 7 (API 36), el APK de depuración compilado con `-p:EmbedAssembliesIntoApk=true` instaló y abrió correctamente. La pantalla de consulta mostró Copa DAM, dos semifinales y el resultado 2–1 con Alex como ganador. El árbol de accesibilidad expuso descripciones completas de los dos cruces. Estos datos son ficticios.

Para reproducir el APK instalable, ejecuta `dotnet build TorneoFifaJyC/TorneoFifaJyC.csproj -f net10.0-android -p:EmbedAssembliesIntoApk=true` e instala el APK firmado de `bin/Debug/net10.0-android` con `adb install -r`.

Quedan la prueba con API real, las capturas de los estados, el nombre largo y la lectura con TalkBack.
