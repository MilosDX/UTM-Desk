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

El frontend web vive en [`frontend/`](frontend/) y la copia que sirve la API en
producción está en [`wwwroot/index.html`](wwwroot/index.html).

## Ejecutar

```powershell
dotnet restore
dotnet run --launch-profile http
```

La API queda en `http://localhost:5254`. Comprueba el estado en `GET /health`.

La configuración privada está en `appsettings.Local.json`; ese archivo está ignorado por Git. La cuenta inicial de SuperAdmin también está ahí. Copia su contraseña a la variable `adminPassword` de Postman, ejecuta **Auth / Login** y la colección guardará el token automáticamente.

## Frontend

El frontend es HTML y JavaScript sin paso de compilación. Está en [`frontend/`](frontend/):

- `frontend/index.html` — panel principal: login, incidencias, analítica y administración.
- `frontend/UTM_Desk_Incidencias.html` — vista de incidencias.
- `frontend/UTM_Desk_Incidencias (2).html` — copia de respaldo de la vista anterior.

`wwwroot/index.html` es la copia que el backend publica en `/`, así que al editar el
frontend hay que copiarla a `wwwroot/` para que Render sirva los cambios.

Para probarlo, abre `frontend/index.html` con la API en marcha. Desde `localhost` o
`file://` apunta solo a `http://localhost:5254`; en cualquier otro origen define
`window.API_BASE_URL` con la URL del backend (por ejemplo, la de Render) antes de cargar
el script.

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

#### Opción A — conservar el link actual `https://utm-desk.onrender.com`

Un Blueprint crea un servicio nuevo y la URL depende del nombre, así que para no
cambiar el enlace hay que actualizar el servicio que ya existe:

1. Render Dashboard → servicio `utm-desk` → **Settings**.
2. En **Build & Deploy**, cambia el repositorio a `MilosDX/UTM-Desk` y la rama
   a `main`.
3. Cambia el runtime a **Docker** y usa `./Dockerfile` como Dockerfile Path.
4. En **Environment** define las variables de entorno (las mismas de `render.yaml`):
   - `MongoDb__ConnectionString`
   - `Supabase__Url`
   - `Supabase__SecretKey`
   - `Supabase__BucketName` = `utm-desk-evidencias`
   - `Jwt__Key` (cadena aleatoria de al menos 32 caracteres)
5. **Manual Deploy → Deploy latest commit**.
6. Verifica `https://utm-desk.onrender.com/health`.

La URL no cambia porque es el mismo servicio.

#### Opción B — crear un servicio nuevo (cambia la URL)

1. En Render: **New → Blueprint** y conecta este repositorio.
2. Render lee `render.yaml` y crea el servicio con el `Dockerfile`.
3. Completa las variables marcadas con `sync: false`.
4. La nueva URL será `https://<nombre>.onrender.com`; si el frontend apunta a la
   anterior, habrá que actualizarla.

Para reutilizar exactamente `utm-desk.onrender.com` con un servicio nuevo, primero
renombra o elimina el servicio viejo para liberar el nombre y crea el nuevo como
`utm-desk`. Es más arriesgado que la Opción A.

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
