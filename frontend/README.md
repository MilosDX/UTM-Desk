# Frontend UTM Desk

Interfaz web de la mesa de ayuda. Es HTML, CSS y JavaScript sin compilación: se abre
directamente en el navegador.

| Archivo | Uso |
|---|---|
| `index.html` | Panel principal: login, incidencias, analítica y administración de usuarios. |
| `UTM_Desk_Incidencias.html` | Vista de incidencias. |
| `UTM_Desk_Incidencias (2).html` | Copia de respaldo de la vista anterior. |

## Conectar con la API

Con la API en `http://localhost:5254` no hace falta configurar nada: el script detecta
`localhost` y `file://`, y apunta a esa URL.

Para otro entorno, define `window.API_BASE_URL` antes de cargar el script de la página:

```html
<script>window.API_BASE_URL = 'https://tu-servicio.onrender.com';</script>
```

## Publicación

El backend sirve `wwwroot/index.html` en `/`. Al modificar el frontend, copia el archivo
a `wwwroot/` para que el contenedor de Render publique los cambios:

```powershell
Copy-Item frontend/index.html wwwroot/index.html -Force
```
