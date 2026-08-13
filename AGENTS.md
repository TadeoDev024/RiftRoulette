# RiftRoulette / MatchMySkin — Contexto para IA

## ¿Qué es este proyecto?
App web para jugadores de League of Legends. Permite registrar skins poseídas, crear salas de lobby
con amigos (máx. 5) y ver las **temáticas de skins en común** agrupadas por rol de juego.

## Stack
- **Backend**: C# ASP.NET Core 8, MySqlConnector, JWT, Swashbuckle (Swagger), Newtonsoft.Json
- **Frontend**: HTML + CSS + Vanilla JS (SPA sin framework, 4 vistas)
- **Base de datos**: MySQL en Aiven Cloud
- **Deploy**: Render.com con Docker

## Estructura de archivos relevantes
```
Controllers/AuthController.cs     → /api/auth/register|login (JWT)
Controllers/RiftController.cs     → /api/Rift/login|register|skins/{id}|inventory/toggle  ← FRONTEND USA ESTE
Controllers/LobbyController.cs    → /api/Lobby/create|join/{code}|teambuilder/{code}
models.cs                         → LoginRequest, SkinDTO
RouletteEngine.cs                 → RouletteService (lógica de temas compartidos, INCOMPLETA)
dataimporter.cs                   → RiotDataService (sync desde Riot API → MySQL)
index.html                        → SPA: vistas auth/home/inventory/lobby
app.js                            → Toda la lógica frontend (263 líneas)
```

## Esquema BD MySQL
```sql
Usuarios(id_usuario, username, password)
Skins(id_skin_riot, id_tematica, nombre_skin, campeon, campeon_id, linea)
Tematicas(id_tematica, nombre)
Usuario_Skins(id_usuario, id_skin_riot)  -- many-to-many
```

## ⚠️ Issues conocidos (NO tocar sin preguntar)
1. Contraseñas en texto plano (sin hashing)
2. Lobbies solo en memoria (ConcurrentDictionary) — no persisten
3. JWT secret hardcodeado en AuthController.cs línea 74
4. AuthController y RiftController duplican funcionalidad — frontend usa RiftController
5. RouletteEngine.cs está incompleto (solo tiene GetThemesSharedByAll)
6. CORS AllowAll (inseguro en producción)

## Variables de entorno (producción en Render)
- `ConnectionStrings__DefaultConnection` → MySQL Aiven
- API URL hardcodeada en app.js línea 1: `https://skinsynergy-api.onrender.com/api`
