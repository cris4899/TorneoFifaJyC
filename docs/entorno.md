# Entorno de desarrollo

En el equipo de Cris, `dotnet workload list` confirmó .NET SDK 10.0.403 y las cargas `android` y `maui-windows`. La compilación inicial de Windows terminó con 0 errores y 0 advertencias. La compilación Android y el entorno de Juan quedan por verificar.

Para verificar la base, ejecutar desde la raíz:

```powershell
dotnet --list-sdks
dotnet workload list
dotnet build TorneoFifaJyC/TorneoFifaJyC.csproj -f net10.0-windows10.0.19041.0
dotnet build TorneoFifaJyC/TorneoFifaJyC.csproj -f net10.0-android
```

La prueba de red Android–Windows se reserva para FC27-06.
