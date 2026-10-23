# UTM Desk API

Backend de UTM Desk construido con ASP.NET Core 8 y MongoDB Atlas.

## Configurar MongoDB Atlas

1. Crea un archivo llamado `appsettings.Local.json`.
2. Agrega la sección `MongoDb` con la cadena de conexión de Atlas.
3. En Atlas, permite tu IP actual desde **Network Access**.

`appsettings.Local.json` está ignorado por Git para evitar publicar la contraseña.

## Ejecutar

```powershell
dotnet run --launch-profile http
```

En Postman, prueba la petición `GET http://localhost:5254/api/incidencias`.
