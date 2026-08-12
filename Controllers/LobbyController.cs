using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RiftRoulette.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LobbyController : ControllerBase
    {
        private readonly string _connectionString;

        public LobbyController(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                                ?? configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateLobby()
        {
            try 
            {
                string code = GenerateUniqueCode();
                if (string.IsNullOrEmpty(code))
                    return StatusCode(500, new { message = "No se pudo generar código" });

                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                string query = "INSERT INTO Lobbies (code) VALUES (@code)";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@code", code);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { lobbyCode = code });
            } 
            catch (Exception ex) 
            {
                return StatusCode(500, new { message = "Error interno del servidor", error = ex.Message });
            }
        }

        [HttpPost("join/{code}")]
        public async Task<IActionResult> JoinLobby(string code, [FromBody] PlayerDto player)
        {
            try 
            {
                code = code.ToUpper();

                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                // Verificar que la sala existe
                string checkLobby = "SELECT COUNT(*) FROM Lobbies WHERE code = @code";
                using (var cmd = new MySqlCommand(checkLobby, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    int lobbyExists = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    if (lobbyExists == 0)
                        return NotFound(new { message = "Sala no encontrada" });
                }

                // Verificar si el jugador ya está en la sala
                string checkPlayer = "SELECT COUNT(*) FROM LobbyPlayers WHERE lobby_code = @code AND user_id = @uid";
                using (var cmd = new MySqlCommand(checkPlayer, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@uid", player.UserId);
                    int alreadyIn = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    if (alreadyIn > 0)
                        return Ok(new { lobbyCode = code, message = "Ya estás en la sala" });
                }

                // Contar jugadores actuales
                string countQuery = "SELECT COUNT(*) FROM LobbyPlayers WHERE lobby_code = @code";
                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    int playerCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    if (playerCount >= 5)
                        return BadRequest(new { message = "Sala llena (5/5)" });
                }

                // Insertar jugador
                string insert = "INSERT INTO LobbyPlayers (lobby_code, user_id, username) VALUES (@code, @uid, @username)";
                using (var cmd = new MySqlCommand(insert, conn))
                {
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@uid", player.UserId);
                    cmd.Parameters.AddWithValue("@username", player.Username);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Obtener lista actualizada de jugadores
                var players = await GetPlayersByLobby(code, conn);
                return Ok(new { lobbyCode = code, players = players });
            } 
            catch (Exception ex) 
            {
                return StatusCode(500, new { message = "Error al unirse a la sala", error = ex.Message });
            }
        }

        [HttpGet("teambuilder/{code}")]
        public async Task<IActionResult> GetTeamBuilder(string code)
        {
            try 
            {
                code = code.ToUpper();

                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                // Obtener jugadores de la sala desde BD
                var players = await GetPlayersByLobby(code, conn);
                if (players.Count == 0)
                    return Ok(new { });

                // Lista de IDs de usuarios en la sala
                var userIds = players.Select(p => p.UserId).ToList();

                // --- Paso 1: Obtener temáticas compartidas por TODOS los jugadores ---
                var sharedThemes = new List<(int Id, string Nombre)>();
                var userParams = new List<MySqlParameter>();
                var userParamNames = new List<string>();

                for (int i = 0; i < userIds.Count; i++)
                {
                    string pName = $"@uid{i}";
                    userParamNames.Add(pName);
                    userParams.Add(new MySqlParameter(pName, userIds[i]));
                }

                string sharedThemesQuery = $@"
                    SELECT t.id_tematica, t.nombre
                    FROM Usuario_Skins us
                    JOIN Skins s ON us.id_skin_riot = s.id_skin_riot
                    JOIN Tematicas t ON s.id_tematica = t.id_tematica
                    WHERE us.id_usuario IN ({string.Join(",", userParamNames)})
                    GROUP BY t.id_tematica, t.nombre
                    HAVING COUNT(DISTINCT us.id_usuario) = @playerCount";

                using (var cmd = new MySqlCommand(sharedThemesQuery, conn))
                {
                    cmd.Parameters.AddRange(userParams.ToArray());
                    cmd.Parameters.AddWithValue("@playerCount", userIds.Count);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        sharedThemes.Add((reader.GetInt32(0), reader.GetString(1)));
                    }
                }

                if (sharedThemes.Count == 0)
                    return Ok(new { });

                // --- Paso 2: Obtener skins de esas temáticas para los jugadores ---
                var themeParams = new List<MySqlParameter>();
                var themeParamNames = new List<string>();
                for (int i = 0; i < sharedThemes.Count; i++)
                {
                    string pName = $"@themeId{i}";
                    themeParamNames.Add(pName);
                    themeParams.Add(new MySqlParameter(pName, sharedThemes[i].Id));
                }

                var allParams = new List<MySqlParameter>(userParams);
                allParams.AddRange(themeParams);

                string skinsQuery = $@"
                    SELECT t.nombre as tematica, s.linea, s.campeon, s.nombre_skin, u.username
                    FROM Usuario_Skins us
                    JOIN Skins s ON us.id_skin_riot = s.id_skin_riot
                    JOIN Tematicas t ON s.id_tematica = t.id_tematica
                    JOIN Usuarios u ON us.id_usuario = u.id_usuario
                    WHERE us.id_usuario IN ({string.Join(",", userParamNames)})
                      AND t.id_tematica IN ({string.Join(",", themeParamNames)})
                    ORDER BY t.nombre, s.linea, s.campeon";

                var teamBuilderData = new Dictionary<string, Dictionary<string, List<object>>>();

                using (var cmd = new MySqlCommand(skinsQuery, conn))
                {
                    cmd.Parameters.AddRange(allParams.ToArray());
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        string tematica = reader.GetString(reader.GetOrdinal("tematica"));
                        string linea = reader.GetString(reader.GetOrdinal("linea"));

                        if (!teamBuilderData.ContainsKey(tematica))
                        {
                            teamBuilderData[tematica] = new Dictionary<string, List<object>>
                            {
                                { "Top", new List<object>() },
                                { "Jungle", new List<object>() },
                                { "Mid", new List<object>() },
                                { "ADC", new List<object>() },
                                { "Support", new List<object>() },
                                { "Flex", new List<object>() }
                            };
                        }

                        if (!teamBuilderData[tematica].ContainsKey(linea))
                        {
                            teamBuilderData[tematica][linea] = new List<object>();
                        }

                        teamBuilderData[tematica][linea].Add(new
                        {
                            campeon = reader.GetString(reader.GetOrdinal("campeon")),
                            skin = reader.GetString(reader.GetOrdinal("nombre_skin")),
                            jugador = reader.GetString(reader.GetOrdinal("username"))
                        });
                    }
                }

                return Ok(teamBuilderData);
            } 
            catch (Exception ex) 
            {
                return StatusCode(500, new { message = "Error al procesar Team Builder", error = ex.Message });
            }
        }

        [HttpGet("teambuilder/{code}/suggest")]
        public async Task<IActionResult> SuggestTeamBuilder(string code)
        {
            try 
            {
                code = code.ToUpper();

                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                var players = await GetPlayersByLobby(code, conn);
                if (players.Count == 0)
                    return Ok(new List<object>()); // CORRECCIÓN CRÍTICA: Devolver una lista vacía, no un objeto

                var userIds = players.Select(p => p.UserId).ToList();

                var sharedThemes = new List<(int Id, string Nombre)>();
                var userParams = new List<MySqlParameter>();
                var userParamNames = new List<string>();
                for (int i = 0; i < userIds.Count; i++)
                {
                    string pName = $"@uid{i}";
                    userParamNames.Add(pName);
                    userParams.Add(new MySqlParameter(pName, userIds[i]));
                }

                string sharedThemesQuery = $@"
                    SELECT t.id_tematica, t.nombre
                    FROM Usuario_Skins us
                    JOIN Skins s ON us.id_skin_riot = s.id_skin_riot
                    JOIN Tematicas t ON s.id_tematica = t.id_tematica
                    WHERE us.id_usuario IN ({string.Join(",", userParamNames)})
                    GROUP BY t.id_tematica, t.nombre
                    HAVING COUNT(DISTINCT us.id_usuario) = @playerCount";

                using (var cmd = new MySqlCommand(sharedThemesQuery, conn))
                {
                    cmd.Parameters.AddRange(userParams.ToArray());
                    cmd.Parameters.AddWithValue("@playerCount", userIds.Count);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                        sharedThemes.Add((reader.GetInt32(0), reader.GetString(1)));
                }

                if (sharedThemes.Count == 0)
                    return Ok(new List<object>()); // CORRECCIÓN CRÍTICA: Devolver lista vacía

                var selectedTheme = sharedThemes[new Random().Next(sharedThemes.Count)];

                string skinsQuery = @"
                    SELECT us.id_usuario, u.username, s.campeon, s.nombre_skin, s.linea
                    FROM Usuario_Skins us
                    JOIN Usuarios u ON us.id_usuario = u.id_usuario
                    JOIN Skins s ON us.id_skin_riot = s.id_skin_riot
                    WHERE us.id_usuario IN (" + string.Join(",", userParamNames) + @")
                      AND s.id_tematica = @themeId
                    ORDER BY us.id_usuario, s.linea, s.campeon";

                var allParams = new List<MySqlParameter>(userParams);
                allParams.Add(new MySqlParameter("@themeId", selectedTheme.Id));

                var playerSkins = new Dictionary<int, List<SkinOption>>();
                using (var cmd = new MySqlCommand(skinsQuery, conn))
                {
                    cmd.Parameters.AddRange(allParams.ToArray());
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int userId = reader.GetInt32(0);
                        if (!playerSkins.ContainsKey(userId))
                            playerSkins[userId] = new List<SkinOption>();
                        playerSkins[userId].Add(new SkinOption
                        {
                            Username = reader.GetString(1),
                            Campeon = reader.GetString(2),
                            Skin = reader.GetString(3),
                            Linea = reader.GetString(4)
                        });
                    }
                }

                var roles = new List<string> { "Top", "Jungle", "Mid", "ADC", "Support" };
                var assignedRoles = new HashSet<string>();
                var suggestion = new List<object>();
                var usedUsers = new HashSet<int>();

                foreach (var player in players.OrderBy(p => Guid.NewGuid()))
                {
                    if (!playerSkins.ContainsKey(player.UserId)) continue;
                    var options = playerSkins[player.UserId];
                    var available = options.Where(o => !assignedRoles.Contains(o.Linea) && roles.Contains(o.Linea)).ToList();
                    if (available.Count == 0) available = options;
                    if (available.Count == 0) continue;

                    var chosen = available[new Random().Next(available.Count)];
                    assignedRoles.Add(chosen.Linea);
                    usedUsers.Add(player.UserId);
                    suggestion.Add(new
                    {
                        rol = chosen.Linea,
                        campeon = chosen.Campeon,
                        skin = chosen.Skin,
                        jugador = chosen.Username
                    });
                }

                return Ok(suggestion);
            } 
            catch (Exception ex) 
            {
                return StatusCode(500, new { message = "Error generando sugerencia", error = ex.Message });
            }
        }

        public class SkinOption
        {
            public string Username { get; set; } = "";
            public string Campeon { get; set; } = "";
            public string Skin { get; set; } = "";
            public string Linea { get; set; } = "";
        }

        private async Task<List<PlayerDto>> GetPlayersByLobby(string code, MySqlConnection conn)
        {
            var players = new List<PlayerDto>();
            string query = "SELECT user_id, username FROM LobbyPlayers WHERE lobby_code = @code";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@code", code);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                players.Add(new PlayerDto
                {
                    UserId = reader.GetInt32(0),
                    Username = reader.GetString(1)
                });
            }
            return players;
        }

        private string GenerateUniqueCode()
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            for (int attempt = 0; attempt < 10; attempt++)
            {
                string code = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
                string query = "SELECT COUNT(*) FROM Lobbies WHERE code = @code";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@code", code);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                    return code;
            }
            return ""; 
        }
    }

    public class PlayerDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
    }
}