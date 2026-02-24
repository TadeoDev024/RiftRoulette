using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using RiftRoulette.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class LobbyController : ControllerBase
{
    private static readonly Dictionary<string, List<PlayerDto>> Lobbies = new();
    private readonly string _connectionString;

    // Inyectamos la configuración para conectar a MySQL
    public LobbyController(IConfiguration configuration) {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
    }

    [HttpPost("create")]
    public IActionResult CreateLobby() {
        string code = System.Guid.NewGuid().ToString().Substring(0, 5).ToUpper();
        Lobbies[code] = new List<PlayerDto>();
        return Ok(new { lobbyCode = code });
    }

    [HttpPost("join/{code}")]
    public IActionResult JoinLobby(string code, [FromBody] PlayerDto player) {
        if (!Lobbies.ContainsKey(code)) return NotFound("Sala no encontrada");
        if (Lobbies[code].Count >= 5) return BadRequest("Sala llena");
        
        if (!Lobbies[code].Any(p => p.UserId == player.UserId))
            Lobbies[code].Add(player);

        return Ok(new { players = Lobbies[code] });
    }

    // EL NUEVO MOTOR: TEAM BUILDER
    [HttpGet("teambuilder/{code}")]
    public async Task<IActionResult> GetTeamBuilder(string code) {
        if (!Lobbies.ContainsKey(code)) return NotFound("Sala no encontrada");
        
        var players = Lobbies[code];
        if (players.Count == 0) return Ok(new { });

        // Extraemos los IDs de todos los jugadores de la sala
        string userIds = string.Join(",", players.Select(p => p.UserId));
        
        // Estructura: Temática -> Línea -> Lista de Opciones (Campeón, Skin, Jugador)
        var teamBuilderData = new Dictionary<string, Dictionary<string, List<object>>>();

        using var conn = new MySqlConnection(_connectionString);
        await conn.OpenAsync();

        // Esta súper-query cruza los inventarios de TODOS los que están en la sala
        string query = $@"
            SELECT t.nombre as tematica, s.linea, s.campeon, s.nombre_skin, u.username
            FROM Usuario_Skins us
            JOIN Skins s ON us.id_skin_riot = s.id_skin_riot
            JOIN Tematicas t ON s.id_tematica = t.id_tematica
            JOIN Usuarios u ON us.id_usuario = u.id_usuario
            WHERE us.id_usuario IN ({userIds})
            ORDER BY t.nombre, s.linea, s.campeon";

        using var cmd = new MySqlCommand(query, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync()) {
            string tematica = reader.GetString(reader.GetOrdinal("tematica"));
            string linea = reader.GetString(reader.GetOrdinal("linea"));
            
            if (!teamBuilderData.ContainsKey(tematica)) {
                teamBuilderData[tematica] = new Dictionary<string, List<object>> {
                    { "Top", new List<object>() },
                    { "Jungle", new List<object>() },
                    { "Mid", new List<object>() },
                    { "ADC", new List<object>() },
                    { "Support", new List<object>() },
                    { "Flex", new List<object>() } // Por si algún campeón no encaja bien
                };
            }

            // Agregamos la skin disponible a su respectiva línea dentro de la temática
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
    public string Username { get; set; }
}