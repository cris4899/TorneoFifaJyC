# Entorno de desarrollo

En el equipo de Cris se detectó el SDK .NET 10.0.403 y los paquetes de MAUI y Android. La comprobación `dotnet workload list` no pudo terminar en el entorno aislado por falta de acceso al administrador de servicios de Windows. La compilación queda por verificar.

Para verificar la base, ejecutar desde la raíz:

```powershell
dotnet --list-sdks
dotnet workload list
dotnet build TorneoFifaJyC/TorneoFifaJyC.csproj -f net10.0-windows10.0.19041.0
dotnet build TorneoFifaJyC/TorneoFifaJyC.csproj -f net10.0-android
```

La prueba de red Android–Windows se reserva para FC27-06.
