# UTM Desk API

Backend de la mesa de ayuda UTM Desk, construido con ASP.NET Core 8, MongoDB Atlas y Supabase Storage. La API usa JWT, contraseñas cifradas, permisos por rol y una bitácora embebida dentro de cada incidencia.

## Estado actual

- Inicio de sesión y sesión actual con JWT.
- Roles: `reportante`, `tecnico`, `coordinador`, `directivo` y `superAdmin`.
- Consulta de incidencias limitada automáticamente según el rol.
- Registro, asignación, seguimiento, resolución, cierre y reapertura.
- Comentarios públicos o internos en la bitácora.
- Evidencias JPG, PNG y PDF de hasta 5 MB en un bucket privado de Supabase.
- URLs temporales para consultar evidencias sin exponer el bucket.
- Analítica por estado, categoría, edificio y etiquetas.
- Administración de usuarios y contraseñas por SuperAdmin.
- Colección de Postman lista para importar.

No se incluyó frontend.

## Ejecutar

```powershell
dotnet restore
dotnet run --launch-profile http
```

La API queda en `http://localhost:5254`. Comprueba el estado en `GET /health`.

La configuración privada está en `appsettings.Local.json`; ese archivo está ignorado por Git. La cuenta inicial de SuperAdmin también está ahí. Copia su contraseña a la variable `adminPassword` de Postman, ejecuta **Auth / Login** y la colección guardará el token automáticamente.

## Postman

Importa [postman/UTMDesk.postman_collection.json](postman/UTMDesk.postman_collection.json).

Flujo recomendado:

1. Ejecuta `Sistema / Health`.
2. Copia la contraseña local del SuperAdmin a `adminPassword`.
3. Ejecuta `Auth / Login`.
4. Ejecuta `Usuarios - SuperAdmin / Listar usuarios`.
5. Coloca el `_id` de uno de los cuatro usuarios existentes en `usuarioId` y usa `Asignar contraseña a usuario existente`.
6. Inicia sesión con cuentas de cada rol para probar sus operaciones.

## Permisos principales

| Operación | Rol |
|---|---|
| Crear y consultar sus incidencias | Reportante |
| Ver técnicos y asignar, cerrar o reabrir | Coordinador de la categoría |
| Ver asignadas y registrar trabajo | Técnico asignado |
| Consultar analítica global | Directivo |
| Administrar usuarios y consultar todo | SuperAdmin |

Los coordinadores deben tener en `area` una categoría reconocible: `computo_redes`, `infraestructura` o `academico`. Los técnicos requieren `especialidad` y los reportantes un `tipo_reportante`: `estudiante`, `docente` o `administrativo`.

## Configurar Supabase Storage

Este es el único servicio que todavía requiere configuración manual:

1. En Supabase crea un bucket llamado `utm-desk-evidencias`.
2. Déjalo **privado**.
3. Limita el tamaño a 5 MB y permite `image/jpeg`, `image/png` y `application/pdf`.
4. En la configuración del proyecto copia la URL y una **Secret key** (`sb_secret_...`). La API también acepta la antigua `service_role`.
5. Pega ambos valores en la sección `Supabase` de `appsettings.Local.json`.

La llave secreta solo pertenece al backend. Nunca debe copiarse a React, Vercel ni GitHub.

```json
"Supabase": {
  "Url": "https://TU-PROYECTO.supabase.co",
  "SecretKey": "sb_secret_TU-LLAVE-SECRETA",
  "BucketName": "utm-desk-evidencias",
  "SignedUrlExpirationMinutes": 30
}
```

## Despliegue

La API y el frontend de `wwwroot` se despliegan juntos en un contenedor. El archivo
`render.yaml` describe el servicio para [Render](https://render.com) y el
`Dockerfile` compila y publica la aplicación.

### Render (API + frontend)

1. En Render: **New → Blueprint** y conecta este repositorio.
2. Render lee `render.yaml` y crea el servicio `utm-desk-api` con el `Dockerfile`.
3. Completa en **Environment** las variables marcadas con `sync: false`:
   - `MongoDb__ConnectionString`
   - `Supabase__Url`
   - `Supabase__SecretKey`
4. Guarda los cambios; Render redespliega automáticamente.
5. Verifica `https://<tu-servicio>.onrender.com/health`.

La aplicación queda completa en esa URL: `/` sirve el frontend, `/api/*` la API y
`/health` la comprobación de estado.

> `appsettings.Local.json` no se incluye en la imagen (está en `.dockerignore`), por
> eso la configuración de producción se pasa **siempre** por variables de entorno.

### Vercel (solo frontend estático)

Vercel no ejecuta aplicaciones ASP.NET Core, así que el backend debe vivir en Render
(o cualquier hosting con Docker). Si publicas un frontend aparte en Vercel, apúntalo
a la URL de Render con `VITE_API_URL=https://<tu-servicio>.onrender.com`.

Nunca pongas `Supabase__SecretKey` en Vercel ni en el frontend: esa llave solo
pertenece al backend.

## Endpoints

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET|POST /api/usuarios`
- `GET /api/usuarios/tecnicos`
- `PATCH /api/usuarios/{id}/estado`
- `PATCH /api/usuarios/{id}/password`
- `GET|POST /api/incidencias`
- `GET /api/incidencias/{id}`
- `PATCH /api/incidencias/{id}/asignacion`
- `POST /api/incidencias/{id}/seguimientos`
- `PATCH /api/incidencias/{id}/cerrar`
- `PATCH /api/incidencias/{id}/reabrir`
- `POST|GET /api/incidencias/{id}/evidencias`
- `GET /api/analitica/resumen`

Las búsquedas de incidencias aceptan `estado`, `categoria`, `prioridad`, `tag`, `texto`, `pagina` y `tamanoPagina` como query parameters.
