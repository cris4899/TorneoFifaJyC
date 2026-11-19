# Guía de entrega FC27

## Arranque en Windows

1. Compila y ejecuta el proyecto MAUI con destino Windows.
2. Crea un torneo de cuatro jugadores ficticios. Añade las cuatro inscripciones, inicia el cuadro y registra las semifinales y la final.
3. Cierra y vuelve a abrir la app. Abre el torneo finalizado y comprueba el campeón.

La app crea `fc27.db` en su directorio de datos local. La API debe apuntar a ese mismo archivo. Para una prueba en este equipo, desde PowerShell:

```powershell
$env:Torneo__RutaBase = 'RUTA COMPLETA A fc27.db'
dotnet run --project src/FC27.Api -- --urls http://127.0.0.1:5080
```

En otra consola ejecuta `Invoke-RestMethod http://127.0.0.1:5080/api/v1/torneos`. Usa el ID devuelto en `/api/v1/torneos/{id}/cuadro`. La prueba en localhost verifica la API en Windows. La conexión desde Android se valida aparte.

## Android

La entrega se valida en el emulador Pixel_8 (API 36). Por decisión del equipo, la prueba con un móvil físico queda fuera del alcance de esta entrega. La conexión con la API se comprobó mediante ADB y localhost.

Sigue el [manual Android](manual-android.md). Sin una dirección en `ConsultaConfig.BaseAddress`, la pantalla inicia con datos ficticios. Para la prueba real, acuerda una dirección accesible desde el dispositivo y configura esa URI y el ID real del torneo en `ConsultaConfig` antes de compilar. Usa HTTPS con un certificado que el dispositivo pueda validar.

## Evidencias

| Comprobación | Estado |
| --- | --- |
| Windows hasta campeón y reapertura | Verificado en FC27-05. Aceptación completa sobre c35fada (validación, sorteo, penaltis, corrección, reinicio, teclado y CSV) y reinicio a mitad de ronda: [informe Windows](pruebas-windows.md). |
| API y cliente de consulta | 53 pruebas correctas en FC27-06. |
| Compilación Android | Verificada en FC27-06, sin errores ni advertencias. |
| P09–P11: actualización, recuperación, nombres largos y TalkBack | En emulador: actualización en 4,99 s tras escribir en SQLite, estado vacío, recuperación y lectura de la final con TalkBack. Desde la app Windows: resultado visible en Android en 7 s y recuperación tras caída de la API en 4,4 s ([informe Windows](pruebas-windows.md)). El nombre largo se comprobó en pantalla y en el árbol de accesibilidad. |
| Capturas Android | [Seis capturas y registro de verificación](capturas/verificacion-android.md). |

## Defectos abiertos y limitaciones

| Elemento | Estado |
| --- | --- |
| Aviso de sin conexión en Android | Abierto, de Juan. «Sin conexión. Se muestra la última versión.» y «Reintentar» aparecen al final de la pantalla y hay que desplazarse para verlos. Se corrige en `TorneoFifaJyC/Features/Consulta/CuadroPage.xaml`. |
| Móvil físico | Fuera del alcance por decisión del equipo. La consulta se validó en el emulador Pixel_8 con ADB y localhost. |
| Narrador de Windows | No probado. La navegación por teclado sí se comprobó en la aceptación Windows. |
| TalkBack | Fuera del alcance acordado. Solo consta una lectura de la final en el emulador. |
