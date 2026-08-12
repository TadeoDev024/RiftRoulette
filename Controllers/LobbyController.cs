using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RiftRoulette.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // Todos los métodos requieren token
    public class LobbyController : ControllerBase
    {
        private static readonly ConcurrentDictionary<string, List<PlayerDto>> Lobbies = new();
        private readonly string _connectionString;

        public LobbyController(IConfiguration configuration) {
            _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") 
                                ?? configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpPost("create")]
        public IActionResult CreateLobby() {
            string code = System.Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            Lobbies[code] = new List<PlayerDto>();
            return Ok(new { lobbyCode = code });
        }

        [HttpPost("join/{code}")]
        public IActionResult JoinLobby(string code, [FromBody] PlayerDto player) {
            code = code.ToUpper();
            
            if (!Lobbies.ContainsKey(code)) return NotFound(new { message = "Sala no encontrada" });
            
            var players = Lobbies[code];
            if (players.Count >= 5 && !players.Any(p => p.UserId == player.UserId)) 
                return BadRequest(new { message = "Sala llena (5/5)" });
            
            if (!players.Any(p => p.UserId == player.UserId))
                players.Add(player);

            return Ok(new { lobbyCode = code, players = players });
        }

        [HttpGet("teambuilder/{code}")]
        public async Task<IActionResult> GetTeamBuilder(string code) {
            code = code.ToUpper();
            if (!Lobbies.TryGetValue(code, out var players) || players.Count == 0) 
                return Ok(new { });

            var teamBuilderData = new Dictionary<string, Dictionary<string, List<object>>>();

            using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            // Construir parámetros para la cláusula IN
            var parameters = new List<MySqlParameter>();
            var paramNames = new List<string>();
            for (int i = 0; i < players.Count; i++)
            {
                string pName = $"@uid{i}";
                paramNames.Add(pName);
                parameters.Add(new MySqlParameter(pName, players[i].UserId));
            }

            string query = $@"
                SELECT t.nombre as tematica, s.linea, s.campeon, s.nombre_skin, u.username
                FROM Usuario_Skins us
                JOIN Skins s ON us.id_skin_riot = s.id_skin_riot
                JOIN Tematicas t ON s.id_tematica = t.id_tematica
                JOIN Usuarios u ON us.id_usuario = u.id_usuario
                WHERE us.id_usuario IN ({string.Join(",", paramNames)})
                ORDER BY t.nombre, s.linea, s.campeon";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters.ToArray());
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync()) {
                string tematica = reader.GetString(reader.GetOrdinal("tematica"));
                string linea = reader.GetString(reader.GetOrdinal("linea"));
                
                if (!teamBuilderData.ContainsKey(tematica)) {
                    teamBuilderData[tematica] = new Dictionary<string, List<object>> {
                        { "Top", new List<object>() }, { "Jungle", new List<object>() },
                        { "Mid", new List<object>() }, { "ADC", new List<object>() },
                        { "Support", new List<object>() }, { "Flex", new List<object>() }
                    };
                }

                if (teamBuilderData[tematica].ContainsKey(linea)) {
                    teamBuilderData[tematica][linea].Add(new {
                        campeon = reader.GetString(reader.GetOrdinal("campeon")),
                        skin = reader.GetString(reader.GetOrdinal("nombre_skin")),
                        jugador = reader.GetString(reader.GetOrdinal("username"))
                    });
                }
            }
            return Ok(teamBuilderData);
        }
    }

    public class PlayerDto {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
    }
}