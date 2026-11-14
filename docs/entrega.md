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

Sigue el [manual Android](manual-android.md). Sin una dirección en `ConsultaConfig.BaseAddress`, la pantalla inicia con datos ficticios. Para la prueba real, acuerda una dirección accesible desde el dispositivo y configura esa URI y el ID real del torneo en `ConsultaConfig` antes de compilar. Usa HTTPS con un certificado que el dispositivo pueda validar.

## Evidencias

| Comprobación | Estado |
| --- | --- |
| Windows hasta campeón y reapertura | Verificado en FC27-05. |
| API y cliente de consulta | 53 pruebas correctas en FC27-06. |
| Compilación Android | Verificada en FC27-06, sin errores ni advertencias. |
| P09–P11: actualización, recuperación, nombres largos y TalkBack | Pendiente de ejecutar con Android. |
| Capturas Android | Pendientes según el [plan](manual-android.md#plan-de-capturas-datos-ficticios). |
